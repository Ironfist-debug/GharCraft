using GharCraft.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GharCraft.Infrastructure.Persistence.Configurations;

public class PhoneOtpRecordConfiguration : IEntityTypeConfiguration<PhoneOtpRecord>
{
    public void Configure(EntityTypeBuilder<PhoneOtpRecord> builder)
    {
        builder.ToTable("PhoneOtpRecords");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PhoneNumber)
            .IsRequired()
            .HasMaxLength(15);

        // Stored as SHA-256 hex digest → exactly 64 characters.
        builder.Property(x => x.OtpCode)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.AttemptCount).IsRequired().HasDefaultValue(0);

        // One OTP record per phone number at a time
        builder.HasIndex(x => x.PhoneNumber).IsUnique();

        // Index to efficiently purge expired records (background cleanup job in Phase 2)
        builder.HasIndex(x => x.ExpiresAt);
    }
}
