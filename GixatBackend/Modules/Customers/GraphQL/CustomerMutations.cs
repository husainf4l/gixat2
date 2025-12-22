using GixatBackend.Data;
using GixatBackend.Modules.Customers.Models;
using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Common.Services;
using GixatBackend.Modules.Common.Exceptions;
using GixatBackend.Modules.Customers.Services;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Authorization;

namespace GixatBackend.Modules.Customers.GraphQL;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required by HotChocolate for schema discovery")]
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by HotChocolate")]
public sealed record CreateCustomerInput(
    string FirstName,
    string LastName,
    string? Email,
    string PhoneNumber,
    string? Country,
    string? City,
    string? Street,
    string? PhoneCountryCode);

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required by HotChocolate for schema discovery")]
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by HotChocolate")]
public sealed record CreateCarInput(
    Guid CustomerId,
    string Make,
    string Model,
    int Year,
    string LicensePlate,
    string? VIN,
    string? Color);

[ExtendObjectType(OperationTypeNames.Mutation)]
[Authorize]
internal static class CustomerMutations
{
    public static async Task<Customer> CreateCustomerAsync(
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
                Street = input.Street ?? "",
                PhoneCountryCode = input.PhoneCountryCode ?? ""
            };
        }

        context.Customers.Add(customer);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return customer;
    }

    public static async Task<Car> CreateCarAsync(
        CreateCarInput input,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        // Verify customer belongs to the same organization (Global Filter handles this)
        // Use AsNoTracking to avoid customer being detached during SaveChanges
        var customer = await context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == input.CustomerId)
            .ConfigureAwait(false);
            
        if (customer == null)
        {
            throw new EntityNotFoundException("Customer", input.CustomerId);
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

    // Export customers to CSV
    public static async Task<string> ExportCustomersToCsvAsync(
        [Service] CustomerExportService exportService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(exportService);

        var csvBytes = await exportService.ExportCustomersToCsvAsync(cancellationToken).ConfigureAwait(false);
        var base64 = Convert.ToBase64String(csvBytes);
        
        return base64; // Return base64 encoded CSV that frontend can download
    }
}
