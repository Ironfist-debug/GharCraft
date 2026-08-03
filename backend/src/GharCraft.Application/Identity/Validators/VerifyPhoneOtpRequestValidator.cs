using FluentValidation;
using GharCraft.Application.Identity.Dtos;

namespace GharCraft.Application.Identity.Validators;

public class VerifyPhoneOtpRequestValidator : AbstractValidator<VerifyPhoneOtpRequest>
{
    public VerifyPhoneOtpRequestValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^[6-9]\d{9}$")
            .WithMessage("A valid 10-digit Indian mobile number starting with 6, 7, 8, or 9 is required.");

        RuleFor(x => x.Otp)
            .NotEmpty().WithMessage("OTP is required.")
            .Matches(@"^\d{6}$").WithMessage("OTP must be a 6-digit number.");

        // FirstName + LastName are only required when it's a new user registration.
        // We validate this in AuthService (we don't know if the phone is new here).
        RuleFor(x => x.FirstName)
            .MaximumLength(100).When(x => x.FirstName is not null)
            .WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .MaximumLength(100).When(x => x.LastName is not null)
            .WithMessage("Last name cannot exceed 100 characters.");
    }
}
