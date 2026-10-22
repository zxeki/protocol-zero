using Godot;
using ProtocolZero.Combat;
using ProtocolZero.UI;
using ProtocolZero.World;

namespace ProtocolZero;

public partial class Main : Node2D
{
    CombatManager _combat = null!;

    public override void _Ready()
    {
        var battlefield = new Battlefield();
        AddChild(battlefield);

        _combat = new CombatManager();
        AddChild(_combat);
        _combat.SpawnSquad();

        var hudLayer = new CanvasLayer();
        AddChild(hudLayer);
        var hud = new Hud();
        hudLayer.AddChild(hud);
        hud.Init(_combat);

        _combat.StartCombat();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            _combat.HandleWorldClick(GetGlobalMousePosition());
    }
}
