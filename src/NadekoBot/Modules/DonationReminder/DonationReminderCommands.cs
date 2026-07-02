using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using NadekoBot.Services;

namespace NadekoBot.Modules;

public class DonationReminderCommands : NadekoModule<DonationReminderService>
{
    [Cmd]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task DonationReminder()
    {
        var newState = await _service.ToggleAsync(ctx.Guild.Id, ctx.Channel.Id);
        if (newState)
        {
            var config = await _service.GetOrCreateConfigAsync(ctx.Guild.Id);
            
            var previewEmbed = new EmbedBuilder()
                .WithColor(new Color(0x5865F2))
                .WithTitle("Support Nihilister")
                .WithDescription(config.Message)
                .WithFooter("Thank you for supporting the Heathen's Garden ❤️")
                .Build();

            var button = new ButtonBuilder(
                label: "💰 Donate",
                url: "https://prav.lol/nihilister/donate.html",
                style: ButtonStyle.Link
            );

            var components = new ComponentBuilder()
                .WithButton(button)
                .Build();

            await ctx.Channel.SendMessageAsync(
                content: $"✅ Donation reminder **enabled** in <#{ctx.Channel.Id}>.",
                embed: previewEmbed,
                components: components);
        }
        else
        {
            await Response()
                .Confirm($"Donation reminder **disabled** in <#{ctx.Channel.Id}>.")
                .SendAsync();
        }
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task DonationReminderMessage([Leftover] string message)
    {
        await _service.SetMessageAsync(ctx.Guild.Id, message);
        await Response()
            .Confirm($"Donation reminder message set to:\n{message}")
            .SendAsync();
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task DonationReminderInterval(int hours)
    {
        if (hours < 1 || hours > 168)
        {
            await Response().Error("Interval must be between 1 and 168 hours.").SendAsync();
            return;
        }

        await _service.SetIntervalAsync(ctx.Guild.Id, hours);
        await Response()
            .Confirm($"Donation reminder interval set to **{hours} hour(s)**.")
            .SendAsync();
    }
}