using Godot;
using ProtocolZero.Combat;
using ProtocolZero.Core;
using ProtocolZero.UI;
using ProtocolZero.Util;
using ProtocolZero.World;

namespace ProtocolZero;

public partial class Main : Node2D
{
    CombatManager _combat = null!;
    Hud _hud = null!;
    int _indicatorId = -1;

    public override void _Ready()
    {
        var settings = GameState.Instance.Settings;
        WindowManager.ApplyLayout(settings);

        var battlefield = new Battlefield();
        AddChild(battlefield);

        _combat = new CombatManager();
        AddChild(_combat);
        battlefield.Combat = _combat;
        _combat.SpawnSquad();

        var hudLayer = new CanvasLayer();
        AddChild(hudLayer);

        _hud = new Hud();
        hudLayer.AddChild(_hud);

        var recruitPanel = new RecruitPanel();
        hudLayer.AddChild(recruitPanel);

        var armoryPanel = new ArmoryPanel();
        hudLayer.AddChild(armoryPanel);

        var settingsPanel = new SettingsPanel();
        hudLayer.AddChild(settingsPanel);

        _hud.Init(_combat, recruitPanel, armoryPanel, settingsPanel);
        _hud.MinimizeRequested += OnMinimizeRequested;
        _hud.CloseRequested += OnCloseRequested;

        if (GameState.Instance.OfflineSummary is { } offline)
            _hud.ShowOfflineSummary(offline);

        SetupTray();

        GameState.Instance.SquadChanged += OnSquadChanged;

        _combat.StartCombat();
    }

    void OnCloseRequested() => QuitGame();

    void OnMinimizeRequested()
    {
        if (GameState.Instance.Settings.MinimizeToTray && _indicatorId >= 0)
            GetWindow().Visible = false;
        else
            GetWindow().Mode = Window.ModeEnum.Minimized;
    }

    void SetupTray()
    {
        var icon = Util.Assets.Load("ui/menu_0") ?? PixelArt.BuildStriker();
        _indicatorId = DisplayServer.CreateStatusIndicator(icon, "Protocol Zero",
            Callable.From(RestoreFromTray));

        var menuRid = NativeMenu.CreateMenu();
        NativeMenu.AddItem(menuRid, "Open", Callable.From(RestoreFromTray));
        NativeMenu.AddItem(menuRid, "Quit", Callable.From(QuitGame));
        DisplayServer.StatusIndicatorSetMenu(_indicatorId, menuRid);
    }

    void RestoreFromTray()
    {
        var win = GetWindow();
        win.Visible = true;
        win.Mode = Window.ModeEnum.Windowed;
        DisplayServer.WindowMoveToForeground();
    }

    void QuitGame()
    {
        GameState.Instance.Save();
        if (_indicatorId >= 0)
            DisplayServer.DeleteStatusIndicator(_indicatorId);
        GetTree().Quit();
    }

    void OnSquadChanged()
    {
        _combat.RespawnSquad();
        _hud.RebuildCards();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
            GameState.Instance.Save();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            _combat.HandleWorldClick(GetGlobalMousePosition());
    }
}
