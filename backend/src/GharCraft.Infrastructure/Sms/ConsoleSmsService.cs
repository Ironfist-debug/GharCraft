using GharCraft.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace GharCraft.Infrastructure.Sms;

/// <summary>
/// Development SMS service that logs the OTP to the console instead of sending a real SMS.
/// Swap for Msg91SmsService / Fast2SmsService in production via DI configuration.
/// </summary>
public sealed class ConsoleSmsService : ISmsService
{
    private readonly ILogger<ConsoleSmsService> _logger;

    public ConsoleSmsService(ILogger<ConsoleSmsService> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendOtpAsync(string phoneNumber, string otp, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[DEV SMS] OTP for +91{Phone}: {Otp}  (expires in 5 minutes)",
            phoneNumber, otp);

        // Also write to console so it's visible without a logging sink configured
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n  ┌── DEV OTP ──────────────────────────────┐");
        Console.WriteLine($"  │  Phone : +91 {phoneNumber,-27}│");
        Console.WriteLine($"  │  OTP   : {otp,-33}│");
        Console.WriteLine($"  └─────────────────────────────────────────┘\n");
        Console.ResetColor();

        return Task.FromResult(true);
    }
}
