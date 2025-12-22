using Microsoft.AspNetCore.Identity;
using Moq;

namespace GixatBackend.Tests.Helpers;

/// <summary>
/// Factory for creating mocked RoleManager instances
/// </summary>
public static class MockRoleManagerFactory
{
    public static Mock<RoleManager<IdentityRole>> Create()
    {
        var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
        return new Mock<RoleManager<IdentityRole>>(
            roleStoreMock.Object, null!, null!, null!, null!);
    }
}
