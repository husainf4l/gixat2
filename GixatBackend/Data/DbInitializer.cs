using GixatBackend.Modules.Lookup.Models;
using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Modules.Customers.Models;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Data;

internal static class DbInitializer
{
    public static async Task SeedLookupDataAsync(ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (await context.LookupItems.AnyAsync().ConfigureAwait(false))
        {
            return; // Already seeded
        }

        var makes = new List<LookupItem>
        {
            new LookupItem { Category = "CarMake", Value = "Toyota", SortOrder = 1 },
            new LookupItem { Category = "CarMake", Value = "BMW", SortOrder = 2 },
            new LookupItem { Category = "CarMake", Value = "Mercedes-Benz", SortOrder = 3 },
            new LookupItem { Category = "CarMake", Value = "Honda", SortOrder = 4 },
            new LookupItem { Category = "CarMake", Value = "Ford", SortOrder = 5 },
            new LookupItem { Category = "CarMake", Value = "Hyundai", SortOrder = 6 },
            new LookupItem { Category = "CarMake", Value = "Kia", SortOrder = 7 },
            new LookupItem { Category = "CarMake", Value = "Nissan", SortOrder = 8 },
            new LookupItem { Category = "CarMake", Value = "Volkswagen", SortOrder = 9 },
            new LookupItem { Category = "CarMake", Value = "Audi", SortOrder = 10 }
        };

        context.LookupItems.AddRange(makes);
        await context.SaveChangesAsync().ConfigureAwait(false);

        var toyota = makes.First(m => m.Value == "Toyota");
        var bmw = makes.First(m => m.Value == "BMW");

        var models = new List<LookupItem>
        {
            // Toyota Models
            new LookupItem { Category = "CarModel", Value = "Camry", ParentId = toyota.Id, SortOrder = 1 },
            new LookupItem { Category = "CarModel", Value = "Corolla", ParentId = toyota.Id, SortOrder = 2 },
            new LookupItem { Category = "CarModel", Value = "RAV4", ParentId = toyota.Id, SortOrder = 3 },
            new LookupItem { Category = "CarModel", Value = "Hilux", ParentId = toyota.Id, SortOrder = 4 },
            new LookupItem { Category = "CarModel", Value = "Land Cruiser", ParentId = toyota.Id, SortOrder = 5 },

            // BMW Models
            new LookupItem { Category = "CarModel", Value = "X5", ParentId = bmw.Id, SortOrder = 1 },
            new LookupItem { Category = "CarModel", Value = "3 Series", ParentId = bmw.Id, SortOrder = 2 },
            new LookupItem { Category = "CarModel", Value = "5 Series", ParentId = bmw.Id, SortOrder = 3 },
            new LookupItem { Category = "CarModel", Value = "X3", ParentId = bmw.Id, SortOrder = 4 },
            new LookupItem { Category = "CarModel", Value = "7 Series", ParentId = bmw.Id, SortOrder = 5 }
        };

        context.LookupItems.AddRange(models);

        // Colors
        var colors = new List<LookupItem>
        {
            new LookupItem { Category = "CarColor", Value = "White", Metadata = "#FFFFFF" },
            new LookupItem { Category = "CarColor", Value = "Black", Metadata = "#000000" },
            new LookupItem { Category = "CarColor", Value = "Silver", Metadata = "#C0C0C0" },
            new LookupItem { Category = "CarColor", Value = "Grey", Metadata = "#808080" },
            new LookupItem { Category = "CarColor", Value = "Red", Metadata = "#FF0000" },
            new LookupItem { Category = "CarColor", Value = "Blue", Metadata = "#0000FF" }
        };

        context.LookupItems.AddRange(colors);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public static async Task SeedExampleDataAsync(ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (await context.Organizations.AnyAsync().ConfigureAwait(false))
        {
            return; // Already seeded
        }

        var organization = new Organization
        {
            Name = "Gixat Garage Main"
        };

        context.Organizations.Add(organization);
        await context.SaveChangesAsync().ConfigureAwait(false);

        var customers = new List<Customer>
        {
            new Customer 
            { 
                FirstName = "John", 
                LastName = "Doe", 
                Email = "john.doe@example.com", 
                PhoneNumber = "+1234567890",
                OrganizationId = organization.Id
            },
            new Customer 
            { 
                FirstName = "Jane", 
                LastName = "Smith", 
                Email = "jane.smith@example.com", 
                PhoneNumber = "+0987654321",
                OrganizationId = organization.Id
            }
        };

        context.Customers.AddRange(customers);
        await context.SaveChangesAsync().ConfigureAwait(false);

        var cars = new List<Car>
        {
            new Car 
            { 
                Make = "Toyota", 
                Model = "Camry", 
                Year = 2022, 
                LicensePlate = "ABC-123", 
                Color = "White",
                CustomerId = customers[0].Id,
                OrganizationId = organization.Id
            },
            new Car 
            { 
                Make = "BMW", 
                Model = "X5", 
                Year = 2021, 
                LicensePlate = "XYZ-789", 
                Color = "Black",
                CustomerId = customers[0].Id,
                OrganizationId = organization.Id
            },
            new Car 
            { 
                Make = "Mercedes-Benz", 
                Model = "C-Class", 
                Year = 2023, 
                LicensePlate = "GIX-001", 
                Color = "Silver",
                CustomerId = customers[1].Id,
                OrganizationId = organization.Id
            }
        };

        context.Cars.AddRange(cars);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }
}
