using System;
using System.ComponentModel.DataAnnotations;

namespace NadekoBot.Db.Models;

public class DonationReminderSettings
{
    [Key]
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public string Message { get; set; } = "If you enjoy using this bot, feel free to support the project!\n\n**Monthly Costs:**\n• DigitalOcean VPS — $6-12/month\n• Grok AI API — $15-30/month\n• Domain & SSL — $10/year";
    public int IntervalHours { get; set; } = 4;
    public bool IsEnabled { get; set; }
    public DateTime LastSentAt { get; set; } = DateTime.MinValue;
}
