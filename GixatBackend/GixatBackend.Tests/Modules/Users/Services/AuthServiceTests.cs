using FluentAssertions;
using GixatBackend.Data;
using GixatBackend.Modules.Invites.Enums;
using GixatBackend.Modules.Invites.Models;
using GixatBackend.Modules.Users.Enums;
using GixatBackend.Modules.Users.Models;
using GixatBackend.Modules.Users.Services;
using GixatBackend.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GixatBackend.Tests.Modules.Users.Services;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly ApplicationDbContext _context;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userManagerMock = MockUserManagerFactory.Create();
        _roleManagerMock = MockRoleManagerFactory.Create();
        _configurationMock = MockConfigurationFactory.CreateWithJwtSettings();
        _context = TestDbContextFactory.CreateInMemoryContext();

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

    [Fact]
    public async Task RegisterAsync_ShouldCreateRoleIfNotExists()
    {
        // Arrange
        var input = new RegisterInput("test@example.com", "Password123!", "Test User", "NewRole", UserType.Organizational, Guid.NewGuid());

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), input.Password))
            .ReturnsAsync(IdentityResult.Success);

        _roleManagerMock.Setup(x => x.RoleExistsAsync(input.Role))
            .ReturnsAsync(false);

        _roleManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityRole>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), input.Role))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { input.Role });

        // Act
        var result = await _authService.RegisterAsync(input);

        // Assert
        result.Token.Should().NotBeNullOrEmpty();
        _roleManagerMock.Verify(x => x.CreateAsync(It.Is<IdentityRole>(r => r.Name == input.Role)), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldUseInviteCode_WhenProvided()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var inviteCode = "INVITE123";
        var invite = new UserInvite
        {
            Id = Guid.NewGuid(),
            InviteCode = inviteCode,
            Email = "test@example.com",
            Role = "Manager",
            OrganizationId = orgId,
            Status = InviteStatus.Pending,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        _context.UserInvites.Add(invite);
        await _context.SaveChangesAsync();

        var input = new RegisterInput("test@example.com", "Password123!", "Test User", "User", UserType.Organizational, null, inviteCode);

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), input.Password))
            .ReturnsAsync(IdentityResult.Success);

        _roleManagerMock.Setup(x => x.RoleExistsAsync("Manager"))
            .ReturnsAsync(true);

        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Manager"))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "Manager" });

        // Act
        var result = await _authService.RegisterAsync(input);

        // Assert
        result.Token.Should().NotBeNullOrEmpty();
        result.User.OrganizationId.Should().Be(orgId);
        result.User.UserType.Should().Be(UserType.Organizational);

        // Verify invite was marked as accepted
        var updatedInvite = await _context.UserInvites.FindAsync(invite.Id);
        updatedInvite!.Status.Should().Be(InviteStatus.Accepted);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnError_WhenInviteCodeIsInvalid()
    {
        // Arrange
        var input = new RegisterInput("test@example.com", "Password123!", "Test User", "User", UserType.Organizational, null, "INVALID_CODE");

        // Act
        var result = await _authService.RegisterAsync(input);

        // Assert
        result.Token.Should().BeNull();
        result.User.Should().BeNull();
        result.Error.Should().Contain("Invalid or expired invite code");
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnError_WhenInviteCodeIsExpired()
    {
        // Arrange
        var expiredInvite = new UserInvite
        {
            Id = Guid.NewGuid(),
            InviteCode = "EXPIRED123",
            Email = "test@example.com",
            Role = "User",
            OrganizationId = Guid.NewGuid(),
            Status = InviteStatus.Pending,
            ExpiryDate = DateTime.UtcNow.AddDays(-1), // Expired
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };

        _context.UserInvites.Add(expiredInvite);
        await _context.SaveChangesAsync();

        var input = new RegisterInput("test@example.com", "Password123!", "Test User", "User", UserType.Organizational, null, "EXPIRED123");

        // Act
        var result = await _authService.RegisterAsync(input);

        // Assert
        result.Token.Should().BeNull();
        result.Error.Should().Contain("Invalid or expired invite code");
    }

    [Fact]
    public async Task RefreshTokenForUserAsync_ShouldGenerateNewToken()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser("test@example.com", Guid.NewGuid());

        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var result = await _authService.RefreshTokenForUserAsync(user);

        // Assert
        result.Token.Should().NotBeNullOrEmpty();
        result.User.Should().Be(user);
    }

    [Fact]
    public async Task RefreshTokenForUserAsync_ShouldThrow_WhenUserIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _authService.RefreshTokenForUserAsync(null!));
    }

    [Fact]
    public async Task GenerateJwtToken_ShouldIncludeOrganizationIdClaim_WhenUserHasOrganization()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var user = TestDataBuilder.CreateUser("test@example.com", orgId);

        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var result = await _authService.RefreshTokenForUserAsync(user);

        // Assert
        result.Token.Should().NotBeNullOrEmpty();

        // Decode JWT to verify claims
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(result.Token) as JwtSecurityToken;

        var orgIdClaim = jsonToken!.Claims.FirstOrDefault(c => c.Type == "OrganizationId");
        orgIdClaim.Should().NotBeNull();
        orgIdClaim!.Value.Should().Be(orgId.ToString());
    }

    [Fact]
    public async Task GenerateJwtToken_ShouldNotIncludeOrganizationIdClaim_WhenUserHasNoOrganization()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser("admin@example.com", null, UserType.System);

        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Admin" });

        // Act
        var result = await _authService.RefreshTokenForUserAsync(user);

        // Assert
        result.Token.Should().NotBeNullOrEmpty();

        // Decode JWT to verify claims
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(result.Token) as JwtSecurityToken;

        var orgIdClaim = jsonToken!.Claims.FirstOrDefault(c => c.Type == "OrganizationId");
        orgIdClaim.Should().BeNull();
    }

    [Fact]
    public async Task GenerateJwtToken_ShouldIncludeRoleClaims()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser("test@example.com");

        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Admin", "User" });

        // Act
        var result = await _authService.RefreshTokenForUserAsync(user);

        // Assert
        result.Token.Should().NotBeNullOrEmpty();

        // Decode JWT to verify claims
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(result.Token) as JwtSecurityToken;

        var roleClaims = jsonToken!.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        roleClaims.Should().HaveCount(2);
        roleClaims.Should().Contain(c => c.Value == "Admin");
        roleClaims.Should().Contain(c => c.Value == "User");
    }

    [Fact]
    public async Task GenerateJwtToken_ShouldIncludeStandardClaims()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser("test@example.com");

        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var result = await _authService.RefreshTokenForUserAsync(user);

        // Assert
        result.Token.Should().NotBeNullOrEmpty();

        // Decode JWT to verify claims
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(result.Token) as JwtSecurityToken;

        jsonToken!.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id);
        jsonToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        jsonToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
        jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id);
    }
}
