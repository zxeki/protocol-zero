using System.Text.Json;
using Godot;
using ProtocolZero.Data;
using FileAccess = Godot.FileAccess;

namespace ProtocolZero.Core;

public partial class Balance : Node
{
    public static Balance Instance { get; private set; } = null!;

    public BalanceConfig Config { get; private set; } = new();
    public List<OperatorDef> Operators { get; } = new();
    public Dictionary<string, EnemyDef> Enemies { get; } = new();
    public Dictionary<string, EquipmentDef> Equipment { get; } = new();

    public override void _Ready()
    {
        Instance = this;

        Config = Load<BalanceConfig>("res://data/balance.json") ?? new BalanceConfig();
        foreach (var op in Load<List<OperatorDef>>("res://data/operators.json") ?? [])
            Operators.Add(op);
        foreach (var enemy in Load<List<EnemyDef>>("res://data/enemies.json") ?? [])
            Enemies[enemy.Id] = enemy;
        foreach (var item in Load<List<EquipmentDef>>("res://data/equipment.json") ?? [])
            Equipment[item.Id] = item;

        GD.Print($"[Balance] {Operators.Count} operators, {Enemies.Count} enemy types, {Equipment.Count} equipment loaded");
    }

    public OperatorDef GetOperator(string id) =>
        Operators.FirstOrDefault(o => o.Id == id)
        ?? throw new InvalidOperationException($"Unknown operator '{id}'");

    public int XpToNext(int level) =>
        (int)MathF.Round(Config.XpBase * MathF.Pow(Config.XpGrowth, level - 1));

    static T? Load<T>(string path) where T : class
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"[Balance] Failed to open {path}");
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(file.GetAsText(), Data.Json.Options);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[Balance] Failed to parse {path}: {e.Message}");
            return null;
        }
    }
}
