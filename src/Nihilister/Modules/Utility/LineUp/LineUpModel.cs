using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nihilister.Modules.Utility.LineUp;

/// <summary>
/// Represents a user waiting in a lineup for a specific channel.
/// </summary>
public class LineUpUser
{
    /// <summary>
    /// The Guild where the lineup exists.
    /// </summary>
    public ulong GuildId { get; set; }

    /// <summary>
    /// The Channel where the lineup exists.
    /// </summary>
    public ulong ChannelId { get; set; }

    /// <summary>
    /// The User in the lineup.
    /// </summary>
    public ulong UserId { get; set; }

    /// <summary>
    /// Optional text the user provided when joining.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// When the user joined the lineup.
    /// </summary>
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Configures the database mapping for the <see cref="LineUpUser"/> entity.
/// </summary>
public class LineUpUserConfiguration : IEntityTypeConfiguration<LineUpUser>
{
    public void Configure(EntityTypeBuilder<LineUpUser> builder)
    {
        builder.HasKey(lu => new { lu.GuildId, lu.ChannelId, lu.UserId });

        builder.Property(lu => lu.Reason)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(lu => lu.DateAdded)
            .IsRequired();

        builder.HasIndex(lu => new { lu.GuildId, lu.ChannelId, lu.DateAdded });
    }
}
