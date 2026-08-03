namespace GharCraft.Application.Identity.Dtos;

public record RefreshTokenRequest(
    string AccessToken,
    string RefreshToken
);
