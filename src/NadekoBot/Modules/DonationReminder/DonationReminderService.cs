using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Data;
using NadekoBot.Db.Models;

namespace NadekoBot.Services;

public class DonationReminderService : INService
{
    private readonly DiscordSocketClient _client;
    private readonly DbService _db;
    private readonly Timer _timer;

    public DonationReminderService(DiscordSocketClient client, DbService db)
    {
        _client = client;
        _db = db;
        _timer = new Timer(OnTimerTick, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30));
    }

    private async void OnTimerTick(object? state)
    {
        try
        {
            await SendRemindersAsync();
        }
        catch
        {
            // Silent fail
        }
    }

    private async Task SendRemindersAsync()
    {
        using var uow = _db.GetDbContext();
        var now = DateTime.UtcNow;

        var configs = await uow.Set<DonationReminderSettings>()
            .Where(x => x.IsEnabled && x.LastSentAt.AddHours(x.IntervalHours) <= now)
            .ToListAsync();

        foreach (var config in configs)
        {
            var channel = _client.GetChannel(config.ChannelId) as IMessageChannel;
            if (channel == null) continue;

            var guild = _client.GetGuild(config.GuildId);
            if (guild == null) continue;

            var botMember = guild.CurrentUser;
            if (botMember == null) continue;

            var perms = botMember.GetPermissions(channel as IGuildChannel);
            if (!perms.SendMessages) continue;

            try
            {
                await channel.SendMessageAsync(config.Message);
                config.LastSentAt = now;
            }
            catch
            {
                // Skip and try next time
            }
        }

        await uow.SaveChangesAsync();
    }

    public async Task<DonationReminderSettings> GetOrCreateConfigAsync(ulong guildId)
    {
        using var uow = _db.GetDbContext();
        var config = await uow.Set<DonationReminderSettings>()
            .FirstOrDefaultAsync(x => x.GuildId == guildId);

        if (config == null)
        {
            config = new DonationReminderSettings { GuildId = guildId };
            uow.Set<DonationReminderSettings>().Add(config);
            await uow.SaveChangesAsync();
        }

        return config;
    }

    public async Task ToggleAsync(ulong guildId, ulong channelId)
    {
        using var uow = _db.GetDbContext();
        var config = await uow.Set<DonationReminderSettings>()
            .FirstOrDefaultAsync(x => x.GuildId == guildId);

        if (config == null)
        {
            config = new DonationReminderSettings
            {
                GuildId = guildId,
                ChannelId = channelId,
                IsEnabled = true
            };
            uow.Set<DonationReminderSettings>().Add(config);
        }
        else
        {
            config.IsEnabled = !config.IsEnabled;
            if (config.IsEnabled && channelId != 0)
                config.ChannelId = channelId;
        }

        await uow.SaveChangesAsync();
    }

    public async Task SetMessageAsync(ulong guildId, string message)
    {
        using var uow = _db.GetDbContext();
        var config = await uow.Set<DonationReminderSettings>()
            .FirstOrDefaultAsync(x => x.GuildId == guildId);

        if (config == null)
        {
            config = new DonationReminderSettings { GuildId = guildId, Message = message };
            uow.Set<DonationReminderSettings>().Add(config);
        }
        else
        {
            config.Message = message;
        }

        await uow.SaveChangesAsync();
    }

    public async Task SetIntervalAsync(ulong guildId, int hours)
    {
        if (hours < 1 || hours > 168) return;

        using var uow = _db.GetDbContext();
        var config = await uow.Set<DonationReminderSettings>()
            .FirstOrDefaultAsync(x => x.GuildId == guildId);

        if (config == null)
        {
            config = new DonationReminderSettings { GuildId = guildId, IntervalHours = hours };
            uow.Set<DonationReminderSettings>().Add(config);
        }
        else
        {
            config.IntervalHours = hours;
        }

        await uow.SaveChangesAsync();
    }
}
