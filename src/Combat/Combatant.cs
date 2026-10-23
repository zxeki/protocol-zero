using Godot;
using ProtocolZero.UI;

namespace ProtocolZero.Combat;

public abstract partial class Combatant : Node2D
{
    public float MaxHp { get; protected set; } = 100f;
    public float Hp { get; protected set; } = 100f;
    public float Shield { get; protected set; }
    public float BaseAttack { get; protected set; } = 10f;
    public float AttackInterval { get; protected set; } = 1f;
    public float VisualHeight { get; protected set; } = 32f;
    public bool IsAlive => Hp > 0f;
    public float HpFraction => MaxHp <= 0f ? 0f : Hp / MaxHp;

    public event Action<Combatant>? Died;
    public event Action<Combatant, float>? Damaged;
    public event Action<Combatant, float>? Healed;

    protected Sprite2D Sprite = null!;
    protected HealthBar Bar = null!;
    protected Vector2 SpriteBaseOffset = Vector2.Zero;

    protected Texture2D[] IdleFrames = System.Array.Empty<Texture2D>();
    protected Texture2D[] MoveFrames = System.Array.Empty<Texture2D>();
    protected Texture2D[] AttackFrames = System.Array.Empty<Texture2D>();
    protected Texture2D[] HurtFrames = System.Array.Empty<Texture2D>();
    protected Texture2D[] DefeatFrames = System.Array.Empty<Texture2D>();
    protected float AttackPoseTimer;

    float _flashTimer;
    float _hurtPoseTimer;
    float _animTimer;
    int _animIndex;
    Texture2D[]? _animRow;

    protected virtual bool IsMoving => false;

    public override void _Ready()
    {
        Sprite = new Sprite2D
        {
            Centered = true,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        };
        AddChild(Sprite);

        Bar = new HealthBar();
        AddChild(Bar);

        BuildVisuals();

        Bar.Position = new Vector2(0f, -VisualHeight - 6f);
        Bar.SetBar(Hp, MaxHp, Shield);
    }

    protected abstract void BuildVisuals();

    public virtual void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f)
            return;

        float remaining = amount;
        if (Shield > 0f)
        {
            float absorbed = Mathf.Min(Shield, remaining);
            Shield -= absorbed;
            remaining -= absorbed;
        }

        if (remaining > 0f)
            Hp = Mathf.Max(0f, Hp - remaining);

        Bar.SetBar(Hp, MaxHp, Shield);
        _flashTimer = 0.12f;
        _hurtPoseTimer = 0.12f;
        Damaged?.Invoke(this, amount);
        OnDamagedFeedback();

        if (Hp <= 0f)
            Die();
    }

    public float Heal(float amount)
    {
        if (!IsAlive || amount <= 0f || Hp >= MaxHp)
            return 0f;

        float healed = Mathf.Min(amount, MaxHp - Hp);
        Hp += healed;
        Bar.SetBar(Hp, MaxHp, Shield);
        Healed?.Invoke(this, healed);
        return healed;
    }

    public void SetShield(float amount)
    {
        Shield = Mathf.Max(0f, amount);
        Bar?.SetBar(Hp, MaxHp, Shield);
    }

    protected virtual void OnDamagedFeedback()
    {
    }

    public virtual void Die()
    {
        Died?.Invoke(this);
    }

    public override void _Process(double delta)
    {
        if (_flashTimer > 0f)
        {
            _flashTimer -= (float)delta;
            Sprite.Modulate = new Color(1f, 0.4f, 0.4f);
        }
        else if (Sprite.Modulate != Colors.White)
        {
            Sprite.Modulate = Colors.White;
        }

        UpdateAnimation(delta);
    }

    void UpdateAnimation(double delta)
    {
        if (IdleFrames.Length == 0)
            return;

        float dt = (float)delta;
        if (_hurtPoseTimer > 0f)
            _hurtPoseTimer -= dt;
        if (AttackPoseTimer > 0f)
            AttackPoseTimer -= dt;

        var row =
            !IsAlive && DefeatFrames.Length > 0 ? DefeatFrames :
            _hurtPoseTimer > 0f && HurtFrames.Length > 0 ? HurtFrames :
            AttackPoseTimer > 0f && AttackFrames.Length > 0 ? AttackFrames :
            IsMoving && MoveFrames.Length > 0 ? MoveFrames :
            IdleFrames;

        bool loop = ReferenceEquals(row, IdleFrames) || ReferenceEquals(row, MoveFrames);
        if (!ReferenceEquals(row, _animRow))
        {
            _animRow = row;
            _animIndex = 0;
            _animTimer = 0f;
        }

        float step = ReferenceEquals(row, IdleFrames) ? 0.15f
            : ReferenceEquals(row, MoveFrames) ? 0.13f
            : ReferenceEquals(row, DefeatFrames) ? 0.22f
            : 1f;

        _animTimer += dt;
        if (_animTimer >= step)
        {
            _animTimer = 0f;
            int next = _animIndex + 1;
            if (next >= row.Length)
                next = loop ? 0 : row.Length - 1;
            _animIndex = next;
        }

        var tex = row[_animIndex];
        if (Sprite.Texture != tex)
        {
            Sprite.Texture = tex;
            SpriteBaseOffset = new Vector2(0f, -tex.GetHeight() / 2f);
            if (!IsAlive)
                Sprite.Position = SpriteBaseOffset;
        }
    }
}
