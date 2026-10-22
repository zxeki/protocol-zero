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

    CombatManager _manager = null!;
    float _attackTimer;
    float _burstTimer;
    int _burstShotsLeft;
    float _burstCooldownTimer;
    float _ultCharge;
    float _overdriveTimer;
    float _recoilTimer;
    Texture2D? _boltTexture;

    public Texture2D? PortraitTexture { get; private set; }
    public bool BurstReady => _burstCooldownTimer <= 0f && _burstShotsLeft == 0 && IsAlive;
    public bool UltReady => _ultCharge >= 1f && _overdriveTimer <= 0f && IsAlive;
    public float BurstCooldownRemaining => _burstCooldownTimer;
    public float OverdriveRemaining => _overdriveTimer;
    public float UltCharge01 => _ultCharge;

    public void Init(OperatorDef def, CombatManager manager, int level)
    {
        Def = def;
        _manager = manager;
        Level = level;
        ApplyStats();
    }

    void ApplyStats()
    {
        var cfg = Balance.Instance.Config;
        float hpMult = 1f + cfg.HpPerLevelPct * (Level - 1);
        float atkMult = 1f + cfg.AttackPerLevelPct * (Level - 1);
        MaxHp = Def.BaseHp * hpMult;
        Hp = MaxHp;
        BaseAttack = Def.BaseAttack * atkMult;
        AttackInterval = Def.AttackInterval;
        Bar?.SetBar(Hp, MaxHp);
    }

    protected override void BuildVisuals()
    {
        PortraitTexture = PixelArt.BuildStriker();
        Sprite.Texture = PortraitTexture;
        VisualHeight = 28f;
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

        if (_burstCooldownTimer > 0f)
        {
            _burstCooldownTimer -= dt;
            if (_burstCooldownTimer <= 0f)
                StateChanged?.Invoke();
        }

        bool overdrive = _overdriveTimer > 0f;
        if (overdrive)
        {
            _overdriveTimer -= dt;
            if (_overdriveTimer <= 0f)
                StateChanged?.Invoke();
        }

        if (_recoilTimer > 0f)
            _recoilTimer -= dt;

        if (_burstShotsLeft > 0)
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
    }

    Combatant? AcquireTarget() => _manager.GetTargetForOperator(this);

    void Fire()
    {
        var target = AcquireTarget();
        if (target == null || !target.IsAlive || _boltTexture == null)
            return;

        _recoilTimer = 0.09f;
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
    }

    public float ComputeDamage(Combatant target, out bool crit)
    {
        var cfg = Balance.Instance.Config;
        float dmg = BaseAttack * (0.9f + GD.Randf() * 0.2f);
        crit = target.HpFraction < cfg.ExecutionThresholdPct;
        if (crit)
            dmg *= cfg.ExecutionBonusMult;
        return dmg;
    }

    public void ActivateActive()
    {
        if (!BurstReady)
            return;
        var cfg = Balance.Instance.Config;
        _burstShotsLeft = cfg.RapidBurstShots;
        _burstTimer = 0f;
        _burstCooldownTimer = cfg.RapidBurstCooldownSec;
        StateChanged?.Invoke();
    }

    public void ActivateUltimate()
    {
        if (!UltReady)
            return;
        _ultCharge = 0f;
        _overdriveTimer = Balance.Instance.Config.OverdriveDurationSec;
        StateChanged?.Invoke();
    }

    public override void Die()
    {
        base.Die();
        Modulate = new Color(0.45f, 0.45f, 0.5f, 0.85f);
        Bar.Visible = false;
        _manager.OnOperatorDowned(this);
    }

    public void Revive()
    {
        Hp = MaxHp;
        Modulate = Colors.White;
        Bar.Visible = true;
        Bar.SetBar(Hp, MaxHp);
        _attackTimer = 0.4f;
        StateChanged?.Invoke();
    }
}
