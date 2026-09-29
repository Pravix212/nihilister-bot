using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nihilister.Modules.Waifus.Waifu.Db;

/// <summary>
/// Represents a Waifu or Hubby in the backing system.
/// </summary>
public class WaifuInfo
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Discord User ID of the waifu.
    /// </summary>
    public ulong UserId { get; set; }

    /// <summary>
    /// Mood stat (0-1000).
    /// </summary>
    public int Mood { get; set; } = 500;

    /// <summary>
    /// Food/Hunger stat (0-1000).
    /// </summary>
    public int Food { get; set; } = 500;

    /// <summary>
    /// Last time stats decayed.
    /// </summary>
    public DateTime LastDecayTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Waifu's fee percentage (1-5%).
    /// </summary>
    public int WaifuFeePercent { get; set; } = 5;

    /// <summary>
    /// Manager buyout price.
    /// </summary>
    public long Price { get; set; } = 10_000;

    /// <summary>
    /// Discord User ID of the current manager (null if no manager).
    /// </summary>
    public ulong? ManagerUserId { get; set; }

    /// <summary>
    /// Total returns produced lifetime.
    /// </summary>
    public long TotalProduced { get; set; }

    /// <summary>
    /// Maximum backed amount used for returns calculation.
    /// </summary>
    public long ReturnsCap { get; set; } = 1_000_000;

    /// <summary>
    /// Whether this is a Hubby (true) or Waifu (false).
    /// </summary>
    public bool IsHubby { get; set; }

    /// <summary>
    /// Custom avatar URL.
    /// </summary>
    public string? CustomAvatarUrl { get; set; }

    /// <summary>
    /// Bio/description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Quote.
    /// </summary>
    public string? Quote { get; set; }
}

/// <summary>
/// Entity configuration for WaifuInfo.
/// </summary>
public class WaifuInfoEntityConfiguration : IEntityTypeConfiguration<WaifuInfo>
{
    public void Configure(EntityTypeBuilder<WaifuInfo> builder)
    {
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasIndex(x => x.ManagerUserId);
    }
}
