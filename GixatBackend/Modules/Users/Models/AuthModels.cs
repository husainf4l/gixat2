using GixatBackend.Modules.Users.Enums;

namespace GixatBackend.Modules.Users.Models;

public record LoginInput(string Email, string Password);
public record RegisterInput(string Email, string Password, string FullName, string Role, UserType UserType, Guid OrganizationId);
public record AuthPayload(string? Token, ApplicationUser? User, string? Error = null);
