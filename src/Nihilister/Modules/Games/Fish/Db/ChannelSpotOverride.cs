using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nihilister.Modules.Games.Fish.Db;

/// <summary>
/// Stores fishing spot override for a channel.
/// </summary>
public class ChannelSpotOverride
{
    /// <summary>
    /// The Discord channel ID.
    /// </summary>
    public ulong ChannelId { get; set; }

    /// <summary>
    /// The overridden fishing spot for this channel.
    /// </summary>
    public FishingSpot Spot { get; set; }
}

/// <summary>
/// Entity configuration for ChannelSpotOverride.
/// </summary>
public class ChannelSpotOverrideConfiguration : IEntityTypeConfiguration<ChannelSpotOverride>
{
    public void Configure(EntityTypeBuilder<ChannelSpotOverride> builder)
    {
        builder.HasKey(x => x.ChannelId);
    }
}
