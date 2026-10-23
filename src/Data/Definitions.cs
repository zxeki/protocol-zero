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
    public string ActiveLabel { get; set; } = "ACTIVE";
    public string UltLabel { get; set; } = "ULT";
    public float ActiveCooldownSec { get; set; } = 8.0f;

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

    public int SquadSize { get; set; } = 3;
    public List<string> DefaultSquad { get; set; } = new() { "torque", "striker", "vex" };
    public long StartingCredits { get; set; }

    public int EnemiesPerWaveBase { get; set; } = 1;
    public int EnemiesPerWaveGrowthDivisor { get; set; } = 3;
    public int EnemiesPerWaveMax { get; set; } = 3;
    public int EliteEscortCount { get; set; } = 2;

    public int ChainEveryAttacks { get; set; } = 3;
    public int ChainTargets { get; set; } = 2;
    public float ChainDamageMult { get; set; } = 0.5f;

    public float PulseDamageMult { get; set; } = 0.7f;
    public float PulseSlowPct { get; set; } = 0.4f;
    public float PulseSlowSec { get; set; } = 3.0f;

    public float BlackoutStunSec { get; set; } = 6.0f;
    public float BlackoutFallbackStunSec { get; set; } = 2.5f;

    public float MarkBonusMult { get; set; } = 1.2f;
    public float ScoutMarkSec { get; set; } = 10.0f;
    public int LockShots { get; set; } = 5;
    public float LockShotIntervalSec { get; set; } = 0.25f;
    public float LockMarkSec { get; set; } = 6.0f;

    public float RepairHealFlat { get; set; } = 12.0f;
    public float RepairHealAttackMult { get; set; } = 1.6f;
    public float SecondWindRevivePct { get; set; } = 0.5f;
    public float SecondWindFallbackPct { get; set; } = 0.25f;

    public float NaniteIntervalSec { get; set; } = 4.0f;
    public float NaniteHealFlat { get; set; } = 8.0f;
    public float NaniteHealAttackMult { get; set; } = 0.8f;

    public float BarrierTakenMult { get; set; } = 0.6f;
    public float BarrierSec { get; set; } = 4.0f;
    public float FortifyTakenMult { get; set; } = 0.8f;
    public float FortressTakenMult { get; set; } = 0.6f;
    public float FortressShieldPct { get; set; } = 0.6f;
    public float FortressSec { get; set; } = 6.0f;
}

public sealed class SaveData
{
    public int Version { get; set; } = 1;
    public long Credits { get; set; }
    public List<string> Owned { get; set; } = new();
    public List<string> Squad { get; set; } = new();
    public Dictionary<string, SavedOperator> Operators { get; set; } = new();
}

public sealed class SavedOperator
{
    public int Level { get; set; } = 1;
    public float Xp { get; set; }
}
