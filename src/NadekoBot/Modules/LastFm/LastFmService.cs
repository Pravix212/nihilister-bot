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
        
        return await _cache.GetOrAddAsync(
            new TypedKey<List<LastFmTrack>>($"lastfm_recent_{username}_{limit}"),
            async () => await GetRecentTracksFactoryAsync(username, limit),
            TimeSpan.FromMinutes(2));
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
            TimeSpan.FromMinutes(5));
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

}

    #endregion

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

    #endregion
