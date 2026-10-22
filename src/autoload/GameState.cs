using Godot;

namespace ProtocolZero.Core;

public partial class GameState : Node
{
    public static GameState Instance { get; private set; } = null!;

    public sealed class OperatorProgress
    {
        public int Level { get; set; } = 1;
        public float Xp { get; set; }
    }

    public long Credits { get; private set; }
    public Dictionary<string, OperatorProgress> Progress { get; } = new();

    public event Action<long>? CreditsChanged;
    public event Action<string, OperatorProgress>? XpChanged;

    public override void _Ready()
    {
        Instance = this;
        foreach (var op in Balance.Instance.Operators)
            Progress[op.Id] = new OperatorProgress();
    }

    public void AddCredits(long amount)
    {
        if (amount <= 0)
            return;
        Credits += amount;
        CreditsChanged?.Invoke(Credits);
    }

    public bool TrySpend(long amount)
    {
        if (amount < 0 || Credits < amount)
            return false;
        Credits -= amount;
        CreditsChanged?.Invoke(Credits);
        return true;
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
    }
}
