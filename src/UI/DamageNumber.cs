using Godot;

namespace ProtocolZero.UI;

public static class DamageNumber
{
    public static void Spawn(Node parent, Vector2 pos, float amount, Color color, bool big = false)
    {
        var label = new Label
        {
            Text = Mathf.RoundToInt(amount).ToString(),
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = pos + new Vector2(-20f, 0f),
            Size = new Vector2(40f, 12f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", big ? 10 : 8);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
        label.AddThemeConstantOverride("outline_size", 3);
        parent.AddChild(label);

        var tw = label.CreateTween();
        tw.TweenProperty(label, "position:y", label.Position.Y - 14f, 0.55f).SetEase(Tween.EaseType.Out);
        tw.Parallel().TweenProperty(label, "modulate:a", 0f, 0.25f).SetDelay(0.3f);
        tw.TweenCallback(Callable.From(label.QueueFree));
    }
}
