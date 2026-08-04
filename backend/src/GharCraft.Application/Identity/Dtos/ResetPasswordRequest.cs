namespace GharCraft.Application.Identity.Dtos;

public record ResetPasswordRequest(string Email, string Token, string NewPassword);
