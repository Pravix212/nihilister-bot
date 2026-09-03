using System.Text.Json;
using NadekoBot.Modules.BlackDesertOnline.Models;
using NadekoBot.Services;

namespace NadekoBot.Modules.BlackDesertOnline;

public class BdoGearService : INService
{
    private readonly string _profilesPath = Path.Combine("data", "bdo_profiles.json");
    private Dictionary<ulong, BdoGearProfile> _profiles = new();
    private readonly JsonSerializerOptions _jsonOpts = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public static readonly List<BdoGrindZone> GrindZones = new()
    {
        new() { Name = "Olucas Farm",              MinAp = 260, MinDp = 260, SilverPerHour = "~300M",  Notes = "Beginner zone" },
        new() { Name = "Aakman Temple",             MinAp = 280, MinDp = 300, SilverPerHour = "~400M",  Notes = "Mid-tier" },
        new() { Name = "Gyfin Rhasia (Surface)",    MinAp = 300, MinDp = 320, SilverPerHour = "~500M",  Notes = "Party recommended" },
        new() { Name = "Kratuga Ancient Ruins",     MinAp = 310, MinDp = 340, SilverPerHour = "~600M",  Notes = "Efficient solo" },
        new() { Name = "Ash Forest",                MinAp = 320, MinDp = 360, SilverPerHour = "~700M",  Notes = "Good drops" },
        new() { Name = "Thornwood Forest",          MinAp = 330, MinDp = 370, SilverPerHour = "~800M",  Notes = "Elvia-based" },
        new() { Name = "Hystria Ruins",             MinAp = 340, MinDp = 380, SilverPerHour = "~900M",  Notes = "Great crystals" },
        new() { Name = "Star's End",                MinAp = 360, MinDp = 390, SilverPerHour = "~1.1B",  Notes = "High yield" },
        new() { Name = "Sycraia Underwater Ruins",  MinAp = 370, MinDp = 400, SilverPerHour = "~1.3B",  Notes = "Top tier" },
        new() { Name = "Gyfin Rhasia (Underground)",MinAp = 380, MinDp = 410, SilverPerHour = "~1.5B",  Notes = "Best party zone" },
        new() { Name = "Mountain of Eternal Winter", MinAp = 390, MinDp = 420, SilverPerHour = "~1.7B", Notes = "Elvia top-tier" },
        new() { Name = "Dehkia's Pit",              MinAp = 400, MinDp = 430, SilverPerHour = "~2B+",   Notes = "Endgame" },
    };

    // PVP Caps
    public static readonly Dictionary<string, Dictionary<string, Dictionary<string, int>>> PvpCaps = new()
    {
        ["Nodewar"] = new()
        {
            ["Tier 1"] = new()
            {
                ["Total AP"] = 680,
                ["Total AAP"] = 680,
                ["Evasion"] = 908,
                ["Damage Reduction"] = 530,
                ["Accuracy"] = 820,
                ["Max HP"] = 11000,
            },
            ["Tier 2"] = new()
            {
                ["Total AP"] = 750,
                ["Total AAP"] = 750,
                ["Evasion"] = 1050,
                ["Damage Reduction"] = 600,
                ["Accuracy"] = 900,
                ["Max HP"] = 13000,
            },
        },
        ["Siege"] = new()
        {
            ["Tier 1"] = new()
            {
                ["Total AP"] = 750,
                ["Total AAP"] = 750,
                ["Evasion"] = 1000,
                ["Damage Reduction"] = 580,
                ["Accuracy"] = 870,
                ["Max HP"] = 12000,
            },
            ["Tier 2"] = new()
            {
                ["Total AP"] = 820,
                ["Total AAP"] = 820,
                ["Evasion"] = 1100,
                ["Damage Reduction"] = 650,
                ["Accuracy"] = 950,
                ["Max HP"] = 14000,
            },
        },
    };

    public BdoGearService()
    {
        LoadProfiles();
    }

    private void LoadProfiles()
    {
        try
        {
            if (File.Exists(_profilesPath))
            {
                var json = File.ReadAllText(_profilesPath);
                _profiles = JsonSerializer.Deserialize<Dictionary<ulong, BdoGearProfile>>(json, _jsonOpts)
                            ?? new();
            }
        }
        catch { _profiles = new(); }
    }

    public void SaveProfiles()
    {
        Directory.CreateDirectory("data");
        File.WriteAllText(_profilesPath, JsonSerializer.Serialize(_profiles, _jsonOpts));
    }

    public BdoGearProfile? GetProfile(ulong userId)
        => _profiles.TryGetValue(userId, out var p) ? p : null;

    public void SaveProfile(BdoGearProfile profile)
    {
        _profiles[profile.UserId] = profile;
        SaveProfiles();
    }

    public void DeleteProfile(ulong userId)
    {
        _profiles.Remove(userId);
        SaveProfiles();
    }

    public BdoGrindZone? GetRecommendedZone(int ap, int dp)
        => GrindZones
            .Where(z => ap >= z.MinAp && dp >= z.MinDp)
            .OrderByDescending(z => z.MinAp)
            .FirstOrDefault();
}