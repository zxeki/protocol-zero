using Godot;
using ProtocolZero.Combat;
using ProtocolZero.Core;
using ProtocolZero.UI;
using ProtocolZero.World;

namespace ProtocolZero;

public partial class Main : Node2D
{
    CombatManager _combat = null!;
    Hud _hud = null!;

    public override void _Ready()
    {
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.AlwaysOnTop,
            GameState.Instance.Settings.AlwaysOnTop);

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

        _hud.Init(_combat, recruitPanel, armoryPanel);

        if (GameState.Instance.OfflineSummary is { } offline)
            _hud.ShowOfflineSummary(offline);

        GameState.Instance.SquadChanged += OnSquadChanged;

        _combat.StartCombat();
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
