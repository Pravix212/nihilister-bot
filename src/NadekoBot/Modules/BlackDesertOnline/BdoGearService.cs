using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using NadekoBot.Modules.BlackDesertOnline.Models;
using NadekoBot.Services;

namespace NadekoBot.Modules.BlackDesertOnline;

public class BdoGearService : INService
{
    private readonly string _profilesPath = Path.Combine("data", "bdo_profiles.json");
    private readonly string _imagesDir = Path.Combine("data", "bdo_gear_images");
    private Dictionary<ulong, BdoGearProfile> _profiles = new();
    private readonly JsonSerializerOptions _jsonOpts = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public static readonly List<BdoGrindZone> GrindZones = new()
    {
        new() { Name = "Olucas Farm",               MinAp = 260, MinDp = 260, SilverPerHour = "~300M - 400M", Notes = "Starter Zone · Easy entry" },
        new() { Name = "Aakman Temple",              MinAp = 280, MinDp = 300, SilverPerHour = "~450M - 600M", Notes = "Mid-tier · Good trash value" },
        new() { Name = "Gyfin Rhasia (Surface)",     MinAp = 300, MinDp = 320, SilverPerHour = "~600M - 750M", Notes = "Party recommended · Exp focus" },
        new() { Name = "Kratuga Ancient Ruins",      MinAp = 310, MinDp = 340, SilverPerHour = "~700M - 850M", Notes = "Efficient solo · Elkarr crystals" },
        new() { Name = "Ash Forest",                MinAp = 320, MinDp = 360, SilverPerHour = "~800M - 950M", Notes = "Deboreka Necklace drops" },
        new() { Name = "Thornwood Forest (Elvia)",   MinAp = 330, MinDp = 370, SilverPerHour = "~900M - 1.1B", Notes = "Elvia Calpheon · Despair drops" },
        new() { Name = "Hystria Ruins",              MinAp = 340, MinDp = 380, SilverPerHour = "~1.0B - 1.2B", Notes = "Great artifacts & accessories" },
        new() { Name = "Star's End",                 MinAp = 340, MinDp = 390, SilverPerHour = "~1.1B - 1.3B", Notes = "Distortion Earrings · High RNG yield" },
        new() { Name = "Sycraia Underwater (Abyssal)",MinAp = 350, MinDp = 400, SilverPerHour = "~1.3B - 1.5B", Notes = "Tungrad Rings · Consistent silver" },
        new() { Name = "Gyfin Rhasia (Underground)", MinAp = 360, MinDp = 410, SilverPerHour = "~1.5B - 1.7B", Notes = "Top solo tier · High Caphras & accessories" },
        new() { Name = "Mountain of Eternal Winter",  MinAp = 370, MinDp = 420, SilverPerHour = "~1.7B - 2.0B", Notes = "Flame of Frost · Top endgame zone" },
        new() { Name = "Dehkia's Lantern (Hystria/Aakman)", MinAp = 380, MinDp = 430, SilverPerHour = "~2.0B - 2.5B+", Notes = "Dehkia endgame · Extreme silver/hr" },
        new() { Name = "Edania: Throne & High Spire",MinAp = 390, MinDp = 440, SilverPerHour = "~2.5B - 3.0B+", Notes = "Primordial Sovereign & Edana region" }
    };

