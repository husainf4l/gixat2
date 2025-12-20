using GixatBackend.Data;
using GixatBackend.Modules.Customers.Models;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;

namespace GixatBackend.Modules.Customers.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
[Authorize]
internal static class CustomerQueries
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Customer> GetCustomers(ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Customers;
    }

    public static async Task<Customer?> GetCustomerByIdAsync(Guid id, ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return await context.Customers
            .Include(c => c.Cars)
            .Include(c => c.Address)
            .FirstOrDefaultAsync(c => c.Id == id).ConfigureAwait(false);
    }

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
