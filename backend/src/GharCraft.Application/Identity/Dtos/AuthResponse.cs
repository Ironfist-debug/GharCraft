namespace GharCraft.Application.Identity.Dtos;

public record AuthResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyCollection<string> Roles,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt
);
