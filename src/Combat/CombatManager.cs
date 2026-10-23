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
    bool _waveEnding;

    public override void _Ready()
    {
        _unitLayer = new Node2D { Name = "Units" };
        AddChild(_unitLayer);
    }

    public void SpawnSquad()
    {
        var cfg = Balance.Instance.Config;
        float[] slotX = { 304f, 264f, 224f };
        int slot = 0;

        foreach (var id in GameState.Instance.Squad)
        {
            if (slot >= cfg.SquadSize || slot >= slotX.Length)
                break;

            var def = Balance.Instance.Operators.FirstOrDefault(o => o.Id == id);
            if (def == null)
            {
                GD.Print($"[Combat] Unknown squad member '{id}', skipping");
                continue;
            }

            int level = GameState.Instance.Progress[def.Id].Level;
            var op = new OperatorUnit();
            op.Init(def, this, level);
            op.Position = new Vector2(slotX[slot], Battlefield.GroundY);
            _unitLayer.AddChild(op);
            Operators.Add(op);
            slot++;
        }

        GD.Print($"[Combat] Squad deployed ({Operators.Count}/{cfg.SquadSize}): {string.Join(", ", Operators.Select(o => o.Def.Id))}");
    }

    public void RespawnSquad()
    {
        SetFocus(null);

        foreach (var op in Operators)
        {
            if (GodotObject.IsInstanceValid(op))
                op.QueueFree();
        }
        Operators.Clear();

        SpawnSquad();
    }

    public void StartCombat() => SpawnNextWave();

    void SpawnNextWave()
    {
        _waveEnding = false;
        Wave++;
        WaveChanged?.Invoke(Wave);

        var cfg = Balance.Instance.Config;
        bool bossWave = cfg.BossWaveInterval > 0 && Wave % cfg.BossWaveInterval == 0;
        bool eliteWave = !bossWave && Wave % cfg.EliteWaveInterval == 0;

        if (bossWave)
        {
            SpawnEnemy("the_warden", 0, true);
            for (int i = 0; i < cfg.BossEscortCount; i++)
                SpawnEnemy("hunter_drone", i + 1, false);
        }
        else if (eliteWave)
        {
            SpawnEnemy("enforcer", 0, true);
            for (int i = 0; i < cfg.EliteEscortCount; i++)
                SpawnEnemy("watcher", i + 1, false);
        }
        else
        {
            int growth = Mathf.Max(1, cfg.EnemiesPerWaveGrowthDivisor);
            int count = Mathf.Clamp(cfg.EnemiesPerWaveBase + (Wave - 1) / growth, 1, cfg.EnemiesPerWaveMax);

            int hunters = 0;
            if (cfg.HunterDroneFromWave > 0 && Wave >= cfg.HunterDroneFromWave && count > 1)
            {
                hunters = 1 + (Wave - cfg.HunterDroneFromWave) / Mathf.Max(1, cfg.HunterDroneRampWaves);
                hunters = Mathf.Clamp(hunters, 0, count - 1);
            }

            for (int i = 0; i < count; i++)
                SpawnEnemy(i >= count - hunters ? "hunter_drone" : "watcher", i, false);
        }
    }

    void SpawnEnemy(string id, int slot, bool elite)
    {
        var def = Balance.Instance.Enemies[id];
        var enemy = new EnemyUnit();
        enemy.Init(def, this, Wave, elite);
        enemy.Position = new Vector2(436f + slot * 26f, Battlefield.GroundY);
        enemy.Died += OnEnemyDied;
        _unitLayer.AddChild(enemy);
        Enemies.Add(enemy);
    }

    void OnEnemyDied(Combatant c)
    {
        if (c is not EnemyUnit e)
            return;

        Enemies.Remove(e);
        if (FocusTarget == e)
            SetFocus(null);

        var cfg = Balance.Instance.Config;
        float scale = 1f + cfg.CreditsScalingPerWavePct * (Wave - 1);
        long credits = (long)Mathf.Round(GD.RandRange(e.Def.CreditsMin, e.Def.CreditsMax) * scale);
        float xp = (float)GD.RandRange(e.Def.XpMin, e.Def.XpMax) * scale;

        GameState.Instance.AddCredits(credits);
        foreach (var op in Operators)
            GameState.Instance.AddXp(op.Def.Id, xp);

        long milestoneBonus = 0;
        if (e.Def.Id == "the_warden" && cfg.BossWaveInterval > 0)
            milestoneBonus = GameState.Instance.ClaimBossMilestone(Mathf.Max(1, Wave / cfg.BossWaveInterval));

        RewardsAwarded?.Invoke(credits + milestoneBonus, xp);
        EnemyKilled?.Invoke(e);
        GD.Print($"[Combat] Wave {Wave}: {e.Def.DisplayName} defeated  +{credits} CR  +{xp:0} XP"
            + (milestoneBonus > 0 ? $"  +{milestoneBonus} CR milestone" : ""));

        if (Enemies.Count == 0 && !_waveEnding)
        {
            _waveEnding = true;
            GetTree().CreateTimer(cfg.RespawnDelaySec).Timeout += SpawnNextWave;
        }
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

    public OperatorUnit? GetPassiveCarrier(string passiveId) =>
        Operators.FirstOrDefault(o => o.IsAlive && o.Def.PassiveId == passiveId);

    public OperatorUnit? GetFortressTaunter() =>
        Operators.FirstOrDefault(o => o.IsAlive && o.FortressTauntRemaining > 0f);

    public OperatorUnit? GetTargetForEnemy(EnemyUnit enemy)
    {
        var alive = Operators.Where(o => o.IsAlive).ToList();
        if (alive.Count == 0)
            return null;

        if (enemy.Def.Id == "hunter_drone")
            return alive.OrderBy(o => o.HpFraction).First();

        if (GD.Randf() < 0.5f)
            return alive.OrderBy(o => o.HpFraction).First();

        int idx = Mathf.Clamp((int)(GD.Randf() * alive.Count), 0, alive.Count - 1);
        return alive[idx];
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
