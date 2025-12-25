using GixatBackend.Data;
using GixatBackend.Modules.Organizations.GraphQL;
using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Modules.Users.Models;
using GixatBackend.Modules.Users.Services;
using GixatBackend.Modules.Users.GraphQL;
using GixatBackend.Tests.Helpers;
using GixatBackend.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using Xunit;
using FluentAssertions;
using GixatBackend.Modules.Common.Services.Tenant;

namespace GixatBackend.Tests.Modules.Organizations.GraphQL;

public class OrganizationMutationsTests : MultiTenancyTestBase
{
    /// <summary>
    /// Null implementation of ITenantService for testing.
    /// </summary>
    private class NullTenantService : ITenantService
    {
        public Guid? OrganizationId => null;
    }
    [Fact]
    public async Task CreateOrganizationAsync_ShouldAssignOrgAdminRoleToCreator()
    {
        // Arrange
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging();

        // Add tenant service
        services.AddScoped<ITenantService, NullTenantService>();

        // Add DbContext with in-memory database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("TestDb")
                   .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

        // Add Identity services
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        var serviceProvider = services.BuildServiceProvider();

        // Create scope and get services
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Create test user
        var user = new ApplicationUser
        {
            Id = "test-user-id",
            UserName = "test@example.com",
            Email = "test@example.com",
            OrganizationId = null // Will be set when org is created
        };

        var createResult = await userManager.CreateAsync(user, "password123");
        createResult.Succeeded.Should().BeTrue();

        // Ensure OrgAdmin role exists
        if (!await roleManager.RoleExistsAsync("OrgAdmin"))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole("OrgAdmin"));
            roleResult.Succeeded.Should().BeTrue();
        }

        // Create AuthService mock
        var authServiceMock = new Mock<IAuthService>();
        authServiceMock.Setup(x => x.RefreshTokenForUserAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new AuthPayload("test-token", AuthUserInfo.FromApplicationUser(user)));

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        }));

        var httpContextAccessor = new HttpContextAccessor();

        var input = new CreateOrganizationInput(
            "Test Organization",
            new AddressInput("Jordan", "Amman", "Test Street", "+962"),
            null,
            null
        );

        // Act
        var result = await OrganizationMutations.CreateOrganizationAsync(
            input,
            claimsPrincipal,
            context,
            userManager,
            authServiceMock.Object,
            httpContextAccessor
        );

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("test-token");

        // Verify user is assigned to organization
        var updatedUser = await context.Users.FindAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.OrganizationId.Should().NotBeNull();

        // Verify OrgAdmin role is assigned
        var roles = await userManager.GetRolesAsync(updatedUser);
        roles.Should().Contain("OrgAdmin");

        // Verify organization exists
        var organization = await context.Organizations.FindAsync(updatedUser.OrganizationId);
        organization.Should().NotBeNull();
        organization!.Name.Should().Be("Test Organization");
    }
}