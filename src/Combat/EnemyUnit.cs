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
            _ => PixelArt.BuildWatcher(),
        };
        VisualHeight = Def.Id == "enforcer" ? 32f : 16f;
        SpriteBaseOffset = new Vector2(0f, -VisualHeight / 2f);
        Sprite.Position = SpriteBaseOffset;
        _boltTexture = PixelArt.BuildBolt(new Color("#ffd2c4"), new Color("#e0483a"));
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!IsAlive)
            return;

        float dt = (float)delta;
        var target = _manager.GetTargetForEnemy(this);
        if (target == null)
            return;

        float dx = GlobalPosition.X - target.GlobalPosition.X;
        if (dx > Def.AttackRange)
        {
            GlobalPosition += Vector2.Left * Def.MoveSpeed * dt;
        }
        else
        {
            _attackTimer -= dt;
            if (_attackTimer <= 0f)
            {
                Fire(target);
                _attackTimer = AttackInterval;
            }
        }

        int bob = (Time.GetTicksMsec() / 280) % 2 == 0 ? 0 : 1;
        Sprite.Position = SpriteBaseOffset + new Vector2(0, bob);
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
