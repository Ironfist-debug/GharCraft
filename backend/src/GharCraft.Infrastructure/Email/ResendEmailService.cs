using GharCraft.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Resend;

namespace GharCraft.Infrastructure.Email;

public class ResendEmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(IResend resend, IConfiguration configuration, ILogger<ResendEmailService> logger)
    {
        _resend = resend;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken ct = default)
    {
        var fromAddress = _configuration["Email:FromAddress"] ?? "GharCraft <noreply@gharcraft.com>";
        var appName = _configuration["Email:AppName"] ?? "GharCraft";

        var message = new EmailMessage
        {
            From = fromAddress,
            To = [toEmail],
            Subject = $"Reset your {appName} password",
            HtmlBody = BuildPasswordResetHtml(resetLink, appName),
            TextBody = BuildPasswordResetText(resetLink, appName)
        };

        var response = await _resend.EmailSendAsync(message, ct);
        _logger.LogInformation("Password reset email sent to {Email}, Resend ID: {Id}", toEmail, response.Content);
    }

    private static string BuildPasswordResetHtml(string resetLink, string appName) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:sans-serif;max-width:600px;margin:0 auto;padding:24px;color:#1a1a1a">
          <h2 style="color:#1a1a1a">Reset your password</h2>
          <p>You requested a password reset for your {appName} account.</p>
          <p>Click the button below to set a new password. This link expires in <strong>1 hour</strong>.</p>
          <p style="margin:32px 0">
            <a href="{resetLink}"
               style="background:#1a1a1a;color:#fff;padding:12px 24px;text-decoration:none;border-radius:6px;display:inline-block">
              Reset password
            </a>
          </p>
          <p style="color:#666;font-size:13px">If you didn't request this, you can safely ignore this email.</p>
          <p style="color:#666;font-size:13px">
            Or copy this link:<br>
            <a href="{resetLink}" style="color:#666;word-break:break-all">{resetLink}</a>
          </p>
        </body>
        </html>
        """;

    private static string BuildPasswordResetText(string resetLink, string appName) =>
        $"Reset your {appName} password\n\n" +
        $"Click the link below to reset your password (expires in 1 hour):\n{resetLink}\n\n" +
        "If you didn't request this, you can safely ignore this email.";
}
