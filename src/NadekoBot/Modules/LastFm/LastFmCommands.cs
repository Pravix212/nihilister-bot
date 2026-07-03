#nullable disable
using Discord;
using NadekoBot.Modules.LastFm.Services;

namespace NadekoBot.Modules.LastFm;

public partial class LastFm : NadekoModule
{
    private readonly LastFmService _svc;
    
    public LastFm(LastFmService svc)
    {
        _svc = svc;
    }
    
    [Cmd]
    public async Task Login([Leftover] string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            await Response().Error("Please provide a Last.fm username.").SendAsync();
            return;
        }
        
        // Verify the user exists on Last.fm
        var userInfo = await _svc.GetUserInfoAsync(username);
        if (userInfo == null)
        {
            await Response().Error($"Could not find a Last.fm user named '{username}'.").SendAsync();
            return;
        }
        
        await _svc.SetUsernameAsync(ctx.User.Id, username);
        await Response().Confirm($"✅ Linked your Discord account to Last.fm user **{userInfo.Name}**.\nTotal scrobbles: **{userInfo.Playcount}**").SendAsync();
    }
    
    [Cmd]
    public async Task Fm(IUser user = null)
    {
        user ??= ctx.User;
        
        var username = await _svc.GetUsernameAsync(user.Id);
        if (string.IsNullOrEmpty(username))
        {
            if (user.Id == ctx.User.Id)
                await Response().Error("You haven't linked your Last.fm account yet. Use `.login <username>` to link it.").SendAsync();
            else
                await Response().Error($"{user.Mention} hasn't linked their Last.fm account yet.").SendAsync();
            return;
        }
        
        var tracks = await _svc.GetRecentTracksAsync(username, 2);
        if (tracks == null || tracks.Count == 0)
        {
            await Response().Error("No recent tracks found.").SendAsync();
            return;
        }
        
        var currentTrack = tracks.FirstOrDefault(t => t.Attr?.NowPlaying == "true") ?? tracks.First();
        
        var embed = new EmbedBuilder()
            .WithAuthor(user.ToString(), user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
            .WithTitle(currentTrack.Name)
            .WithDescription($"by **{currentTrack.Artist?.Text}**\n on **{currentTrack.Album?.Text}**")
            .WithColor(new Color(185, 35, 35));
        
        if (currentTrack.Attr?.NowPlaying == "true")
            embed.WithFooter("🎵 Now Playing • Last.fm");
        else if (currentTrack.Date != null)
            embed.WithFooter($"Last played {currentTrack.Date.Text} • Last.fm");
        else
            embed.WithFooter("Last.fm");
        
        var imageUrl = currentTrack.Images?.FirstOrDefault(i => i.Size == "large")?.Url
            ?? currentTrack.Images?.LastOrDefault()?.Url;
        if (!string.IsNullOrEmpty(imageUrl))
            embed.WithThumbnailUrl(imageUrl);
        
        await ctx.Channel.SendMessageAsync(embed: embed.Build());
    }
    
    [Cmd]
    public async Task TopArtists([Leftover] string period = "overall")
    {
        var validPeriods = new[] { "overall", "7day", "1month", "3month", "6month", "12month" };
        if (!validPeriods.Contains(period.ToLowerInvariant()))
        {
            await Response().Error($"Invalid period. Valid periods: {string.Join(", ", validPeriods)}").SendAsync();
            return;
        }
        
        var username = await _svc.GetUsernameAsync(ctx.User.Id);
        if (string.IsNullOrEmpty(username))
        {
            await Response().Error("You haven't linked your Last.fm account yet. Use `.login <username>` to link it.").SendAsync();
            return;
        }
        
        var artists = await _svc.GetTopArtistsAsync(username, period);
        if (artists == null || artists.Count == 0)
        {
            await Response().Error("No top artists found.").SendAsync();
            return;
        }
        
        var embed = new EmbedBuilder()
            .WithAuthor(ctx.User.ToString(), ctx.User.GetAvatarUrl() ?? ctx.User.GetDefaultAvatarUrl())
            .WithTitle($"Top Artists ({GetPeriodDisplay(period)})")
            .WithColor(new Color(185, 35, 35));
        
        var description = string.Join("\n", artists.Select((a, i) => $"{i + 1}. **{a.Name}** — {a.Playcount} plays"));
        embed.WithDescription(description);
        embed.WithFooter($"Last.fm • {username}");
        
        await ctx.Channel.SendMessageAsync(embed: embed.Build());
    }

    [Cmd]
    public async Task Recent()
    {
        var username = await _svc.GetUsernameAsync(ctx.User.Id);
        if (string.IsNullOrEmpty(username))
        {
            await Response().Error("You haven't linked your Last.fm account yet. Use `.login <username>` to link it.").SendAsync();
            return;
        }

        var tracks = await _svc.GetRecentTracksAsync(username, 10);
        if (tracks == null || tracks.Count == 0)
        {
            await Response().Error("No recent tracks found.").SendAsync();
            return;
        }

        var embed = new EmbedBuilder()
            .WithAuthor(ctx.User.ToString(), ctx.User.GetAvatarUrl() ?? ctx.User.GetDefaultAvatarUrl())
            .WithTitle("Recent Scrobbles")
            .WithColor(new Color(185, 35, 35));

        var lines = tracks.Select((t, i) =>
            $"{i + 1}. **{t.Name}** by **{t.Artist?.Text}** — {t.Date?.Text ?? "Now"}");
        embed.WithDescription(string.Join("\n", lines));
        embed.WithFooter($"Last.fm • {username}");

        await ctx.Channel.SendMessageAsync(embed: embed.Build());
    }

    [Cmd]
    public async Task TopAlbums([Leftover] string period = "overall")
    {
        var validPeriods = new[] { "overall", "7day", "1month", "3month", "6month", "12month" };
        if (!validPeriods.Contains(period.ToLowerInvariant()))
        {
            await Response().Error($"Invalid period. Valid: {string.Join(", ", validPeriods)}").SendAsync();
            return;
        }

        var username = await _svc.GetUsernameAsync(ctx.User.Id);
        if (string.IsNullOrEmpty(username))
        {
            await Response().Error("You haven't linked your Last.fm account yet.").SendAsync();
            return;
        }

        var albums = await _svc.GetTopAlbumsAsync(username, period);
        if (albums == null || albums.Count == 0)
        {
            await Response().Error("No top albums found.").SendAsync();
            return;
        }

        var embed = new EmbedBuilder()
            .WithAuthor(ctx.User.ToString(), ctx.User.GetAvatarUrl() ?? ctx.User.GetDefaultAvatarUrl())
            .WithTitle($"Top Albums ({GetPeriodDisplay(period)})")
            .WithColor(new Color(185, 35, 35));

        var desc = string.Join("\n", albums.Select((a, i) =>
            $"{i + 1}. **{a.Name}** by {a.Artist?.Text} — {a.Playcount} plays"));
        embed.WithDescription(desc);
        embed.WithFooter($"Last.fm • {username}");

        await ctx.Channel.SendMessageAsync(embed: embed.Build());
    }

    [Cmd]
    public async Task TopTracks([Leftover] string period = "overall")
    {
        var validPeriods = new[] { "overall", "7day", "1month", "3month", "6month", "12month" };
        if (!validPeriods.Contains(period.ToLowerInvariant()))
        {
            await Response().Error($"Invalid period. Valid: {string.Join(", ", validPeriods)}").SendAsync();
            return;
        }

        var username = await _svc.GetUsernameAsync(ctx.User.Id);
        if (string.IsNullOrEmpty(username))
        {
            await Response().Error("You haven't linked your Last.fm account yet.").SendAsync();
            return;
        }

        var tracks = await _svc.GetTopTracksAsync(username, period);
        if (tracks == null || tracks.Count == 0)
        {
            await Response().Error("No top tracks found.").SendAsync();
            return;
        }

        var embed = new EmbedBuilder()
            .WithAuthor(ctx.User.ToString(), ctx.User.GetAvatarUrl() ?? ctx.User.GetDefaultAvatarUrl())
            .WithTitle($"Top Tracks ({GetPeriodDisplay(period)})")
            .WithColor(new Color(185, 35, 35));

        var desc = string.Join("\n", tracks.Select((t, i) =>
            $"{i + 1}. **{t.Name}** by {t.Artist?.Text} — {t.Playcount} plays"));
        embed.WithDescription(desc);
        embed.WithFooter($"Last.fm • {username}");

        await ctx.Channel.SendMessageAsync(embed: embed.Build());
    }

    private static string GetPeriodDisplay(string period)
    {
        return period switch
        {
            "7day" => "Last 7 Days",
            "1month" => "Last Mon th",
            "3month" => "Last 3 Months",
            "6month" => "Last 6 Months",
            "12month" => "Last Year",
            _ => "All Time"
        };
    }
}
