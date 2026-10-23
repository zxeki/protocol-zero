using Godot;
using ProtocolZero.Core;
using ProtocolZero.Data;
using ProtocolZero.Util;

namespace ProtocolZero.UI;

public partial class SettingsPanel : Control
{
    public event Action? Changed;

    sealed class Row
    {
        public required Label Name { get; init; }
        public required Button Toggle { get; init; }
        public required Func<(string Text, Color Color)> Read { get; init; }
        public required Action ToggleAction { get; init; }
    }

    readonly List<Row> _rows = new();

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        SetAnchorsPreset(LayoutPreset.FullRect);
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;

        Build();
    }

    void Build()
    {
        var dim = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.6f),
            MouseFilter = MouseFilterEnum.Stop,
        };
        dim.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(dim);

        var panel = new PanelContainer
        {
            Position = new Vector2(20f, 10f),
            CustomMinimumSize = new Vector2(440f, 250f),
            MouseFilter = MouseFilterEnum.Stop,
        };
        var sb = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.07f, 0.10f, 0.97f),
            BorderColor = new Color("#4a9eff"),
        };
        sb.SetBorderWidthAll(1);
        sb.SetCornerRadiusAll(2);
        panel.AddThemeStyleboxOverride("panel", sb);
        AddChild(panel);

        var root = new VBoxContainer();
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.OffsetLeft = 8f;
        root.OffsetRight = -8f;
        root.OffsetTop = 7f;
        root.OffsetBottom = -7f;
        root.AddThemeConstantOverride("separation", 3);
        root.MouseFilter = MouseFilterEnum.Ignore;
        panel.AddChild(root);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 8);
        header.MouseFilter = MouseFilterEnum.Ignore;
        root.AddChild(header);

        var title = new Label { Text = "SETTINGS", MouseFilter = MouseFilterEnum.Ignore };
        title.AddThemeFontSizeOverride("font_size", 9);
        title.AddThemeColorOverride("font_color", new Color("#4a9eff"));
        header.AddChild(title);

        var headerSpacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
        header.AddChild(headerSpacer);

        AddSettingsRow(root, "Offline rewards",
            s => s.OfflineRewardsEnabled,
            (s, v) => s.OfflineRewardsEnabled = v);

        AddSettingsRow(root, "Always on top",
            s => s.AlwaysOnTop,
            (s, v) => s.AlwaysOnTop = v);

        AddSettingsRow(root, "Borderless window",
            s => s.Borderless,
            (s, v) => s.Borderless = v);

        AddSettingsRow(root, "Minimize to tray",
            s => s.MinimizeToTray,
            (s, v) => s.MinimizeToTray = v);

        AddSettingsRow(root, "Window size",
            s => s.WindowExpanded,
            (s, v) => s.WindowExpanded = v,
            onText: "LARGE", offText: "COMPACT");

        AddPauseRow(root);

        var footer = new HBoxContainer();
        footer.AddThemeConstantOverride("separation", 8);
        footer.MouseFilter = MouseFilterEnum.Ignore;
        root.AddChild(footer);

        var hint = new Label
        {
            Text = "Changes apply instantly and are saved. Menus pause combat.",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        hint.AddThemeFontSizeOverride("font_size", 7);
        hint.AddThemeColorOverride("font_color", new Color("#6b7a8d"));
        footer.AddChild(hint);

        var fSpacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
        footer.AddChild(fSpacer);

        var closeBtn = MakeButton("CLOSE", new Color("#9fb2c8"), 70f);
        closeBtn.Pressed += Close;
        footer.AddChild(closeBtn);
    }

    void AddSettingsRow(
        VBoxContainer root,
        string name,
        Func<UserSettings, bool> get,
        Action<UserSettings, bool> set,
        string onText = "ON",
        string offText = "OFF")
    {
        var gs = GameState.Instance;

        var (nameLabel, btn) = AddRow(root, name);
        var row = new Row
        {
            Name = nameLabel,
            Toggle = btn,
            Read = () => get(gs.Settings) ? (onText, new Color("#3ecfb2")) : (offText, new Color("#6b7a8d")),
            ToggleAction = () =>
            {
                gs.UpdateSettings(s => set(s, !get(s)));
                WindowManager.ApplyLayout(gs.Settings);
                Changed?.Invoke();
            },
        };
        _rows.Add(row);
    }

    void AddPauseRow(VBoxContainer root)
    {
        var gs = GameState.Instance;

        var (nameLabel, btn) = AddRow(root, "Game pause");
        var row = new Row
        {
            Name = nameLabel,
            Toggle = btn,
            Read = () => gs.ManualPause ? ("RESUME", new Color("#ffd75e")) : ("PAUSE", new Color("#3ecfb2")),
            ToggleAction = () =>
            {
                gs.ManualPause = !gs.ManualPause;
                Changed?.Invoke();
            },
        };
        _rows.Add(row);
    }

    (Label Name, Button Btn) AddRow(VBoxContainer root, string name)
    {
        var rowPanel = new PanelContainer { CustomMinimumSize = new Vector2(424f, 28f) };
        var rsb = new StyleBoxFlat
        {
            BgColor = new Color(0.07f, 0.09f, 0.12f, 0.9f),
            BorderColor = new Color("#4a9eff", 0.35f),
        };
        rsb.SetBorderWidthAll(1);
        rsb.SetCornerRadiusAll(2);
        rowPanel.AddThemeStyleboxOverride("panel", rsb);
        rowPanel.MouseFilter = MouseFilterEnum.Ignore;

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 6);
        hbox.MouseFilter = MouseFilterEnum.Ignore;
        rowPanel.AddChild(hbox);

        var nameLabel = new Label { Text = name, MouseFilter = MouseFilterEnum.Ignore };
        nameLabel.AddThemeFontSizeOverride("font_size", 8);
        nameLabel.AddThemeColorOverride("font_color", new Color("#9fb2c8"));
        nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hbox.AddChild(nameLabel);

        var btn = MakeButton("", new Color("#4a9eff"), 86f);
        hbox.AddChild(btn);

        root.AddChild(rowPanel);
        return (nameLabel, btn);
    }

    void RefreshRows()
    {
        foreach (var row in _rows)
        {
            var (text, color) = row.Read();
            row.Toggle.Text = text;
            row.Toggle.AddThemeColorOverride("font_color", color);
            row.Toggle.AddThemeColorOverride("font_hover_color", color);
            row.Toggle.AddThemeColorOverride("font_pressed_color", color);
        }
    }

    public void Open()
    {
        RefreshRows();
        Visible = true;
        GetTree().Paused = true;
    }

    public void Close()
    {
        Visible = false;
        GetTree().Paused = GameState.Instance.ManualPause;
    }

    static Button MakeButton(string text, Color accent, float width)
    {
        var b = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(width, 18f),
            FocusMode = FocusModeEnum.None,
        };
        b.AddThemeFontSizeOverride("font_size", 7);

        var normal = new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.12f, 0.16f, 0.95f),
            BorderColor = new Color(accent, 0.7f),
        };
        normal.SetBorderWidthAll(1);
        normal.SetCornerRadiusAll(2);

        var hover = (StyleBoxFlat)normal.Duplicate();
        hover.BgColor = new Color(0.14f, 0.17f, 0.22f, 0.95f);

        var pressed = (StyleBoxFlat)normal.Duplicate();
        pressed.BgColor = new Color(0.20f, 0.24f, 0.30f, 0.95f);

        var disabled = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.09f, 0.12f, 0.6f),
            BorderColor = new Color(accent, 0.25f),
        };
        disabled.SetBorderWidthAll(1);
        disabled.SetCornerRadiusAll(2);

        b.AddThemeStyleboxOverride("normal", normal);
        b.AddThemeStyleboxOverride("hover", hover);
        b.AddThemeStyleboxOverride("pressed", pressed);
        b.AddThemeStyleboxOverride("disabled", disabled);
        b.AddThemeColorOverride("font_color", Colors.White);
        b.AddThemeColorOverride("font_hover_color", Colors.White);
        b.AddThemeColorOverride("font_pressed_color", Colors.White);
        b.AddThemeColorOverride("font_disabled_color", new Color(accent, 0.45f));
        return b;
    }
}
