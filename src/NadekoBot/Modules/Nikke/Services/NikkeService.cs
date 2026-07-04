#nullable disable
using NadekoBot.Common;
using Newtonsoft.Json;

namespace NadekoBot.Modules.Nikke.Services;

public class NikkeService : INService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IBotCache _cache;
    private readonly List<NikkeCharacter> _localCharacters;
    private List<DotggCharacterListItem> _cachedApiList;
    private DateTime _cachedApiListTime = DateTime.MinValue;
    private const string CHARACTERS_API = "https://api.dotgg.gg/nikke/characters";
    private const string CHARACTER_API = "https://api.dotgg.gg/nikke/character/";
    private const string STATIC_IMG_BASE = "https://static.dotgg.gg/nikke/characters/";
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

        // v3 cache key to invalidate old v2 cached data
        var cacheKey = $"nikke_char_v3_{name.ToLowerInvariant()}";
        return await _cache.GetOrAddAsync(
            new TypedKey<NikkeCharacter>(cacheKey),
            async () => await GetCharacterFactoryAsync(name),
            TimeSpan.FromHours(1));
    }

    private async Task<NikkeCharacter> GetCharacterFactoryAsync(string name)
    {
        // 1. Try NIKKE.gg API first (live data is the source of truth)
        try
        {
            var apiChar = await FetchFromApiAsync(name);
            if (apiChar != null)
                return apiChar;
        }
        catch { /* API error, fall through to local */ }

        // 2. Try local JSON (exact match)
        var local = _localCharacters.FirstOrDefault(c =>
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
        if (local != null)
            return local;

        // 3. Fuzzy local fallback
        var fuzzy = _localCharacters.FirstOrDefault(c =>
            c.Name.Contains(name, StringComparison.OrdinalIgnoreCase) ||
            name.Contains(c.Name, StringComparison.OrdinalIgnoreCase));
        return fuzzy;
    }

    private async Task<NikkeCharacter> FetchFromApiAsync(string name)
    {
        // Fetch character list (cached for 10 minutes)
        if (_cachedApiList == null || DateTime.UtcNow - _cachedApiListTime > TimeSpan.FromMinutes(10))
        {
            using var listHttp = _httpFactory.CreateClient();
            listHttp.Timeout = TimeSpan.FromSeconds(15);

            var listJson = await listHttp.GetStringAsync(CHARACTERS_API);
            _cachedApiList = JsonConvert.DeserializeObject<List<DotggCharacterListItem>>(listJson);
            _cachedApiListTime = DateTime.UtcNow;
        }

        if (_cachedApiList == null || _cachedApiList.Count == 0)
            return null;

        // Find matching character (exact first, then fuzzy)
        var match = _cachedApiList.FirstOrDefault(c =>
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)) ??
            _cachedApiList.FirstOrDefault(c =>
                c.Name.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                name.Contains(c.Name, StringComparison.OrdinalIgnoreCase));

        if (match == null)
            return null;

        // Fetch character details
        using var detailHttp = _httpFactory.CreateClient();
        detailHttp.Timeout = TimeSpan.FromSeconds(15);
        var detailJson = await detailHttp.GetStringAsync($"{CHARACTER_API}{match.Url}");
        var detail = JsonConvert.DeserializeObject<DotggCharacterDetail>(detailJson);

        if (detail == null)
            return null;

        return ConvertToNikkeCharacter(detail);
    }

    private NikkeCharacter ConvertToNikkeCharacter(DotggCharacterDetail d)
    {
        var weaponMap = new Dictionary<string, string>
        {
            ["AR"] = "Assault Rifle", ["SG"] = "Shotgun", ["SMG"] = "SMG",
            ["SR"] = "Sniper Rifle", ["RL"] = "Rocket Launcher", ["MG"] = "Minigun"
        };

        var weaponName = weaponMap.TryGetValue(d.Weapon, out var w) ? w : d.Weapon;

        var skills = new NikkeSkills
        {
            Normal = new NikkeSkill
            {
                Name = "Normal Attack",
                Mode = d.ChargeTime > 0 ? "Charge" : "Normal",
                Ammo = d.MaxAmmo,
                ReloadTime = $"{d.ReloadTime}s",
                Description = new List<string> { $"Deals {d.Damage} ATK as damage." }
            }
        };

        if (d.Skills != null)
        {
            for (int i = 0; i < d.Skills.Count; i++)
            {
                var s = d.Skills[i];
                var level = s.Levels?.Count >= 10 ? s.Levels[9] : s.Levels?.FirstOrDefault(); // Level 10
                var parsedDesc = ParseSkillDescription(s.Description, level);

                var skill = new NikkeSkill
                {
                    Name = s.Name,
                    Type = i == 0 ? "Passive" : (i == 1 ? "Passive" : "Active"),
                    Cooldown = string.IsNullOrEmpty(s.Cooldown) ? null : $"{s.Cooldown}s",
                    BaseDescription = parsedDesc,
                    MaxDescription = parsedDesc
                };

                if (i == 0) skills.Skill1 = skill;
                else if (i == 1) skills.Skill2 = skill;
                else if (i == 2) skills.Burst = skill;
            }
        }

        var voiceActors = new NikkeVoiceActors
        {
            En = CleanCv(d.CvEn),
            Jp = CleanCv(d.CvJp),
            Kr = CleanCv(d.CvKr)
        };

        var images = new NikkeImages
        {
            Icon = string.IsNullOrEmpty(d.Img) ? null : $"{STATIC_IMG_BASE}{d.Img}.png",
            Card = string.IsNullOrEmpty(d.ImgBig) ? null : $"{STATIC_IMG_BASE}{d.ImgBig}.png",
            Full = string.IsNullOrEmpty(d.ImgBig) ? null : $"{STATIC_IMG_BASE}{d.ImgBig}.png"
        };

        var tierlist = new NikkeTierlist
        {
            Combined = d.Tierlist?.Combined,
            Story = d.Tierlist?.Story,
            Boss = d.Tierlist?.Boss,
            PvP = d.Tierlist?.PvP
        };

        var skillPrio = new NikkeSkillPriority
        {
            Budget = d.Skillprio?.Budget,
            Recommended = d.Skillprio?.Recommended,
            Order = d.Skillprio?.Order
        };

        var cubes = new NikkeCubes
        {
            Main = d.Recommendations?.Main,
            Alternative = d.Recommendations?.Alternative
        };

        return new NikkeCharacter
        {
            Name = d.Name,
            Rarity = d.Rarity,
            Element = d.Element,
            Weapon = weaponName,
            WeaponName = d.Weapon,
            Class = d.Class,
            BurstType = d.Burst?.ToUpperInvariant() switch
            {
                "1" or "I" => "I",
                "2" or "II" => "II",
                "3" or "III" => "III",
                "P" or "P" => "P",
                _ => d.Burst
            },
            Manufacturer = d.Manufacturer,
            Squad = d.Squad,
            Backstory = d.Description?.Replace("\n", " "),
            ReleaseDate = null,
            Stats = new NikkeStats
            {
                // API doesn't provide max stats directly
                Hp = null, Atk = null, Def = null
            },
            Skills = skills,
            VoiceActors = voiceActors,
            Images = images,
            Tierlist = tierlist,
            SkillPriority = skillPrio,
            Cubes = cubes,
            BurstGeneration = d.BurstGen,
            MaxAmmo = d.MaxAmmo,
            DamagePercent = d.Damage,
            ReloadTime = d.ReloadTime,
            ChargeTime = d.ChargeTime > 0 ? $"{d.ChargeTime}s" : null,
            ChargeDamage = d.ChargeDamage
        };
    }

    private static List<string> ParseSkillDescription(string rawDescription, DotggSkillLevel level)
    {
        if (string.IsNullOrWhiteSpace(rawDescription))
            return new List<string> { "No description available." };

        var result = rawDescription;

        // Substitute level values
        if (level != null)
        {
            var props = typeof(DotggSkillLevel).GetProperties();
            foreach (var prop in props)
            {
                if (!prop.Name.StartsWith("DescriptionValue")) continue;
                var idx = prop.Name.Replace("DescriptionValue", "");
                var val = prop.GetValue(level) as string;
                if (!string.IsNullOrEmpty(val))
                    result = result.Replace($"{{description_value_{idx}}}", val);
            }
        }

        // Remove remaining placeholders
        result = System.Text.RegularExpressions.Regex.Replace(result, @"\{description_value_\d+\}", "");

        // Strip <color=...> tags (keep inner text)
        result = System.Text.RegularExpressions.Regex.Replace(result, @"<color=[^>]+>", "");
        result = System.Text.RegularExpressions.Regex.Replace(result, @"</color>", "");

        // Strip <word_group=...> tags (keep inner text)
        result = System.Text.RegularExpressions.Regex.Replace(result, @"<word_group=[^>]+>", "");
        result = System.Text.RegularExpressions.Regex.Replace(result, @"</word_group>", "");

        // Split into lines and clean up
        var lines = result.Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        return lines.Count > 0 ? lines : new List<string> { "No description available." };
    }

    private static string CleanCv(string cv)
    {
        if (string.IsNullOrWhiteSpace(cv)) return null;
        return cv.Replace("CV: ", "").Trim();
    }
}

