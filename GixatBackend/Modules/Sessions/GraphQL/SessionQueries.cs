using GixatBackend.Data;
using GixatBackend.Modules.Sessions.Models;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;

namespace GixatBackend.Modules.Sessions.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
[Authorize]
internal static class SessionQueries
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<GarageSession> GetSessions(ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.GarageSessions;
    }

    public static async Task<GarageSession?> GetSessionByIdAsync(Guid id, ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return await context.GarageSessions
            .Include(s => s.Car)
            .Include(s => s.Customer)
            .Include(s => s.Media)
                .ThenInclude(m => m.Media)
            .Include(s => s.Logs)
            .FirstOrDefaultAsync(s => s.Id == id).ConfigureAwait(false);
    }
}
