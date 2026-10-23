using Godot;
using ProtocolZero.Core;
using ProtocolZero.Data;
using ProtocolZero.UI;
using ProtocolZero.Util;

namespace ProtocolZero.Combat;

public partial class OperatorUnit : Combatant
{
    public OperatorDef Def { get; private set; } = null!;
    public int Level { get; private set; } = 1;

    public event Action? StateChanged;

    static readonly bool SelfTest = System.Environment.GetEnvironmentVariable("PZ_SELFTEST") == "1";

    CombatManager _manager = null!;
    float _attackTimer;
    float _burstTimer;
    int _burstShotsLeft;
    float _activeCooldownTimer;
    float _ultCharge;
    float _ultStateTimer;
    float _barrierTimer;
    float _fortressTimer;
    float _recoilTimer;
    int _attackCount;
    float _naniteTimer;
    float _lockTimer;
    int _lockShotsLeft;
    EnemyUnit? _lockTarget;
    Texture2D? _boltTexture;

    public Texture2D? PortraitTexture { get; private set; }
    public bool ActiveReady => _activeCooldownTimer <= 0f && _burstShotsLeft == 0 && _lockShotsLeft == 0 && IsAlive;
    public bool UltReady => _ultCharge >= 1f && _ultStateTimer <= 0f && _lockShotsLeft == 0 && IsAlive;
    public float ActiveCooldownRemaining => _activeCooldownTimer;
    public float UltStateRemaining => _ultStateTimer;
    public float UltCharge01 => _ultCharge;
    public bool BarrierActive => _barrierTimer > 0f;
    public float FortressTauntRemaining => _fortressTimer;

    public void Init(OperatorDef def, CombatManager manager, int level)
    {
        Def = def;
        _manager = manager;
        Level = level;
        ApplyStats();
    }

    void ApplyStats() => ApplyStats(true);

    void ApplyStats(bool fullHeal)
    {
        var cfg = Balance.Instance.Config;
        var gs = GameState.Instance;
        float hpMult = 1f + cfg.HpPerLevelPct * (Level - 1);
        float atkMult = 1f + cfg.AttackPerLevelPct * (Level - 1);
        var (eqAtk, eqHp) = gs.EquipBonus(Def.Id);
        var up = gs.UpgradesFor(Def.Id);
        float powerMult = 1f + cfg.UpgradeStatPerRankPct * up.PowerRank;

        float prevFrac = MaxHp > 0f ? Hp / MaxHp : 1f;
        MaxHp = (Def.BaseHp * hpMult + eqHp) * powerMult;
        BaseAttack = (Def.BaseAttack * atkMult + eqAtk) * powerMult;
        AttackInterval = Def.AttackInterval;
        if (fullHeal)
            Hp = MaxHp;
        else if (IsAlive)
            Hp = Mathf.Clamp(prevFrac * MaxHp, 1f, MaxHp);
        else
            Hp = 0f;
        Bar?.SetBar(Hp, MaxHp, Shield);
    }

    public void RefreshStats() => ApplyStats(false);

    public float EffectiveActiveCooldown()
    {
        var cfg = Balance.Instance.Config;
        int tech = GameState.Instance.UpgradesFor(Def.Id).TechRank;
        return MathF.Max(1f, Def.ActiveCooldownSec * (1f - cfg.UpgradeTechCooldownPct * tech));
    }

    protected override void BuildVisuals()
    {
        IdleFrames = Assets.Frames($"operators/{Def.Id}", "idle", 4);
        if (IdleFrames.Length > 0)
        {
            MoveFrames = Assets.Frames($"operators/{Def.Id}", "move", 4);
            AttackFrames = Assets.Frames($"operators/{Def.Id}", "attack", 4);
            HurtFrames = Assets.Frames($"operators/{Def.Id}", "hurt", 4);
            DefeatFrames = Assets.Frames($"operators/{Def.Id}", "defeat", 4);
            VisualHeight = IdleFrames[0].GetHeight();
            PortraitTexture = IdleFrames[0];
        }
        else
        {
            (PortraitTexture, VisualHeight) = Def.Id switch
            {
                "volt" => (PixelArt.BuildVolt(), 28f),
                "hawk" => (PixelArt.BuildHawk(), 28f),
                "vex" => (PixelArt.BuildVex(), 28f),
                "torque" => (PixelArt.BuildTorque(), 30f),
                _ => (PixelArt.BuildStriker(), 28f),
            };
        }
        Sprite.Texture = PortraitTexture;
        SpriteBaseOffset = new Vector2(0f, -VisualHeight / 2f);
        Sprite.Position = SpriteBaseOffset;
        _boltTexture = PixelArt.BuildBolt(new Color("#fff0e8"), Def.Accent);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!IsAlive)
            return;

