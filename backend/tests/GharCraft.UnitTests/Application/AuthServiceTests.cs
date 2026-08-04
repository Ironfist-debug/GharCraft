using GharCraft.Application.Common.Interfaces;
using GharCraft.Application.Identity.Dtos;
using GharCraft.Application.Identity.Services;
using GharCraft.Domain.Entities.Identity;
using GharCraft.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace GharCraft.UnitTests.Application;

public class AuthServiceTests
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly IApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ISmsService _smsService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var store = Substitute.For<IUserStore<ApplicationUser>>();
        _userManager = Substitute.For<UserManager<ApplicationUser>>(store, null, null, null, null, null, null, null, null);

        var roleStore = Substitute.For<IRoleStore<IdentityRole<Guid>>>();
        _roleManager = Substitute.For<RoleManager<IdentityRole<Guid>>>(roleStore, null, null, null, null);

        _tokenService = Substitute.For<ITokenService>();
        _context = Substitute.For<IApplicationDbContext>();
        _smsService = Substitute.For<ISmsService>();
        _emailService = Substitute.For<IEmailService>();
        _logger = Substitute.For<ILogger<AuthService>>();

        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Jwt:Secret", "SuperSecretSigningKeyForTestingPurposeOnly12345!"},
            {"Jwt:Issuer", "GharCraft.Api"},
            {"Jwt:Audience", "GharCraft.Client"},
            {"Jwt:AccessTokenExpirationMinutes", "15"},
            {"Jwt:RefreshTokenExpirationDays", "7"}
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _authService = new AuthService(
            _userManager,
            _roleManager,
            _tokenService,
            _context,
            _configuration,
            _smsService,
            _emailService,
            _logger);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnUnauthorized_WhenPasswordIsIncorrect()
    {
        // Arrange
        var request = new LoginRequest("jane.doe@example.com", "WrongPassword");
        var user = new ApplicationUser { Email = request.Email, IsActive = true };
        _userManager.FindByEmailAsync(request.Email).Returns(Task.FromResult<ApplicationUser?>(user));
        _userManager.CheckPasswordAsync(user, request.Password).Returns(Task.FromResult(false));

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidCredentials", result.Error.Code);
    }

    [Fact]
    public async Task AdminLoginAsync_ShouldReturnForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        var request = new LoginRequest("customer@example.com", "Password123!");
        var user = new ApplicationUser { Email = request.Email, IsActive = true };
        _userManager.FindByEmailAsync(request.Email).Returns(Task.FromResult<ApplicationUser?>(user));
        _userManager.CheckPasswordAsync(user, request.Password).Returns(Task.FromResult(true));
        _userManager.GetRolesAsync(user).Returns(Task.FromResult<IList<string>>(new List<string> { "Customer" }));

        // Act
        var result = await _authService.AdminLoginAsync(request);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Auth.AdminAccessDenied", result.Error.Code);
    }

    [Fact]
    public async Task VerifyPhoneOtpAsync_ShouldReturnUnauthorized_WhenOtpRecordNotFound()
    {
        // Arrange
        var request = new VerifyPhoneOtpRequest("9876543210", "123456", "John", "Doe");
        var emptyList = new List<PhoneOtpRecord>();
        var mockSet = emptyList.AsQueryable().BuildMockDbSet();
        _context.PhoneOtpRecords.Returns(mockSet);

        // Act
        var result = await _authService.VerifyPhoneOtpAsync(request);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Auth.OtpExpiredOrNotFound", result.Error.Code);
    }

    [Fact]
    public async Task SendPhoneOtpAsync_ShouldSucceedAndDeliverSms()
    {
        // Arrange
        var request = new SendOtpRequest("9876543210");
        var emptyList = new List<PhoneOtpRecord>();
        var mockSet = emptyList.AsQueryable().BuildMockDbSet();
        _context.PhoneOtpRecords.Returns(mockSet);
        _smsService.SendOtpAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        var result = await _authService.SendPhoneOtpAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        await _smsService.Received(1).SendOtpAsync("9876543210", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
