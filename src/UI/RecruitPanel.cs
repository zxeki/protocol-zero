using Godot;
using ProtocolZero.Core;
using ProtocolZero.Data;

namespace ProtocolZero.UI;

public partial class RecruitPanel : Control
{
    readonly List<Control> _rows = new();
    Label _headerLabel = null!;
    Button _closeBtn = null!;

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
            Size = new Vector2(440f, 250f),
            CustomMinimumSize = new Vector2(440f, 250f),
            MouseFilter = MouseFilterEnum.Stop,
        };
        var sb = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.07f, 0.10f, 0.97f),
            BorderColor = new Color("#3ecfb2"),
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

        var title = new Label { Text = "SQUAD DEPLOYMENT", MouseFilter = MouseFilterEnum.Ignore };
        title.AddThemeFontSizeOverride("font_size", 9);
        title.AddThemeColorOverride("font_color", new Color("#3ecfb2"));
        header.AddChild(title);

        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
        header.AddChild(spacer);

        _headerLabel = new Label { MouseFilter = MouseFilterEnum.Ignore };
        _headerLabel.AddThemeFontSizeOverride("font_size", 8);
        header.AddChild(_headerLabel);

        foreach (var op in Balance.Instance.Operators)
        {
            var row = BuildRow(op);
            root.AddChild(row);
            _rows.Add(row);
        }

        var footer = new HBoxContainer();
        footer.AddThemeConstantOverride("separation", 8);
        footer.MouseFilter = MouseFilterEnum.Ignore;
        root.AddChild(footer);

        var hint = new Label
        {
            Text = "Recruit with credits. Squad changes redeploy instantly.",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        hint.AddThemeFontSizeOverride("font_size", 7);
        hint.AddThemeColorOverride("font_color", new Color("#6b7a8d"));
        footer.AddChild(hint);

        var fSpacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
        footer.AddChild(fSpacer);

        _closeBtn = MakeButton("CLOSE", new Color("#9fb2c8"), 70f);
        _closeBtn.Pressed += Close;
        footer.AddChild(_closeBtn);

        GameState.Instance.CreditsChanged += OnModelChanged;
        GameState.Instance.OwnedChanged += OnOwnedChanged;
        GameState.Instance.SquadChanged += OnSquadChanged;
    }

    Control BuildRow(OperatorDef def)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(424f, 32f) };
        var sb = new StyleBoxFlat
        {
            BgColor = new Color(0.07f, 0.09f, 0.12f, 0.9f),
            BorderColor = new Color(def.Accent, 0.35f),
        };
        sb.SetBorderWidthAll(1);
        sb.SetCornerRadiusAll(2);
        panel.AddThemeStyleboxOverride("panel", sb);
        panel.MouseFilter = MouseFilterEnum.Ignore;

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 6);
        hbox.MouseFilter = MouseFilterEnum.Ignore;
        panel.AddChild(hbox);

        var chip = new PanelContainer { CustomMinimumSize = new Vector2(8f, 24f) };
        var chipSb = new StyleBoxFlat { BgColor = def.Accent };
        chipSb.SetCornerRadiusAll(1);
        chip.AddThemeStyleboxOverride("panel", chipSb);
        chip.MouseFilter = MouseFilterEnum.Ignore;
        hbox.AddChild(chip);

        var nameLabel = new Label { Text = def.DisplayName, MouseFilter = MouseFilterEnum.Ignore };
        nameLabel.AddThemeFontSizeOverride("font_size", 8);
        nameLabel.AddThemeColorOverride("font_color", def.Accent);
        nameLabel.CustomMinimumSize = new Vector2(56f, 0f);
        hbox.AddChild(nameLabel);

        var infoLabel = new Label
        {
            Text = $"{def.Role}  HP {def.BaseHp:0}  ATK {def.BaseAttack:0}",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        infoLabel.AddThemeFontSizeOverride("font_size", 7);
        infoLabel.AddThemeColorOverride("font_color", new Color("#8c9bb0"));
        infoLabel.CustomMinimumSize = new Vector2(150f, 0f);
        hbox.AddChild(infoLabel);

        var stateLabel = new Label { MouseFilter = MouseFilterEnum.Ignore };
        stateLabel.AddThemeFontSizeOverride("font_size", 7);
        stateLabel.AddThemeColorOverride("font_color", new Color("#ffd75e"));
        stateLabel.CustomMinimumSize = new Vector2(58f, 0f);
        hbox.AddChild(stateLabel);

        var actionBtn = MakeButton("", def.Accent, 100f);
        actionBtn.Pressed += () => OnActionButton(def.Id);
        hbox.AddChild(actionBtn);

        panel.SetMeta("op_id", def.Id);
        panel.SetMeta("name_label", nameLabel);
        panel.SetMeta("info_label", infoLabel);
        panel.SetMeta("state_label", stateLabel);
        panel.SetMeta("action_btn", actionBtn);
        return panel;
    }

    void RefreshRow(Control row)
    {
        string id = (string)row.GetMeta("op_id");
        var def = Balance.Instance.GetOperator(id);
        var gs = GameState.Instance;
        var cfg = Balance.Instance.Config;
        bool owned = gs.Owns(id);
        bool inSquad = gs.InSquad(id);

        var nameLabel = (Label)row.GetMeta("name_label");
        var infoLabel = (Label)row.GetMeta("info_label");
        var stateLabel = (Label)row.GetMeta("state_label");
        var btn = (Button)row.GetMeta("action_btn");

        if (owned)
        {
            var p = gs.Progress[id];
            nameLabel.Text = $"{def.DisplayName}  Lv.{p.Level}";
            stateLabel.Text = inSquad ? "DEPLOYED" : "BENCHED";
            stateLabel.AddThemeColorOverride("font_color",
                inSquad ? new Color("#3ecfb2") : new Color("#8c9bb0"));

            if (inSquad)
            {
                btn.Text = "BENCH";
                btn.Disabled = false;
            }
            else
            {
                btn.Text = "DEPLOY";
                btn.Disabled = gs.Squad.Count >= cfg.SquadSize;
            }
        }
        else
        {
            nameLabel.Text = def.DisplayName;
            stateLabel.Text = "";
            bool affordable = gs.Credits >= def.RecruitmentCost;
            btn.Text = $"RECRUIT {def.RecruitmentCost:N0}";
            btn.Disabled = !affordable;
        }

        infoLabel.Text = $"{def.Role}  HP {def.BaseHp:0}  ATK {def.BaseAttack:0}";
    }

    void OnActionButton(string id)
    {
        var gs = GameState.Instance;
        if (!gs.Owns(id))
            gs.TryRecruit(id);
        else
            gs.ToggleSquadMember(id);
        RefreshAll();
    }

    void RefreshAll()
    {
        var gs = GameState.Instance;
        var cfg = Balance.Instance.Config;
        _headerLabel.Text = $"CREDITS {gs.Credits:N0}    DEPLOYED {gs.Squad.Count}/{cfg.SquadSize}";
        _headerLabel.AddThemeColorOverride("font_color", new Color("#ffd75e"));
        foreach (var row in _rows)
            RefreshRow(row);
    }

    void OnModelChanged(long _) => RefreshAll();
    void OnOwnedChanged(string _) => RefreshAll();
    void OnSquadChanged() => RefreshAll();

    public void Open()
    {
        RefreshAll();
        Visible = true;
        GetTree().Paused = true;
    }

    public void Close()
    {
        Visible = false;
        GetTree().Paused = false;
        GameState.Instance.Save();
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
