using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nihilister.Modules.Waifus.Waifu.Db;

/// <summary>
/// Represents a fan delegation to a waifu.
/// </summary>
public class WaifuFan
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Discord User ID of the fan.
    /// </summary>
    public ulong UserId { get; set; }

    /// <summary>
    /// Discord User ID of the waifu being delegated to.
    /// </summary>
    public ulong WaifuUserId { get; set; }

    /// <summary>
    /// When the delegation started.
    /// </summary>
    public DateTime DelegatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Entity configuration for WaifuFan.
/// </summary>
public class WaifuFanEntityConfiguration : IEntityTypeConfiguration<WaifuFan>
{
    public void Configure(EntityTypeBuilder<WaifuFan> builder)
    {
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasIndex(x => x.WaifuUserId);
    }
}
