using FluentAssertions;
using GixatBackend.Modules.Customers.GraphQL;
using GixatBackend.Tests.Helpers;
using GixatBackend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Tests.Modules.Customers.GraphQL;

public class CustomerQueriesTests : IntegrationTestBase
{
    [Fact]
    public async Task GetCustomers_ShouldReturnOnlyOrganizationCustomers()
    {
        // Arrange
        await CleanDatabaseAsync();
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        // Create organizations first (required for foreign key constraint)
        await CreateOrganizationsAsync(org1Id, org2Id);

        var contextOrg1 = CreateDbContext(org1Id);
        var contextOrg2 = CreateDbContext(org2Id);

        // Add customers to org1
        var customer1 = CreateTestCustomer(org1Id, "Customer One");
        var customer2 = CreateTestCustomer(org1Id, "Customer Two");
        contextOrg1.Customers.AddRange(customer1, customer2);
        await contextOrg1.SaveChangesAsync();

        // Add customers to org2
        var customer3 = CreateTestCustomer(org2Id, "Customer Three");
        contextOrg2.Customers.Add(customer3);
        await contextOrg2.SaveChangesAsync();

        // Act
        var org1Customers = await CustomerQueries.GetCustomers(contextOrg1).ToListAsync();
        var org2Customers = await CustomerQueries.GetCustomers(contextOrg2).ToListAsync();

        // Assert
        org1Customers.Should().HaveCount(2);
        org1Customers.Should().Contain(c => c.FirstName == "Customer" && c.LastName == "One");
        org1Customers.Should().Contain(c => c.FirstName == "Customer" && c.LastName == "Two");

        org2Customers.Should().HaveCount(1);
        org2Customers.Should().Contain(c => c.FirstName == "Customer" && c.LastName == "Three");
    }

    [Fact]
    public async Task SearchCustomers_ShouldFilterByOrganization()
    {
        // Arrange
        await CleanDatabaseAsync();
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        // Create organizations first (required for foreign key constraint)
        await CreateOrganizationsAsync(org1Id, org2Id);

        var contextOrg1 = CreateDbContext(org1Id);
        var contextOrg2 = CreateDbContext(org2Id);

        var customer1 = CreateTestCustomer(org1Id, "John Doe");
        var customer2 = CreateTestCustomer(org2Id, "John Smith");
        
        contextOrg1.Customers.Add(customer1);
        await contextOrg1.SaveChangesAsync();
        
        contextOrg2.Customers.Add(customer2);
        await contextOrg2.SaveChangesAsync();

        // Act
        var org1Results = await CustomerQueries.SearchCustomers("John", contextOrg1).ToListAsync();
        var org2Results = await CustomerQueries.SearchCustomers("John", contextOrg2).ToListAsync();

        // Assert
        org1Results.Should().HaveCount(1);
        org1Results[0].LastName.Should().Be("Doe");

        org2Results.Should().HaveCount(1);
        org2Results[0].LastName.Should().Be("Smith");
    }

    [Fact]
    public async Task GetCustomerById_ShouldReturnNull_WhenCustomerBelongsToAnotherOrganization()
    {
        // Arrange
        await CleanDatabaseAsync();
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        // Create organizations first (required for foreign key constraint)
        await CreateOrganizationsAsync(org1Id, org2Id);

        var contextOrg1 = CreateDbContext(org1Id);
        var contextOrg2 = CreateDbContext(org2Id);

        var customer = CreateTestCustomer(org1Id, "Test Customer");
        contextOrg1.Customers.Add(customer);
        await contextOrg1.SaveChangesAsync();

        // Act
        var resultOrg1 = await CustomerQueries.GetCustomerByIdAsync(customer.Id, contextOrg1);
        var resultOrg2 = await CustomerQueries.GetCustomerByIdAsync(customer.Id, contextOrg2);

        // Assert
        resultOrg1.Should().NotBeNull();
        resultOrg1!.Id.Should().Be(customer.Id);

        resultOrg2.Should().BeNull("Customer belongs to another organization");
    }

