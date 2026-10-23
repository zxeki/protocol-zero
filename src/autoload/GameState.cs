using System.Text.Json;
using Godot;
using ProtocolZero.Data;
using FileAccess = Godot.FileAccess;

namespace ProtocolZero.Core;

public partial class GameState : Node
{
    public static GameState Instance { get; private set; } = null!;

    const string SavePath = "user://save.json";

    public sealed class OperatorProgress
    {
        public int Level { get; set; } = 1;
        public float Xp { get; set; }
    }

    public long Credits { get; private set; }
    public Dictionary<string, OperatorProgress> Progress { get; } = new();
    public List<string> Owned { get; } = new();
    public List<string> Squad { get; } = new();

    public event Action<long>? CreditsChanged;
    public event Action<string, OperatorProgress>? XpChanged;
    public event Action<string>? OwnedChanged;
    public event Action? SquadChanged;

    public override void _Ready()
    {
        Instance = this;
        foreach (var op in Balance.Instance.Operators)
            Progress[op.Id] = new OperatorProgress();

        if (!TryLoad())
            InitNewGame();
    }

    void InitNewGame()
    {
        var cfg = Balance.Instance.Config;

        foreach (var op in Balance.Instance.Operators)
        {
            Progress[op.Id] = new OperatorProgress();
            if (op.RecruitmentCost <= 0)
                Owned.Add(op.Id);
        }

        Credits = cfg.StartingCredits;

        foreach (var id in cfg.DefaultSquad)
        {
            if (Squad.Count >= cfg.SquadSize)
                break;
            if (Owned.Contains(id))
                Squad.Add(id);
        }
        foreach (var id in Owned)
        {
            if (Squad.Count >= cfg.SquadSize)
                break;
            if (!Squad.Contains(id))
                Squad.Add(id);
        }

        GD.Print($"[GameState] New game: {Owned.Count} owned, squad [{string.Join(", ", Squad)}], {Credits} CR");
    }

    public void AddCredits(long amount)
    {
        if (amount <= 0)
            return;
        Credits += amount;
        CreditsChanged?.Invoke(Credits);
        Save();
    }

    public bool TrySpend(long amount)
    {
        if (amount < 0 || Credits < amount)
            return false;
        Credits -= amount;
        CreditsChanged?.Invoke(Credits);
        Save();
        return true;
    }

    public bool Owns(string operatorId) => Owned.Contains(operatorId);

    public bool InSquad(string operatorId) => Squad.Contains(operatorId);

    public bool TryRecruit(string operatorId)
    {
        var def = Balance.Instance.Operators.FirstOrDefault(o => o.Id == operatorId);
        if (def == null || Owned.Contains(operatorId))
            return false;

        if (!TrySpend(def.RecruitmentCost))
            return false;

        Owned.Add(operatorId);
        OwnedChanged?.Invoke(operatorId);
        GD.Print($"[GameState] Recruited {def.DisplayName} for {def.RecruitmentCost} CR");

        if (Squad.Count < Balance.Instance.Config.SquadSize)
        {
            Squad.Add(operatorId);
            SquadChanged?.Invoke();
        }

        Save();
        return true;
    }

    public bool SetSquad(List<string> squad)
    {
        var cfg = Balance.Instance.Config;

        if (squad.Count < 1 || squad.Count > cfg.SquadSize)
            return false;
        if (squad.Distinct().Count() != squad.Count)
            return false;
        if (squad.Any(id => !Owned.Contains(id)))
            return false;

        Squad.Clear();
        Squad.AddRange(squad);
        SquadChanged?.Invoke();
        Save();
        return true;
    }

    public bool ToggleSquadMember(string operatorId)
    {
        if (!Owned.Contains(operatorId))
            return false;

        var next = new List<string>(Squad);
        if (next.Contains(operatorId))
        {
            if (next.Count <= 1)
                return false;
            next.Remove(operatorId);
        }
        else
        {
            if (next.Count >= Balance.Instance.Config.SquadSize)
                return false;
            next.Add(operatorId);
        }

        return SetSquad(next);
    }

    public void AddXp(string operatorId, float xp)
    {
        if (xp <= 0 || !Progress.TryGetValue(operatorId, out var progress))
            return;

        progress.Xp += xp;

        var cfg = Balance.Instance.Config;
        while (progress.Level < cfg.LevelCap)
        {
            int need = Balance.Instance.XpToNext(progress.Level);
            if (progress.Xp < need)
                break;
            progress.Xp -= need;
            progress.Level++;
        }

        if (progress.Level >= cfg.LevelCap)
            progress.Xp = 0f;

        XpChanged?.Invoke(operatorId, progress);
        Save();
    }

    public void Save()
    {
        var data = new SaveData
        {
            Credits = Credits,
            Owned = new List<string>(Owned),
            Squad = new List<string>(Squad),
            Operators = Progress.ToDictionary(
                kv => kv.Key,
                kv => new SavedOperator { Level = kv.Value.Level, Xp = kv.Value.Xp }),
        };

        try
        {
            using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.PrintErr($"[GameState] Cannot write {SavePath}");
                return;
            }
            file.StoreString(System.Text.Json.JsonSerializer.Serialize(data, Data.Json.Options));
        }
        catch (Exception e)
        {
            GD.PrintErr($"[GameState] Save failed: {e.Message}");
        }
    }

    bool TryLoad()
    {
        if (!FileAccess.FileExists(SavePath))
            return false;

        SaveData? data;
        try
        {
            using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
            if (file == null)
                return false;
            data = JsonSerializer.Deserialize<SaveData>(file.GetAsText(), Data.Json.Options);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[GameState] Load failed: {e.Message}");
            return false;
        }

        if (data == null)
            return false;

        var cfg = Balance.Instance.Config;
        var validIds = Balance.Instance.Operators.Select(o => o.Id).ToHashSet();

        Credits = Math.Max(0, data.Credits);

        Owned.Clear();
        foreach (var id in data.Owned.Distinct())
        {
            if (validIds.Contains(id) && !Owned.Contains(id))
                Owned.Add(id);
        }
        if (Owned.Count == 0)
        {
            foreach (var op in Balance.Instance.Operators)
            {
                if (op.RecruitmentCost <= 0)
                    Owned.Add(op.Id);
            }
        }

        foreach (var (id, saved) in data.Operators)
        {
            if (!Progress.TryGetValue(id, out var p))
                continue;
            p.Level = Mathf.Clamp(saved.Level, 1, cfg.LevelCap);
            p.Xp = Math.Max(0f, saved.Xp);
        }

        Squad.Clear();
        foreach (var id in data.Squad)
        {
            if (Squad.Count >= cfg.SquadSize)
                break;
            if (Owned.Contains(id) && !Squad.Contains(id))
                Squad.Add(id);
        }
        if (Squad.Count == 0 && Owned.Count > 0)
            Squad.Add(Owned[0]);

        GD.Print($"[GameState] Loaded save: {Owned.Count} owned, squad [{string.Join(", ", Squad)}], {Credits} CR");
        return true;
    }
}
