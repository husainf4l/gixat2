using System.Globalization;
using GixatBackend.Data;
using GixatBackend.Modules.Customers.Models;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;

namespace GixatBackend.Modules.Customers.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
[Authorize]
internal static class CustomerQueries
{
    [UsePaging]
    [UseFiltering(typeof(CustomerFilterType))]
    [UseSorting]
    public static IQueryable<Customer> GetCustomers(ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Customers.Include(c => c.Cars).Include(c => c.Address);
    }

    [UsePaging]
    [UseProjection]
    public static IQueryable<Customer> SearchCustomers(
        string query,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        if (string.IsNullOrWhiteSpace(query))
        {
            return context.Customers.AsNoTracking();
        }
        
        var searchQuery = $"%{query.Trim()}%";
        
        return context.Customers
            .AsNoTracking()
            .Where(c => 
                EF.Functions.ILike(c.FirstName, searchQuery) ||
                EF.Functions.ILike(c.LastName, searchQuery) ||
                EF.Functions.ILike(c.PhoneNumber, searchQuery)
            );
    }

    public static async Task<Customer?> GetCustomerByIdAsync(Guid id, ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return await context.Customers
            .Include(c => c.Cars)
            .Include(c => c.Address)
            .FirstOrDefaultAsync(c => c.Id == id).ConfigureAwait(false);
    }

    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Car> GetCars(ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Cars;
    }

    public static async Task<Car?> GetCarByIdAsync(Guid id, ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return await context.Cars
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == id).ConfigureAwait(false);
    }

    // Customer statistics
    public static async Task<CustomerStatistics> GetCustomerStatisticsAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        var totalCustomers = await context.Customers.CountAsync(cancellationToken).ConfigureAwait(false);
        var customersThisMonth = await context.Customers
            .CountAsync(c => c.CreatedAt >= startOfMonth, cancellationToken)
            .ConfigureAwait(false);

        var activeCustomers = await context.Customers
            .Where(c => c.Sessions.Any(s => s.CreatedAt >= now.AddMonths(-3)))
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        // Handle case where there are no completed job cards (returns 0 instead of null)
        var completedJobCards = await context.JobCards
            .Where(j => j.Status == GixatBackend.Modules.JobCards.Models.JobCardStatus.Completed)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var totalRevenue = completedJobCards.Sum(j => j.TotalActualCost);

        return new CustomerStatistics
        {
            TotalCustomers = totalCustomers,
            CustomersThisMonth = customersThisMonth,
            ActiveCustomers = activeCustomers,
            TotalRevenue = totalRevenue
        };
    }
}
