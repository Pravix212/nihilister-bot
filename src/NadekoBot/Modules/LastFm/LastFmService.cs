#nullable disable
using NadekoBot.Db;
using NadekoBot.Db.Models;
using Nadeko.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.EntityFrameworkCore;

namespace NadekoBot.Modules.LastFm.Services;

public class LastFmService : INService
{
    private readonly IBotCredsProvider _creds;
    private readonly IHttpClientFactory _httpFactory;
    private readonly DbService _db;
    private readonly IBotCache _cache;
    
    private const string BASE_URL = "https://ws.audioscrobbler.com/2.0/";
    
    public LastFmService(IBotCredsProvider creds, IHttpClientFactory httpFactory, DbService db, IBotCache cache)
    {
        _creds = creds;
        _httpFactory = httpFactory;
        _db = db;
        _cache = cache;
    }
    
    #region Database Helpers
    
    public async Task<string> GetUsernameAsync(ulong discordUserId)
    {
        await using var ctx = _db.GetDbContext();
        var user = await ctx.LastFmUsers.AsNoTracking().FirstOrDefaultAsync(x => x.DiscordUserId == discordUserId);
        return user?.LastFmUsername;
    }
    
    public async Task<List<LastFmUser>> GetGuildUsersAsync(IEnumerable<ulong> userIds)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.LastFmUsers.AsNoTracking()
            .Where(x => userIds.Contains(x.DiscordUserId))
            .ToListAsync();
    }
    
    public async Task<LastFmArtistCrown> GetCrownAsync(ulong guildId, string artistName)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.LastFmArtistCrowns.AsNoTracking()
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.ArtistName == artistName);
    }
    
    public async Task SetCrownAsync(ulong guildId, string artistName, ulong discordUserId, int playcount)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.LastFmArtistCrowns
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.ArtistName == artistName);
        
        if (existing != null)
        {
            existing.DiscordUserId = discordUserId;
            existing.Playcount = playcount;
            existing.ClaimedAt = DateTime.UtcNow;
        }
        else
        {
            ctx.LastFmArtistCrowns.Add(new LastFmArtistCrown
            {
                GuildId = guildId,
                ArtistName = artistName,
                DiscordUserId = discordUserId,
                Playcount = playcount,
                ClaimedAt = DateTime.UtcNow
            });
        }
        
        await ctx.SaveChangesAsync();
    }
    
    public async Task SetUsernameAsync(ulong discordUserId, string lastFmUsername)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.LastFmUsers.FirstOrDefaultAsync(x => x.DiscordUserId == discordUserId);
        if (existing != null)
        {
            existing.LastFmUsername = lastFmUsername;
            existing.LinkedAt = DateTime.UtcNow;
        }
        else
        {
            ctx.LastFmUsers.Add(new LastFmUser
            {
                DiscordUserId = discordUserId,
                LastFmUsername = lastFmUsername,
                LinkedAt = DateTime.UtcNow
            });
        }
        await ctx.SaveChangesAsync();
    }
    
    #endregion
    
    #region API Methods
    
    public async Task<LastFmUserInfo> GetUserInfoAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(_creds.GetCreds().LastFmApiKey))
            return null;
        
        return await _cache.GetOrAddAsync(
            new TypedKey<LastFmUserInfo>($"lastfm_userinfo_{username}"),
            async () => await GetUserInfoFactoryAsync(username),
            TimeSpan.FromMinutes(5));
    }
    
    private async Task<LastFmUserInfo> GetUserInfoFactoryAsync(string username)
    {
        using var http = _httpFactory.CreateClient();
        var url = $"{BASE_URL}?method=user.getInfo&user={Uri.EscapeDataString(username)}&api_key={_creds.GetCreds().LastFmApiKey}&format=json";
        try
        {
            var response = await http.GetStringAsync(url);
            if (string.IsNullOrWhiteSpace(response))
                return null;
            
            var jObject = JObject.Parse(response);
            if (jObject["error"] != null)
                return null;
            
            return jObject["user"]?.ToObject<LastFmUserInfo>();
        }
        catch
        {
            return null;
        }
    }
    
    public async Task<List<LastFmTrack>> GetRecentTracksAsync(string username, int limit = 2)
    {
        if (string.IsNullOrWhiteSpace(_creds.GetCreds().LastFmApiKey))
            return null;
        
        return await GetRecentTracksFactoryAsync(username, limit);
    }
    
    private async Task<List<LastFmTrack>> GetRecentTracksFactoryAsync(string username, int limit)
    {
        using var http = _httpFactory.CreateClient();
        var url = $"{BASE_URL}?method=user.getRecentTracks&user={Uri.EscapeDataString(username)}&limit={limit}&api_key={_creds.GetCreds().LastFmApiKey}&format=json";
        try
        {
            var response = await http.GetStringAsync(url);
            if (string.IsNullOrWhiteSpace(response))
                return new List<LastFmTrack>();
            
            var jObject = JObject.Parse(response);
            if (jObject["error"] != null)
                return new List<LastFmTrack>();
            
            var tracks = jObject["recenttracks"]?["track"]?.ToObject<List<LastFmTrack>>();
            return tracks ?? new List<LastFmTrack>();
        }
        catch
        {
            return new List<LastFmTrack>();
        }
    }
    
    public async Task<List<LastFmArtist>> GetTopArtistsAsync(string username, string period = "overall", int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(_creds.GetCreds().LastFmApiKey))
            return null;
        
        return await _cache.GetOrAddAsync(
            new TypedKey<List<LastFmArtist>>($"lastfm_topartists_{username}_{period}_{limit}"),
            async () => await GetTopArtistsFactoryAsync(username, period, limit),
            TimeSpan.FromMinutes(5));
    }
    
    private async Task<List<LastFmArtist>> GetTopArtistsFactoryAsync(string username, string period, int limit)
    {
        using var http = _httpFactory.CreateClient();
        var url = $"{BASE_URL}?method=user.getTopArtists&user={Uri.EscapeDataString(username)}&period={period}&limit={limit}&api_key={_creds.GetCreds().LastFmApiKey}&format=json";
        try
        {
            var response = await http.GetStringAsync(url);
            if (string.IsNullOrWhiteSpace(response))
                return new List<LastFmArtist>();
            
            var jObject = JObject.Parse(response);
            if (jObject["error"] != null)
                return new List<LastFmArtist>();
            
            var artists = jObject["topartists"]?["artist"]?.ToObject<List<LastFmArtist>>();
            return artists ?? new List<LastFmArtist>();
        }
        catch
        {
            return new List<LastFmArtist>();
        }
    }
    
    public async Task<List<LastFmAlbum>> GetTopAlbumsAsync(string username, string period = "overall", int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(_creds.GetCreds().LastFmApiKey))
            return null;
        
        return await _cache.GetOrAddAsync(
            new TypedKey<List<LastFmAlbum>>($"lastfm_topalbums_{username}_{period}_{limit}"),
            async () => await GetTopAlbumsFactoryAsync(username, period, limit),
            TimeSpan.FromMinutes(5));
    }
    
    private async Task<List<LastFmAlbum>> GetTopAlbumsFactoryAsync(string username, string period, int limit)
    {
        using var http = _httpFactory.CreateClient();
        var url = $"{BASE_URL}?method=user.getTopAlbums&user={Uri.EscapeDataString(username)}&period={period}&limit={limit}&api_key={_creds.GetCreds().LastFmApiKey}&format=json";
        try
        {
            var response = await http.GetStringAsync(url);
            if (string.IsNullOrWhiteSpace(response))
                return new List<LastFmAlbum>();
            
            var jObject = JObject.Parse(response);
            if (jObject["error"] != null)
                return new List<LastFmAlbum>();
            
            var albums = jObject["topalbums"]?["album"]?.ToObject<List<LastFmAlbum>>();
            return albums ?? new List<LastFmAlbum>();
        }
        catch
        {
            return new List<LastFmAlbum>();
        }
    }
    
    public async Task<List<LastFmTopTrack>> GetTopTracksAsync(string username, string period = "overall", int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(_creds.GetCreds().LastFmApiKey))
            return null;
        
        return await _cache.GetOrAddAsync(
            new TypedKey<List<LastFmTopTrack>>($"lastfm_toptracks_{username}_{period}_{limit}"),
            async () => await GetTopTracksFactoryAsync(username, period, limit),
            TimeSpan.FromSeconds(30));
    }
    
    private async Task<List<LastFmTopTrack>> GetTopTracksFactoryAsync(string username, string period, int limit)
    {
        using var http = _httpFactory.CreateClient();
        var url = $"{BASE_URL}?method=user.getTopTracks&user={Uri.EscapeDataString(username)}&period={period}&limit={limit}&api_key={_creds.GetCreds().LastFmApiKey}&format=json";
        try
        {
            var response = await http.GetStringAsync(url);
            if (string.IsNullOrWhiteSpace(response))
                return new List<LastFmTopTrack>();
            
            var jObject = JObject.Parse(response);
            if (jObject["error"] != null)
                return new List<LastFmTopTrack>();
            
            var tracks = jObject["toptracks"]?["track"]?.ToObject<List<LastFmTopTrack>>();
            return tracks ?? new List<LastFmTopTrack>();
        }
        catch
        {
            return new List<LastFmTopTrack>();
        }
    }
    
    public async Task<LastFmTrackInfo> GetTrackInfoAsync(string artist, string track, string username)
    {
        if (string.IsNullOrWhiteSpace(_creds.GetCreds().LastFmApiKey))
            return null;
        
        return await _cache.GetOrAddAsync(
            new TypedKey<LastFmTrackInfo>($"lastfm_trackinfo_{artist}_{track}_{username}"),
            async () => await GetTrackInfoFactoryAsync(artist, track, username),
            TimeSpan.FromSeconds(10));
    }
    
    private async Task<LastFmTrackInfo> GetTrackInfoFactoryAsync(string artist, string track, string username)
    {
        using var http = _httpFactory.CreateClient();
        var url = $"{BASE_URL}?method=track.getInfo&artist={Uri.EscapeDataString(artist)}&track={Uri.EscapeDataString(track)}&username={Uri.EscapeDataString(username)}&api_key={_creds.GetCreds().LastFmApiKey}&format=json";
        try
        {
            var response = await http.GetStringAsync(url);
            if (string.IsNullOrWhiteSpace(response))
                return null;
            
            var jObject = JObject.Parse(response);
            if (jObject["error"] != null)
                return null;
            
            return jObject["track"]?.ToObject<LastFmTrackInfo>();
        }
        catch
        {
            return null;
        }
    }
    
    public async Task<string> GetLyricsAsync(string artist, string track)
    {
        if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(track))
            return null;
        
        return await _cache.GetOrAddAsync(
            new TypedKey<string>($"lyrics_{artist}_{track}"),
            async () => await GetLyricsFactoryAsync(artist, track),
            TimeSpan.FromHours(6));
    }
    
    private async Task<string> GetLyricsFactoryAsync(string artist, string track)
    {
        using var http = _httpFactory.CreateClient();
        var url = $"https://api.lyrics.ovh/v1/{Uri.EscapeDataString(artist)}/{Uri.EscapeDataString(track)}";
        try
        {
            var response = await http.GetStringAsync(url);
            if (string.IsNullOrWhiteSpace(response))
                return null;
            
            var jObject = JObject.Parse(response);
            return jObject["lyrics"]?.Value<string>();
        }
        catch
        {
            return null;
        }
    }
    
    public async Task<LastFmArtistInfo> GetArtistInfoAsync(string artistName)
    {
        if (string.IsNullOrWhiteSpace(_creds.GetCreds().LastFmApiKey))
            return null;
        
        return await _cache.GetOrAddAsync(
            new TypedKey<LastFmArtistInfo>($"lastfm_artistinfo_{artistName}"),
            async () => await GetArtistInfoFactoryAsync(artistName),
            TimeSpan.FromMinutes(10));
    }
    
    private async Task<LastFmArtistInfo> GetArtistInfoFactoryAsync(string artistName)
    {
        using var http = _httpFactory.CreateClient();
        var url = $"{BASE_URL}?method=artist.getInfo&artist={Uri.EscapeDataString(artistName)}&api_key={_creds.GetCreds().LastFmApiKey}&format=json";
        try
        {
            var response = await http.GetStringAsync(url);
            if (string.IsNullOrWhiteSpace(response))
                return null;
            
            var jObject = JObject.Parse(response);
            if (jObject["error"] != null)
                return null;
            
            return jObject["artist"]?.ToObject<LastFmArtistInfo>();
        }
        catch
        {
            return null;
        }
    }
    
    public async Task<int?> GetArtistUserPlaycountAsync(string artistName, string username)
    {
        if (string.IsNullOrWhiteSpace(_creds.GetCreds().LastFmApiKey))
            return null;
        
        var cacheKey = $"lastfm_artist_userpc_{artistName}_{username}";
        return await _cache.GetOrAddAsync(
            new TypedKey<int?>(cacheKey),
            async () => await GetArtistUserPlaycountFactoryAsync(artistName, username),
            TimeSpan.FromMinutes(5));
    }
    
    private async Task<int?> GetArtistUserPlaycountFactoryAsync(string artistName, string username)
    {
        using var http = _httpFactory.CreateClient();
        var url = $"{BASE_URL}?method=artist.getInfo&artist={Uri.EscapeDataString(artistName)}&username={Uri.EscapeDataString(username)}&api_key={_creds.GetCreds().LastFmApiKey}&format=json";
        try
        {
            var response = await http.GetStringAsync(url);
            if (string.IsNullOrWhiteSpace(response))
                return null;
            
            var jObject = JObject.Parse(response);
            if (jObject["error"] != null)
                return null;
            
            var userplaycount = jObject["artist"]?["stats"]?["userplaycount"]?.Value<string>();
            if (int.TryParse(userplaycount, out var count))
                return count;
            
            return null;
        }
        catch
        {
            return null;
        }
    }
    
    public async Task<WhoKnowsResult> GetWhoKnowsAsync(ulong guildId, string artistName, IEnumerable<IGuildUser> guildUsers)
    {
        var linkedUsers = await GetGuildUsersAsync(guildUsers.Select(u => u.Id));
        if (linkedUsers == null || linkedUsers.Count == 0)
            return null;
        
        var results = new List<WhoKnowsEntry>();
        var semaphore = new SemaphoreSlim(5, 5); // Limit concurrent API calls
        
        var tasks = linkedUsers.Select(async linkedUser =>
        {
            await semaphore.WaitAsync();
            try
            {
                var playcount = await GetArtistUserPlaycountAsync(artistName, linkedUser.LastFmUsername);
                if (playcount.HasValue && playcount.Value > 0)
                {
                    var guildUser = guildUsers.FirstOrDefault(u => u.Id == linkedUser.DiscordUserId);
                    if (guildUser != null)
                    {
                        return new WhoKnowsEntry
                        {
                            DiscordUser = guildUser,
                            LastFmUsername = linkedUser.LastFmUsername,
                            Playcount = playcount.Value
                        };
                    }
                }
                return null;
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        var entries = await Task.WhenAll(tasks);
        results = entries.Where(e => e != null).OrderByDescending(e => e.Playcount).ToList();
        
        if (results.Count == 0)
            return null;
        
        // Update crown
        var top = results.First();
        await SetCrownAsync(guildId, artistName, top.DiscordUser.Id, top.Playcount);
        
        var existingCrown = await GetCrownAsync(guildId, artistName);
        
        return new WhoKnowsResult
        {
            ArtistName = artistName,
            Entries = results,
            CrownHolder = top.DiscordUser,
            CrownPlaycount = top.Playcount,
            CrownClaimedAt = existingCrown?.ClaimedAt
        };
    }
    
    
    #endregion
}

#region DTOs

public class LastFmUserInfo
{
    [JsonProperty("name")]
    public string Name { get; set; }
    
    [JsonProperty("playcount")]
    public string Playcount { get; set; }
    
    [JsonProperty("artist_count")]
    public string ArtistCount { get; set; }
    
    [JsonProperty("track_count")]
    public string TrackCount { get; set; }
    
    [JsonProperty("album_count")]
    public string AlbumCount { get; set; }
    
    [JsonProperty("image")]
    public List<LastFmImage> Images { get; set; }
}

public class LastFmImage
{
    [JsonProperty("#text")]
    public string Url { get; set; }
    
    [JsonProperty("size")]
    public string Size { get; set; }
}

public class LastFmTrack
{
    [JsonProperty("name")]
    public string Name { get; set; }
    
    [JsonProperty("artist")]
    public LastFmTextField Artist { get; set; }
    
    [JsonProperty("album")]
    public LastFmTextField Album { get; set; }
    
    [JsonProperty("image")]
    public List<LastFmImage> Images { get; set; }
    
    [JsonProperty("date")]
    public LastFmDate Date { get; set; }
    
    [JsonProperty("@attr")]
    public LastFmTrackAttr Attr { get; set; }
}

public class LastFmTextField
{
    [JsonProperty("#text")]
    public string Text { get; set; }
}

public class LastFmDate
{
    [JsonProperty("#text")]
    public string Text { get; set; }
}

public class LastFmTrackAttr
{
    [JsonProperty("nowplaying")]
    public string NowPlaying { get; set; }
}

public class LastFmArtist
{
    [JsonProperty("name")]
    public string Name { get; set; }
    
    [JsonProperty("playcount")]
    public string Playcount { get; set; }
    
    [JsonProperty("listeners")]
    public string Listeners { get; set; }
    
    [JsonProperty("image")]
    public List<LastFmImage> Images { get; set; }
}

public class LastFmAlbum
{
    [JsonProperty("name")]
    public string Name { get; set; }
    
    [JsonProperty("artist")]
    public LastFmTextField Artist { get; set; }
    
    [JsonProperty("playcount")]
    public string Playcount { get; set; }
    
    [JsonProperty("image")]
    public List<LastFmImage> Images { get; set; }
}

public class LastFmTopTrack
{
    [JsonProperty("name")]
    public string Name { get; set; }
    
    [JsonProperty("playcount")]
    public string Playcount { get; set; }
    
    [JsonProperty("artist")]
    public LastFmTextField Artist { get; set; }
    
    [JsonProperty("image")]
    public List<LastFmImage> Images { get; set; }
}

public class LastFmTrackInfo
{
    [JsonProperty("name")]
    public string Name { get; set; }
    
    [JsonProperty("artist")]
    public LastFmTextField Artist { get; set; }
    
    [JsonProperty("playcount")]
    public string Playcount { get; set; }
    
    [JsonProperty("userplaycount")]
    public string UserPlaycount { get; set; }
    
    [JsonProperty("listeners")]
    public string Listeners { get; set; }
    
    [JsonProperty("image")]
    public List<LastFmImage> Images { get; set; }
}

public class LastFmArtistInfo
{
    [JsonProperty("name")]
    public string Name { get; set; }
    
    [JsonProperty("stats")]
    public LastFmArtistStats Stats { get; set; }
    
    [JsonProperty("tags")]
    public LastFmArtistTags Tags { get; set; }
    
    [JsonProperty("image")]
    public List<LastFmImage> Images { get; set; }
    
    [JsonProperty("bio")]
    public LastFmArtistBio Bio { get; set; }
}

public class LastFmArtistStats
{
    [JsonProperty("listeners")]
    public string Listeners { get; set; }
    
    [JsonProperty("playcount")]
    public string Playcount { get; set; }
    
    [JsonProperty("userplaycount")]
    public string UserPlaycount { get; set; }
}

public class LastFmArtistTags
{
    [JsonProperty("tag")]
    public List<LastFmArtistTag> TagList { get; set; }
}

public class LastFmArtistTag
{
    [JsonProperty("name")]
    public string Name { get; set; }
    
    [JsonProperty("url")]
    public string Url { get; set; }
}

public class LastFmArtistBio
{
    [JsonProperty("summary")]
    public string Summary { get; set; }
}

public class WhoKnowsEntry
{
    public IGuildUser DiscordUser { get; set; }
    public string LastFmUsername { get; set; }
    public int Playcount { get; set; }
}

public class WhoKnowsResult
{
    public string ArtistName { get; set; }
    public List<WhoKnowsEntry> Entries { get; set; }
    public IGuildUser CrownHolder { get; set; }
    public int CrownPlaycount { get; set; }
    public DateTime? CrownClaimedAt { get; set; }
}

#endregion
