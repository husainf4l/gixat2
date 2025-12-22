using FluentAssertions;
using GixatBackend.Modules.Customers.Services;
using GixatBackend.Tests.Helpers;
using System.Text;

namespace GixatBackend.Tests.Modules.Customers.Services;

public class CustomerExportServiceTests
{
    [Fact]
    public async Task ExportCustomersToCsvAsync_ShouldGenerateCsv_WithHeaders()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateInMemoryContextWithTenant(orgId);
        var service = new CustomerExportService(context);

        // Act
        var result = await service.ExportCustomersToCsvAsync();

        // Assert
        var csvContent = Encoding.UTF8.GetString(result);
        csvContent.Should().Contain("First Name,Last Name,Email,Phone Number,City,Number of Cars,Created At");
    }

    [Fact]
    public async Task ExportCustomersToCsvAsync_ShouldIncludeCustomerData()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateInMemoryContextWithTenant(orgId);
        var service = new CustomerExportService(context);

        var customer = TestDataBuilder.CreateCustomer(orgId, "John Doe", "john@example.com", "1234567890");
        var car = TestDataBuilder.CreateCar(customer.Id, orgId, "Toyota", "Camry");

        context.Customers.Add(customer);
        context.Cars.Add(car);
        await context.SaveChangesAsync();

        // Act
        var result = await service.ExportCustomersToCsvAsync();

        // Assert
        var csvContent = Encoding.UTF8.GetString(result);
        csvContent.Should().Contain("John Doe");
        csvContent.Should().Contain("john@example.com");
        csvContent.Should().Contain("1234567890");
        csvContent.Should().Contain("1"); // Number of cars
    }

    [Fact]
    public async Task ExportCustomersToCsvAsync_ShouldEscapeSpecialCharacters()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateInMemoryContextWithTenant(orgId);
        var service = new CustomerExportService(context);

        var customer = TestDataBuilder.CreateCustomer(orgId, "Doe, John", "test@example.com");
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        // Act
        var result = await service.ExportCustomersToCsvAsync();

        // Assert
        var csvContent = Encoding.UTF8.GetString(result);
        csvContent.Should().Contain("\"Doe, John\""); // Should be quoted
    }

    [Fact]
    public async Task ExportCustomersToCsvAsync_ShouldHandleEmptyData()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateInMemoryContextWithTenant(orgId);
        var service = new CustomerExportService(context);

        // Act
        var result = await service.ExportCustomersToCsvAsync();

        // Assert
        var csvContent = Encoding.UTF8.GetString(result);
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().ContainSingle(); // Only header
    }

    [Fact]
    public async Task ExportCustomersToCsvAsync_ShouldSortByName()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateInMemoryContextWithTenant(orgId);
        var service = new CustomerExportService(context);

        var customer1 = TestDataBuilder.CreateCustomer(orgId, "Zara Wilson");
        var customer2 = TestDataBuilder.CreateCustomer(orgId, "Alice Brown");
        var customer3 = TestDataBuilder.CreateCustomer(orgId, "Bob Smith");

        context.Customers.AddRange(customer1, customer2, customer3);
        await context.SaveChangesAsync();

        // Act
        var result = await service.ExportCustomersToCsvAsync();

        // Assert
        var csvContent = Encoding.UTF8.GetString(result);
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines[1].Should().Contain("Alice Brown");
        lines[2].Should().Contain("Bob Smith");
        lines[3].Should().Contain("Zara Wilson");
    }

    [Fact]
    public async Task ExportCustomersToCsvAsync_ShouldOnlyIncludeTenantCustomers()
    {
        // Arrange
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        var context1 = TestDbContextFactory.CreateInMemoryContextWithTenant(org1Id);

        var customer1 = TestDataBuilder.CreateCustomer(org1Id, "Org1 Customer");
        var customer2 = TestDataBuilder.CreateCustomer(org2Id, "Org2 Customer");

        context1.Customers.AddRange(customer1, customer2);
        await context1.SaveChangesAsync();

        var service = new CustomerExportService(context1);

        // Act
        var result = await service.ExportCustomersToCsvAsync();

        // Assert
        var csvContent = Encoding.UTF8.GetString(result);
        csvContent.Should().Contain("Org1 Customer");
        csvContent.Should().NotContain("Org2 Customer");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenContextIsNull()
    {
        // Act & Assert
        var act = () => new CustomerExportService(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
