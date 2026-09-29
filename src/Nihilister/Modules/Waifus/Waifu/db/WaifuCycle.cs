using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nihilister.Modules.Waifus.Waifu.Db;

/// <summary>
/// Per-waifu cycle record. Created at cycle start as a snapshot of frozen fields,
/// then updated with computed results after payout processing.
/// </summary>
public class WaifuCycle
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Discord User ID of the waifu.
    /// </summary>
    public ulong WaifuUserId { get; set; }

    /// <summary>
    /// Cycle number (cycles since epoch).
    /// </summary>
    public int CycleNumber { get; set; }

    // --- Snapshot fields (frozen at cycle start) ---

    /// <summary>
    /// Manager at snapshot time. 0 if no manager.
    /// </summary>
    public ulong ManagerUserId { get; set; }

    /// <summary>
    /// Waifu fee percent at snapshot time.
    /// </summary>
    public int WaifuFeePercent { get; set; }

    /// <summary>
    /// Returns cap at snapshot time.
    /// </summary>
    public long ReturnsCap { get; set; }

    /// <summary>
    /// Manager cut percent at snapshot time (from config).
    /// </summary>
    public double ManagerCutPercent { get; set; }

    /// <summary>
    /// Waifu price at snapshot time (for managerless decay).
    /// </summary>
    public long Price { get; set; }

    // --- Result fields (filled after processing) ---

    /// <summary>
    /// Total backed amount from fan snapshots (set at snapshot time).
    /// </summary>
    public long TotalBacked { get; set; }

    /// <summary>
    /// Whether this cycle has been processed (payouts computed and applied).
    /// </summary>
    public bool Processed { get; set; }

    /// <summary>
    /// When this cycle was processed. Null if not yet processed.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}

/// <summary>
/// Entity configuration for WaifuCycle.
/// </summary>
public class WaifuCycleEntityConfiguration : IEntityTypeConfiguration<WaifuCycle>
{
    public void Configure(EntityTypeBuilder<WaifuCycle> builder)
    {
        builder.HasIndex(x => new { x.WaifuUserId, x.CycleNumber }).IsUnique();
        builder.HasIndex(x => new { x.CycleNumber, x.Processed });
    }
}
