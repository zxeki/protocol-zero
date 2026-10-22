using Godot;

namespace ProtocolZero.Combat;

public partial class Projectile : Node2D
{
    Combatant? _target;
    Vector2 _lastTargetPos;
    float _speed;
    Action? _onHit;
    Sprite2D _sprite = null!;

    public static Projectile Create(Vector2 from, Combatant target, float speed, Color color, Texture2D texture, Action onHit)
    {
        var p = new Projectile
        {
            _target = target,
            _lastTargetPos = target.GlobalPosition,
            _speed = speed,
            _onHit = onHit,
            _sprite = new Sprite2D
            {
                Texture = texture,
                Centered = true,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                Modulate = color,
            },
        };
        p.GlobalPosition = from;
        p.AddChild(p._sprite);
        return p;
    }

    public override void _Process(double delta)
    {
        if (_target != null && GodotObject.IsInstanceValid(_target) && _target.IsAlive)
            _lastTargetPos = _target.GlobalPosition;

        Vector2 to = _lastTargetPos - GlobalPosition;
        float step = _speed * (float)delta;
        Rotation = to.Angle();

        if (to.Length() <= step + 4f)
        {
            Impact();
            return;
        }

        GlobalPosition += to.Normalized() * step;
    }

    void Impact()
    {
        if (_target != null && GodotObject.IsInstanceValid(_target) && _target.IsAlive)
            _onHit?.Invoke();
        QueueFree();
    }
}
