using Godot;
using ProtocolZero.Combat;
using ProtocolZero.Core;
using ProtocolZero.Data;

namespace ProtocolZero.UI;

public partial class Hud : Control
{
    CombatManager _combat = null!;
    RecruitPanel _recruitPanel = null!;
    ArmoryPanel _armoryPanel = null!;
    SettingsPanel _settingsPanel = null!;
    Label _creditsLabel = null!;
    Label _sectorLabel = null!;
    HBoxContainer _cardRow = null!;
    Button _winMinBtn = null!;
    Button _winCloseBtn = null!;
    Button _resumeBtn = null!;
    readonly List<OperatorCard> _cards = new();
    float _refreshTimer;
    bool _dragging;
    Vector2I _dragOffset;

    public event Action? MinimizeRequested;
    public event Action? CloseRequested;

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

    public void Init(CombatManager combat, RecruitPanel recruitPanel, ArmoryPanel armoryPanel, SettingsPanel settingsPanel)
    {
        _combat = combat;
        _recruitPanel = recruitPanel;
        _armoryPanel = armoryPanel;
        _settingsPanel = settingsPanel;
        Build();

        GameState.Instance.CreditsChanged += OnCreditsChanged;
        GameState.Instance.XpChanged += OnXpChanged;
        _combat.WaveChanged += OnWaveChanged;
        _combat.RewardsAwarded += OnRewards;
        _combat.ItemDropped += OnItemDropped;

        OnCreditsChanged(GameState.Instance.Credits);
        OnWaveChanged(_combat.Wave);
    }

    void Build()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        ProcessMode = ProcessModeEnum.Always;

        var dragStrip = new Control
        {
            Position = new Vector2(0f, 0f),
            Size = new Vector2(480f, 18f),
            MouseFilter = MouseFilterEnum.Stop,
        };
        dragStrip.GuiInput += OnDragStripInput;
        AddChild(dragStrip);

