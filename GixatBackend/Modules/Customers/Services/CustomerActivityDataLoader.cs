using GixatBackend.Data;
using GixatBackend.Modules.JobCards.Models;
using GixatBackend.Modules.Customers.Models;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.Customers.Services;

internal sealed class CustomerActivityDataLoader
{
    private readonly ApplicationDbContext _context;

    public CustomerActivityDataLoader(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // Batch load last session dates for multiple customers
    public async Task<IReadOnlyDictionary<Guid, DateTime?>> GetLastSessionDatesAsync(
        IReadOnlyList<Guid> customerIds,
        CancellationToken cancellationToken)
    {
        var results = await _context.GarageSessions
            .AsNoTracking()
            .Where(s => customerIds.Contains(s.CustomerId))
            .GroupBy(s => s.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                LastDate = g.Max(s => s.CreatedAt)
            })
            .ToDictionaryAsync(x => x.CustomerId, x => (DateTime?)x.LastDate, cancellationToken)
            .ConfigureAwait(false);

        return results;
    }

    // Batch load visit counts
    public async Task<IReadOnlyDictionary<Guid, int>> GetVisitCountsAsync(
        IReadOnlyList<Guid> customerIds,
        CancellationToken cancellationToken)
    {
        var results = await _context.GarageSessions
            .AsNoTracking()
            .Where(s => customerIds.Contains(s.CustomerId))
            .GroupBy(s => s.CustomerId)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CustomerId, x => x.Count, cancellationToken)
            .ConfigureAwait(false);

        return results;
    }

    // Batch load total spent
    public async Task<IReadOnlyDictionary<Guid, decimal>> GetTotalSpentAsync(
        IReadOnlyList<Guid> customerIds,
        CancellationToken cancellationToken)
    {
        var results = await _context.JobCards
            .AsNoTracking()
            .Where(j => customerIds.Contains(j.CustomerId) && j.Status == JobCardStatus.Completed)
            .GroupBy(j => j.CustomerId)
            .Select(g => new { CustomerId = g.Key, Total = g.Sum(j => j.TotalActualCost) })
            .ToDictionaryAsync(x => x.CustomerId, x => x.Total, cancellationToken)
            .ConfigureAwait(false);

        return results;
    }

    // Batch load active job counts
    public async Task<IReadOnlyDictionary<Guid, int>> GetActiveJobCountsAsync(
        IReadOnlyList<Guid> customerIds,
        CancellationToken cancellationToken)
    {
        var results = await _context.JobCards
            .AsNoTracking()
            .Where(j => customerIds.Contains(j.CustomerId) && 
                       (j.Status == JobCardStatus.Pending || j.Status == JobCardStatus.InProgress))
            .GroupBy(j => j.CustomerId)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CustomerId, x => x.Count, cancellationToken)
            .ConfigureAwait(false);

        return results;
    }

    // Batch load car counts
    public async Task<IReadOnlyDictionary<Guid, int>> GetCarCountsAsync(
        IReadOnlyList<Guid> customerIds,
        CancellationToken cancellationToken)
    {
        var results = await _context.Cars
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.CustomerId))
            .GroupBy(c => c.CustomerId)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CustomerId, x => x.Count, cancellationToken)
            .ConfigureAwait(false);

        return results;
    }

    // Batch load cars for multiple customers
    public async Task<IReadOnlyDictionary<Guid, ICollection<Car>>> GetCarsAsync(
        IReadOnlyList<Guid> customerIds,
        CancellationToken cancellationToken)
    {
        var cars = await _context.Cars
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.CustomerId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return cars
            .GroupBy(c => c.CustomerId)
            .ToDictionary(g => g.Key, g => (ICollection<Car>)g.ToList());
    }
}
