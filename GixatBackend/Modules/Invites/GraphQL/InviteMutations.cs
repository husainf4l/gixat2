using System.Security.Claims;
using System.Security.Cryptography;
using GixatBackend.Data;
using GixatBackend.Modules.Invites.Models;
using GixatBackend.Modules.Invites.Enums;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Invites.GraphQL;

[ExtendObjectType(OperationTypeNames.Mutation)]
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by HotChocolate")]
internal sealed class InviteMutations
{
    [Authorize(Roles = new[] { "OrgAdmin", "OrgManager" })]
    public static async Task<InvitePayload> InviteUserAsync(
        InviteUserInput input,
        ClaimsPrincipal claimsPrincipal,
        [Service] ApplicationDbContext context,
        [Service] IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(configuration);

        var inviterId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        
        // Generate a 12-character alphanumeric code
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var code = new string(Enumerable.Range(0, 12)
            .Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)]).ToArray());

        var invite = new UserInvite
        {
            Email = input.Email,
            Role = input.Role,
            InviteCode = code,
            ExpiryDate = DateTime.UtcNow.AddHours(24),
            InviterId = inviterId,
            Status = InviteStatus.Pending
        };

        context.UserInvites.Add(invite);
        await context.SaveChangesAsync().ConfigureAwait(false);

        var frontendUrl = configuration["FrontendUrl"] ?? "http://localhost:4200";
        var link = $"{frontendUrl}/register?code={invite.InviteCode}";

        return new InvitePayload(invite, link);
    }

    [Authorize(Roles = new[] { "OrgAdmin", "OrgManager" })]
    public static async Task<bool> CancelInviteAsync(
        Guid id,
        [Service] ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var invite = await context.UserInvites.FindAsync(id).ConfigureAwait(false);
        if (invite == null) return false;

        invite.Status = InviteStatus.Canceled;
        await context.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }
}

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public record InviteUserInput(string Email, string Role);

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public record InvitePayload(UserInvite? Invite, string? Link, string? Error = null);
