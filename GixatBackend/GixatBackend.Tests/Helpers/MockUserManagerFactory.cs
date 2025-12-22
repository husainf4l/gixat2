using GixatBackend.Modules.Users.Models;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace GixatBackend.Tests.Helpers;

/// <summary>
/// Factory for creating mocked UserManager instances
/// </summary>
public static class MockUserManagerFactory
{
    public static Mock<UserManager<ApplicationUser>> Create()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
