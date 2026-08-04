using GharCraft.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GharCraft.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(t => t.Id);

        // Stored as SHA-256 hex digest → exactly 64 characters.
        builder.Property(t => t.Token)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(t => t.JwtId)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(t => t.Token)
            .IsUnique();

        builder.HasIndex(t => t.UserId);
    }
}
