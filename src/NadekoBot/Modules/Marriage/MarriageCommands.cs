using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;

namespace NadekoBot.Modules.Marriage;

public partial class Marriage : NadekoModule
{
    private readonly MarriageService _svc;

    public Marriage(DbService db)
    {
        _svc = new MarriageService(db);
    }

    private async Task CheckAndNotifyExpiredAsync()
    {
        var expired = await _svc.CleanupExpiredProposals();
        foreach (var proposal in expired)
        {
            if (proposal.ChannelId == 0) continue;
            try
            {
                var guild = await ctx.Client.GetGuildAsync(proposal.GuildId);
                var channel = await guild.GetTextChannelAsync(proposal.ChannelId);
                var proposer = await ctx.Client.GetUserAsync(proposal.ProposerId);
                var targetUser = await ctx.Client.GetUserAsync(proposal.TargetId);
                if (proposal.Type == "marriage")
                    await channel.SendMessageAsync($"ðŸ’” {proposer?.Mention ?? "Someone"}'s marriage proposal to {targetUser?.Mention ?? "someone"} expired after 1 minute! Try again with `.marry`.");
                else
                    await channel.SendMessageAsync($"ðŸ‘¨â€ðŸ‘©â€ðŸ‘§ {proposer?.Mention ?? "Someone"}'s adoption proposal for {targetUser?.Mention ?? "someone"} expired after 1 minute! Try again with `.adopt`.");
            }
            catch { /* channel/guild access issue */ }
        }
    }

