namespace GharCraft.Application.Identity.Dtos;

/// <summary>
/// Returned after any successful authentication (email or phone flow).
/// Either Email or PhoneNumber will be set — not necessarily both.
/// </summary>
public record AuthResponse(
    Guid UserId,
    string? Email,
    string? PhoneNumber,
    string FirstName,
    string LastName,
    List<string> Roles,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt
);
