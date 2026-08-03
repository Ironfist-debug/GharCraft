namespace GharCraft.Application.Identity.Dtos;

/// <summary>
/// Step 1 of phone auth — request an OTP to be sent to the given mobile number.
/// Works for both new registrations and returning customers.
/// </summary>
public record SendOtpRequest(
    /// <summary>10-digit Indian mobile number without country code, e.g. "9876543210".</summary>
    string PhoneNumber
);
