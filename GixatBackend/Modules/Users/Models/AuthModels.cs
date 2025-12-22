using GixatBackend.Modules.Users.Enums;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Users.Models;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed record LoginInput(string Email, string Password);

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed record RegisterInput(string Email, string Password, string FullName, string Role, UserType UserType, Guid? OrganizationId = null, string? InviteCode = null);

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed record AuthPayload(string? Token, ApplicationUser? User, string? Error = null);
