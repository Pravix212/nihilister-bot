using System.Text.Json;
using NadekoBot.Modules.BlackDesertOnline.Models;
using NadekoBot.Services;

namespace NadekoBot.Modules.BlackDesertOnline;

public class BdoMarketService : INService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static readonly Dictionary<string, string> RegionNames = new()
    {
        ["na"] = "NA (North America)",
        ["eu"] = "EU (Europe)",
        ["sea"] = "SEA (Southeast Asia)",
        ["mena"] = "MENA (Middle East)",
        ["kr"] = "KR (Korea)",
        ["jp"] = "JP (Japan)",
        ["sa"] = "SA (South America)",
    };

    public BdoMarketService(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    public async Task<List<ArshiaSearchResult>?> SearchItemsAsync(string region, string query)
    {
        try
        {
            using var http = _httpFactory.CreateClient();
            var url = $"https://api.arsha.io/v2/{region}/GetWorldMarketSearchList?searchText={Uri.EscapeDataString(query)}&lang=en";
            var resp = await http.GetStringAsync(url);
            var results = JsonSerializer.Deserialize<List<ArshiaSearchResult>>(resp, _jsonOptions);
            return results;
        }
        catch
        {
            return null;
        }
    }

    public async Task<ArshiaMarketItem?> GetItemAsync(string region, long id, int sid = 0)
    {
        try
        {
            using var http = _httpFactory.CreateClient();
            var url = $"https://api.arsha.io/v2/{region}/item?id={id}&sid={sid}&lang=en";
            var resp = await http.GetStringAsync(url);
            var item = JsonSerializer.Deserialize<ArshiaMarketItem>(resp, _jsonOptions);
            return item;
        }
        catch
        {
            return null;
        }
    }

    public string FormatSilver(long amount)
    {
        if (amount == 0) return "N/A";
        if (amount >= 1_000_000_000_000)
            return $"{amount / 1_000_000_000_000.0:F2}T";
        if (amount >= 1_000_000_000)
            return $"{amount / 1_000_000_000.0:F2}B";
        if (amount >= 1_000_000)
            return $"{amount / 1_000_000.0:F1}M";
        return $"{amount:N0}";
    }

    public string FormatLastSoldTime(long unixTime)
    {
        if (unixTime <= 0) return "Never";
        var dt = DateTimeOffset.FromUnixTimeSeconds(unixTime);
        var diff = DateTimeOffset.UtcNow - dt;
        if (diff.TotalMinutes < 1) return "Just now";
        if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalDays < 1) return $"{(int)diff.TotalHours}h ago";
        return $"{(int)diff.TotalDays}d ago";
    }
}