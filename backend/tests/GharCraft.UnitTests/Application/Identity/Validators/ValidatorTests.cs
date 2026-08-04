using GharCraft.Application.Identity.Dtos;
using GharCraft.Application.Identity.Validators;
using Xunit;

namespace GharCraft.UnitTests.Application.Identity.Validators;

public class ValidatorTests
{
    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("asd", false)]
    [InlineData("asd@asd", false)]
    [InlineData("asd@.com", false)]
    public void RegisterRequestValidator_ShouldValidateEmailFormat(string email, bool expectedIsValid)
    {
        // Arrange
        var validator = new RegisterRequestValidator();
        var request = new RegisterRequest("John", "Doe", email, "Password@123");

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.Equal(expectedIsValid, result.IsValid);
    }

    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("asd", false)]
    public void LoginRequestValidator_ShouldValidateEmailFormat(string email, bool expectedIsValid)
    {
        // Arrange
        var validator = new LoginRequestValidator();
        var request = new LoginRequest(email, "Password@123");

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.Equal(expectedIsValid, result.IsValid);
    }

    [Theory]
    [InlineData("9876543210", true)]
    [InlineData("123", false)] // 3 digits
    [InlineData("1234567890", false)] // Invalid start digit
    [InlineData("98765432101", false)] // Too long
    [InlineData("abcdefghij", false)] // Not numbers
    public void SendOtpRequestValidator_ShouldValidatePhoneNumber(string phone, bool expectedIsValid)
    {
        // Arrange
        var validator = new SendOtpRequestValidator();
        var request = new SendOtpRequest(phone);

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.Equal(expectedIsValid, result.IsValid);
    }

    [Theory]
    [InlineData("123456", true)]
    [InlineData("123", false)] // 3 digits
    [InlineData("abcdef", false)] // Letters
    [InlineData("1234567", false)] // Too long
    public void VerifyPhoneOtpRequestValidator_ShouldValidateOtpAndPhone(string otp, bool expectedIsValid)
    {
        // Arrange
        var validator = new VerifyPhoneOtpRequestValidator();
        var request = new VerifyPhoneOtpRequest("9876543210", otp, "John", "Doe");

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.Equal(expectedIsValid, result.IsValid);
    }
}
