using GixatBackend.Data;
using GixatBackend.Modules.Invites.Models;
using GixatBackend.Modules.Invites.Enums;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Invites.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by HotChocolate")]
internal sealed class InviteQueries
{
    [Authorize(Roles = new[] { "OrgAdmin", "OrgManager" })]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<UserInvite> GetInvites([Service] ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.UserInvites;
    }

    [AllowAnonymous]
    public static async Task<UserInvite?> GetInviteByCodeAsync(
        string code,
        [Service] ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return await context.UserInvites
            .FirstOrDefaultAsync(i => i.InviteCode == code && i.Status == InviteStatus.Pending && i.ExpiryDate > DateTime.UtcNow)
            .ConfigureAwait(false);
    }
}
