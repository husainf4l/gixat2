using GixatBackend.Modules.Users.Models;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Authorization;

namespace GixatBackend.Modules.Users.GraphQL;

[ExtendObjectType(typeof(ApplicationUser))]
[Authorize]
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by HotChocolate")]
internal static class UserExtensions
{
    public static async Task<IList<string>> GetRolesAsync(
        [Parent] ApplicationUser user,
        [Service] UserManager<ApplicationUser> userManager)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(userManager);
        
        return await userManager.GetRolesAsync(user).ConfigureAwait(false);
    }
}