        float dt = (float)delta;
        var cfg = Balance.Instance.Config;

        if (_activeCooldownTimer > 0f)
        {
            _activeCooldownTimer -= dt;
            if (_activeCooldownTimer <= 0f)
                StateChanged?.Invoke();
        }

        if (_ultStateTimer > 0f)
        {
            _ultStateTimer -= dt;
            if (_ultStateTimer <= 0f)
                StateChanged?.Invoke();
        }

        if (_barrierTimer > 0f)
            _barrierTimer -= dt;
        if (_fortressTimer > 0f)
            _fortressTimer -= dt;
        if (_recoilTimer > 0f)
            _recoilTimer -= dt;

        if (Def.PassiveId == "nanite_recovery")
        {
            _naniteTimer -= dt;
            if (_naniteTimer <= 0f)
            {
                _naniteTimer = cfg.NaniteIntervalSec;
                NanitePulse(cfg);
            }
        }

        if (_lockShotsLeft > 0)
        {
            _lockTimer -= dt;
            if (_lockTimer <= 0f)
            {
                FireLockShot();
                _lockShotsLeft--;
                _lockTimer = cfg.LockShotIntervalSec;
                if (_lockShotsLeft == 0)
                    StateChanged?.Invoke();
            }
        }
        else if (_burstShotsLeft > 0)
        {
            _burstTimer -= dt;
            if (_burstTimer <= 0f)
            {
                Fire();
                _burstShotsLeft--;
                _burstTimer = cfg.RapidBurstShotIntervalSec;
                if (_burstShotsLeft == 0)
                    StateChanged?.Invoke();
            }
        }
        else
        {
            bool overdrive = Def.UltimateId == "overdrive" && _ultStateTimer > 0f;
            float speedMult = overdrive ? 2f : 1f;
            _attackTimer -= dt * speedMult;
            if (_attackTimer <= 0f && AcquireTarget() != null)
            {
                Fire();
                _attackTimer = AttackInterval;
            }
        }

        int bob = (Time.GetTicksMsec() / 350) % 2 == 0 ? 0 : -1;
        float recoilX = _recoilTimer > 0f ? -2f : 0f;
        Sprite.Position = SpriteBaseOffset + new Vector2(recoilX, bob);

