using GharCraft.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace GharCraft.Infrastructure.Email;

/// <summary>Used in Development when Resend is not configured — prints the reset link to the console.</summary>
public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger) => _logger = logger;

    public Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "[DEV] Password reset email (not sent — configure Resend:ApiKey to send real emails)\n" +
            "  To:   {Email}\n" +
            "  Link: {Link}",
            toEmail, resetLink);

        return Task.CompletedTask;
    }
}
