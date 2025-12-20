using GixatBackend.Data;
using GixatBackend.Modules.Sessions.Enums;
using GixatBackend.Modules.Sessions.Models;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;

namespace GixatBackend.Modules.Sessions.GraphQL;

[ExtendObjectType(OperationTypeNames.Mutation)]
[Authorize]
internal static class SessionMutations
{
    public static async Task<GarageSession> CreateSessionAsync(
        Guid carId,
        Guid customerId,
        string? intakeNotes,
        string? customerRequests,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // The global query filter will ensure we only find cars/customers in our organization
        var car = await context.Cars.FindAsync(carId).ConfigureAwait(false);
        var customer = await context.Customers.FindAsync(customerId).ConfigureAwait(false);

        if (car == null || customer == null)
        {
            throw new InvalidOperationException("Car or Customer not found in your organization");
        }

        var session = new GarageSession
        {
            CarId = carId,
            CustomerId = customerId,
            IntakeNotes = intakeNotes,
            CustomerRequests = customerRequests,
            Status = SessionStatus.Intake
        };

        session.Logs.Add(new SessionLog
        {
            FromStatus = SessionStatus.Intake,
            ToStatus = SessionStatus.Intake,
            Notes = "Session started"
        });

        context.GarageSessions.Add(session);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return session;
    }

    public static async Task<GarageSession> UpdateSessionStatusAsync(
        Guid sessionId,
        SessionStatus newStatus,
        string? notes,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var session = await context.GarageSessions
            .Include(s => s.Logs)
            .FirstOrDefaultAsync(s => s.Id == sessionId).ConfigureAwait(false);

        if (session == null)
        {
            throw new InvalidOperationException("Session not found");
        }

        var oldStatus = session.Status;
        session.Status = newStatus;
        session.UpdatedAt = DateTime.UtcNow;

        session.Logs.Add(new SessionLog
        {
            FromStatus = oldStatus,
            ToStatus = newStatus,
            Notes = notes
        });

        await context.SaveChangesAsync().ConfigureAwait(false);
        return session;
    }

    public static async Task<GarageSession> UpdateIntakeAsync(
        Guid sessionId,
        string? intakeNotes,
        string? customerRequests,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var session = await context.GarageSessions.FindAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException("Session not found");
        }

        session.IntakeNotes = intakeNotes;
        session.CustomerRequests = customerRequests;
        session.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync().ConfigureAwait(false);
        return session;
    }

    public static async Task<GarageSession> UpdateInspectionAsync(
        Guid sessionId,
        string? inspectionNotes,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var session = await context.GarageSessions.FindAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException("Session not found");
        }

        session.InspectionNotes = inspectionNotes;
        session.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync().ConfigureAwait(false);
        return session;
    }

    public static async Task<GarageSession> UpdateTestDriveAsync(
        Guid sessionId,
        string? testDriveNotes,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var session = await context.GarageSessions.FindAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException("Session not found");
        }

        session.TestDriveNotes = testDriveNotes;
        session.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync().ConfigureAwait(false);
        return session;
    }

    public static async Task<GarageSession> GenerateInitialReportAsync(
        Guid sessionId,
        string report,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var session = await context.GarageSessions.FindAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException("Session not found");
        }

        session.InitialReport = report;
        session.Status = SessionStatus.ReportGenerated;
        session.UpdatedAt = DateTime.UtcNow;

        session.Logs.Add(new SessionLog
        {
            FromStatus = SessionStatus.TestDrive, // Assuming coming from test drive
            ToStatus = SessionStatus.ReportGenerated,
            Notes = "Initial report generated"
        });

        await context.SaveChangesAsync().ConfigureAwait(false);
        return session;
    }
}
