using GharCraft.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace GharCraft.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<UserAddress> UserAddresses { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
