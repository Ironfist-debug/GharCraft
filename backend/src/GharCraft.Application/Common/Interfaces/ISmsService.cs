namespace GharCraft.Application.Common.Interfaces;

/// <summary>
/// Port for sending SMS messages. Swappable between dev (console log) and
/// production providers (MSG91, Fast2SMS, Twilio, etc.).
/// </summary>
public interface ISmsService
{
    Task<bool> SendOtpAsync(string phoneNumber, string otp, CancellationToken ct = default);
}
