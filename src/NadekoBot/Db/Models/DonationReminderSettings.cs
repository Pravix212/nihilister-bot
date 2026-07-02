using System;
using System.ComponentModel.DataAnnotations;

namespace NadekoBot.Db.Models;

public class DonationReminderSettings
{
    [Key]
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public string Message { get; set; } = "If you enjoy using this bot, feel free to support the project! https://prav.lol/nihilister/donate";
    public int IntervalHours { get; set; } = 4;
    public bool IsEnabled { get; set; }
    public DateTime LastSentAt { get; set; } = DateTime.MinValue;
}
