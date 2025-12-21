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
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Customer> GetCustomers(ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Customers;
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
}
