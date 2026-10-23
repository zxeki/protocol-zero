using System.Text.Json;
using Godot;
using ProtocolZero.Data;
using FileAccess = Godot.FileAccess;

namespace ProtocolZero.Core;

public partial class GameState : Node
{
    public static GameState Instance { get; private set; } = null!;

    const string SavePath = "user://save.json";
    const string SettingsPath = "user://settings.json";

    public sealed class OperatorProgress
    {
        public int Level { get; set; } = 1;
        public float Xp { get; set; }
    }

    public long Credits { get; private set; }
    public int BossesDefeated { get; private set; }
    public Dictionary<string, OperatorProgress> Progress { get; } = new();
    public List<string> Owned { get; } = new();
    public List<string> Squad { get; } = new();

    public Dictionary<string, int> Inventory { get; } = new();
    public Dictionary<string, Dictionary<string, string>> Loadout { get; } = new();
    public Dictionary<string, SavedUpgrades> Upgrades { get; } = new();

    public UserSettings Settings { get; private set; } = new();
    public OfflineSummary? OfflineSummary { get; private set; }

    public event Action<long>? CreditsChanged;
    public event Action<string, OperatorProgress>? XpChanged;
    public event Action<string>? OwnedChanged;
    public event Action? SquadChanged;
    public event Action? EquipmentChanged;
    public event Action? SettingsChanged;

    string? _loadedLastSaveUtc;

    public override void _Ready()
    {
        Instance = this;
        foreach (var op in Balance.Instance.Operators)
            Progress[op.Id] = new OperatorProgress();

        if (!TryLoad())
            InitNewGame();

        LoadSettings();
        EvaluateOfflineRewards();
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
        Save();
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

    public long ClaimBossMilestone(int bossIndex)
    {
        if (bossIndex <= BossesDefeated)
            return 0;

        BossesDefeated = bossIndex;
        long bonus = Balance.Instance.Config.BossMilestoneBonusCredits;
        AddCredits(bonus);
        Save();
        GD.Print($"[GameState] Boss milestone #{bossIndex} claimed: +{bonus} CR");
        return bonus;
    }

    public void AddEquipment(string itemId, int count = 1)
    {
        if (!Balance.Instance.Equipment.ContainsKey(itemId) || count <= 0)
            return;
        Inventory[itemId] = Inventory.GetValueOrDefault(itemId) + count;
        EquipmentChanged?.Invoke();
        Save();
    }

    public int InventoryCount(string itemId) => Inventory.GetValueOrDefault(itemId);

    public Dictionary<string, string> LoadoutFor(string operatorId) =>
        Loadout.TryGetValue(operatorId, out var slots) ? slots : new Dictionary<string, string>();

    public string? Equipped(string operatorId, string slot) =>
        LoadoutFor(operatorId).GetValueOrDefault(slot);

    public bool Equip(string operatorId, string itemId)
    {
        if (!Balance.Instance.Equipment.TryGetValue(itemId, out var item))
            return false;
        if (Inventory.GetValueOrDefault(itemId) <= 0)
            return false;
        if (!Owned.Contains(operatorId))
            return false;

        var slots = LoadoutFor(operatorId);
        string? previous = slots.GetValueOrDefault(item.Slot);

        Inventory[itemId]--;
        if (Inventory[itemId] <= 0)
            Inventory.Remove(itemId);

        slots[item.Slot] = itemId;
        Loadout[operatorId] = slots;
        if (previous != null)
            Inventory[previous] = Inventory.GetValueOrDefault(previous) + 1;

        EquipmentChanged?.Invoke();
        Save();
        return true;
    }

    public bool Unequip(string operatorId, string slot)
    {
        if (!Loadout.TryGetValue(operatorId, out var slots))
            return false;
        if (!slots.TryGetValue(slot, out var itemId))
            return false;

        slots.Remove(slot);
        if (slots.Count == 0)
            Loadout.Remove(operatorId);
        Inventory[itemId] = Inventory.GetValueOrDefault(itemId) + 1;

        EquipmentChanged?.Invoke();
        Save();
        return true;
    }

    public (float attack, float hp) EquipBonus(string operatorId)
    {
        float atk = 0f, hp = 0f;
        foreach (var itemId in LoadoutFor(operatorId).Values)
        {
            if (!Balance.Instance.Equipment.TryGetValue(itemId, out var item))
                continue;
            atk += item.Attack;
            hp += item.Hp;
        }
        return (atk, hp);
    }

    public SavedUpgrades UpgradesFor(string operatorId)
    {
        if (!Upgrades.TryGetValue(operatorId, out var up))
            Upgrades[operatorId] = up = new SavedUpgrades();
        return up;
    }

    public long UpgradeCost(int rank) =>
        (long)MathF.Round(Balance.Instance.Config.UpgradeBaseCost
            * MathF.Pow(Balance.Instance.Config.UpgradeCostGrowth, rank));

    public bool TryUpgrade(string operatorId, bool tech)
    {
        var cfg = Balance.Instance.Config;
        var up = UpgradesFor(operatorId);
        int rank = tech ? up.TechRank : up.PowerRank;
        if (rank >= cfg.UpgradeMaxRank)
            return false;
        if (!TrySpend(UpgradeCost(rank)))
            return false;

        if (tech)
            up.TechRank++;
        else
            up.PowerRank++;

        EquipmentChanged?.Invoke();
        Save();
        return true;
    }

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
            BossesDefeated = BossesDefeated,
            Owned = new List<string>(Owned),
            Squad = new List<string>(Squad),
            Operators = Progress.ToDictionary(
                kv => kv.Key,
                kv => new SavedOperator { Level = kv.Value.Level, Xp = kv.Value.Xp }),
            Inventory = new Dictionary<string, int>(Inventory),
            Loadout = Loadout.ToDictionary(
                kv => kv.Key,
                kv => new Dictionary<string, string>(kv.Value)),
            Upgrades = Upgrades.ToDictionary(
                kv => kv.Key,
                kv => new SavedUpgrades { PowerRank = kv.Value.PowerRank, TechRank = kv.Value.TechRank }),
            LastSaveUtc = DateTimeOffset.UtcNow.ToString("O"),
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

        _loadedLastSaveUtc = data.LastSaveUtc;

        var cfg = Balance.Instance.Config;
        var validIds = Balance.Instance.Operators.Select(o => o.Id).ToHashSet();

        Credits = Math.Max(0, data.Credits);
        BossesDefeated = Math.Max(0, data.BossesDefeated);

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

        var validItems = Balance.Instance.Equipment;
        Inventory.Clear();
        foreach (var (itemId, count) in data.Inventory)
        {
            if (validItems.ContainsKey(itemId) && count > 0)
                Inventory[itemId] = count;
        }

        Loadout.Clear();
        foreach (var (opId, slots) in data.Loadout)
        {
            if (!Owned.Contains(opId))
            {
                foreach (var itemId in slots.Values)
                {
                    if (validItems.ContainsKey(itemId))
                        Inventory[itemId] = Inventory.GetValueOrDefault(itemId) + 1;
                }
                continue;
            }
            var kept = new Dictionary<string, string>();
            foreach (var (slot, itemId) in slots)
            {
                if (validItems.TryGetValue(itemId, out var item) && item.Slot == slot && !kept.ContainsKey(slot))
                    kept[slot] = itemId;
                else
                    Inventory[itemId] = Inventory.GetValueOrDefault(itemId) + 1;
            }
            if (kept.Count > 0)
                Loadout[opId] = kept;
        }

        Upgrades.Clear();
        foreach (var (opId, up) in data.Upgrades)
        {
            if (!Owned.Contains(opId))
                continue;
            Upgrades[opId] = new SavedUpgrades
            {
                PowerRank = Mathf.Clamp(up.PowerRank, 0, cfg.UpgradeMaxRank),
                TechRank = Mathf.Clamp(up.TechRank, 0, cfg.UpgradeMaxRank),
            };
        }

        GD.Print($"[GameState] Loaded save: {Owned.Count} owned, squad [{string.Join(", ", Squad)}], {Credits} CR"
            + (Inventory.Count > 0 ? $", {Inventory.Values.Sum()} items" : ""));
        return true;
    }

