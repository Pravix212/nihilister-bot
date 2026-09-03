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
    public int TotalAttackAp { get; set; }

    [JsonPropertyName("monsterAp")]
    public int MonsterAp { get; set; }

    [JsonPropertyName("humanAp")]
    public int HumanAp { get; set; }

    [JsonPropertyName("demihumanAp")]
    public int DemihumanAp { get; set; }

    [JsonPropertyName("hiddenAp")]
    public int HiddenAp { get; set; }

    // Offense - Awakening
    [JsonPropertyName("totalAwakeningAp")]
    public int TotalAwakeningAp { get; set; }

    [JsonPropertyName("monsterAap")]
    public int MonsterAap { get; set; }

    [JsonPropertyName("humanAap")]
    public int HumanAap { get; set; }

    [JsonPropertyName("demihumanAap")]
    public int DemihumanAap { get; set; }

    // Defense
    [JsonPropertyName("evasion")]
    public int Evasion { get; set; }

    [JsonPropertyName("damageReduction")]
    public int DamageReduction { get; set; }

    [JsonPropertyName("accuracy")]
    public int Accuracy { get; set; }

    [JsonPropertyName("maxHp")]
    public int MaxHp { get; set; }
}