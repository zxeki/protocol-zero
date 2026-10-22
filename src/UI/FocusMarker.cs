using Godot;

namespace ProtocolZero.UI;

public partial class FocusMarker : Node2D
{
    public override void _Process(double delta)
    {
        Modulate = new Color(1f, 1f, 1f, (Time.GetTicksMsec() / 250) % 2 == 0 ? 0.95f : 0.55f);
    }

    public override void _Draw()
    {
        var c = new Color("#ffd75e");
        const float w = 16f, h = 22f, t = 1.5f, arm = 5f;
        float x0 = -w / 2f, x1 = w / 2f, y0 = -h, y1 = 0f;

        DrawRect(new Rect2(x0, y0, arm, t), c);
        DrawRect(new Rect2(x0, y0, t, arm), c);
        DrawRect(new Rect2(x1 - arm, y0, arm, t), c);
        DrawRect(new Rect2(x1 - t, y0, t, arm), c);
        DrawRect(new Rect2(x0, y1 - t, arm, t), c);
        DrawRect(new Rect2(x0, y1 - arm, t, arm), c);
        DrawRect(new Rect2(x1 - arm, y1 - t, arm, t), c);
        DrawRect(new Rect2(x1 - t, y1 - arm, t, arm), c);
    }
}
