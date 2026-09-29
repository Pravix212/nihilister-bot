using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nihilister.Modules.Games;

public sealed class UserFishStatsConfiguration : IEntityTypeConfiguration<UserFishStats>
{
    public void Configure(EntityTypeBuilder<UserFishStats> builder)
    {
        builder.HasIndex(x => x.UserId)
            .IsUnique();
    }
}