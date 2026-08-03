using GharCraft.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GharCraft.Infrastructure.Persistence.Configurations;

public class UserAddressConfiguration : IEntityTypeConfiguration<UserAddress>
{
    public void Configure(EntityTypeBuilder<UserAddress> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.FullName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(a => a.PhoneNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.AddressLine1)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(a => a.AddressLine2)
            .HasMaxLength(250);

        builder.Property(a => a.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.State)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.PostalCode)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.Country)
            .HasMaxLength(100)
            .HasDefaultValue("India")
            .IsRequired();

        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
