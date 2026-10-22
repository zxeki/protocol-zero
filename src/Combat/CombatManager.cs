using Godot;
using ProtocolZero.Core;
using ProtocolZero.UI;
using ProtocolZero.World;

namespace ProtocolZero.Combat;

public partial class CombatManager : Node2D
{
    public List<OperatorUnit> Operators { get; } = new();
    public List<EnemyUnit> Enemies { get; } = new();
    public EnemyUnit? FocusTarget { get; private set; }
    public int Wave { get; private set; }

    public event Action<int>? WaveChanged;
    public event Action<long, float>? RewardsAwarded;
    public event Action<EnemyUnit>? EnemyKilled;

    Node _unitLayer = null!;
    FocusMarker? _focusMarker;

    public override void _Ready()
    {
        _unitLayer = new Node2D { Name = "Units" };
        AddChild(_unitLayer);
    }

    public void SpawnSquad()
    {
        var def = Balance.Instance.GetOperator("striker");
        int level = GameState.Instance.Progress[def.Id].Level;
        var op = new OperatorUnit();
        op.Init(def, this, level);
        op.Position = new Vector2(120f, Battlefield.GroundY);
        _unitLayer.AddChild(op);
        Operators.Add(op);
    }

    public void StartCombat() => SpawnNextWave();

    void SpawnNextWave()
    {
        Wave++;
        WaveChanged?.Invoke(Wave);

        var cfg = Balance.Instance.Config;
        bool elite = Wave % cfg.EliteWaveInterval == 0;
        string enemyId = elite ? "enforcer" : "watcher";
        var def = Balance.Instance.Enemies[enemyId];

        var enemy = new EnemyUnit();
        enemy.Init(def, this, Wave, elite);
        enemy.Position = new Vector2(440f, Battlefield.GroundY);
        enemy.Died += OnEnemyDied;
        _unitLayer.AddChild(enemy);
        Enemies.Add(enemy);
    }

    void OnEnemyDied(Combatant c)
    {
        if (c is not EnemyUnit e)
            return;

        Enemies.Remove(e);
        SetFocus(null);

        var cfg = Balance.Instance.Config;
        float scale = 1f + cfg.CreditsScalingPerWavePct * (Wave - 1);
        long credits = (long)Mathf.Round(GD.RandRange(e.Def.CreditsMin, e.Def.CreditsMax) * scale);
        float xp = (float)GD.RandRange(e.Def.XpMin, e.Def.XpMax) * scale;

        GameState.Instance.AddCredits(credits);
        foreach (var op in Operators)
            GameState.Instance.AddXp(op.Def.Id, xp);

        RewardsAwarded?.Invoke(credits, xp);
        EnemyKilled?.Invoke(e);
        GD.Print($"[Combat] Wave {Wave}: {e.Def.DisplayName} defeated  +{credits} CR  +{xp:0} XP");

        GetTree().CreateTimer(cfg.RespawnDelaySec).Timeout += SpawnNextWave;
    }

    public void OnOperatorDowned(OperatorUnit op)
    {
        float delay = Balance.Instance.Config.OperatorReviveSec;
        GetTree().CreateTimer(delay).Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(op) && !op.IsAlive)
                op.Revive();
        };
    }

    public Combatant? GetTargetForOperator(OperatorUnit op)
    {
        if (FocusTarget != null && GodotObject.IsInstanceValid(FocusTarget) && FocusTarget.IsAlive)
            return FocusTarget;

        EnemyUnit? best = null;
        float bestDist = float.MaxValue;
        foreach (var e in Enemies)
        {
            if (!e.IsAlive)
                continue;
            float d = Mathf.Abs(e.GlobalPosition.X - op.GlobalPosition.X);
            if (d < bestDist)
            {
                bestDist = d;
                best = e;
            }
        }
        return best;
    }

    public OperatorUnit? GetTargetForEnemy(EnemyUnit enemy)
    {
        OperatorUnit? best = null;
        float bestHp = float.MaxValue;
        foreach (var op in Operators)
        {
            if (!op.IsAlive)
                continue;
            if (op.HpFraction < bestHp)
            {
                bestHp = op.HpFraction;
                best = op;
            }
        }
        return best;
    }

    public void HandleWorldClick(Vector2 worldPos)
    {
        EnemyUnit? clicked = null;
        foreach (var e in Enemies)
        {
            if (!e.IsAlive)
                continue;
            var p = e.GlobalPosition;
            if (Mathf.Abs(worldPos.X - p.X) < 16f
                && worldPos.Y > p.Y - e.VisualHeight - 8f
                && worldPos.Y < p.Y + 6f)
            {
                clicked = e;
                break;
            }
        }

        if (clicked == null || FocusTarget == clicked)
            SetFocus(null);
        else
            SetFocus(clicked);
    }

    void SetFocus(EnemyUnit? target)
    {
        if (_focusMarker != null && GodotObject.IsInstanceValid(_focusMarker))
            _focusMarker.QueueFree();
        _focusMarker = null;

        FocusTarget = target;
        if (target != null && GodotObject.IsInstanceValid(target))
        {
            _focusMarker = new FocusMarker();
            target.AddChild(_focusMarker);
        }
    }
}
