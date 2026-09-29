#nullable disable
using Nihilister.Modules.Gambling.Common.Blackjack;

namespace Nihilister.Modules.Gambling.Services;

public class BlackJackService : INService
{
    public ConcurrentDictionary<ulong, Blackjack> Games { get; } = new();
}