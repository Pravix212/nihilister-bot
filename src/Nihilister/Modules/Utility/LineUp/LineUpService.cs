using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Nihilister.Modules.Utility.LineUp;

/// <summary>
/// Service responsible for managing channel lineups.
/// </summary>
public class LineUpService(DbService db) : INService
{
    /// <summary>
    /// Tries to add a user to the lineup for a specific channel.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="userId">The ID of the user joining.</param>
    /// <param name="reason">Optional reason provided by the user.</param>
    /// <returns>A tuple indicating success, the user's position if successful, or 0 otherwise.</returns>
    public async Task<(bool Success, int Position)> TryJoinLineupAsync(
        ulong guildId,
        ulong channelId,
        ulong userId,
        string? reason)
    {
        await using var ctx = db.GetDbContext();

        var exists = await ctx.GetTable<LineUpUser>()
                           .AnyAsyncLinqToDB(lu => lu.GuildId == guildId && lu.ChannelId == channelId && lu.UserId == userId);

        if (exists)
            return (false, 0);

        var dateAdded = DateTime.UtcNow;
        await ctx.GetTable<LineUpUser>().InsertAsync(() => new LineUpUser
        {
            GuildId = guildId,
            ChannelId = channelId,
            UserId = userId,
            Reason = reason,
            DateAdded = dateAdded
        });

        var position = await GetPositionAsync(guildId, channelId, userId);
        return (true, position ?? 0); // Position should not be null here, but handle defensively
    }

    /// <summary>
    /// Tries to remove a user from the lineup for a specific channel.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="userId">The ID of the user leaving or being removed.</param>
    /// <returns>True if the user was successfully removed, false otherwise.</returns>
    public async Task<bool> TryLeaveLineupAsync(ulong guildId, ulong channelId, ulong userId)
    {
        await using var ctx = db.GetDbContext();

        var rowsAffected = await ctx.GetTable<LineUpUser>()
                                  .Where(lu => lu.GuildId == guildId && lu.ChannelId == channelId && lu.UserId == userId)
                                  .DeleteAsync();

        return rowsAffected > 0;
    }

    /// <summary>
    /// Gets the next user in the lineup for a specific channel and removes them.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="channelId">The ID of the channel.</param>
    /// <returns>The <see cref="LineUpUser"/> who was next, or null if the lineup is empty.</returns>
    public async Task<LineUpUser?> GetNextInLineupAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = db.GetDbContext();
        await using var tr = await ctx.Database.BeginTransactionAsync();

        var nextUser = await ctx.GetTable<LineUpUser>()
                             .Where(lu => lu.GuildId == guildId && lu.ChannelId == channelId)
                             .OrderBy(lu => lu.DateAdded)
                             .FirstOrDefaultAsyncLinqToDB();

        if (nextUser is null)
            return null;

        await ctx.GetTable<LineUpUser>()
                 .Where(lu => lu.GuildId == guildId && lu.ChannelId == channelId && lu.UserId == nextUser.UserId)
                 .DeleteAsync();

        await tr.CommitAsync();
        return nextUser;
    }

    /// <summary>
    /// Gets the current lineup for a specific channel.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="channelId">The ID of the channel.</param>
    /// <returns>A list of <see cref="LineUpUser"/> currently in the lineup, ordered by joining time.</returns>
    public async Task<List<LineUpUser>> GetLineupAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = db.GetDbContext();

        return await ctx.GetTable<LineUpUser>()
                       .AsNoTracking()
                       .Where(lu => lu.GuildId == guildId && lu.ChannelId == channelId)
                       .OrderBy(lu => lu.DateAdded)
                       .ToListAsyncLinqToDB();
    }

    /// <summary>
    /// Gets the position of a specific user in the lineup.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>The 1-based position of the user, or null if not in the lineup.</returns>
    public async Task<int?> GetPositionAsync(ulong guildId, ulong channelId, ulong userId)
    {
        await using var ctx = db.GetDbContext();

        // Get all users in order
        var lineup = await ctx.GetTable<LineUpUser>()
                             .AsNoTracking()
                             .Where(lu => lu.GuildId == guildId && lu.ChannelId == channelId)
                             .OrderBy(lu => lu.DateAdded)
                             .Select(lu => lu.UserId)
                             .ToListAsyncLinqToDB();

        var index = lineup.IndexOf(userId);
        return index == -1 ? null : (int?)(index + 1);
    }

    /// <summary>
    /// Checks if a user is currently in the lineup for a specific channel.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>True if the user is in the lineup, false otherwise.</returns>
    public async Task<bool> IsInLineupAsync(ulong guildId, ulong channelId, ulong userId)
    {
        await using var ctx = db.GetDbContext();
        return await ctx.GetTable<LineUpUser>()
                       .AsNoTracking()
                       .AnyAsyncLinqToDB(lu => lu.GuildId == guildId && lu.ChannelId == channelId && lu.UserId == userId);
    }

    /// <summary>
    /// Clears the lineup for a specific channel.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="channelId">The ID of the channel.</param>
    /// <returns>The number of users removed from the lineup.</returns>
    public async Task<int> ClearLineupAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = db.GetDbContext();

        return await ctx.GetTable<LineUpUser>()
                      .Where(lu => lu.GuildId == guildId && lu.ChannelId == channelId)
                      .DeleteAsync();
    }
}
