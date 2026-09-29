using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nihilister.Modules.Waifus.Waifu.Db;

/// <summary>
/// Stores unclaimed cycle payouts for a user. Amount is decimal to preserve
/// fractional earnings across multiple cycles. Claiming floors to whole units,
/// deletes the row, and discards any fractional remainder.
/// </summary>
public class WaifuPendingPayout
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Discord User ID (one row per user).
    /// </summary>
    public ulong UserId { get; set; }

    /// <summary>
    /// Accumulated unclaimed payout amount (decimal for precision).
    /// </summary>
    public decimal Amount { get; set; }
}

/// <summary>
/// Entity configuration for WaifuPendingPayout.
/// </summary>
public class WaifuPendingPayoutEntityConfiguration : IEntityTypeConfiguration<WaifuPendingPayout>
{
    public void Configure(EntityTypeBuilder<WaifuPendingPayout> builder)
    {
        builder.HasIndex(x => x.UserId).IsUnique();
    }
}
