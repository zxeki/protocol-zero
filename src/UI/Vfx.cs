using Godot;

namespace ProtocolZero.UI;

public partial class RingFx : Node2D
{
    float _radius;
    Color _color;

    public static RingFx Spawn(Node parent, Vector2 pos, float radius, Color color)
    {
        var fx = new RingFx { _radius = radius, _color = color };
        fx.GlobalPosition = pos;
        parent.AddChild(fx);

        var tw = fx.CreateTween();
        tw.TweenProperty(fx, "scale", new Vector2(1.4f, 1.4f), 0.35f).SetEase(Tween.EaseType.Out);
        tw.Parallel().TweenProperty(fx, "modulate:a", 0f, 0.35f).SetEase(Tween.EaseType.In);
        tw.TweenCallback(Callable.From(fx.QueueFree));
        return fx;
    }

    public override void _Draw()
    {
        DrawArc(Vector2.Zero, _radius, 0f, Mathf.Tau, 28, _color, 2f);
    }
}

public partial class MarkIndicator : Node2D
{
    public static MarkIndicator Attach(Combat.Combatant host)
    {
        var ind = new MarkIndicator();
        host.AddChild(ind);
        ind.Position = new Vector2(0f, -host.VisualHeight - 7f);
        return ind;
    }

    public override void _Draw()
    {
        var teal = new Color("#3ecfb2");
        var pts = new[]
        {
            new Vector2(0f, -4f),
            new Vector2(3.5f, 0f),
            new Vector2(0f, 4f),
            new Vector2(-3.5f, 0f),
        };
        DrawColoredPolygon(pts, teal);
        DrawPolyline(pts, Colors.Black, 1f);
    }
}

public partial class BurstFx : Node2D
{
    Texture2D _texture = null!;

    public static BurstFx Spawn(Node parent, Vector2 pos, Texture2D texture)
    {
        var fx = new BurstFx { _texture = texture };
        fx.GlobalPosition = pos;
        parent.AddChild(fx);

        var tw = fx.CreateTween();
        tw.TweenProperty(fx, "scale", new Vector2(1.35f, 1.35f), 0.35f).SetEase(Tween.EaseType.Out);
        tw.Parallel().TweenProperty(fx, "modulate:a", 0f, 0.3f).SetDelay(0.15f);
        tw.TweenCallback(Callable.From(fx.QueueFree));
        return fx;
    }

    public override void _Draw()
    {
        var s = _texture.GetSize();
        DrawTextureRect(_texture, new Rect2(-s.X / 2f, -s.Y / 2f, s.X, s.Y), false);
    }
}
