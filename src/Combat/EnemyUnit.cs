using Godot;
using ProtocolZero.Core;
using ProtocolZero.Data;
using ProtocolZero.Util;

namespace ProtocolZero.Combat;

public partial class EnemyUnit : Combatant
{
    public EnemyDef Def { get; private set; } = null!;
    public int Wave { get; private set; } = 1;
    public bool IsElite { get; private set; }

    CombatManager _manager = null!;
    float _attackTimer;
    Texture2D? _boltTexture;
    float _slowTimer;
    float _slowPct;
    float _stunTimer;
    float _markTimer;
    UI.MarkIndicator? _markIndicator;
    OperatorUnit? _targetRef;

    public bool IsStunned => _stunTimer > 0f;
    public bool IsMarked => _markTimer > 0f;
    public float StunRemaining => _stunTimer;

    public void Init(EnemyDef def, CombatManager manager, int wave, bool isElite)
    {
        Def = def;
        _manager = manager;
        Wave = wave;
        IsElite = isElite;

        var cfg = Balance.Instance.Config;
        float hpScale = 1f + cfg.EnemyHpPerWavePct * (wave - 1);
        float atkScale = 1f + cfg.EnemyAttackPerWavePct * (wave - 1);
        MaxHp = def.BaseHp * hpScale;
        Hp = MaxHp;
        BaseAttack = def.BaseAttack * atkScale;
        AttackInterval = def.AttackInterval;
        _attackTimer = AttackInterval * (0.4f + GD.Randf() * 0.4f);
    }

    protected override void BuildVisuals()
    {
        Sprite.Texture = Def.Id switch
        {
            "enforcer" => PixelArt.BuildEnforcer(),
            "hunter_drone" => PixelArt.BuildHunterDrone(),
            "the_warden" => PixelArt.BuildWarden(),
            _ => PixelArt.BuildWatcher(),
        };
        VisualHeight = Def.Id switch
        {
            "enforcer" => 32f,
            "hunter_drone" => 14f,
            "the_warden" => 52f,
            _ => 16f,
        };
        SpriteBaseOffset = new Vector2(0f, -VisualHeight / 2f);
        Sprite.Position = SpriteBaseOffset;
        _boltTexture = PixelArt.BuildBolt(new Color("#ffd2c4"), new Color("#e0483a"));
    }

    public void ApplySlow(float pct, float sec)
    {
        _slowPct = Mathf.Max(_slowPct, pct);
        _slowTimer = Mathf.Max(_slowTimer, sec);
    }

    public void Stun(float sec)
    {
        _stunTimer = Mathf.Max(_stunTimer, sec);
    }

    public void Mark(float sec)
    {
        _markTimer = Mathf.Max(_markTimer, sec);
        if (_markIndicator == null || !GodotObject.IsInstanceValid(_markIndicator))
            _markIndicator = UI.MarkIndicator.Attach(this);
    }

    public override void TakeDamage(float amount)
    {
        if (IsMarked)
        {
            amount *= Balance.Instance.Config.MarkBonusMult;
            if (_manager.GetPassiveCarrier("target_analysis") != null)
                amount *= Balance.Instance.Config.TargetAnalysisBonusMult;
        }
        base.TakeDamage(amount);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!IsAlive)
            return;

        float dt = (float)delta;

        if (_slowTimer > 0f)
        {
            _slowTimer -= dt;
            if (_slowTimer <= 0f)
                _slowPct = 0f;
        }
        if (_stunTimer > 0f)
            _stunTimer -= dt;
        if (_markTimer > 0f)
        {
            _markTimer -= dt;
            if (_markTimer <= 0f && _markIndicator != null && GodotObject.IsInstanceValid(_markIndicator))
                _markIndicator.QueueFree();
        }

        if (IsStunned)
        {
            Sprite.Modulate = new Color(0.55f, 0.65f, 0.95f);
            return;
        }

        var target = ResolveTarget();
        if (target == null)
            return;

        float dx = GlobalPosition.X - target.GlobalPosition.X;
        if (dx > Def.AttackRange)
        {
            GlobalPosition += Vector2.Left * Def.MoveSpeed * (1f - _slowPct) * dt;
        }
        else
        {
            _attackTimer -= dt * (1f - _slowPct * 0.5f);
            if (_attackTimer <= 0f)
            {
                Fire(target);
                _attackTimer = AttackInterval;
            }
        }

        int bob = (Time.GetTicksMsec() / 280) % 2 == 0 ? 0 : 1;
        Sprite.Position = SpriteBaseOffset + new Vector2(0, bob);
    }

    OperatorUnit? ResolveTarget()
    {
        var taunter = _manager.GetFortressTaunter();
        if (taunter != null)
            return taunter;

        if (_targetRef != null && GodotObject.IsInstanceValid(_targetRef) && _targetRef.IsAlive)
            return _targetRef;

        _targetRef = _manager.GetTargetForEnemy(this);
        return _targetRef;
    }

    void Fire(OperatorUnit target)
    {
        if (_boltTexture == null)
            return;

        var muzzle = GlobalPosition + new Vector2(-8f, -VisualHeight * 0.5f);
        float dmg = BaseAttack * (0.9f + GD.Randf() * 0.2f);
        var proj = Projectile.Create(muzzle, target, Balance.Instance.Config.ProjectileSpeed * 0.75f,
            Colors.White, _boltTexture, () => target.TakeDamage(dmg));
        _manager.AddChild(proj);
    }

    public override void Die()
    {
        base.Die();
        SetProcess(false);
        Bar.Visible = false;
        var tw = CreateTween();
        tw.TweenProperty(this, "modulate:a", 0f, 0.35f).SetEase(Tween.EaseType.Out);
        tw.TweenCallback(Callable.From(QueueFree));
    }
}
