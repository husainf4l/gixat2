using GixatBackend.Data;
using GixatBackend.Modules.Customers.Models;
using GixatBackend.Modules.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.Customers.GraphQL;

public record CreateCustomerInput(
    string FirstName,
    string LastName,
    string? Email,
    string PhoneNumber,
    string? Country,
    string? City,
    string? Street);

public record CreateCarInput(
    Guid CustomerId,
    string Make,
    string Model,
    int Year,
    string LicensePlate,
    string? VIN,
    string? Color);

[ExtendObjectType(OperationTypeNames.Mutation)]
public class CustomerMutations
{
    public async Task<Customer> CreateCustomerAsync(
        CreateCustomerInput input,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        var customer = new Customer
        {
            FirstName = input.FirstName,
            LastName = input.LastName,
            Email = input.Email,
            PhoneNumber = input.PhoneNumber
        };

        if (!string.IsNullOrEmpty(input.Country))
        {
            customer.Address = new Address
            {
                Country = input.Country,
                City = input.City ?? "",
                Street = input.Street ?? ""
            };
        }

        context.Customers.Add(customer);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return customer;
    }

    public async Task<Car> CreateCarAsync(
        CreateCarInput input,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        // Verify customer belongs to the same organization (Global Filter handles this)
        var customer = await context.Customers.FindAsync(input.CustomerId).ConfigureAwait(false);
        if (customer == null)
        {
            throw new InvalidOperationException("Customer not found in your organization");
        }

        var car = new Car
        {
            CustomerId = input.CustomerId,
            Make = input.Make,
            Model = input.Model,
            Year = input.Year,
            LicensePlate = input.LicensePlate,
            VIN = input.VIN,
            Color = input.Color
        };

        context.Cars.Add(car);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return car;
    }
}
