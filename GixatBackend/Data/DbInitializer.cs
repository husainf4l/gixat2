using GixatBackend.Modules.Common.Lookup.Models;
using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Modules.Customers.Models;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Data;

internal static class DbInitializer
{
    public static async Task SeedLookupDataAsync(ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!await context.LookupItems.AnyAsync(l => l.Category == "CarMake").ConfigureAwait(false))
        {
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
            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        if (!await context.LookupItems.AnyAsync(l => l.Category == "CarColor").ConfigureAwait(false))
        {
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

        var existingCountries = await context.LookupItems
            .Where(l => l.Category == "Country")
            .ToListAsync()
            .ConfigureAwait(false);

        if (existingCountries.Count == 0)
        {
            var countries = new List<LookupItem>
            {
                new LookupItem { Category = "Country", Value = "Jordan", Metadata = "{\"phoneCode\": \"+962\", \"phoneLength\": 9}", SortOrder = 1 },
                new LookupItem { Category = "Country", Value = "UAE", Metadata = "{\"phoneCode\": \"+971\", \"phoneLength\": 9}", SortOrder = 2 },
                new LookupItem { Category = "Country", Value = "KSA", Metadata = "{\"phoneCode\": \"+966\", \"phoneLength\": 9}", SortOrder = 3 },
                new LookupItem { Category = "Country", Value = "Qatar", Metadata = "{\"phoneCode\": \"+974\", \"phoneLength\": 8}", SortOrder = 4 },
                new LookupItem { Category = "Country", Value = "Bahrain", Metadata = "{\"phoneCode\": \"+973\", \"phoneLength\": 8}", SortOrder = 5 },
                new LookupItem { Category = "Country", Value = "Kuwait", Metadata = "{\"phoneCode\": \"+965\", \"phoneLength\": 8}", SortOrder = 6 },
                new LookupItem { Category = "Country", Value = "Lebanon", Metadata = "{\"phoneCode\": \"+961\", \"phoneLength\": 8}", SortOrder = 7 },
                new LookupItem { Category = "Country", Value = "Palestine", Metadata = "{\"phoneCode\": \"+970\", \"phoneLength\": 9}", SortOrder = 8 },
                new LookupItem { Category = "Country", Value = "Egypt", Metadata = "{\"phoneCode\": \"+20\", \"phoneLength\": 10}", SortOrder = 9 }
            };

            context.LookupItems.AddRange(countries);
            await context.SaveChangesAsync().ConfigureAwait(false);
            existingCountries = countries;
        }
        else 
        {
            // Update Jordan if it was seeded with 10
            var jordan = existingCountries.FirstOrDefault(c => c.Value == "Jordan");
            if (jordan != null && jordan.Metadata == "{\"phoneCode\": \"+962\", \"phoneLength\": 10}")
            {
                jordan.Metadata = "{\"phoneCode\": \"+962\", \"phoneLength\": 9}";
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        if (!await context.LookupItems.AnyAsync(l => l.Category == "City").ConfigureAwait(false))
        {
            var jordan = existingCountries.First(c => c.Value == "Jordan");
            var uae = existingCountries.First(c => c.Value == "UAE");
            var ksa = existingCountries.First(c => c.Value == "KSA");
            var qatar = existingCountries.First(c => c.Value == "Qatar");
            var bahrain = existingCountries.First(c => c.Value == "Bahrain");
            var kuwait = existingCountries.First(c => c.Value == "Kuwait");
            var lebanon = existingCountries.First(c => c.Value == "Lebanon");
            var palestine = existingCountries.First(c => c.Value == "Palestine");
            var egypt = existingCountries.First(c => c.Value == "Egypt");

            var cities = new List<LookupItem>
            {
                // Jordan
                new LookupItem { Category = "City", Value = "Amman", ParentId = jordan.Id },
                new LookupItem { Category = "City", Value = "Ajloun", ParentId = jordan.Id },
                new LookupItem { Category = "City", Value = "Irbid", ParentId = jordan.Id },
                new LookupItem { Category = "City", Value = "Zarqa", ParentId = jordan.Id },
                new LookupItem { Category = "City", Value = "Madaba", ParentId = jordan.Id },
                new LookupItem { Category = "City", Value = "Salt", ParentId = jordan.Id },
                new LookupItem { Category = "City", Value = "Aqaba", ParentId = jordan.Id },
                new LookupItem { Category = "City", Value = "Mafraq", ParentId = jordan.Id },
                new LookupItem { Category = "City", Value = "Jerash", ParentId = jordan.Id },
                new LookupItem { Category = "City", Value = "Ma'an", ParentId = jordan.Id },
                new LookupItem { Category = "City", Value = "Tafilah", ParentId = jordan.Id },
                new LookupItem { Category = "City", Value = "Karak", ParentId = jordan.Id },

                // UAE
                new LookupItem { Category = "City", Value = "Dubai", ParentId = uae.Id },
                new LookupItem { Category = "City", Value = "Abu Dhabi", ParentId = uae.Id },
                new LookupItem { Category = "City", Value = "Sharjah", ParentId = uae.Id },
                new LookupItem { Category = "City", Value = "Ajman", ParentId = uae.Id },
                new LookupItem { Category = "City", Value = "Umm Al Quwain", ParentId = uae.Id },
                new LookupItem { Category = "City", Value = "Ras Al Khaimah", ParentId = uae.Id },
                new LookupItem { Category = "City", Value = "Fujairah", ParentId = uae.Id },

                // KSA
                new LookupItem { Category = "City", Value = "Riyadh", ParentId = ksa.Id },
                new LookupItem { Category = "City", Value = "Jeddah", ParentId = ksa.Id },
                new LookupItem { Category = "City", Value = "Mecca", ParentId = ksa.Id },
                new LookupItem { Category = "City", Value = "Medina", ParentId = ksa.Id },
                new LookupItem { Category = "City", Value = "Dammam", ParentId = ksa.Id },
                new LookupItem { Category = "City", Value = "Khobar", ParentId = ksa.Id },
                new LookupItem { Category = "City", Value = "Abha", ParentId = ksa.Id },
                new LookupItem { Category = "City", Value = "Tabuk", ParentId = ksa.Id },
                new LookupItem { Category = "City", Value = "Buraidah", ParentId = ksa.Id },
                new LookupItem { Category = "City", Value = "Khamis Mushait", ParentId = ksa.Id },

                // Qatar
                new LookupItem { Category = "City", Value = "Doha", ParentId = qatar.Id },
                new LookupItem { Category = "City", Value = "Al Rayyan", ParentId = qatar.Id },
                new LookupItem { Category = "City", Value = "Al Wakrah", ParentId = qatar.Id },
                new LookupItem { Category = "City", Value = "Al Khor", ParentId = qatar.Id },
                new LookupItem { Category = "City", Value = "Umm Salal", ParentId = qatar.Id },

                // Bahrain
                new LookupItem { Category = "City", Value = "Manama", ParentId = bahrain.Id },
                new LookupItem { Category = "City", Value = "Riffa", ParentId = bahrain.Id },
                new LookupItem { Category = "City", Value = "Muharraq", ParentId = bahrain.Id },
                new LookupItem { Category = "City", Value = "Hamad Town", ParentId = bahrain.Id },

                // Kuwait
                new LookupItem { Category = "City", Value = "Kuwait City", ParentId = kuwait.Id },
                new LookupItem { Category = "City", Value = "Jahra", ParentId = kuwait.Id },
                new LookupItem { Category = "City", Value = "Hawally", ParentId = kuwait.Id },
                new LookupItem { Category = "City", Value = "Salmiya", ParentId = kuwait.Id },

                // Lebanon
                new LookupItem { Category = "City", Value = "Beirut", ParentId = lebanon.Id },
                new LookupItem { Category = "City", Value = "Tripoli", ParentId = lebanon.Id },
                new LookupItem { Category = "City", Value = "Sidon", ParentId = lebanon.Id },
                new LookupItem { Category = "City", Value = "Tyre", ParentId = lebanon.Id },
                new LookupItem { Category = "City", Value = "Baalbek", ParentId = lebanon.Id },
                new LookupItem { Category = "City", Value = "Byblos", ParentId = lebanon.Id },

                // Palestine
                new LookupItem { Category = "City", Value = "Jerusalem", ParentId = palestine.Id },
                new LookupItem { Category = "City", Value = "Ramallah", ParentId = palestine.Id },
                new LookupItem { Category = "City", Value = "Gaza", ParentId = palestine.Id },
                new LookupItem { Category = "City", Value = "Nablus", ParentId = palestine.Id },
                new LookupItem { Category = "City", Value = "Hebron", ParentId = palestine.Id },
                new LookupItem { Category = "City", Value = "Bethlehem", ParentId = palestine.Id },
                new LookupItem { Category = "City", Value = "Jenin", ParentId = palestine.Id },
                new LookupItem { Category = "City", Value = "Jericho", ParentId = palestine.Id },
                new LookupItem { Category = "City", Value = "Tulkarm", ParentId = palestine.Id },
                new LookupItem { Category = "City", Value = "Qalqilya", ParentId = palestine.Id },

                // Egypt
                new LookupItem { Category = "City", Value = "Cairo", ParentId = egypt.Id },
                new LookupItem { Category = "City", Value = "Alexandria", ParentId = egypt.Id },
                new LookupItem { Category = "City", Value = "Giza", ParentId = egypt.Id },
                new LookupItem { Category = "City", Value = "Sharm El Sheikh", ParentId = egypt.Id },
                new LookupItem { Category = "City", Value = "Luxor", ParentId = egypt.Id },
                new LookupItem { Category = "City", Value = "Aswan", ParentId = egypt.Id },
                new LookupItem { Category = "City", Value = "Mansoura", ParentId = egypt.Id },
                new LookupItem { Category = "City", Value = "Tanta", ParentId = egypt.Id },
                new LookupItem { Category = "City", Value = "Port Said", ParentId = egypt.Id }
            };

            context.LookupItems.AddRange(cities);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
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
