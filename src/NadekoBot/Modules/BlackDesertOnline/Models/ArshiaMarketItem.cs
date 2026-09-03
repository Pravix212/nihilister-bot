using System.Text.Json.Serialization;

namespace NadekoBot.Modules.BlackDesertOnline.Models;

public class ArshiaMarketItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("sid")]
    public int Sid { get; set; }

    [JsonPropertyName("minEnhance")]
    public int MinEnhance { get; set; }

    [JsonPropertyName("maxEnhance")]
    public int MaxEnhance { get; set; }

    [JsonPropertyName("basePrice")]
    public long BasePrice { get; set; }

    [JsonPropertyName("currentStock")]
    public long CurrentStock { get; set; }

    [JsonPropertyName("totalTrades")]
    public long TotalTrades { get; set; }

    [JsonPropertyName("priceMin")]
    public long PriceMin { get; set; }

    [JsonPropertyName("priceMax")]
    public long PriceMax { get; set; }

    [JsonPropertyName("lastSoldPrice")]
    public long LastSoldPrice { get; set; }

    [JsonPropertyName("lastSoldTime")]
    public long LastSoldTime { get; set; }
}

public class ArshiaSearchResult
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("sid")]
    public int Sid { get; set; }
}