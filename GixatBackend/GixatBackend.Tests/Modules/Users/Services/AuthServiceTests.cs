using FluentAssertions;
using GixatBackend.Data;
using GixatBackend.Modules.Common.Services;
using GixatBackend.Modules.Users.Enums;
using GixatBackend.Modules.Users.Models;
using GixatBackend.Modules.Users.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace GixatBackend.Tests.Modules.Users.Services;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly ApplicationDbContext _context;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // Mock UserManager
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // Mock RoleManager
        var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
        _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            roleStoreMock.Object, null!, null!, null!, null!);

        // Mock Configuration
        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(c => c["Jwt:Key"]).Returns("SuperSecretKeyForTestingOnly12345!");
        _configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _configurationMock.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
        _configurationMock.Setup(c => c["Jwt:ExpireDays"]).Returns("1");
        
        // Setup RSA Keys for testing
        using var rsa = System.Security.Cryptography.RSA.Create();
        var privateKey = rsa.ExportRSAPrivateKeyPem();
        _configurationMock.Setup(c => c["Jwt:PrivateKey"]).Returns(privateKey);

        // Mock TenantService
        _tenantServiceMock = new Mock<ITenantService>();
        _tenantServiceMock.Setup(x => x.OrganizationId).Returns((Guid?)null);

        // Setup InMemory Database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options, _tenantServiceMock.Object);

        _authService = new AuthService(
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _configurationMock.Object,
            _context);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnError_WhenUserNotFound()
    {
        // Arrange
        var input = new LoginInput("test@example.com", "Password123!");
        _userManagerMock.Setup(x => x.FindByEmailAsync(input.Email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _authService.LoginAsync(input);

        // Assert
        result.Token.Should().BeNull();
        result.Error.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnError_WhenPasswordIsInvalid()
    {
        // Arrange
        var input = new LoginInput("test@example.com", "WrongPassword");
        var user = new ApplicationUser { Email = input.Email, UserName = input.Email };
        
        _userManagerMock.Setup(x => x.FindByEmailAsync(input.Email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, input.Password))
            .ReturnsAsync(false);

        // Act
        var result = await _authService.LoginAsync(input);

        // Assert
        result.Token.Should().BeNull();
        result.Error.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreValid()
    {
        // Arrange
        var input = new LoginInput("test@example.com", "Password123!");
        var user = new ApplicationUser { Id = "user-1", Email = input.Email, UserName = input.Email };
        
        _userManagerMock.Setup(x => x.FindByEmailAsync(input.Email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, input.Password))
            .ReturnsAsync(true);
        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var result = await _authService.LoginAsync(input);

        // Assert
        result.Token.Should().NotBeNullOrEmpty();
        result.User.Should().BeEquivalentTo(user);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnError_WhenUserCreationFails()
    {
        // Arrange
        var input = new RegisterInput("test@example.com", "Password123!", "Test User", "User", UserType.Organizational, Guid.NewGuid());
        
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), input.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

        // Act
        var result = await _authService.RegisterAsync(input);

        // Assert
        result.Token.Should().BeNull();
        result.Error.Should().Contain("Password too weak");
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUserAndReturnToken_WhenInputIsValid()
    {
        // Arrange
        var input = new RegisterInput("test@example.com", "Password123!", "Test User", "User", UserType.Organizational, Guid.NewGuid());
        
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), input.Password))
            .ReturnsAsync(IdentityResult.Success);
        
        _roleManagerMock.Setup(x => x.RoleExistsAsync(input.Role))
            .ReturnsAsync(true);

        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), input.Role))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { input.Role });

        // Act
        var result = await _authService.RegisterAsync(input);

        // Assert
        result.Token.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be(input.Email);
    }
}
