using System.Security.Claims;
using GharCraft.Domain.Entities.Identity;

namespace GharCraft.Application.Common.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt, string JwtId) GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
