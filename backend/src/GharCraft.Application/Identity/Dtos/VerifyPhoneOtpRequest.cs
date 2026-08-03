namespace GharCraft.Application.Identity.Dtos;

/// <summary>
/// Step 2 of phone auth — verify OTP and create/login the user.
/// If the phone number is new → account is created (FirstName/LastName required).
/// If the phone number already exists → the user is logged in (name fields ignored).
/// </summary>
public record VerifyPhoneOtpRequest(
    string PhoneNumber,
    string Otp,

    /// <summary>Required only when creating a new account.</summary>
    string? FirstName,

    /// <summary>Required only when creating a new account.</summary>
    string? LastName
);
