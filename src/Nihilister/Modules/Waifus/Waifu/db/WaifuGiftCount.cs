using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nihilister.Modules.Waifus.Waifu.Db;

/// <summary>
/// Tracks cumulative gift counts per item per waifu.
/// </summary>
public class WaifuGiftCount
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Discord User ID of the waifu who received the gifts.
    /// </summary>
    public ulong WaifuUserId { get; set; }

    /// <summary>
    /// The gift item identifier (matches <see cref="WaifuGiftItem.Id"/>).
    /// </summary>
    public Guid GiftItemId { get; set; }

    /// <summary>
    /// Total number of this item received.
    /// </summary>
    public int Count { get; set; }
}

/// <summary>
/// Entity configuration for WaifuGiftCount.
/// </summary>
public class WaifuGiftCountEntityConfiguration : IEntityTypeConfiguration<WaifuGiftCount>
{
    public void Configure(EntityTypeBuilder<WaifuGiftCount> builder)
    {
        builder.HasIndex(x => new { x.WaifuUserId, x.GiftItemId }).IsUnique();
    }
}