    [Fact]
    public async Task GetCars_ShouldReturnOnlyOrganizationCars()
    {
        // Arrange
        await CleanDatabaseAsync();
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        // Create organizations first (required for foreign key constraint)
        await CreateOrganizationsAsync(org1Id, org2Id);

        var contextOrg1 = CreateDbContext(org1Id);
        var contextOrg2 = CreateDbContext(org2Id);

        var customer1 = CreateTestCustomer(org1Id, "Test Customer");
        contextOrg1.Customers.Add(customer1);
        await contextOrg1.SaveChangesAsync();

        var customer2 = CreateTestCustomer(org2Id, "Test Customer");
        contextOrg2.Customers.Add(customer2);
        await contextOrg2.SaveChangesAsync();

        var car1 = CreateTestCar(customer1.Id, org1Id, "ABC123");
        var car2 = CreateTestCar(customer1.Id, org1Id, "XYZ789");
        contextOrg1.Cars.AddRange(car1, car2);
        await contextOrg1.SaveChangesAsync();

        var car3 = CreateTestCar(customer2.Id, org2Id, "DEF456");
        contextOrg2.Cars.Add(car3);
        await contextOrg2.SaveChangesAsync();

        // Act
        var org1Cars = await CustomerQueries.GetCars(contextOrg1).ToListAsync();
        var org2Cars = await CustomerQueries.GetCars(contextOrg2).ToListAsync();

        // Assert
        org1Cars.Should().HaveCount(2);
        org1Cars.Should().Contain(c => c.LicensePlate == "ABC123");
        org1Cars.Should().Contain(c => c.LicensePlate == "XYZ789");

        org2Cars.Should().HaveCount(1);
        org2Cars.Should().Contain(c => c.LicensePlate == "DEF456");
    }

    [Fact]
    public async Task GetCarById_ShouldReturnNull_WhenCarBelongsToAnotherOrganization()
    {
        // Arrange
        await CleanDatabaseAsync();
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        // Create organizations first (required for foreign key constraint)
        await CreateOrganizationsAsync(org1Id, org2Id);

        var contextOrg1 = CreateDbContext(org1Id);
        var contextOrg2 = CreateDbContext(org2Id);

        var customer = CreateTestCustomer(org1Id, "Test Customer");
        contextOrg1.Customers.Add(customer);
        await contextOrg1.SaveChangesAsync();

        var car = CreateTestCar(customer.Id, org1Id, "ABC123");
        contextOrg1.Cars.Add(car);
        await contextOrg1.SaveChangesAsync();

        // Act
        var resultOrg1 = await CustomerQueries.GetCarByIdAsync(car.Id, contextOrg1);
        var resultOrg2 = await CustomerQueries.GetCarByIdAsync(car.Id, contextOrg2);

        // Assert
        resultOrg1.Should().NotBeNull();
        resultOrg2.Should().BeNull("Car belongs to another organization");
    }

    [Fact]
    public async Task GetCustomerStatistics_ShouldCalculateOnlyForOrganization()
    {
        // Arrange
        await CleanDatabaseAsync();
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        // Create organizations first (required for foreign key constraint)
        await CreateOrganizationsAsync(org1Id, org2Id);

        var contextOrg1 = CreateDbContext(org1Id);
        var contextOrg2 = CreateDbContext(org2Id);

        // Add 3 customers to org1
        for (int i = 0; i < 3; i++)
        {
            var customer = TestDataBuilder.CreateCustomer(org1Id, $"Customer {i}", $"customer{i}@org1.com", $"555000100{i}");
            contextOrg1.Customers.Add(customer);
        }
        await contextOrg1.SaveChangesAsync();

        // Add 5 customers to org2
        for (int i = 0; i < 5; i++)
        {
            var customer = TestDataBuilder.CreateCustomer(org2Id, $"Customer {i}", $"customer{i}@org2.com", $"555000200{i}");
            contextOrg2.Customers.Add(customer);
        }
        await contextOrg2.SaveChangesAsync();

        // Act
        var statsOrg1 = await CustomerQueries.GetCustomerStatisticsAsync(contextOrg1, CancellationToken.None);
        var statsOrg2 = await CustomerQueries.GetCustomerStatisticsAsync(contextOrg2, CancellationToken.None);

        // Assert
        statsOrg1.TotalCustomers.Should().Be(3);
        statsOrg2.TotalCustomers.Should().Be(5);
    }
}
