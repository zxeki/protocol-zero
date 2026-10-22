using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace ProtocolZero.Data;

public static class Json
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };
}

public sealed class OperatorDef
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = "";
    public string AccentColor { get; set; } = "#ffffff";
    public float BaseHp { get; set; }
    public float BaseAttack { get; set; }
    public float AttackInterval { get; set; }
    public long RecruitmentCost { get; set; }
    public string PassiveId { get; set; } = "";
    public string ActiveId { get; set; } = "";
    public string UltimateId { get; set; } = "";

    [JsonIgnore]
    public Color Accent => new(AccentColor);
}

public sealed class EnemyDef
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public float BaseHp { get; set; }
    public float BaseAttack { get; set; }
    public float AttackInterval { get; set; }
    public float AttackRange { get; set; }
    public float MoveSpeed { get; set; }
    public int CreditsMin { get; set; }
    public int CreditsMax { get; set; }
    public int XpMin { get; set; }
    public int XpMax { get; set; }
}

public sealed class BalanceConfig
{
    public int LevelCap { get; set; } = 50;
    public float XpBase { get; set; } = 40;
    public float XpGrowth { get; set; } = 1.18f;
    public float HpPerLevelPct { get; set; } = 0.12f;
    public float AttackPerLevelPct { get; set; } = 0.1f;
    public float EnemyHpPerWavePct { get; set; } = 0.14f;
    public float EnemyAttackPerWavePct { get; set; } = 0.1f;
    public float CreditsScalingPerWavePct { get; set; } = 0.03f;
    public float RespawnDelaySec { get; set; } = 1.6f;
    public float OperatorReviveSec { get; set; } = 8.0f;
    public float ExecutionThresholdPct { get; set; } = 0.3f;
    public float ExecutionBonusMult { get; set; } = 1.5f;
    public float RapidBurstCooldownSec { get; set; } = 8.0f;
    public int RapidBurstShots { get; set; } = 3;
    public float RapidBurstShotIntervalSec { get; set; } = 0.18f;
    public float OverdriveDurationSec { get; set; } = 6.0f;
    public float UltimateChargePerAttack { get; set; } = 0.05f;
    public int EliteWaveInterval { get; set; } = 5;
    public float ProjectileSpeed { get; set; } = 260.0f;
}
