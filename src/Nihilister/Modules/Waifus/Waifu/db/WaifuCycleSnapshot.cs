using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nihilister.Modules.Waifus.Waifu.Db;

/// <summary>
/// Snapshot of a fan's bank balance at cycle start.
/// </summary>
public class WaifuCycleSnapshot
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Cycle number this snapshot belongs to.
    /// </summary>
    public int CycleNumber { get; set; }

    /// <summary>
    /// Discord User ID of the waifu being backed.
    /// </summary>
    public ulong WaifuUserId { get; set; }

    /// <summary>
    /// Discord User ID of the fan.
    /// </summary>
    public ulong UserId { get; set; }

    /// <summary>
    /// Fan's bank balance at cycle start.
    /// </summary>
    public long SnapshotBalance { get; set; }
}

/// <summary>
/// Entity configuration for WaifuCycleSnapshot.
/// </summary>
public class WaifuCycleSnapshotEntityConfiguration : IEntityTypeConfiguration<WaifuCycleSnapshot>
{
    public void Configure(EntityTypeBuilder<WaifuCycleSnapshot> builder)
    {
        builder.HasIndex(x => new { x.CycleNumber, x.WaifuUserId });
        builder.HasIndex(x => new { x.CycleNumber, x.WaifuUserId, x.UserId }).IsUnique();
    }
}
