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
        var artistName = currentTrack.Artist?.Text;
        var trackName = currentTrack.Name;

        // Get the user's playcount for this specific track
        var trackInfo = !string.IsNullOrEmpty(artistName) && !string.IsNullOrEmpty(trackName)
            ? await _svc.GetTrackInfoAsync(artistName, trackName, username)
            : null;

        var embed = new EmbedBuilder()
            .WithAuthor(user.ToString(), user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
            .WithTitle(currentTrack.Name)
            .WithDescription($"by **{artistName}**\n on **{currentTrack.Album?.Text}**")
            .WithColor(new Color(185, 35, 35));

        // Build footer with playcount
        var footerParts = new List<string>();
        if (currentTrack.Attr?.NowPlaying == "true")
            footerParts.Add("🎵 Now Playing");
        else if (currentTrack.Date != null)
            footerParts.Add($"Last played {currentTrack.Date.Text}");

        if (trackInfo?.UserPlaycount != null)
            footerParts.Add($"▶ {trackInfo.UserPlaycount} scrobbles");

        footerParts.Add("Last.fm");
        embed.WithFooter(string.Join(" • ", footerParts));

        var imageUrl = currentTrack.Images?.FirstOrDefault(i => i.Size == "large")?.Url
            ?? currentTrack.Images?.LastOrDefault()?.Url;
        if (!string.IsNullOrEmpty(imageUrl))
            embed.WithThumbnailUrl(imageUrl);

        var sentMessage = await ctx.Channel.SendMessageAsync(embed: embed.Build());

        // Add reactions so others can vote
        await sentMessage.AddReactionAsync(new Emoji("👍"));
        await sentMessage.AddReactionAsync(new Emoji("👎"));
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

    [Cmd]
    public async Task FmLyrics(IUser user = null)
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

        // Get the most recent track
        var tracks = await _svc.GetRecentTracksAsync(username, 1);
        if (tracks == null || tracks.Count == 0)
        {
            await Response().Error("No recent tracks found.").SendAsync();
            return;
        }

        var currentTrack = tracks.First();
        var artist = currentTrack.Artist?.Text;
        var track = currentTrack.Name;

        if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(track))
        {
            await Response().Error("Could not determine the track name.").SendAsync();
            return;
        }

        await Response().Pending($"Searching lyrics for **{track}** by **{artist}**...").SendAsync();

        var lyrics = await _svc.GetLyricsAsync(artist, track);
        if (string.IsNullOrWhiteSpace(lyrics))
        {
            await Response().Error($"Could not find lyrics for **{track}** by **{artist}**.").SendAsync();
            return;
        }

        // Discord embed description limit is 4096 chars
        if (lyrics.Length > 4000)
            lyrics = lyrics[..4000] + "\n\n... (truncated)";

        var embed = new EmbedBuilder()
            .WithTitle($"🎵 {track}")
            .WithDescription($"by **{artist}**\n\n{lyrics}")
            .WithColor(new Color(185, 35, 35))
            .WithFooter($"Requested by {ctx.User.Username} • Powered by lyrics.ovh");

        await ctx.Channel.SendMessageAsync(embed: embed.Build());
    }

    [Cmd]
    public async Task WhoKnows([Leftover] string artist = null)
    {
        // If no artist provided, try to get it from the user's current track
        if (string.IsNullOrWhiteSpace(artist))
        {
            var username = await _svc.GetUsernameAsync(ctx.User.Id);
            if (string.IsNullOrEmpty(username))
            {
                await Response().Error("You haven't linked your Last.fm account yet. Use `.login <username>` to link it.\nOr provide an artist name: `.whoknows <artist>`").SendAsync();
                return;
            }

            var tracks = await _svc.GetRecentTracksAsync(username, 1);
            if (tracks == null || tracks.Count == 0)
            {
                await Response().Error("No recent tracks found. Please provide an artist name: `.whoknows <artist>`").SendAsync();
                return;
            }

            artist = tracks.First().Artist?.Text;
            if (string.IsNullOrWhiteSpace(artist))
            {
                await Response().Error("Could not determine artist from your recent tracks. Please provide one: `.whoknows <artist>`").SendAsync();
                return;
            }
        }

        await Response().Pending($"Searching who knows **{artist}** in **{ctx.Guild.Name}**...").SendAsync();

        // Get all guild members who have linked Last.fm
        var guildUsers = await ctx.Guild.GetUsersAsync();
        var result = await _svc.GetWhoKnowsAsync(ctx.Guild.Id, artist, guildUsers);

        if (result == null || result.Entries.Count == 0)
        {
            await Response().Error($"No one in this server has scrobbled **{artist}**.").SendAsync();
            return;
        }

        // Get artist info for image and tags
        var artistInfo = await _svc.GetArtistInfoAsync(artist);

        var embed = new EmbedBuilder()
            .WithTitle($"{result.ArtistName} in {ctx.Guild.Name}")
            .WithColor(new Color(185, 35, 35));

        // Build the leaderboard description
        var lines = new List<string>();
        
        // Crown holder gets special treatment
        var crownEntry = result.Entries.First();
        lines.Add($"👑 **{crownEntry.DiscordUser.DisplayName ?? crownEntry.DiscordUser.Username}** — **{crownEntry.Playcount}** plays");
        
        // Rest of the leaderboard
        foreach (var entry in result.Entries.Skip(1).Take(14)) // Show top 15 total
        {
            lines.Add($"**{entry.DiscordUser.DisplayName ?? entry.DiscordUser.Username}** — {entry.Playcount} plays");
        }

        embed.WithDescription(string.Join("\n", lines));

        // Crown claimed message
        var crownMessage = $"Crown claimed by **{crownEntry.DiscordUser.DisplayName ?? crownEntry.DiscordUser.Username}**!";
        if (result.CrownClaimedAt.HasValue)
        {
            var daysAgo = (DateTime.UtcNow - result.CrownClaimedAt.Value).TotalDays;
            if (daysAgo < 1)
                crownMessage += " *(just now)*";
            else if (daysAgo < 2)
                crownMessage += " *(yesterday)*";
            else
                crownMessage += $" *({(int)daysAgo} days ago)*";
        }
        embed.AddField("\u200B", crownMessage);

        // Add genre tags if available
        var tags = artistInfo?.Tags?.TagList?.Take(3).Select(t => t.Name).ToList();
        if (tags != null && tags.Count > 0)
        {
            embed.AddField("Tags", string.Join(" • ", tags), inline: true);
        }

        // Artist image
        var imageUrl = artistInfo?.Images?.FirstOrDefault(i => i.Size == "large")?.Url
            ?? artistInfo?.Images?.LastOrDefault()?.Url;
        if (!string.IsNullOrEmpty(imageUrl))
            embed.WithThumbnailUrl(imageUrl);

        embed.WithFooter($"{result.Entries.Count} listener{(result.Entries.Count != 1 ? "s" : "")} • Last.fm");

        await ctx.Channel.SendMessageAsync(embed: embed.Build());
    }

    private static string GetPeriodDisplay(string period)
    {
        return period switch
        {
            "7day" => "Last 7 Days",
            "1month" => "Last Month",
            "3month" => "Last 3 Months",
            "6month" => "Last 6 Months",
            "12month" => "Last Year",
            _ => "All Time"
        };
    }
}
