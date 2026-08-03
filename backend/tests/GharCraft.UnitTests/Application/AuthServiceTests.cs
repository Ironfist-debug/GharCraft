using GharCraft.Application.Common.Interfaces;
using GharCraft.Application.Identity.Dtos;
using GharCraft.Application.Identity.Services;
using GharCraft.Domain.Entities.Identity;
using GharCraft.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var store = Substitute.For<IUserStore<ApplicationUser>>();
        _userManager = Substitute.For<UserManager<ApplicationUser>>(store, null, null, null, null, null, null, null, null);

        var roleStore = Substitute.For<IRoleStore<IdentityRole<Guid>>>();
        _roleManager = Substitute.For<RoleManager<IdentityRole<Guid>>>(roleStore, null, null, null, null);

        _tokenService = Substitute.For<ITokenService>();

        _context = Substitute.For<IApplicationDbContext>();

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

        _authService = new AuthService(_userManager, _roleManager, _tokenService, _context, _configuration);
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
}
