#nullable disable
using Nihilister.Modules.Gambling.Common.AnimalRacing;

namespace Nihilister.Modules.Gambling.Services;

public class AnimalRaceService : INService
{
    public ConcurrentDictionary<ulong, AnimalRace> AnimalRaces { get; } = new();
}