        if (SelfTest)
        {
            if (ActiveReady)
                ActivateActive();
            if (UltReady)
                ActivateUltimate();
        }
    }

    Combatant? AcquireTarget() => _manager.GetTargetForOperator(this);

    void Fire()
    {
        var target = AcquireTarget();
        if (target == null)
            return;
        FireAt(target);
    }

    void FireLockShot()
    {
        var target = _lockTarget != null && GodotObject.IsInstanceValid(_lockTarget) && _lockTarget.IsAlive
            ? _lockTarget
            : AcquireTarget() as EnemyUnit;
        if (target == null)
        {
            _lockShotsLeft = 0;
            return;
        }
        _lockTarget = target;
        FireAt(target);
    }

    void FireAt(Combatant target)
    {
        if (!target.IsAlive || _boltTexture == null)
            return;

        _recoilTimer = 0.09f;
        AttackPoseTimer = 0.18f;
        var muzzle = GlobalPosition + new Vector2(16f, -16f);
        var proj = Projectile.Create(muzzle, target, Balance.Instance.Config.ProjectileSpeed,
            Colors.White, _boltTexture, () => OnHit(target));
        _manager.AddChild(proj);
    }

    void OnHit(Combatant target)
    {
        float dmg = ComputeDamage(target, out bool crit);
        target.TakeDamage(dmg);
        DamageNumber.Spawn(_manager,
            target.GlobalPosition + new Vector2(-6f, -target.VisualHeight - 4f),
            dmg, crit ? new Color("#ffd75e") : Colors.White, crit);
        _ultCharge = Mathf.Min(1f, _ultCharge + Balance.Instance.Config.UltimateChargePerAttack);
        StateChanged?.Invoke();

        if (Def.PassiveId == "arc_conduction")
        {
            _attackCount++;
            var cfg = Balance.Instance.Config;
            if (_attackCount % Mathf.Max(1, cfg.ChainEveryAttacks) == 0)
                ChainFrom(target, dmg, cfg);
        }
    }

    void ChainFrom(Combatant primary, float dmg, BalanceConfig cfg)
    {
        float chainDmg = dmg * cfg.ChainDamageMult;
        int added = 0;
        var others = _manager.Enemies
            .Where(e => e.IsAlive && e != primary)
            .OrderBy(e => e.GlobalPosition.DistanceSquaredTo(primary.GlobalPosition));
        foreach (var e in others)
        {
            if (added >= cfg.ChainTargets)
                break;
            e.TakeDamage(chainDmg);
            DamageNumber.Spawn(_manager,
                e.GlobalPosition + new Vector2(-6f, -e.VisualHeight - 4f),
                chainDmg, new Color("#9fc8ff"));
            added++;
        }
        if (added > 0)
            RingFx.Spawn(_manager, primary.GlobalPosition, 16f, new Color("#4a9eff", 0.9f));
    }

    public float ComputeDamage(Combatant target, out bool crit)
    {
        var cfg = Balance.Instance.Config;
        float dmg = BaseAttack * (0.9f + GD.Randf() * 0.2f);
        crit = Def.PassiveId == "execution_protocol" && target.HpFraction < cfg.ExecutionThresholdPct;
        if (crit)
            dmg *= cfg.ExecutionBonusMult;
        return dmg;
    }

    public void ActivateActive()
    {
        if (!ActiveReady)
            return;
        var cfg = Balance.Instance.Config;
        _activeCooldownTimer = EffectiveActiveCooldown();

        switch (Def.ActiveId)
        {
            case "rapid_burst":
                _burstShotsLeft = cfg.RapidBurstShots;
                _burstTimer = 0f;
                break;
            case "pulse_field":
                PulseField(cfg);
                break;
            case "scout_drone":
                ScoutDrone(cfg);
                break;
            case "repair_wave":
                RepairWave(cfg);
                break;
            case "deploy_barrier":
                _barrierTimer = cfg.BarrierSec;
                RingFx.Spawn(_manager, GlobalPosition, 22f, new Color("#f0a030", 0.9f));
                break;
        }
        GD.Print($"[Combat] {Def.DisplayName} used {Def.ActiveLabel}");
        StateChanged?.Invoke();
    }

    public void ActivateUltimate()
    {
        if (!UltReady)
            return;
        var cfg = Balance.Instance.Config;
        _ultCharge = 0f;

        switch (Def.UltimateId)
        {
            case "overdrive":
                _ultStateTimer = cfg.OverdriveDurationSec;
                RingFx.Spawn(_manager, GlobalPosition, 20f, new Color("#e04848", 0.9f));
                break;
            case "blackout":
                Blackout(cfg);
                break;
            case "target_lock":
                TargetLock(cfg);
                break;
            case "second_wind":
                SecondWind(cfg);
                break;
            case "fortress":
                Fortress(cfg);
                break;
        }
        GD.Print($"[Combat] {Def.DisplayName} used {Def.UltLabel}");
        StateChanged?.Invoke();
    }

    void PulseField(BalanceConfig cfg)
    {
        float dmg = BaseAttack * cfg.PulseDamageMult;
        Vector2 centroid = Vector2.Zero;
        int count = 0;
        foreach (var e in AliveEnemies())
        {
            e.TakeDamage(dmg);
            e.ApplySlow(cfg.PulseSlowPct, cfg.PulseSlowSec);
            DamageNumber.Spawn(_manager,
                e.GlobalPosition + new Vector2(-6f, -e.VisualHeight - 4f),
                dmg, new Color("#9fc8ff"));
            centroid += e.GlobalPosition;
            count++;
        }
        var at = count > 0 ? centroid / count : GlobalPosition;
        RingFx.Spawn(_manager, at, 30f, new Color("#4a9eff", 0.9f));
    }

    void ScoutDrone(BalanceConfig cfg)
    {
        EnemyUnit? best = null;
        foreach (var e in AliveEnemies())
        {
            if (best == null || e.Hp > best.Hp)
                best = e;
        }
        if (best == null)
            return;
        best.Mark(cfg.ScoutMarkSec);
        RingFx.Spawn(_manager, best.GlobalPosition, 14f, new Color("#3ecfb2", 0.9f));
    }

    void RepairWave(BalanceConfig cfg)
    {
        float heal = cfg.RepairHealFlat + cfg.RepairHealAttackMult * BaseAttack;
        foreach (var ally in LivingAllies())
        {
            float healed = ally.Heal(heal);
            if (healed > 0f)
            {
                DamageNumber.Spawn(_manager,
                    ally.GlobalPosition + new Vector2(-6f, -ally.VisualHeight - 4f),
                    healed, new Color("#7de89a"), heal: true);
                ally.StateChanged?.Invoke();
            }
        }
        RingFx.Spawn(_manager, GlobalPosition, 26f, new Color("#e05ac8", 0.9f));
    }

    void Blackout(BalanceConfig cfg)
    {
        Vector2 centroid = Vector2.Zero;
        int count = 0;
        foreach (var e in AliveEnemies())
        {
            e.Stun(e.IsElite ? cfg.BlackoutStunSec : cfg.BlackoutFallbackStunSec);
            centroid += e.GlobalPosition;
            count++;
        }
        var at = count > 0 ? centroid / count : GlobalPosition;
        RingFx.Spawn(_manager, at, 34f, new Color("#4a9eff", 0.95f));
    }

    void TargetLock(BalanceConfig cfg)
    {
        var target = AcquireTarget() as EnemyUnit;
        if (target == null)
            return;

        _lockTarget = target;
        _lockShotsLeft = cfg.LockShots;
        _lockTimer = 0f;
        _ultStateTimer = cfg.LockShots * cfg.LockShotIntervalSec + 0.2f;
        target.Mark(cfg.LockMarkSec);
        RingFx.Spawn(_manager, target.GlobalPosition, 18f, new Color("#3ecfb2", 0.95f));
    }

    void SecondWind(BalanceConfig cfg)
    {
        var downed = _manager.Operators.FirstOrDefault(o => !o.IsAlive);
        if (downed != null)
        {
            downed.Revive(cfg.SecondWindRevivePct);
            RingFx.Spawn(_manager, downed.GlobalPosition, 20f, new Color("#e05ac8", 0.95f));
            return;
        }

        float heal = MaxHp * cfg.SecondWindFallbackPct;
        foreach (var ally in LivingAllies())
        {
            float healed = ally.Heal(heal);
            if (healed > 0f)
            {
                DamageNumber.Spawn(_manager,
                    ally.GlobalPosition + new Vector2(-6f, -ally.VisualHeight - 4f),
                    healed, new Color("#7de89a"), heal: true);
                ally.StateChanged?.Invoke();
            }
        }
        RingFx.Spawn(_manager, GlobalPosition, 24f, new Color("#e05ac8", 0.95f));
    }

    void Fortress(BalanceConfig cfg)
    {
        _fortressTimer = cfg.FortressSec;
        _ultStateTimer = cfg.FortressSec;
        SetShield(Mathf.Max(Shield, MaxHp * cfg.FortressShieldPct));
        RingFx.Spawn(_manager, GlobalPosition, 26f, new Color("#f0a030", 0.95f));
    }

    void NanitePulse(BalanceConfig cfg)
    {
        OperatorUnit? weakest = null;
        foreach (var o in _manager.Operators)
        {
            if (o.IsAlive && (weakest == null || o.HpFraction < weakest.HpFraction))
                weakest = o;
        }
        if (weakest == null || weakest.HpFraction >= 1f)
            return;

        float heal = cfg.NaniteHealFlat + cfg.NaniteHealAttackMult * BaseAttack;
        float healed = weakest.Heal(heal);
        if (healed > 0f)
        {
            DamageNumber.Spawn(_manager,
                weakest.GlobalPosition + new Vector2(-6f, -weakest.VisualHeight - 4f),
                healed, new Color("#7de89a"), heal: true);
            weakest.StateChanged?.Invoke();
        }
    }

    List<EnemyUnit> AliveEnemies()
    {
        var list = new List<EnemyUnit>();
        foreach (var e in _manager.Enemies)
            if (e.IsAlive)
                list.Add(e);
        return list;
    }

    List<OperatorUnit> LivingAllies()
    {
        var list = new List<OperatorUnit>();
        foreach (var o in _manager.Operators)
            if (o.IsAlive)
                list.Add(o);
        return list;
    }

    public override void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f)
            return;

        var cfg = Balance.Instance.Config;
        var fortifier = _manager.GetPassiveCarrier("fortified_plating");
        if (fortifier != null)
            amount *= cfg.FortifyTakenMult;
        if (_barrierTimer > 0f)
            amount *= cfg.BarrierTakenMult;
        if (_fortressTimer > 0f)
            amount *= cfg.FortressTakenMult;

        base.TakeDamage(amount);
        StateChanged?.Invoke();
    }

    public override void Die()
    {
        base.Die();
        GD.Print($"[Combat] {Def.DisplayName} downed");
        _burstShotsLeft = 0;
        _lockShotsLeft = 0;
        Modulate = new Color(0.45f, 0.45f, 0.5f, 0.85f);
        Bar.Visible = false;
        _manager.OnOperatorDowned(this);
    }

    public void Revive(float pct = 1f)
    {
        Hp = Mathf.Clamp(MaxHp * pct, 1f, MaxHp);
        Shield = 0f;
        Modulate = Colors.White;
        Bar.Visible = true;
        Bar.SetBar(Hp, MaxHp, Shield);
        _attackTimer = 0.4f;
        GD.Print($"[Combat] {Def.DisplayName} revived");
        StateChanged?.Invoke();
    }
}
