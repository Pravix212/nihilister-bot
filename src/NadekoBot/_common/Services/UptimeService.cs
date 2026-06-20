using Discord;
using Discord.WebSocket;
using NadekoBot.Common;

namespace NadekoBot.Services;

public class UptimeService : INService
{
    private readonly DiscordSocketClient _client;
    private readonly DateTime _startTime = DateTime.UtcNow;
    private Timer? _timer;

    public UptimeService(DiscordSocketClient client)
    {
        _client = client;
    }

    public Task StartAsync()
    {
        _timer = new Timer(UpdateStatus, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        return Task.CompletedTask;
    }

    private void UpdateStatus(object? state)
    {
        var uptime = DateTime.UtcNow - _startTime;
        var uptimeStr = $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
        var status = $"over Heathen's Garden | Uptime: {uptimeStr}";
        _client.SetGameAsync(status, type: ActivityType.Watching);
    }
}