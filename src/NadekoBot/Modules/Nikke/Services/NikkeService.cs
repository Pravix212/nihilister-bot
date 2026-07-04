#nullable disable
using NadekoBot.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NadekoBot.Modules.Nikke.Services;

public class NikkeService : INService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IBotCache _cache;
    private readonly List<NikkeCharacter> _localCharacters;
    private const string BASE_URL = "https://nikke-api.vercel.app/characters/";
    private const string JSON_PATH = "Modules/Nikke/nikke_characters.json";

    public NikkeService(IHttpClientFactory httpFactory, IBotCache cache)
    {
        _httpFactory = httpFactory;
        _cache = cache;
        _localCharacters = LoadLocalCharacters();
    }

    private List<NikkeCharacter> LoadLocalCharacters()
    {
        try
        {
            // Try multiple paths: project dir (dev), output dir (published), base dir
            var paths = new[]
            {
                Path.Combine(Environment.CurrentDirectory, JSON_PATH),
                Path.Combine(AppContext.BaseDirectory, JSON_PATH),
                Path.Combine(Directory.GetCurrentDirectory(), JSON_PATH)
            };

            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    return JsonConvert.DeserializeObject<List<NikkeCharacter>>(json) ?? new List<NikkeCharacter>();
                }
            }
            return new List<NikkeCharacter>();
        }
        catch
        {
            return new List<NikkeCharacter>();
        }
    }

    public async Task<NikkeCharacter> GetCharacterAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var cacheKey = $"nikke_char_{name.ToLowerInvariant()}";
        return await _cache.GetOrAddAsync(
            new TypedKey<NikkeCharacter>(cacheKey),
            async () => await GetCharacterFactoryAsync(name),
            TimeSpan.FromHours(1));
    }

    private async Task<NikkeCharacter> GetCharacterFactoryAsync(string name)
    {
        // 1. Try local JSON first (case-insensitive)
        var local = _localCharacters.FirstOrDefault(c =>
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
        if (local != null)
            return local;

        // 2. Fall back to API
        using var http = _httpFactory.CreateClient();
        try
        {
            var response = await http.GetStringAsync($"{BASE_URL}{Uri.EscapeDataString(name)}");
            if (string.IsNullOrWhiteSpace(response))
                return null;

            var jObject = JObject.Parse(response);
            if (jObject["error"] != null || jObject["data"]?.Value<string>() == "No NIKKE Found!")
                return null;

            return jObject.ToObject<NikkeCharacter>();
        }
        catch
        {
            return null;
        }
    }
}

public class NikkeCharacter
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("rarity")]
    public string Rarity { get; set; }

    [JsonProperty("element")]
    public string Element { get; set; }

    [JsonProperty("weapon")]
    public string Weapon { get; set; }

    [JsonProperty("weaponName")]
    public string WeaponName { get; set; }

    [JsonProperty("class")]
    public string Class { get; set; }

    [JsonProperty("burstType")]
    public string BurstType { get; set; }

    [JsonProperty("manufacturer")]
    public string Manufacturer { get; set; }

    [JsonProperty("squad")]
    public string Squad { get; set; }

    [JsonProperty("backstory")]
    public string Backstory { get; set; }

    [JsonProperty("releaseDate")]
    public string ReleaseDate { get; set; }

    [JsonProperty("stats")]
    public NikkeStats Stats { get; set; }

    [JsonProperty("skills")]
    public NikkeSkills Skills { get; set; }

    [JsonProperty("voiceActors")]
    public NikkeVoiceActors VoiceActors { get; set; }

    [JsonProperty("images")]
    public NikkeImages Images { get; set; }
}

public class NikkeStats
{
    [JsonProperty("hp")]
    public int? Hp { get; set; }

    [JsonProperty("atk")]
    public int? Atk { get; set; }

    [JsonProperty("def")]
    public int? Def { get; set; }
}

public class NikkeSkills
{
    [JsonProperty("normal")]
    public NikkeSkill Normal { get; set; }

    [JsonProperty("skill1")]
    public NikkeSkill Skill1 { get; set; }

    [JsonProperty("skill2")]
    public NikkeSkill Skill2 { get; set; }

    [JsonProperty("burst")]
    public NikkeSkill Burst { get; set; }
}

public class NikkeSkill
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("cooldown")]
    public string Cooldown { get; set; }

    [JsonProperty("baseDescription")]
    public List<string> BaseDescription { get; set; }

    [JsonProperty("maxDescription")]
    public List<string> MaxDescription { get; set; }

    [JsonProperty("mode")]
    public string Mode { get; set; }

    [JsonProperty("ammo")]
    public int? Ammo { get; set; }

    [JsonProperty("reloadTime")]
    public string ReloadTime { get; set; }

    [JsonProperty("description")]
    public List<string> Description { get; set; }
}

public class NikkeVoiceActors
{
    [JsonProperty("kr")]
    public string Kr { get; set; }

    [JsonProperty("jp")]
    public string Jp { get; set; }

    [JsonProperty("en")]
    public string En { get; set; }
}

public class NikkeImages
{
    [JsonProperty("icon")]
    public string Icon { get; set; }

    [JsonProperty("card")]
    public string Card { get; set; }

    [JsonProperty("full")]
    public string Full { get; set; }
}
