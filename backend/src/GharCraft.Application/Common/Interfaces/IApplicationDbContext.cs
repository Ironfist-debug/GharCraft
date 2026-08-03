using GharCraft.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace GharCraft.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<UserAddress> UserAddresses { get; }
    DbSet<PhoneOtpRecord> PhoneOtpRecords { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
