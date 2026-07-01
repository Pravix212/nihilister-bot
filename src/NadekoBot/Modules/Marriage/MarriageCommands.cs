using System;
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

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task Marry([Leftover] IUser? target = null)
    {
        if (target == null || target.Id == ctx.User.Id)
        {
            await Response().Error("Mention someone to marry! Example: .marry @user").SendAsync();
            return;
        }

        if (target.IsBot)
        {
            await Response().Error("You can't marry a bot! 💔").SendAsync();
            return;
        }

        if (await _svc.IsMarriedAsync(ctx.User.Id))
        {
            await Response().Error("You're already married! Use .divorce first.").SendAsync();
            return;
        }

        if (await _svc.IsMarriedAsync(target.Id))
        {
            await Response().Error($"{target.Mention} is already married! 💔").SendAsync();
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
        await Response().Confirm($"💍 {ctx.User.Mention} has proposed to {target.Mention}!\n\n{target.Mention}, type `.accept` to accept! (Expires in 1 minute)").SendAsync();
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task Accept()
    {
        // Check for marriage proposal first
        var marriageProposer = await _svc.GetPendingProposalAsync(ctx.User.Id);
        if (marriageProposer.HasValue)
        {
            if (await _svc.TryAcceptAsync(ctx.User.Id, marriageProposer.Value))
            {
                var proposer = await ctx.Guild.GetUserAsync(marriageProposer.Value);
                await Response().Confirm($"💕 {ctx.User.Mention} and {proposer?.Mention ?? "someone"} are now married! 💍").SendAsync();
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

        await Response().Error("You don't have any pending proposals to accept! 💔").SendAsync();
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public Task Wife() => SpouseAsync();

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public Task Husband() => SpouseAsync();

    private async Task SpouseAsync()
    {
        var spouseId = await _svc.GetSpouseAsync(ctx.User.Id);
        if (spouseId == null)
        {
            await Response().Error("You're not married! 💔 Use .marry @user to find your soulmate.").SendAsync();
            return;
        }

        var spouse = await ctx.Guild.GetUserAsync(spouseId.Value);
        var duration = await _svc.GetDurationAsync(ctx.User.Id);
        var durationStr = duration.HasValue ? FormatDuration(duration.Value) : "just now";

        var embed = new EmbedBuilder()
            .WithTitle("💕 Marriage Certificate")
            .WithDescription($"**{ctx.User.Username}** is married to **{spouse?.Username ?? "Unknown User"}**\n\nMarried for: **{durationStr}** 💍")
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
        var spouseId = await _svc.GetSpouseAsync(ctx.User.Id);
        if (spouseId == null)
        {
            await Response().Error("You're not married! 💔").SendAsync();
            return;
        }

        var marriage = await _svc.GetMarriageAsync(ctx.User.Id);
        if (marriage != null)
        {
            var timeSinceMarriage = DateTime.UtcNow - marriage.MarriedAt;
            if (timeSinceMarriage < TimeSpan.FromHours(1))
            {
                var remaining = TimeSpan.FromHours(1) - timeSinceMarriage;
                await Response().Error($"You can only divorce after 1 hour! Time remaining: **{FormatDuration(remaining)}** 💔").SendAsync();
                return;
            }
        }

        var spouse = await ctx.Guild.GetUserAsync(spouseId.Value);
        await _svc.DivorceAsync(ctx.User.Id);
        await Response().Confirm($"💔 {ctx.User.Mention} has divorced {spouse?.Mention ?? "their spouse"}. The marriage is over.").SendAsync();
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task Lovequiz()
    {
        var spouseId = await _svc.GetSpouseAsync(ctx.User.Id);
        if (spouseId == null)
        {
            await Response().Error("You need to be married to take the love quiz! 💔").SendAsync();
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
            >= 95 => "Soulmates 💕✨",
            >= 85 => "Perfect Match 💕",
            >= 75 => "Great Couple 💖",
            >= 65 => "Good Match 💝",
            _ => "Work in Progress 💗"
        };

        var embed = new EmbedBuilder()
            .WithTitle("💕 Love Quiz Results")
            .WithDescription($"**{ctx.User.Username}** + **{spouse?.Username ?? "Unknown"}**\n\nCompatibility Score: **{score}/100**\nRating: **{rating}**\n\nMarried for: **{durationStr}** 💍")
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