// === DTOs for NIKKE.gg API ===

public class DotggCharacterListItem
{
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("url")] public string Url { get; set; }
    [JsonProperty("img")] public string Img { get; set; }
    [JsonProperty("rarity")] public string Rarity { get; set; }
    [JsonProperty("class")] public string Class { get; set; }
    [JsonProperty("weapon")] public string Weapon { get; set; }
    [JsonProperty("burst")] public string Burst { get; set; }
    [JsonProperty("element")] public string Element { get; set; }
    [JsonProperty("manufacturer")] public string Manufacturer { get; set; }
    [JsonProperty("squad")] public string Squad { get; set; }
}

public class DotggCharacterDetail
{
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("url")] public string Url { get; set; }
    [JsonProperty("img")] public string Img { get; set; }
    [JsonProperty("imgBig")] public string ImgBig { get; set; }
    [JsonProperty("description")] public string Description { get; set; }
    [JsonProperty("cv_en")] public string CvEn { get; set; }
    [JsonProperty("cv_kr")] public string CvKr { get; set; }
    [JsonProperty("cv_jp")] public string CvJp { get; set; }
    [JsonProperty("manufacturer")] public string Manufacturer { get; set; }
    [JsonProperty("squad")] public string Squad { get; set; }
    [JsonProperty("class")] public string Class { get; set; }
    [JsonProperty("burst")] public string Burst { get; set; }
    [JsonProperty("rarity")] public string Rarity { get; set; }
    [JsonProperty("weapon")] public string Weapon { get; set; }
    [JsonProperty("burstGen")] public string BurstGen { get; set; }
    [JsonProperty("maxAmmo")] public int MaxAmmo { get; set; }
    [JsonProperty("damage")] public string Damage { get; set; }
    [JsonProperty("chargeTime")] public double ChargeTime { get; set; }
    [JsonProperty("chargeDamage")] public string ChargeDamage { get; set; }
    [JsonProperty("reloadTime")] public double ReloadTime { get; set; }
    [JsonProperty("element")] public string Element { get; set; }
    [JsonProperty("skills")] public List<DotggSkill> Skills { get; set; }
    [JsonProperty("tierlist")] public DotggTierlist Tierlist { get; set; }
    [JsonProperty("skillprio")] public DotggSkillPrio Skillprio { get; set; }
    [JsonProperty("recommendations")] public DotggRecommendations Recommendations { get; set; }
}

