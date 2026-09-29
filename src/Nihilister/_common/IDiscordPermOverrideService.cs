#nullable disable
namespace Nihilister.Common;

public interface IDiscordPermOverrideService
{
    bool TryGetOverrides(ulong guildId, string commandName, out Nihilister.Db.GuildPerm? perm);
}