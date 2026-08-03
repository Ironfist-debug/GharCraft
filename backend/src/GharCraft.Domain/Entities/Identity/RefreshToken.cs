using GharCraft.Domain.Common;

namespace GharCraft.Domain.Entities.Identity;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string JwtId { get; set; } = string.Empty;
    public bool IsUsed { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    public bool IsActive => !IsUsed && !IsRevoked && DateTime.UtcNow < ExpiresAt;

    public ApplicationUser User { get; set; } = null!;
}
