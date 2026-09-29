using Microsoft.EntityFrameworkCore;
using Nihilister.Db.Models;

namespace Nihilister.Extensions;

public static class DbExtensions
{
    public static DiscordUser GetOrCreateUser(this DbContext ctx, IUser original, Func<IQueryable<DiscordUser>, IQueryable<DiscordUser>>? includes = null)
        => ctx.GetOrCreateUser(original.Id, original.Username, original.AvatarId, includes);
}