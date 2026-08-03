using GharCraft.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GharCraft.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Token)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(t => t.JwtId)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(t => t.Token)
            .IsUnique();

        builder.HasIndex(t => t.UserId);
    }
}