    void EvaluateOfflineRewards()
    {
        var cfg = Balance.Instance.Config;
        float awayHours = 0f;

        if (Settings.OfflineRewardsEnabled && !string.IsNullOrEmpty(_loadedLastSaveUtc)
            && DateTimeOffset.TryParse(_loadedLastSaveUtc, null,
                System.Globalization.DateTimeStyles.None, out var savedAt))
        {
            float elapsed = (float)(DateTimeOffset.UtcNow - savedAt).TotalHours;
            if (elapsed > 0f && elapsed * 60f >= cfg.OfflineMinMinutes)
                awayHours = elapsed;
        }

        float rewardHours = Mathf.Min(awayHours, cfg.OfflineCapHours);
        long credits = rewardHours > 0f ? (long)MathF.Floor(rewardHours * cfg.OfflineCreditsPerHour) : 0L;
        float xp = rewardHours > 0f ? MathF.Floor(rewardHours * cfg.OfflineXpPerHour) : 0f;

        if (credits > 0)
        {
            Credits += credits;
            CreditsChanged?.Invoke(Credits);
        }
        if (xp > 0f)
        {
            foreach (var id in Squad)
                AddXp(id, xp);
        }

        if (credits > 0 || xp > 0f)
        {
            OfflineSummary = new OfflineSummary { Hours = awayHours, Credits = credits, Xp = xp };
            GD.Print($"[GameState] Offline: {awayHours:0.##}h away (reward {rewardHours:0.##}h)"
                + $" -> +{credits} CR, +{xp:0} XP per squad member");
        }

        Save();
    }

    void LoadSettings()
    {
        Settings = new UserSettings();
        if (!FileAccess.FileExists(SettingsPath))
        {
            SaveSettings();
            return;
        }

        try
        {
            using var file = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Read);
            var data = file != null
                ? JsonSerializer.Deserialize<UserSettings>(file.GetAsText(), Data.Json.Options)
                : null;
            if (data != null && data.Version >= 1)
                Settings = data;
            else
                SaveSettings();
        }
        catch (Exception e)
        {
            GD.PrintErr($"[GameState] Settings load failed, using defaults: {e.Message}");
            Settings = new UserSettings();
            SaveSettings();
        }
    }

    public void UpdateSettings(Action<UserSettings> mutate)
    {
        mutate(Settings);
        SaveSettings();
        SettingsChanged?.Invoke();
    }

    void SaveSettings()
    {
        try
        {
            using var file = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.PrintErr($"[GameState] Cannot write {SettingsPath}");
                return;
            }
            file.StoreString(JsonSerializer.Serialize(Settings, Data.Json.Options));
        }
        catch (Exception e)
        {
            GD.PrintErr($"[GameState] Settings save failed: {e.Message}");
        }
    }
}
