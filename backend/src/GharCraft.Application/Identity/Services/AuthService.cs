using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GharCraft.Application.Common.Interfaces;
using GharCraft.Application.Identity.Dtos;
using GharCraft.Domain.Common;
using GharCraft.Domain.Entities.Identity;
using GharCraft.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GharCraft.Application.Identity.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly IApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ISmsService _smsService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    private const int OtpExpiryMinutes = 5;
    private const int OtpMaxAttempts = 3;
    private const int OtpResendCooldownSeconds = 60;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ITokenService tokenService,
        IApplicationDbContext context,
        IConfiguration configuration,
        ISmsService smsService,
        IEmailService emailService,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _context = context;
        _configuration = configuration;
        _smsService = smsService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return Result.Failure<AuthResponse>(Error.Conflict("Auth.EmailExists", "An account with this email address already exists."));
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            UserName = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure<AuthResponse>(Error.Validation("Auth.RegistrationFailed", errors));
        }

        // Ensure Customer role exists and assign
        var roleName = UserRole.Customer.ToString();
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
        }
        await _userManager.AddToRoleAsync(user, roleName);

        return await GenerateAuthResponseAsync(user, ct);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            return Result.Failure<AuthResponse>(Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password."));
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return Result.Failure<AuthResponse>(Error.Unauthorized(
                "Auth.AccountLocked",
                "Account is temporarily locked due to too many failed attempts. Please try again later."));
        }

        var isValidPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isValidPassword)
        {
            await _userManager.AccessFailedAsync(user);
            return Result.Failure<AuthResponse>(Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password."));
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return await GenerateAuthResponseAsync(user, ct);
    }

    public async Task<Result<AuthResponse>> AdminLoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            return Result.Failure<AuthResponse>(Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password."));
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return Result.Failure<AuthResponse>(Error.Unauthorized(
                "Auth.AccountLocked",
                "Account is temporarily locked due to too many failed attempts. Please try again later."));
        }

        var isValidPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isValidPassword)
        {
            await _userManager.AccessFailedAsync(user);
            return Result.Failure<AuthResponse>(Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password."));
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(UserRole.Admin.ToString()))
        {
            return Result.Failure<AuthResponse>(Error.Forbidden("Auth.AdminAccessDenied", "User does not have admin permissions."));
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return await GenerateAuthResponseAsync(user, ct);
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal is null)
        {
            return Result.Failure<AuthResponse>(Error.Validation("Auth.InvalidToken", "Invalid access token format."));
        }

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Result.Failure<AuthResponse>(Error.Validation("Auth.InvalidToken", "Invalid token claims."));
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            return Result.Failure<AuthResponse>(Error.Unauthorized("Auth.UserNotFound", "User account is inactive or not found."));
        }

        var hash = HashToken(request.RefreshToken);
        var storedRefreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == hash && t.UserId == userId, ct);

        if (storedRefreshToken is null || !storedRefreshToken.IsActive)
        {
            return Result.Failure<AuthResponse>(Error.Unauthorized("Auth.InvalidRefreshToken", "Refresh token is invalid or expired."));
        }

        // Revoke current refresh token (rotation)
        storedRefreshToken.IsUsed = true;
        _context.RefreshTokens.Update(storedRefreshToken);
        await _context.SaveChangesAsync(ct);

        return await GenerateAuthResponseAsync(user, ct);
    }

    public async Task<Result> RevokeTokenAsync(string refreshToken, Guid userId, CancellationToken ct = default)
    {
        var hash = HashToken(refreshToken);
        var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == hash, ct);
        if (storedToken is null)
        {
            return Result.Failure(Error.NotFound("Auth.TokenNotFound", "Refresh token not found."));
        }

        if (storedToken.UserId != userId)
        {
            return Result.Failure(Error.Forbidden("Auth.Forbidden", "You do not have permission to revoke this token."));
        }

        storedToken.IsRevoked = true;
        _context.RefreshTokens.Update(storedToken);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> SendPhoneOtpAsync(SendOtpRequest request, CancellationToken ct = default)
    {
        // Check cooldown: prevent OTP spam to the same number within 60 seconds
        var existingOtp = await _context.PhoneOtpRecords
            .FirstOrDefaultAsync(r => r.PhoneNumber == request.PhoneNumber, ct);

        if (existingOtp is not null)
        {
            var cooldownExpiry = existingOtp.CreatedAt.AddSeconds(OtpResendCooldownSeconds);
            if (DateTime.UtcNow < cooldownExpiry)
            {
                var waitSeconds = (int)(cooldownExpiry - DateTime.UtcNow).TotalSeconds;
                return Result.Failure(Error.Conflict(
                    "Auth.OtpCooldown",
                    $"Please wait {waitSeconds} seconds before requesting a new OTP."));
            }

            // Remove stale record — a fresh one will be created
            _context.PhoneOtpRecords.Remove(existingOtp);
        }

        // Generate 6-digit OTP
        var otp = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString("D6");
        var otpHash = HashOtp(otp);

        var record = new PhoneOtpRecord
        {
            PhoneNumber = request.PhoneNumber,
            OtpCode = otpHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
            CreatedAt = DateTime.UtcNow,
            AttemptCount = 0
        };

        _context.PhoneOtpRecords.Add(record);
        await _context.SaveChangesAsync(ct);

        var sent = await _smsService.SendOtpAsync(request.PhoneNumber, otp, ct);
        if (!sent)
        {
            _logger.LogWarning("SMS delivery failed for phone {Phone}", request.PhoneNumber);
            // Do NOT expose delivery failure to the client — prevents phone enumeration
        }

        return Result.Success();
    }

    public async Task<Result<AuthResponse>> VerifyPhoneOtpAsync(VerifyPhoneOtpRequest request, CancellationToken ct = default)
    {
        var record = await _context.PhoneOtpRecords
            .FirstOrDefaultAsync(r => r.PhoneNumber == request.PhoneNumber, ct);

        if (record is null || record.IsExpired)
        {
            return Result.Failure<AuthResponse>(Error.Unauthorized(
                "Auth.OtpExpiredOrNotFound",
                "OTP has expired or was not requested. Please request a new OTP."));
        }

        if (record.IsExhausted)
        {
            return Result.Failure<AuthResponse>(Error.Unauthorized(
                "Auth.OtpExhausted",
                "Too many incorrect attempts. Please request a new OTP."));
        }

        var incoming = HashOtp(request.Otp);
        if (record.OtpCode != incoming)
        {
            record.AttemptCount++;
            _context.PhoneOtpRecords.Update(record);
            await _context.SaveChangesAsync(ct);

            var remaining = OtpMaxAttempts - record.AttemptCount;
            return Result.Failure<AuthResponse>(Error.Unauthorized(
                "Auth.OtpInvalid",
                $"Incorrect OTP. {remaining} attempt(s) remaining."));
        }

        // OTP is correct — clean it up immediately
        _context.PhoneOtpRecords.Remove(record);
        await _context.SaveChangesAsync(ct);

        // Find or create the user
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber, ct);

        if (user is null)
        {
            // New user — name fields required
            if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            {
                return Result.Failure<AuthResponse>(Error.Validation(
                    "Auth.NameRequired",
                    "First name and last name are required when registering with a new phone number."));
            }

            user = new ApplicationUser
            {
                UserName = request.PhoneNumber,    // UserName = phone for phone-users
                PhoneNumber = request.PhoneNumber,
                PhoneNumberConfirmed = true,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
                // Email is intentionally null for phone-only users
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                return Result.Failure<AuthResponse>(Error.Validation("Auth.RegistrationFailed", errors));
            }

            var roleName = UserRole.Customer.ToString();
            if (!await _roleManager.RoleExistsAsync(roleName))
                await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));

            await _userManager.AddToRoleAsync(user, roleName);
        }
        else if (!user.IsActive)
        {
            return Result.Failure<AuthResponse>(Error.Forbidden(
                "Auth.AccountDisabled",
                "Your account has been disabled. Please contact support."));
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return await GenerateAuthResponseAsync(user, ct);
    }

    public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        // Always return success — never reveal whether an email is registered
        if (user is null || !user.IsActive)
        {
            _logger.LogInformation("Password reset requested for unknown/inactive email: {Email}", request.Email);
            return Result.Success();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);

        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var resetLink = $"{frontendBaseUrl}/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={encodedToken}";

        try
        {
            await _emailService.SendPasswordResetEmailAsync(user.Email!, resetLink, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
            return Result.Failure(Error.Failure("Email.SendFailed", "Failed to send password reset email. Please try again."));
        }

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            return Result.Failure(Error.Validation("Auth.InvalidToken", "Password reset link is invalid or has expired."));
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            var description = result.Errors.FirstOrDefault()?.Description
                ?? "Password reset link is invalid or has expired.";
            return Result.Failure(Error.Validation("Auth.InvalidToken", description));
        }

        // Revoke all existing refresh tokens — fresh login required after password reset
        var tokens = await _context.RefreshTokens
            .Where(t => t.UserId == user.Id && !t.IsRevoked)
            .ToListAsync(ct);

        foreach (var t in tokens)
            t.IsRevoked = true;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Password reset completed for user {UserId}", user.Id);
        return Result.Success();
    }

    private async Task<Result<AuthResponse>> GenerateAuthResponseAsync(ApplicationUser user, CancellationToken ct)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, expiresAt, jwtId) = _tokenService.GenerateAccessToken(user, roles);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var tokenHash = HashToken(refreshTokenValue);

        var refreshDays = int.TryParse(_configuration["Jwt:RefreshTokenExpirationDays"], out var days) ? days : 7;

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = tokenHash,
            JwtId = jwtId,
            IsUsed = false,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDays)
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(ct);

        var response = new AuthResponse(
            user.Id,
            user.Email,
            user.PhoneNumber,
            user.FirstName,
            user.LastName,
            roles.ToList(),
            accessToken,
            refreshTokenValue,
            expiresAt
        );

        return Result.Success(response);
    }
    private static string HashToken(string token)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string HashOtp(string otp) => HashToken(otp);
}
