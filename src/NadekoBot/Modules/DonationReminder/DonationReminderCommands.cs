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
        var config = await _service.GetOrCreateConfigAsync(ctx.Guild.Id);
        var newState = !config.IsEnabled;
        await _service.ToggleAsync(ctx.Guild.Id, ctx.Channel.Id);
        await Response()
            .Confirm($"Donation reminder **{(newState ? "enabled" : "disabled")}** in <#{ctx.Channel.Id}>.")
            .SendAsync();
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
