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
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(15));
            return Task.CompletedTask;
        }

        private void UpdateStatus()
        {
            try
            {
                var uptime = DateTime.UtcNow - _startTime;
<<<<<<< HEAD
                var status = $"Uptime: {uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
=======
                var status = $"Uptime: {uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
>>>>>>> e5f4ed1fcb7f1daffb327ec2492faffdf8e5e0cb
                _ = _client.SetGameAsync(status, type: ActivityType.Watching);
            }
            catch { /* ignore */ }
        }
    }
}