public class DotggSkill
{
    [JsonProperty("id")] public int Id { get; set; }
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("description")] public string Description { get; set; }
    [JsonProperty("cooldown")] public string Cooldown { get; set; }
    [JsonProperty("levels")] public List<DotggSkillLevel> Levels { get; set; }
}

public class DotggSkillLevel
{
    [JsonProperty("description_value_01")] public string DescriptionValue01 { get; set; }
    [JsonProperty("description_value_02")] public string DescriptionValue02 { get; set; }
    [JsonProperty("description_value_03")] public string DescriptionValue03 { get; set; }
    [JsonProperty("description_value_04")] public string DescriptionValue04 { get; set; }
    [JsonProperty("description_value_05")] public string DescriptionValue05 { get; set; }
    [JsonProperty("description_value_06")] public string DescriptionValue06 { get; set; }
    [JsonProperty("description_value_07")] public string DescriptionValue07 { get; set; }
    [JsonProperty("description_value_08")] public string DescriptionValue08 { get; set; }
    [JsonProperty("description_value_09")] public string DescriptionValue09 { get; set; }
    [JsonProperty("description_value_10")] public string DescriptionValue10 { get; set; }
    [JsonProperty("description_value_11")] public string DescriptionValue11 { get; set; }
}

public class DotggTierlist
{
    [JsonProperty("Combined")] public string Combined { get; set; }
    [JsonProperty("Story")] public string Story { get; set; }
    [JsonProperty("Boss")] public string Boss { get; set; }
    [JsonProperty("PvP")] public string PvP { get; set; }
}

