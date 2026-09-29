using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nihilister.Db.Models;

public class XpRoleRewardEntityConfiguration : IEntityTypeConfiguration<XpRoleReward>
{
    public void Configure(EntityTypeBuilder<XpRoleReward> builder)
    {
        builder.HasIndex(x => new
              {
                   x.XpSettingsId,
                   x.Level
               })
               .IsUnique();
    }
}