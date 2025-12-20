using GixatBackend.Data;
using GixatBackend.Modules.Customers.Models;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;
using System.Globalization;

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
        
        var searchQuery = query.Trim().ToUpperInvariant();
        
        return context.Customers
            .Include(c => c.Cars)
            .Where(c => 
                c.FirstName.ToUpperInvariant().Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                c.LastName.ToUpperInvariant().Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                (c.Email != null && c.Email.ToUpperInvariant().Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) ||
                c.PhoneNumber.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                c.Cars.Any(car => 
                    car.Make.ToUpperInvariant().Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    car.Model.ToUpperInvariant().Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    car.LicensePlate.ToUpperInvariant().Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    (car.VIN != null && car.VIN.ToUpperInvariant().Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                )
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
