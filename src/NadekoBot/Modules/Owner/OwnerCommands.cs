using NadekoBot.Modules.Gambling.Services;
using NadekoBot.Modules.Patronage;

namespace NadekoBot.Modules.Owner;

[OwnerOnly]
public partial class Owner(VoteRewardService vrs, IPatronageService ps) : NadekoModule
{
    [Cmd]
    public async Task VoteFeed()
    {
        vrs.SetVoiceChannel(ctx.Channel);
        await ctx.OkAsync();
    }

    [Cmd]
    public async Task PatronAdd(long cents, ulong userId)
    {
        if (!ps.GetConfig().IsEnabled)
        {
            await Response().Error(strs.patron_not_enabled).SendAsync();
            return;
        }

        if (cents <= 0)
        {
            await Response().Error(strs.patron_add_invalid_amount).SendAsync();
            return;
        }

        var maybePatron = await ps.AddManualPatronAsync(userId, cents);
        if (maybePatron is not { } patron)
        {
            await Response().Error(strs.patron_add_failed).SendAsync();
            return;
        }

        var eb = CreateEmbed()
            .WithOkColor()
            .WithTitle(GetText(strs.patron_added))
            .AddField(GetText(strs.tier), Format.Bold(patron.Tier.ToFullName()), true)
            .AddField(GetText(strs.pledge), $"**{patron.Amount / 100.0f:N1}$**", true)
            .AddField(GetText(strs.expires),
                patron.ValidThru.AddDays(1).ToShortAndRelativeTimestampTag(),
                true);

        await Response().Embed(eb).SendAsync();
    }

    private static CancellationTokenSource? _cts = null;

    [Cmd]
    public async Task MassPing()
    {
        if (_cts is { } t)
        {
            await t.CancelAsync();
        }
        _cts = new();

        try
        {
            var users = await ctx.Guild.GetUsersAsync().Pipe(u => u.Where(x => !x.IsBot).ToArray());

            var currentIndex = 0;
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var batch = users[currentIndex..(currentIndex += 50)];

                    var mentions = batch.Select(x => x.Mention).Join(" ");
                    var msg = await ctx.Channel.SendMessageAsync(mentions, allowedMentions: AllowedMentions.All);
                    msg.DeleteAfter(3);
                }
                catch
                {
                    // ignored
                }

                await Task.Delay(2500);
            }
        }
        finally
        {
            _cts = null;
        }
    }

    private Discord.WebSocket.SocketTextChannel FindBestChannel(Discord.WebSocket.SocketGuild guild)
    {
        var textChannels = guild.TextChannels
            .Where(c => guild.CurrentUser.GetPermissions(c).SendMessages)
            .OrderBy(c => c.Position)
            .ToList();

        if (!textChannels.Any()) return null;

        string[] avoidKeywords = { "rules", "rule", "announcement", "news", "welcome", "log", "admin", "mod", "info", "faq", "update" };
        var candidateChannels = textChannels.Where(c => !avoidKeywords.Any(k => c.Name.Contains(k, StringComparison.OrdinalIgnoreCase))).ToList();
        
        if (!candidateChannels.Any()) 
            candidateChannels = textChannels;

        string[] preferredKeywords = { "chat", "general", "main", "talk", "discussion", "nikke-text" };
        string[] exactPreferredNames = { "💬𝒞𝒽𝒶𝓉", "┌ㆍ𝑪𝒉𝒂𝒕", "chat", "general" };

        var bestMatch = candidateChannels.FirstOrDefault(c => exactPreferredNames.Any(n => string.Equals(c.Name, n, StringComparison.OrdinalIgnoreCase)));
        if (bestMatch != null) return bestMatch;

        bestMatch = candidateChannels.FirstOrDefault(c => 
        {
            var normName = c.Name.Normalize(System.Text.NormalizationForm.FormKC).ToLowerInvariant();
            return preferredKeywords.Any(k => normName.Contains(k));
        });
        if (bestMatch != null) return bestMatch;

        var defaultChannel = guild.DefaultChannel;
        if (defaultChannel != null && candidateChannels.Contains(defaultChannel))
            return defaultChannel;

        var systemChannel = guild.SystemChannel;
        if (systemChannel != null && candidateChannels.Contains(systemChannel))
            return systemChannel;

        return candidateChannels.FirstOrDefault();
    }

    [Cmd]
    public async Task GlobalBroadcast([Leftover] string message)
    {
        var client = (Discord.WebSocket.DiscordSocketClient)ctx.Client;
        var guilds = client.Guilds;
        int count = 0;
        foreach (var guild in guilds)
        {
            var channel = FindBestChannel(guild);
            if (channel != null)
            {
                try
                {
                    await channel.SendMessageAsync(message);
                    count++;
                }
                catch 
                {
                    // Ignore missing permissions or other errors
                }
            }
            await Task.Delay(1000); // 1 second delay per guild to avoid rate limits
        }
        await ctx.Channel.SendMessageAsync($"Broadcasted message to {count}/{guilds.Count} servers.");
    }
}