using Godot;
using ProtocolZero.Combat;
using ProtocolZero.Core;

namespace ProtocolZero.UI;

public partial class Hud : Control
{
    CombatManager _combat = null!;
    Label _creditsLabel = null!;
    Label _sectorLabel = null!;
    readonly List<OperatorCard> _cards = new();
    float _refreshTimer;

    sealed class OperatorCard
    {
        public OperatorUnit Unit = null!;
        public Label NameLabel = null!;
        public Label StatsLabel = null!;
        public ProgressBar HpBar = null!;
        public ProgressBar XpBar = null!;
        public Button ActiveBtn = null!;
        public Button UltBtn = null!;
    }

    public void Init(CombatManager combat)
    {
        _combat = combat;
        Build();

        GameState.Instance.CreditsChanged += OnCreditsChanged;
        GameState.Instance.XpChanged += OnXpChanged;
        _combat.WaveChanged += OnWaveChanged;
        _combat.RewardsAwarded += OnRewards;

        OnCreditsChanged(GameState.Instance.Credits);
        OnWaveChanged(_combat.Wave);
    }

    void Build()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        _creditsLabel = new Label { Position = new Vector2(8f, 5f), MouseFilter = MouseFilterEnum.Ignore };
        _creditsLabel.AddThemeFontSizeOverride("font_size", 9);
        _creditsLabel.AddThemeColorOverride("font_color", new Color("#ffd75e"));
        _creditsLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.8f));
        _creditsLabel.AddThemeConstantOverride("outline_size", 2);
        AddChild(_creditsLabel);

        _sectorLabel = new Label
        {
            Position = new Vector2(280f, 5f),
            Size = new Vector2(192f, 12f),
            HorizontalAlignment = HorizontalAlignment.Right,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _sectorLabel.AddThemeFontSizeOverride("font_size", 9);
        _sectorLabel.AddThemeColorOverride("font_color", new Color("#9fb2c8"));
        _sectorLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.8f));
        _sectorLabel.AddThemeConstantOverride("outline_size", 2);
        AddChild(_sectorLabel);

        float x = 6f;
        foreach (var op in _combat.Operators)
        {
            var panel = BuildCard(op);
            panel.Position = new Vector2(x, 270f - 66f);
            AddChild(panel);
            x += 164f;
        }
    }

    PanelContainer BuildCard(OperatorUnit op)
    {
        var c = new OperatorCard { Unit = op };

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(158f, 60f) };
        var sb = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.08f, 0.11f, 0.85f),
            BorderColor = new Color(op.Def.Accent, 0.55f),
        };
        sb.SetBorderWidthAll(1);
        sb.SetCornerRadiusAll(2);
        panel.AddThemeStyleboxOverride("panel", sb);
        panel.MouseFilter = MouseFilterEnum.Ignore;

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 5);
        hbox.MouseFilter = MouseFilterEnum.Ignore;
        panel.AddChild(hbox);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 2);
        vbox.MouseFilter = MouseFilterEnum.Ignore;
        hbox.AddChild(vbox);

        c.NameLabel = new Label { Text = $"{op.Def.DisplayName}  Lv.{op.Level}", MouseFilter = MouseFilterEnum.Ignore };
        c.NameLabel.AddThemeFontSizeOverride("font_size", 8);
        c.NameLabel.AddThemeColorOverride("font_color", op.Def.Accent);
        vbox.AddChild(c.NameLabel);

        c.HpBar = MakeBar(new Color("#e04848"), new Color("#2a1518"), 88f);
        vbox.AddChild(c.HpBar);

        c.XpBar = MakeBar(new Color("#4a9eff"), new Color("#141c2a"), 88f);
        vbox.AddChild(c.XpBar);

        c.StatsLabel = new Label { MouseFilter = MouseFilterEnum.Ignore };
        c.StatsLabel.AddThemeFontSizeOverride("font_size", 7);
        c.StatsLabel.AddThemeColorOverride("font_color", new Color("#8c9bb0"));
        vbox.AddChild(c.StatsLabel);

        var btnBox = new VBoxContainer();
        btnBox.AddThemeConstantOverride("separation", 3);
        btnBox.Alignment = BoxContainer.AlignmentMode.Center;
        btnBox.MouseFilter = MouseFilterEnum.Ignore;
        hbox.AddChild(btnBox);

        c.ActiveBtn = MakeButton("BURST", op.Def.Accent);
        c.ActiveBtn.Pressed += op.ActivateActive;
        btnBox.AddChild(c.ActiveBtn);

        c.UltBtn = MakeButton("CHARGE 0%", new Color("#ffd75e"));
        c.UltBtn.Pressed += op.ActivateUltimate;
        btnBox.AddChild(c.UltBtn);

        op.StateChanged += () => RefreshCard(c);
        _cards.Add(c);
        return panel;
    }

    static ProgressBar MakeBar(Color fill, Color bg, float width)
    {
        var bar = new ProgressBar
        {
            CustomMinimumSize = new Vector2(width, 5f),
            MaxValue = 100,
            Value = 100,
            ShowPercentage = false,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        var bgSb = new StyleBoxFlat { BgColor = bg };
        bgSb.SetCornerRadiusAll(1);
        var fillSb = new StyleBoxFlat { BgColor = fill };
        fillSb.SetCornerRadiusAll(1);
        bar.AddThemeStyleboxOverride("background", bgSb);
        bar.AddThemeStyleboxOverride("fill", fillSb);
        return bar;
    }

    static Button MakeButton(string text, Color accent)
    {
        var b = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(58f, 17f),
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

    public override void _Process(double delta)
    {
        _refreshTimer -= (float)delta;
        if (_refreshTimer > 0f)
            return;
        _refreshTimer = 0.1f;

        foreach (var c in _cards)
            RefreshCard(c);
    }

    void RefreshCard(OperatorCard c)
    {
        var u = c.Unit;
        if (!GodotObject.IsInstanceValid(u))
            return;

        c.HpBar.Value = u.HpFraction * 100.0;
        c.NameLabel.Text = $"{u.Def.DisplayName}  Lv.{u.Level}";

        var p = GameState.Instance.Progress[u.Def.Id];
        int need = Balance.Instance.XpToNext(p.Level);
        c.XpBar.Value = need <= 0 ? 0 : p.Xp / need * 100.0;
        c.StatsLabel.Text = $"HP {Mathf.CeilToInt(u.Hp)}/{Mathf.CeilToInt(u.MaxHp)}";

        if (!u.IsAlive)
        {
            c.ActiveBtn.Text = "DOWN";
            c.ActiveBtn.Disabled = true;
            c.UltBtn.Text = "DOWN";
            c.UltBtn.Disabled = true;
            return;
        }

        if (u.BurstCooldownRemaining > 0f)
        {
            c.ActiveBtn.Text = $"BURST {u.BurstCooldownRemaining:0.0}";
            c.ActiveBtn.Disabled = true;
        }
        else
        {
            c.ActiveBtn.Text = "BURST";
            c.ActiveBtn.Disabled = false;
        }

        if (u.OverdriveRemaining > 0f)
        {
            c.UltBtn.Text = $"OVER x{u.OverdriveRemaining:0.0}";
            c.UltBtn.Disabled = true;
        }
        else if (u.UltReady)
        {
            c.UltBtn.Text = "OVERDRIVE!";
            c.UltBtn.Disabled = false;
        }
        else
        {
            c.UltBtn.Text = $"CHARGE {u.UltCharge01 * 100f:0}%";
            c.UltBtn.Disabled = true;
        }
    }

    void OnCreditsChanged(long credits) => _creditsLabel.Text = $"CREDITS {credits:N0}";

    void OnXpChanged(string id, GameState.OperatorProgress progress)
    {
        foreach (var c in _cards)
            RefreshCard(c);
    }

    void OnWaveChanged(int wave)
    {
        int sector = (wave - 1) / 5 + 1;
        int node = (wave - 1) % 5 + 1;
        _sectorLabel.Text = $"SECTOR {sector}-{node}";
    }

    void OnRewards(long credits, float xp)
    {
        var label = new Label
        {
            Text = $"+{credits:N0} CR   +{Mathf.CeilToInt(xp)} XP",
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(140f, 24f),
            Size = new Vector2(200f, 14f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", 9);
        label.AddThemeColorOverride("font_color", new Color("#ffd75e"));
        label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
        label.AddThemeConstantOverride("outline_size", 3);
        AddChild(label);

        var tw = label.CreateTween();
        tw.TweenProperty(label, "position:y", 12f, 0.8f).SetEase(Tween.EaseType.Out);
        tw.Parallel().TweenProperty(label, "modulate:a", 0f, 0.35f).SetDelay(0.45f);
        tw.TweenCallback(Callable.From(label.QueueFree));
    }
}
