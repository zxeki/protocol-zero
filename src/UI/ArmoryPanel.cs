using Godot;
using ProtocolZero.Core;
using ProtocolZero.Data;

namespace ProtocolZero.UI;

public partial class ArmoryPanel : Control
{
    static readonly (string Slot, string Label)[] Slots =
    {
        ("weapon", "WEAPON"),
        ("armor", "ARMOR"),
        ("implant", "IMPLANT"),
    };

    static readonly Dictionary<string, Color> RarityColors = new()
    {
        ["common"] = new Color("#9fb2c8"),
        ["rare"] = new Color("#4a9eff"),
        ["epic"] = new Color("#c06ae0"),
        ["legendary"] = new Color("#ffd75e"),
    };

    string _selectedOp = "";
    Label _headerLabel = null!;
    HBoxContainer _opRow = null!;
    HBoxContainer _slotRow = null!;
    HBoxContainer _upgradeRow = null!;
    VBoxContainer _inventoryList = null!;
    Label _emptyLabel = null!;
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
            CustomMinimumSize = new Vector2(440f, 250f),
            MouseFilter = MouseFilterEnum.Stop,
        };
        var sb = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.07f, 0.10f, 0.97f),
            BorderColor = new Color("#f0a030"),
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
        root.AddThemeConstantOverride("separation", 4);
        root.MouseFilter = MouseFilterEnum.Ignore;
        panel.AddChild(root);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 8);
        header.MouseFilter = MouseFilterEnum.Ignore;
        root.AddChild(header);

        var title = new Label { Text = "ARMORY", MouseFilter = MouseFilterEnum.Ignore };
        title.AddThemeFontSizeOverride("font_size", 9);
        title.AddThemeColorOverride("font_color", new Color("#f0a030"));
        header.AddChild(title);

        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
        header.AddChild(spacer);

        _headerLabel = new Label { MouseFilter = MouseFilterEnum.Ignore };
        _headerLabel.AddThemeFontSizeOverride("font_size", 8);
        header.AddChild(_headerLabel);

        _closeBtn = MakeButton("CLOSE", new Color("#9fb2c8"), 60f);
        _closeBtn.Pressed += Close;
        header.AddChild(_closeBtn);

        _opRow = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        _opRow.AddThemeConstantOverride("separation", 4);
        root.AddChild(_opRow);

        _slotRow = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        _slotRow.AddThemeConstantOverride("separation", 6);
        root.AddChild(_slotRow);

        _upgradeRow = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        _upgradeRow.AddThemeConstantOverride("separation", 6);
        root.AddChild(_upgradeRow);

        var invHeader = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        invHeader.AddThemeConstantOverride("separation", 6);
        root.AddChild(invHeader);

        var invTitle = new Label { Text = "INVENTORY", MouseFilter = MouseFilterEnum.Ignore };
        invTitle.AddThemeFontSizeOverride("font_size", 7);
        invTitle.AddThemeColorOverride("font_color", new Color("#8c9bb0"));
        invHeader.AddChild(invTitle);

        var invSpacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
        invHeader.AddChild(invSpacer);

        var slotsNote = new Label { Text = "click equipped slot to unequip", MouseFilter = MouseFilterEnum.Ignore };
        slotsNote.AddThemeFontSizeOverride("font_size", 7);
        slotsNote.AddThemeColorOverride("font_color", new Color("#6b7a8d"));
        invHeader.AddChild(slotsNote);

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1f,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            MouseFilter = MouseFilterEnum.Stop,
        };
        root.AddChild(scroll);

        _inventoryList = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        _inventoryList.AddThemeConstantOverride("separation", 2);
        _inventoryList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(_inventoryList);

        _emptyLabel = new Label
        {
            Text = "No loot yet - clear enemies to scavenge equipment.",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _emptyLabel.AddThemeFontSizeOverride("font_size", 7);
        _emptyLabel.AddThemeColorOverride("font_color", new Color("#6b7a8d"));
        _inventoryList.AddChild(_emptyLabel);

        GameState.Instance.CreditsChanged += _ => RefreshAll();
        GameState.Instance.OwnedChanged += _ => RefreshAll();
        GameState.Instance.EquipmentChanged += RefreshAll;
    }

    void BuildOperatorButtons()
    {
        foreach (var child in _opRow.GetChildren())
            child.QueueFree();

        foreach (var op in Balance.Instance.Operators)
        {
            if (!GameState.Instance.Owns(op.Id))
                continue;

            bool selected = op.Id == _selectedOp;
            var btn = MakeButton(op.DisplayName.ToUpperInvariant(),
                selected ? op.Accent : new Color("#6b7a8d"), 44f);
            string id = op.Id;
            btn.Pressed += () =>
            {
                _selectedOp = id;
                RefreshAll();
            };
            _opRow.AddChild(btn);
        }

        _opRow.AddChild(new Control
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        });

        for (int i = 0; i < 4; i++)
        {
            var icon = Util.Assets.Load($"operators/{_selectedOp}/ability_{i}");
            if (icon == null)
                continue;
            _opRow.AddChild(new TextureRect
            {
                Texture = icon,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterEnum.Ignore,
            });
        }
    }

    void BuildSlotCards()
    {
        foreach (var child in _slotRow.GetChildren())
            child.QueueFree();

        var gs = GameState.Instance;
        foreach (var (slot, label) in Slots)
        {
            string? itemId = gs.Equipped(_selectedOp, slot);
            var item = itemId != null ? Balance.Instance.Equipment.GetValueOrDefault(itemId) : null;

            var card = new PanelContainer { CustomMinimumSize = new Vector2(137f, 56f) };
            var sb = new StyleBoxFlat
            {
                BgColor = new Color(0.07f, 0.09f, 0.12f, 0.9f),
                BorderColor = item != null ? RarityColor(item.Rarity) : new Color(0.3f, 0.35f, 0.4f, 0.5f),
            };
            sb.SetBorderWidthAll(1);
            sb.SetCornerRadiusAll(2);
            card.AddThemeStyleboxOverride("panel", sb);

            var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            col.AddThemeConstantOverride("separation", 1);
            col.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            card.AddChild(col);

            if (item != null)
            {
                var icon = Util.Assets.Load($"{item.Slot}s/{item.Id}");
                if (icon != null)
                {
                    col.AddChild(new TextureRect
                    {
                        Texture = icon,
                        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                        SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                        MouseFilter = MouseFilterEnum.Ignore,
                    });
                }
            }

            var slotLabel = new Label { Text = label, MouseFilter = MouseFilterEnum.Ignore };
            slotLabel.AddThemeFontSizeOverride("font_size", 6);
            slotLabel.AddThemeColorOverride("font_color", new Color("#6b7a8d"));
            col.AddChild(slotLabel);

            if (item != null)
            {
                var itemLabel = new Label
                {
                    Text = $"{item.DisplayName}  {StatText(item)}",
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                itemLabel.AddThemeFontSizeOverride("font_size", 7);
                itemLabel.AddThemeColorOverride("font_color", RarityColor(item.Rarity));
                col.AddChild(itemLabel);

                string s = slot;
                card.GuiInput += @event => OnSlotClicked(@event, s);
                card.MouseFilter = MouseFilterEnum.Stop;
            }
            else
            {
                var empty = new Label { Text = "- empty -", MouseFilter = MouseFilterEnum.Ignore };
                empty.AddThemeFontSizeOverride("font_size", 7);
                empty.AddThemeColorOverride("font_color", new Color("#4a5566"));
                col.AddChild(empty);
            }

            _slotRow.AddChild(card);
        }
    }

    void OnSlotClicked(InputEvent @event, string slot)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            GameState.Instance.Unequip(_selectedOp, slot);
            RefreshAll();
        }
    }

    void BuildUpgradeButtons()
    {
        foreach (var child in _upgradeRow.GetChildren())
            child.QueueFree();

        var gs = GameState.Instance;
        var cfg = Balance.Instance.Config;
        var up = gs.UpgradesFor(_selectedOp);

        _upgradeRow.AddChild(MakeUpgradeButton(
            $"POWER  Lv.{up.PowerRank}/{cfg.UpgradeMaxRank}  +{up.PowerRank * cfg.UpgradeStatPerRankPct * 100f:0}% ATK/HP",
            gs.UpgradeCost(up.PowerRank),
            up.PowerRank >= cfg.UpgradeMaxRank,
            () => gs.TryUpgrade(_selectedOp, tech: false)));

        _upgradeRow.AddChild(MakeUpgradeButton(
            $"TECH  Lv.{up.TechRank}/{cfg.UpgradeMaxRank}  -{up.TechRank * cfg.UpgradeTechCooldownPct * 100f:0}% CD",
            gs.UpgradeCost(up.TechRank),
            up.TechRank >= cfg.UpgradeMaxRank,
            () => gs.TryUpgrade(_selectedOp, tech: true)));
    }

    Button MakeUpgradeButton(string text, long cost, bool maxed, Action buy)
    {
        var btn = MakeButton(maxed ? $"{text}  MAX" : $"{text}  {cost:N0} CR", new Color("#ffd75e"), 206f);
        btn.Disabled = maxed || GameState.Instance.Credits < cost;
        btn.Pressed += () =>
        {
            buy();
            RefreshAll();
        };
        return btn;
    }

    void BuildInventoryRows()
    {
        foreach (var child in _inventoryList.GetChildren())
            child.QueueFree();

        var gs = GameState.Instance;
        var rows = gs.Inventory
            .Where(kv => Balance.Instance.Equipment.ContainsKey(kv.Key) && kv.Value > 0)
            .OrderByDescending(kv => RarityIndex(Balance.Instance.Equipment[kv.Key].Rarity))
            .ThenBy(kv => Balance.Instance.Equipment[kv.Key].DisplayName)
            .ToList();

        _emptyLabel = new Label
        {
            Text = "No loot yet - clear enemies to scavenge equipment.",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _emptyLabel.AddThemeFontSizeOverride("font_size", 7);
        _emptyLabel.AddThemeColorOverride("font_color", new Color("#6b7a8d"));
        _inventoryList.AddChild(_emptyLabel);
        _emptyLabel.Visible = rows.Count == 0;

        foreach (var (itemId, count) in rows)
        {
            var item = Balance.Instance.Equipment[itemId];

            var row = new PanelContainer { CustomMinimumSize = new Vector2(424f, 20f) };
            var sb = new StyleBoxFlat
            {
                BgColor = new Color(0.07f, 0.09f, 0.12f, 0.85f),
                BorderColor = RarityColor(item.Rarity),
            };
            sb.SetBorderWidthAll(1);
            sb.SetCornerRadiusAll(2);
            row.AddThemeStyleboxOverride("panel", sb);
            row.MouseFilter = MouseFilterEnum.Ignore;

            var hbox = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            hbox.AddThemeConstantOverride("separation", 6);
            hbox.AddThemeConstantOverride("margin_left", 4);
            row.AddChild(hbox);

            var name = new Label
            {
                Text = $"{item.DisplayName}{(count > 1 ? $" x{count}" : "")}",
                MouseFilter = MouseFilterEnum.Ignore,
                CustomMinimumSize = new Vector2(116f, 0f),
            };
            name.AddThemeFontSizeOverride("font_size", 7);
            name.AddThemeColorOverride("font_color", RarityColor(item.Rarity));
            hbox.AddChild(name);

            var slotLabel = new Label
            {
                Text = item.Slot.ToUpperInvariant(),
                MouseFilter = MouseFilterEnum.Ignore,
                CustomMinimumSize = new Vector2(52f, 0f),
            };
            slotLabel.AddThemeFontSizeOverride("font_size", 6);
            slotLabel.AddThemeColorOverride("font_color", new Color("#6b7a8d"));
            hbox.AddChild(slotLabel);

            var stats = new Label
            {
                Text = StatText(item),
                MouseFilter = MouseFilterEnum.Ignore,
                CustomMinimumSize = new Vector2(96f, 0f),
            };
            stats.AddThemeFontSizeOverride("font_size", 7);
            stats.AddThemeColorOverride("font_color", new Color("#8c9bb0"));
            hbox.AddChild(stats);

            var sp = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
            hbox.AddChild(sp);

            var equipBtn = MakeButton("EQUIP", new Color("#3ecfb2"), 56f);
            equipBtn.Disabled = string.IsNullOrEmpty(_selectedOp);
            string id = itemId;
            equipBtn.Pressed += () =>
            {
                GameState.Instance.Equip(_selectedOp, id);
                RefreshAll();
            };
            hbox.AddChild(equipBtn);

            _inventoryList.AddChild(row);
        }
    }

    public void RefreshAll()
    {
        if (!Visible)
            return;

        var gs = GameState.Instance;
        var owned = Balance.Instance.Operators.Where(o => gs.Owns(o.Id)).ToList();
        if (owned.Count == 0)
            return;

        if (string.IsNullOrEmpty(_selectedOp) || !gs.Owns(_selectedOp))
            _selectedOp = owned[0].Id;

        _headerLabel.Text = $"CREDITS {gs.Credits:N0}";
        _headerLabel.AddThemeColorOverride("font_color", new Color("#ffd75e"));

        BuildOperatorButtons();
        BuildSlotCards();
        BuildUpgradeButtons();
        BuildInventoryRows();
    }

    public void Open()
    {
        Visible = true;
        RefreshAll();
        GetTree().Paused = true;
    }

    public void Close()
    {
        Visible = false;
        GetTree().Paused = false;
        GameState.Instance.Save();
    }

    static Color RarityColor(string rarity) =>
        RarityColors.GetValueOrDefault(rarity, new Color("#9fb2c8"));

    static int RarityIndex(string rarity) => rarity switch
    {
        "legendary" => 3,
        "epic" => 2,
        "rare" => 1,
        _ => 0,
    };

    static string StatText(EquipmentDef item)
    {
        var parts = new List<string>();
        if (item.Attack > 0f)
            parts.Add($"+{item.Attack:0} ATK");
        if (item.Hp > 0f)
            parts.Add($"+{item.Hp:0} HP");
        return string.Join(" ", parts);
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
