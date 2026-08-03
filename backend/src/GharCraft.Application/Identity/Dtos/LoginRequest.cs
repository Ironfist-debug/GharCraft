namespace GharCraft.Application.Identity.Dtos;

public record LoginRequest(
    string Email,
    string Password
);
