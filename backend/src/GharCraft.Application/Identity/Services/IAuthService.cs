using GharCraft.Application.Identity.Dtos;
using GharCraft.Domain.Common;

namespace GharCraft.Application.Identity.Services;

public interface IAuthService
{
    // ── Email + Password ──────────────────────────────────────────────
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> AdminLoginAsync(LoginRequest request, CancellationToken ct = default);

    // ── Token lifecycle ───────────────────────────────────────────────
    Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<Result> RevokeTokenAsync(string refreshToken, CancellationToken ct = default);

    // ── Phone + OTP (India-first) ─────────────────────────────────────
    /// <summary>Step 1: generate and deliver a 6-digit OTP to the given phone number.</summary>
    Task<Result> SendPhoneOtpAsync(SendOtpRequest request, CancellationToken ct = default);

    /// <summary>
    /// Step 2: verify OTP and return a JWT pair.
    /// Creates the account if the phone number is new; logs in if it already exists.
    /// </summary>
    Task<Result<AuthResponse>> VerifyPhoneOtpAsync(VerifyPhoneOtpRequest request, CancellationToken ct = default);
}
