using Nihilister.Db.Models;

namespace Nihilister.Modules.Xp.Services;

public interface IUserService
{
    Task<DiscordUser?> GetUserAsync(ulong userId);
}