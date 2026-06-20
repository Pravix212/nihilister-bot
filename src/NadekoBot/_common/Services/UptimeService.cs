using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using NadekoBot.Common.ModuleBehaviors;

namespace NadekoBot.Services
{
    public class UptimeService : INService, IReadyExecutor
    {
        private readonly DiscordSocketClient _client;
        private Timer? _timer;
        private readonly DateTime _startTime;

        public UptimeService(DiscordSocketClient client)
        {
            _client = client;
            _startTime = DateTime.UtcNow;
        }

        public Task OnReadyAsync()
        {
            _timer = new Timer(
                _ => UpdateStatus(),
                null,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(1));
            return Task.CompletedTask;
        }

        private void UpdateStatus()
        {
            try
            {
                var uptime = DateTime.UtcNow - _startTime;
                var status = $"Watching over Heathen's Garden - Uptime: {uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
                _ = _client.SetGameAsync(status);
            }
            catch { /* ignore */ }
        }
    }
}