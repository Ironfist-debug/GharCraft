using FluentValidation;
using GharCraft.Application.Identity.Dtos;

namespace GharCraft.Application.Identity.Validators;

public class SendOtpRequestValidator : AbstractValidator<SendOtpRequest>
{
    public SendOtpRequestValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^[6-9]\d{9}$")
            .WithMessage("A valid 10-digit Indian mobile number starting with 6, 7, 8, or 9 is required.");
    }
}