    // PVP Caps
    public static readonly Dictionary<string, Dictionary<string, Dictionary<string, double>>> PvpCaps = new()
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
            },
            ["Tier 2"] = new()
            {
                ["Total AP"] = 750,
                ["Total AAP"] = 750,
                ["Evasion"] = 1050,
                ["Damage Reduction"] = 600,
                ["Accuracy"] = 900,
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
            },
            ["Tier 2"] = new()
            {
                ["Total AP"] = 820,
                ["Total AAP"] = 820,
                ["Evasion"] = 1100,
                ["Damage Reduction"] = 650,
                ["Accuracy"] = 950,
            },
        },
    };

    public BdoGearService()
    {
        Directory.CreateDirectory(_imagesDir);
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

    public string GetGearImagePath(ulong userId)
    {
        return Path.Combine(_imagesDir, $"{userId}.png");
    }

    public List<BdoGrindZone> GetRecommendedZones(int ap, int dp, int count = 3)
    {
        return GrindZones
            .Where(z => ap >= z.MinAp - 15 && dp >= z.MinDp - 20)
            .OrderByDescending(z => z.MinAp)
            .Take(count)
            .ToList();
    }

    public async Task<(bool Success, string? Error, BdoGearProfile? Profile)> FetchGarmothProfileAsync(
        ulong userId,
        string username,
        string slugOrUrl)
    {
        try
        {
            Directory.CreateDirectory(_imagesDir);
            var imageOutPath = Path.GetFullPath(GetGearImagePath(userId));

            var scriptPath = Path.Combine("data", "bdo_garmoth_fetcher.py");
            if (!File.Exists(scriptPath))
            {
                scriptPath = Path.Combine(AppContext.BaseDirectory, "data", "bdo_garmoth_fetcher.py");
            }

            var pythonExe = OperatingSystem.IsWindows() ? "python" : "python3";
            var psi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"\"{scriptPath}\" \"{slugOrUrl.Trim()}\" \"{imageOutPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process is null)
                return (false, "Could not launch Python fetcher process.", null);

            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (string.IsNullOrWhiteSpace(stdout))
                return (false, $"Fetcher returned no output: {stderr}", null);

            var doc = JsonNode.Parse(stdout);
            if (doc is null)
                return (false, "Could not parse JSON output from Garmoth fetcher.", null);

            if (doc["success"]?.GetValue<bool>() != true)
            {
                var err = doc["error"]?.GetValue<string>() ?? "Unknown error fetching Garmoth character.";
                return (false, err, null);
            }

            var profile = new BdoGearProfile
            {
                UserId = userId,
                Username = username,
                RegisteredAt = DateTime.UtcNow,
                GarmothLink = doc["url"]?.GetValue<string>() ?? slugOrUrl,
                CharacterName = doc["name"]?.GetValue<string>() ?? "Unknown",
                Level = doc["level"]?.GetValue<int>() ?? 0,
                Spec = doc["spec"]?.GetValue<string>() ?? "succ",
                BuildName = doc["build_name"]?.GetValue<string>() ?? "Current",
                Ap = doc["ap"]?.GetValue<int>() ?? 0,
                Aap = doc["aap"]?.GetValue<int>() ?? 0,
                Dp = doc["dp"]?.GetValue<int>() ?? 0,
                GearScore = doc["score"]?.GetValue<int>() ?? 0,
                TotalAttackAp = doc["totalap"]?.GetValue<double>() ?? 0,
                AdventureAp = doc["adventureap"]?.GetValue<double>() ?? 0,
                MonsterAp = doc["monsterap"]?.GetValue<double>() ?? 0,
                HumanAp = doc["humanap"]?.GetValue<double>() ?? 0,
                KamaAp = doc["kamaap"]?.GetValue<double>() ?? 0,
                DemihumanAp = doc["demiap"]?.GetValue<double>() ?? 0,
                EdaniaAp = doc["edaniaap"]?.GetValue<double>() ?? 0,
                NormalAp = doc["normalap"]?.GetValue<double>() ?? 0,
                HiddenAp = doc["hiddenap"]?.GetValue<double>() ?? 0,
                TotalAwakeningAp = doc["totalaap"]?.GetValue<double>() ?? 0,
                Accuracy = doc["acc"]?.GetValue<int>() ?? 0,
                EvasionMelee = doc["evasion_melee"]?.GetValue<int>() ?? 0,
                EvasionRanged = doc["evasion_ranged"]?.GetValue<int>() ?? 0,
                EvasionMagic = doc["evasion_magic"]?.GetValue<int>() ?? 0,
                DrMelee = doc["dr_melee"]?.GetValue<int>() ?? 0,
                DrRanged = doc["dr_ranged"]?.GetValue<int>() ?? 0,
                DrMagic = doc["dr_magic"]?.GetValue<int>() ?? 0,
                DrRate = doc["dr_rate"]?.GetValue<double>() ?? 30.0,
                GearRaw = doc["gear"]?.ToJsonString(),
                ImagePath = File.Exists(imageOutPath) ? imageOutPath : null
            };

            SaveProfile(profile);
            return (true, null, profile);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }
}
