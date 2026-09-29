using Nihilister.Modules.Utility.UserNotifications;

namespace Nihilister.Modules.Utility;

public sealed class GiveawayNotifyRegistrar : INService, IUserNotifyEventRegistrar
{
    public IReadOnlyList<UserNotifyEventInfo> GetEvents() =>
    [
        new("giveaway.won", strs.notify_giveaway_won),
    ];
}
