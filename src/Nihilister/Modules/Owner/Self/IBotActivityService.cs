#nullable disable
using Nihilister.Db.Models;

namespace Nihilister.Modules.Owner.Services;

public interface IBotActivityService
{
    Task SetActivityAsync(string game, ActivityType? type);
    Task SetStreamAsync(string name, string link);
    bool ToggleRotatePlaying();
    Task AddPlaying(ActivityType statusType, string status);
    Task<string> RemovePlayingAsync(int index);
    IReadOnlyList<RotatingPlayingStatus> GetRotatingStatuses();
}