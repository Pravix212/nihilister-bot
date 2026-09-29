#nullable disable
using Nihilister.Db.Models;

namespace Nihilister.Modules.Utility.Services;

public interface IRemindService
{
    Task AddReminderAsync(ulong userId,
        ulong targetId,
        ulong? guildId,
        bool isPrivate,
        DateTime time,
        string message,
        ReminderType reminderType);
}