        _creditsLabel = new Label { Position = new Vector2(8f, 5f), MouseFilter = MouseFilterEnum.Ignore };
        _creditsLabel.AddThemeFontSizeOverride("font_size", 9);
        _creditsLabel.AddThemeColorOverride("font_color", new Color("#ffd75e"));
        _creditsLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.8f));
        _creditsLabel.AddThemeConstantOverride("outline_size", 2);
        AddChild(_creditsLabel);

        var squadBtn = MakeButton("SQUAD", new Color("#3ecfb2"));
        squadBtn.Position = new Vector2(100f, 3f);
        squadBtn.Pressed += _recruitPanel.Open;
        AddChild(squadBtn);

        var armoryBtn = MakeButton("ARMORY", new Color("#f0a030"));
        armoryBtn.Position = new Vector2(164f, 3f);
        armoryBtn.Pressed += _armoryPanel.Open;
        AddChild(armoryBtn);

        var settingsBtn = MakeButton("SETTINGS", new Color("#4a9eff"));
        settingsBtn.Position = new Vector2(228f, 3f);
        settingsBtn.Pressed += _settingsPanel.Open;
        AddChild(settingsBtn);

        _sectorLabel = new Label
        {
            Position = new Vector2(290f, 5f),
            Size = new Vector2(148f, 12f),
            HorizontalAlignment = HorizontalAlignment.Right,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _sectorLabel.AddThemeFontSizeOverride("font_size", 9);
        _sectorLabel.AddThemeColorOverride("font_color", new Color("#9fb2c8"));
        _sectorLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.8f));
        _sectorLabel.AddThemeConstantOverride("outline_size", 2);
        AddChild(_sectorLabel);

        _winMinBtn = MakeButton("v", new Color("#9fb2c8"));
        _winMinBtn.Position = new Vector2(442f, 3f);
        _winMinBtn.CustomMinimumSize = new Vector2(14f, 14f);
        _winMinBtn.Pressed += () => MinimizeRequested?.Invoke();
        AddChild(_winMinBtn);

        _winCloseBtn = MakeButton("x", new Color("#e04848"));
        _winCloseBtn.Position = new Vector2(458f, 3f);
        _winCloseBtn.CustomMinimumSize = new Vector2(14f, 14f);
        _winCloseBtn.Pressed += () => CloseRequested?.Invoke();
        AddChild(_winCloseBtn);

        _resumeBtn = MakeButton("RESUME", new Color("#3ecfb2"));
        _resumeBtn.Position = new Vector2(406f, 20f);
        _resumeBtn.Visible = false;
        _resumeBtn.Pressed += OnResumePressed;
        AddChild(_resumeBtn);

        _cardRow = new HBoxContainer
        {
            Position = new Vector2(6f, 270f - 66f),
        };
        _cardRow.AddThemeConstantOverride("separation", 6);
        _cardRow.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_cardRow);

        RebuildCards();
    }

    public void RebuildCards()
    {
        foreach (var child in _cardRow.GetChildren())
            child.QueueFree();
        _cards.Clear();

        foreach (var op in _combat.Operators)
        {
            var panel = BuildCard(op);
            _cardRow.AddChild(panel);
        }
    }

    PanelContainer BuildCard(OperatorUnit op)
    {
        var c = new OperatorCard { Unit = op };

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(150f, 60f) };
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

        c.HpBar = MakeBar(new Color("#e04848"), new Color("#2a1518"), 78f);
        vbox.AddChild(c.HpBar);

        c.XpBar = MakeBar(new Color("#4a9eff"), new Color("#141c2a"), 78f);
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

        c.ActiveBtn = MakeButton(op.Def.ActiveLabel, op.Def.Accent);
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

    void OnDragStripInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
        {
            _dragging = true;
            _dragOffset = DisplayServer.MouseGetPosition() - DisplayServer.WindowGetPosition();
        }
        else if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false })
            _dragging = false;
    }

    void OnResumePressed()
    {
        GameState.Instance.ManualPause = false;
        GetTree().Paused = false;
    }

    public void RefreshWindowChrome()
    {
        bool borderless = GameState.Instance.Settings.Borderless;
        _winMinBtn.Visible = borderless;
        _winCloseBtn.Visible = borderless;

        bool panelOpen = _recruitPanel.Visible || _armoryPanel.Visible || _settingsPanel.Visible;
        _resumeBtn.Visible = GetTree().Paused && !panelOpen;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMMouseExit)
            _dragging = false;
    }

    public override void _Process(double delta)
    {
        if (_dragging)
            DisplayServer.WindowSetPosition(DisplayServer.MouseGetPosition() - _dragOffset);

        _refreshTimer -= (float)delta;
        if (_refreshTimer > 0f)
            return;
        _refreshTimer = 0.1f;

        foreach (var c in _cards)
            RefreshCard(c);

        RefreshWindowChrome();
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

        if (u.ActiveCooldownRemaining > 0f)
        {
            c.ActiveBtn.Text = $"{u.ActiveCooldownRemaining:0.0}s";
            c.ActiveBtn.Disabled = true;
        }
        else
        {
            c.ActiveBtn.Text = u.Def.ActiveLabel;
            c.ActiveBtn.Disabled = false;
        }

        if (u.UltStateRemaining > 0f)
        {
            c.UltBtn.Text = $"{u.UltStateRemaining:0.0}s";
            c.UltBtn.Disabled = true;
        }
        else if (u.UltReady)
        {
            c.UltBtn.Text = u.Def.UltLabel;
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
        var cfg = Balance.Instance.Config;
        int sector = (wave - 1) / 5 + 1;
        int node = (wave - 1) % 5 + 1;
        bool boss = cfg.BossWaveInterval > 0 && wave % cfg.BossWaveInterval == 0;
        bool elite = !boss && cfg.EliteWaveInterval > 0 && wave % cfg.EliteWaveInterval == 0;
        string arena = ArenaNames[World.Battlefield.ArenaIndex(wave)];

        _sectorLabel.Text = boss ? $"SECTOR {sector}-{node}  WARDEN · {arena}"
            : elite ? $"SECTOR {sector}-{node}  ELITE · {arena}"
            : $"SECTOR {sector}-{node}  · {arena}";
        _sectorLabel.AddThemeColorOverride("font_color",
            boss ? new Color("#e04848") : elite ? new Color("#f0a030") : new Color("#9fb2c8"));
    }

    static readonly string[] ArenaNames = { "ROOFTOP", "FOUNDRY", "DOCKS" };

    void OnRewards(long credits, float xp)
    {
        SpawnToast($"+{credits:N0} CR   +{Mathf.CeilToInt(xp)} XP", new Color("#ffd75e"), 24f);
    }

    void OnItemDropped(EquipmentDef item)
    {
        var rarity = item.Rarity switch
        {
            "legendary" => new Color("#ffd75e"),
            "epic" => new Color("#c06ae0"),
            "rare" => new Color("#4a9eff"),
            _ => new Color("#9fb2c8"),
        };
        SpawnToast($"LOOT  [{item.Rarity.ToUpperInvariant()}] {item.DisplayName}", rarity, 38f);
    }

    public void ShowOfflineSummary(OfflineSummary summary)
    {
        int totalMinutes = Mathf.Max(1, (int)(summary.Hours * 60f));
        string duration = totalMinutes >= 60
            ? $"{totalMinutes / 60}h {totalMinutes % 60:D2}m"
            : $"{totalMinutes}m";
        SpawnToast($"OFFLINE {duration}  +{summary.Credits:N0} CR  +{Mathf.CeilToInt(summary.Xp)} XP",
            new Color("#3ecfb2"), 56f);
    }

    void SpawnToast(string text, Color color, float y)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(140f, y),
            Size = new Vector2(200f, 14f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", 9);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
        label.AddThemeConstantOverride("outline_size", 3);
        AddChild(label);

        var tw = label.CreateTween();
        tw.TweenProperty(label, "position:y", y - 12f, 0.8f).SetEase(Tween.EaseType.Out);
        tw.Parallel().TweenProperty(label, "modulate:a", 0f, 0.35f).SetDelay(0.7f);
        tw.TweenCallback(Callable.From(label.QueueFree));
    }
}
