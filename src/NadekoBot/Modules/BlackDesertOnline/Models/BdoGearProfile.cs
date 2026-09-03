using System.Text.Json.Serialization;

namespace NadekoBot.Modules.BlackDesertOnline.Models;

public class BdoGearProfile
{
    [JsonPropertyName("userId")]
    public ulong UserId { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("registeredAt")]
    public DateTime RegisteredAt { get; set; }

    [JsonPropertyName("garmothLink")]
    public string? GarmothLink { get; set; }

    [JsonPropertyName("characterName")]
    public string CharacterName { get; set; } = "";

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("className")]
    public string ClassName { get; set; } = "";

    [JsonPropertyName("spec")]
    public string Spec { get; set; } = "succ";

    [JsonPropertyName("buildName")]
    public string BuildName { get; set; } = "Current";

    // Core stats
    [JsonPropertyName("ap")]
    public int Ap { get; set; }

    [JsonPropertyName("aap")]
    public int Aap { get; set; }

    [JsonPropertyName("dp")]
    public int Dp { get; set; }

    [JsonPropertyName("gearScore")]
    public int GearScore { get; set; }

    // Offense - Succession
    [JsonPropertyName("totalAttackAp")]
    public double TotalAttackAp { get; set; }

    [JsonPropertyName("adventureAp")]
    public double AdventureAp { get; set; }

    [JsonPropertyName("monsterAp")]
    public double MonsterAp { get; set; }

    [JsonPropertyName("humanAp")]
    public double HumanAp { get; set; }

    [JsonPropertyName("demihumanAp")]
    public double DemihumanAp { get; set; }

    [JsonPropertyName("kamaAp")]
    public double KamaAp { get; set; }

    [JsonPropertyName("edaniaAp")]
    public double EdaniaAp { get; set; }

    [JsonPropertyName("normalAp")]
    public double NormalAp { get; set; }

    [JsonPropertyName("hiddenAp")]
    public double HiddenAp { get; set; }

    // Offense - Awakening
    [JsonPropertyName("totalAwakeningAp")]
    public double TotalAwakeningAp { get; set; }

    // Defense
    [JsonPropertyName("accuracy")]
    public int Accuracy { get; set; }

    [JsonPropertyName("evasionMelee")]
    public int EvasionMelee { get; set; }

    [JsonPropertyName("evasionRanged")]
    public int EvasionRanged { get; set; }

    [JsonPropertyName("evasionMagic")]
    public int EvasionMagic { get; set; }

    [JsonPropertyName("drMelee")]
    public int DrMelee { get; set; }

    [JsonPropertyName("drRanged")]
    public int DrRanged { get; set; }

    [JsonPropertyName("drMagic")]
    public int DrMagic { get; set; }

    [JsonPropertyName("drRate")]
    public double DrRate { get; set; } = 30.0;

    [JsonPropertyName("maxHp")]
    public int MaxHp { get; set; }

    // Gear slots json
    [JsonPropertyName("gearRaw")]
    public string? GearRaw { get; set; }
}