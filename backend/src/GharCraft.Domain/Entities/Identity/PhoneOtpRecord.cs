using GharCraft.Domain.Common;

namespace GharCraft.Domain.Entities.Identity;

/// <summary>
/// Stores a pending OTP for phone number verification.
/// One record per phone number — upserted on each send-otp call.
/// Deleted after successful verification.
/// </summary>
public class PhoneOtpRecord : BaseEntity
{
    /// <summary>Indian mobile number in E.164 format without country code, e.g. "9876543210"</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>6-digit OTP code (stored in plain text — 5 minute TTL makes hashing overkill).</summary>
    public string OtpCode { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Tracks failed verification attempts to prevent brute-force on the 6-digit space.</summary>
    public int AttemptCount { get; set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsExhausted => AttemptCount >= 3;
    public bool IsValid => !IsExpired && !IsExhausted;
}
