using GixatBackend.Modules.Users.Models;

namespace GixatBackend.Modules.Users.Services;

internal interface IAuthService
{
    Task<AuthPayload> RegisterAsync(RegisterInput input);
    Task<AuthPayload> LoginAsync(LoginInput input);
    Task<AuthPayload> RefreshTokenForUserAsync(ApplicationUser user);
}
