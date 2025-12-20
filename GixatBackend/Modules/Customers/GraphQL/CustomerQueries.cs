using GixatBackend.Data;
using GixatBackend.Modules.Customers.Models;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.Customers.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
public class CustomerQueries
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Customer> GetCustomers(ApplicationDbContext context)
    {
        return context.Customers;
    }

    public async Task<Customer?> GetCustomerByIdAsync(Guid id, ApplicationDbContext context)
    {
        return await context.Customers
            .Include(c => c.Cars)
            .Include(c => c.Address)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Car> GetCars(ApplicationDbContext context)
    {
        return context.Cars;
    }

    public async Task<Car?> GetCarByIdAsync(Guid id, ApplicationDbContext context)
    {
        return await context.Cars
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == id);
    }
}
