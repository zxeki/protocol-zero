using Godot;
using ProtocolZero.UI;

namespace ProtocolZero.Combat;

public abstract partial class Combatant : Node2D
{
    public float MaxHp { get; protected set; } = 100f;
    public float Hp { get; protected set; } = 100f;
    public float BaseAttack { get; protected set; } = 10f;
    public float AttackInterval { get; protected set; } = 1f;
    public float VisualHeight { get; protected set; } = 32f;
    public bool IsAlive => Hp > 0f;
    public float HpFraction => MaxHp <= 0f ? 0f : Hp / MaxHp;

    public event Action<Combatant>? Died;
    public event Action<Combatant, float>? Damaged;

    protected Sprite2D Sprite = null!;
    protected HealthBar Bar = null!;
    protected Vector2 SpriteBaseOffset = Vector2.Zero;

    float _flashTimer;

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
        Bar.SetBar(Hp, MaxHp);
    }

    protected abstract void BuildVisuals();

    public void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f)
            return;

        Hp = Mathf.Max(0f, Hp - amount);
        Bar.SetBar(Hp, MaxHp);
        _flashTimer = 0.12f;
        Damaged?.Invoke(this, amount);
        OnDamagedFeedback();

        if (Hp <= 0f)
            Die();
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
    }
}
