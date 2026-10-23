using Godot;
using ProtocolZero.Data;

namespace ProtocolZero.Util;

public static class WindowManager
{
    public static readonly Vector2I CompactSize = new(960, 540);
    public static readonly Vector2I ExpandedSize = new(1440, 810);

    public static void ApplyLayout(UserSettings s)
    {
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, s.Borderless);
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.AlwaysOnTop, s.AlwaysOnTop);
        DisplayServer.WindowSetSize(s.WindowExpanded ? ExpandedSize : CompactSize);
        PositionAboveTaskbar();
    }

    public static void PositionAboveTaskbar()
    {
        var usable = DisplayServer.ScreenGetUsableRect(DisplayServer.GetPrimaryScreen());
        var size = DisplayServer.WindowGetSize();
        int x = usable.Position.X + (usable.Size.X - size.X) / 2;
        int y = usable.Position.Y + usable.Size.Y - size.Y;
        DisplayServer.WindowSetPosition(new Vector2I(x, y));
    }
}