public class DotggSkillPrio
{
    [JsonProperty("Budget Skill investments")] public string Budget { get; set; }
    [JsonProperty("Recommended Skill Investments")] public string Recommended { get; set; }
    [JsonProperty("Skill Order Priority")] public string Order { get; set; }
}

public class DotggRecommendations
{
    [JsonProperty("main")] public string Main { get; set; }
    [JsonProperty("alternative")] public string Alternative { get; set; }
}

// === Presentation Models ===

public class NikkeCharacter
{
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("rarity")] public string Rarity { get; set; }
    [JsonProperty("element")] public string Element { get; set; }
    [JsonProperty("weapon")] public string Weapon { get; set; }
    [JsonProperty("weaponName")] public string WeaponName { get; set; }
    [JsonProperty("class")] public string Class { get; set; }
    [JsonProperty("burstType")] public string BurstType { get; set; }
    [JsonProperty("manufacturer")] public string Manufacturer { get; set; }
    [JsonProperty("squad")] public string Squad { get; set; }
    [JsonProperty("backstory")] public string Backstory { get; set; }
    [JsonProperty("releaseDate")] public string ReleaseDate { get; set; }
    [JsonProperty("stats")] public NikkeStats Stats { get; set; }
    [JsonProperty("skills")] public NikkeSkills Skills { get; set; }
    [JsonProperty("voiceActors")] public NikkeVoiceActors VoiceActors { get; set; }
    [JsonProperty("images")] public NikkeImages Images { get; set; }

    // Extended fields from NIKKE.gg
    [JsonProperty("tierlist")] public NikkeTierlist Tierlist { get; set; }
    [JsonProperty("skillPriority")] public NikkeSkillPriority SkillPriority { get; set; }
    [JsonProperty("cubes")] public NikkeCubes Cubes { get; set; }
    [JsonProperty("burstGeneration")] public string BurstGeneration { get; set; }
    [JsonProperty("maxAmmo")] public int MaxAmmo { get; set; }
    [JsonProperty("damagePercent")] public string DamagePercent { get; set; }
    [JsonProperty("reloadTime")] public double ReloadTime { get; set; }
    [JsonProperty("chargeTime")] public string ChargeTime { get; set; }
    [JsonProperty("chargeDamage")] public string ChargeDamage { get; set; }
}

public class NikkeStats
{
    [JsonProperty("hp")] public int? Hp { get; set; }
    [JsonProperty("atk")] public int? Atk { get; set; }
    [JsonProperty("def")] public int? Def { get; set; }
}

public class NikkeSkills
{
    [JsonProperty("normal")] public NikkeSkill Normal { get; set; }
    [JsonProperty("skill1")] public NikkeSkill Skill1 { get; set; }
    [JsonProperty("skill2")] public NikkeSkill Skill2 { get; set; }
    [JsonProperty("burst")] public NikkeSkill Burst { get; set; }
}

public class NikkeSkill
{
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("type")] public string Type { get; set; }
    [JsonProperty("cooldown")] public string Cooldown { get; set; }
    [JsonProperty("baseDescription")] public List<string> BaseDescription { get; set; }
    [JsonProperty("maxDescription")] public List<string> MaxDescription { get; set; }
    [JsonProperty("mode")] public string Mode { get; set; }
    [JsonProperty("ammo")] public int? Ammo { get; set; }
    [JsonProperty("reloadTime")] public string ReloadTime { get; set; }
    [JsonProperty("description")] public List<string> Description { get; set; }
}

public class NikkeVoiceActors
{
    [JsonProperty("kr")] public string Kr { get; set; }
    [JsonProperty("jp")] public string Jp { get; set; }
    [JsonProperty("en")] public string En { get; set; }
}

public class NikkeImages
{
    [JsonProperty("icon")] public string Icon { get; set; }
    [JsonProperty("card")] public string Card { get; set; }
    [JsonProperty("full")] public string Full { get; set; }
}

public class NikkeTierlist
{
    [JsonProperty("combined")] public string Combined { get; set; }
    [JsonProperty("story")] public string Story { get; set; }
    [JsonProperty("boss")] public string Boss { get; set; }
    [JsonProperty("pvp")] public string PvP { get; set; }
}

public class NikkeSkillPriority
{
    [JsonProperty("budget")] public string Budget { get; set; }
    [JsonProperty("recommended")] public string Recommended { get; set; }
    [JsonProperty("order")] public string Order { get; set; }
}

public class NikkeCubes
{
    [JsonProperty("main")] public string Main { get; set; }
    [JsonProperty("alternative")] public string Alternative { get; set; }
}