    private void ScheduleExpirationTimer(ulong proposerId, ulong targetId, string type)
    {
        var client = ctx.Client;
        var guildId = ctx.Guild.Id;
        var channelId = ctx.Channel.Id;
        _ = Task.Run(async () =>
        {
            await Task.Delay(MarriageService.PROPOSAL_TIMEOUT);
            var expired = await _svc.TryExpireSpecificProposalAsync(proposerId, targetId, type);
            if (expired)
            {
                try
                {
                    var guild = await client.GetGuildAsync(guildId);
                    var channel = await guild.GetTextChannelAsync(channelId);
                    var proposer = await client.GetUserAsync(proposerId);
                    var targetUser = await client.GetUserAsync(targetId);
                    if (type == "marriage")
                        await channel.SendMessageAsync($"ðŸ’” {proposer?.Mention ?? "Someone"}'s marriage proposal to {targetUser?.Mention ?? "someone"} expired after 1 minute! Try again with `.marry`.");
                    else
                        await channel.SendMessageAsync($"ðŸ‘¨â€ðŸ‘©â€ðŸ‘§ {proposer?.Mention ?? "Someone"}'s adoption proposal for {targetUser?.Mention ?? "someone"} expired after 1 minute! Try again with `.adopt`.");
                }
                catch { }
            }
        });
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task Marry([Leftover] IUser? target = null)
    {
        await CheckAndNotifyExpiredAsync();

        if (target == null || target.Id == ctx.User.Id)
        {
            await Response().Error("Mention someone to marry! Example: .marry @user").SendAsync();
            return;
        }

        if (target.IsBot)
        {
            await Response().Error("You can't marry a bot! ðŸ’”").SendAsync();
            return;
        }

        if (await _svc.IsMarriedAsync(ctx.User.Id))
        {
            await Response().Error("You're already married! Use .divorce first.").SendAsync();
            return;
        }

        if (await _svc.IsMarriedAsync(target.Id))
        {
            await Response().Error($"{target.Mention} is already married! ðŸ’”").SendAsync();
            return;
        }

        // Check if I already proposed to the target
        var proposerToTarget = await _svc.GetPendingProposalAsync(target.Id);
        if (proposerToTarget == ctx.User.Id)
        {
            await Response().Error("You already proposed to them! Wait for them to accept. (Expires in 1 minute)").SendAsync();
            return;
        }

        if (proposerToTarget != null)
        {
            await Response().Error($"{target.Mention} already has a pending proposal from someone else!").SendAsync();
            return;
        }

        await _svc.ProposeAsync(ctx.User.Id, target.Id);
        MarriageService.ProposalChannels.Set(ctx.User.Id, target.Id, "marriage", ctx.Channel.Id, ctx.Guild.Id);
        ScheduleExpirationTimer(ctx.User.Id, target.Id, "marriage");

        await Response().Confirm($"ðŸ’ {ctx.User.Mention} has proposed to {target.Mention}!\n\n{target.Mention}, type `.accept` to accept! (Expires in 1 minute)").SendAsync();
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task Accept()
    {
        await CheckAndNotifyExpiredAsync();

        // Check for marriage proposal first
        var marriageProposer = await _svc.GetPendingProposalAsync(ctx.User.Id);
        if (marriageProposer.HasValue)
        {
            if (await _svc.TryAcceptAsync(ctx.User.Id, marriageProposer.Value))
            {
                var proposer = await ctx.Guild.GetUserAsync(marriageProposer.Value);
                await Response().Confirm($"ðŸ’• {ctx.User.Mention} and {proposer?.Mention ?? "someone"} are now married! ðŸ’").SendAsync();
                return;
            }
        }

        // Check for adoption proposal
        var adoptionProposer = await _svc.GetAdoptionProposalAsync(ctx.User.Id);
        if (adoptionProposer.HasValue)
        {
            await _svc.AcceptAdoptionAsync(adoptionProposer.Value, ctx.User.Id);
            var proposer = await ctx.Guild.GetUserAsync(adoptionProposer.Value);
            var spouseId = await _svc.GetSpouseAsync(adoptionProposer.Value);
            var spouse = spouseId != null ? await ctx.Guild.GetUserAsync(spouseId.Value) : null;
            var msg = spouse != null
                ? $"{proposer?.Mention ?? "Someone"} and {spouse.Mention} have adopted {ctx.User.Mention}! Welcome to the family!"
                : $"{proposer?.Mention ?? "Someone"} has adopted {ctx.User.Mention}! Welcome to the family!";
            await Response().Confirm(msg).SendAsync();
            return;
        }

        await Response().Error("You don't have any pending proposals to accept! ðŸ’”").SendAsync();
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public Task Wife() => SpouseAsync();

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public Task Husband() => SpouseAsync();

    private async Task SpouseAsync()
    {
        await CheckAndNotifyExpiredAsync();

        var spouseId = await _svc.GetSpouseAsync(ctx.User.Id);
        if (spouseId == null)
        {
            await Response().Error("You're not married! ðŸ’” Use .marry @user to find your soulmate.").SendAsync();
            return;
        }

        var spouse = await ctx.Guild.GetUserAsync(spouseId.Value);
        var duration = await _svc.GetDurationAsync(ctx.User.Id);
        var durationStr = duration.HasValue ? FormatDuration(duration.Value) : "just now";

        var embed = new EmbedBuilder()
            .WithTitle("ðŸ’• Marriage Certificate")
            .WithDescription($"**{ctx.User.Username}** is married to **{spouse?.Username ?? "Unknown User"}**\n\nMarried for: **{durationStr}** ðŸ’")
            .WithColor(Color.Magenta)
            .WithThumbnailUrl(spouse?.GetAvatarUrl() ?? ctx.User.GetAvatarUrl())
            .WithFooter("Use .divorce to end the marriage")
            .Build();

        await ctx.Channel.SendMessageAsync(embed: embed);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task Divorce()
    {
        await CheckAndNotifyExpiredAsync();

        var spouseId = await _svc.GetSpouseAsync(ctx.User.Id);
        if (spouseId == null)
        {
            await Response().Error("You're not married! ðŸ’”").SendAsync();
            return;
        }

        var marriage = await _svc.GetMarriageAsync(ctx.User.Id);
        if (marriage != null)
        {
            var timeSinceMarriage = DateTime.UtcNow - marriage.MarriedAt;
            if (timeSinceMarriage < TimeSpan.FromHours(1))
            {
                var remaining = TimeSpan.FromHours(1) - timeSinceMarriage;
                await Response().Error($"You can only divorce after 1 hour! Time remaining: **{FormatDuration(remaining)}** ðŸ’”").SendAsync();
                return;
            }
        }

        var spouse = await ctx.Guild.GetUserAsync(spouseId.Value);
        await _svc.DivorceAsync(ctx.User.Id);
        await Response().Confirm($"ðŸ’” {ctx.User.Mention} has divorced {spouse?.Mention ?? "their spouse"}. The marriage is over.").SendAsync();
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task Lovequiz()
    {
        await CheckAndNotifyExpiredAsync();

        var spouseId = await _svc.GetSpouseAsync(ctx.User.Id);
        if (spouseId == null)
        {
            await Response().Error("You need to be married to take the love quiz! ðŸ’”").SendAsync();
            return;
        }

        var spouse = await ctx.Guild.GetUserAsync(spouseId.Value);
        var duration = await _svc.GetDurationAsync(ctx.User.Id);
        var durationStr = duration.HasValue ? FormatDuration(duration.Value) : "just now";

        var seed = (long)ctx.User.Id + (long)spouseId.Value + (long)DateTime.UtcNow.Date.Ticks;
        var rng = new Random((int)(seed & 0x7FFFFFFF));
        var score = rng.Next(60, 101);

        var rating = score switch
        {
            >= 95 => "Soulmates ðŸ’•âœ¨",
            >= 85 => "Perfect Match ðŸ’•",
            >= 75 => "Great Couple ðŸ’–",
            >= 65 => "Good Match ðŸ’",
            _ => "Work in Progress ðŸ’—"
        };

        var embed = new EmbedBuilder()
            .WithTitle("ðŸ’• Love Quiz Results")
            .WithDescription($"**{ctx.User.Username}** + **{spouse?.Username ?? "Unknown"}**\n\nCompatibility Score: **{score}/100**\nRating: **{rating}**\n\nMarried for: **{durationStr}** ðŸ’")
            .WithColor(score >= 85 ? Color.Gold : Color.Magenta)
            .Build();

        await ctx.Channel.SendMessageAsync(embed: embed);
    }

    private static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalDays >= 1) return $"{ts.Days}d {ts.Hours}h {ts.Minutes}m";
        if (ts.TotalHours >= 1) return $"{ts.Hours}h {ts.Minutes}m";
        return $"{ts.Minutes}m {ts.Seconds}s";
    }
}