using Godot;

namespace ProtocolZero.UI;

public partial class HealthBar : Node2D
{
    const float Width = 22f;

    float _pct = 1f;
    Color _fill = new("#5ed86a");

    public void SetBar(float current, float max)
    {
        _pct = max <= 0f ? 0f : Mathf.Clamp(current / max, 0f, 1f);
        _fill = _pct switch
        {
            < 0.3f => new Color("#e04848"),
            < 0.6f => new Color("#f0a030"),
            _ => new Color("#5ed86a"),
        };
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(-Width / 2f - 1f, -1f, Width + 2f, 4f), new Color(0f, 0f, 0f, 0.65f));
        DrawRect(new Rect2(-Width / 2f, 0f, Width * _pct, 2f), _fill);
    }
}
