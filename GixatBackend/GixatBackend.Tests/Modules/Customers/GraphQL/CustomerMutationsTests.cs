using FluentAssertions;
using GixatBackend.Modules.Customers.GraphQL;
using GixatBackend.Tests.Helpers;
using GixatBackend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Tests.Modules.Customers.GraphQL;

public class CustomerMutationsTests : MultiTenancyTestBase
{
    [Fact]
    public async Task CreateCustomerAsync_ShouldCreateCustomer_WithBasicInfo()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateInMemoryContextWithTenant(orgId);

        var input = new CreateCustomerInput(
            FirstName: "John",
            LastName: "Doe",
            Email: "john.doe@example.com",
            PhoneNumber: "+1234567890",
            Country: null,
            City: null,
            Street: null,
            PhoneCountryCode: null
        );

        // Act
        var result = await CustomerMutations.CreateCustomerAsync(input, context);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Email.Should().Be("john.doe@example.com");
        result.PhoneNumber.Should().Be("+1234567890");
        result.OrganizationId.Should().Be(orgId);
        result.Address.Should().BeNull();
    }

    [Fact]
    public async Task CreateCustomerAsync_ShouldCreateCustomer_WithAddress()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateInMemoryContextWithTenant(orgId);

        var input = new CreateCustomerInput(
            FirstName: "Jane",
            LastName: "Smith",
            Email: "jane.smith@example.com",
            PhoneNumber: "+9876543210",
            Country: "USA",
            City: "New York",
            Street: "123 Main St",
            PhoneCountryCode: "+1"
        );

        // Act
        var result = await CustomerMutations.CreateCustomerAsync(input, context);

        // Assert
        result.Should().NotBeNull();
        result.Address.Should().NotBeNull();
        result.Address!.Country.Should().Be("USA");
        result.Address.City.Should().Be("New York");
        result.Address.Street.Should().Be("123 Main St");
        result.Address.PhoneCountryCode.Should().Be("+1");
    }

    [Fact]
    public async Task CreateCustomerAsync_ShouldAssignOrganizationId_Automatically()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateInMemoryContextWithTenant(orgId);

        var input = new CreateCustomerInput("John", "Doe", "john@example.com", "+123", null, null, null, null);

        // Act
        var result = await CustomerMutations.CreateCustomerAsync(input, context);

        // Assert
        result.OrganizationId.Should().Be(orgId);
    }

    [Fact]
    public async Task CreateCarAsync_ShouldCreateCar_ForExistingCustomer()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateInMemoryContextWithTenant(orgId);

        var customer = TestDataBuilder.CreateCustomer(orgId);
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var input = new CreateCarInput(
            CustomerId: customer.Id,
            Make: "Toyota",
            Model: "Camry",
            Year: 2020,
            LicensePlate: "ABC123",
            VIN: "1HGBH41JXMN109186",
            Color: "Blue"
        );

        // Act
        var result = await CustomerMutations.CreateCarAsync(input, context);

        // Assert
        result.Should().NotBeNull();
        result.Make.Should().Be("Toyota");
        result.Model.Should().Be("Camry");
        result.Year.Should().Be(2020);
        result.LicensePlate.Should().Be("ABC123");
        result.VIN.Should().Be("1HGBH41JXMN109186");
        result.Color.Should().Be("Blue");
        result.CustomerId.Should().Be(customer.Id);
        result.OrganizationId.Should().Be(orgId);
    }

    [Fact]
    public async Task CreateCarAsync_ShouldThrow_WhenCustomerNotFound()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateInMemoryContextWithTenant(orgId);

        var input = new CreateCarInput(
            CustomerId: Guid.NewGuid(), // Non-existent customer
            Make: "Toyota",
            Model: "Camry",
            Year: 2020,
            LicensePlate: "ABC123",
            VIN: null,
            Color: null
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CustomerMutations.CreateCarAsync(input, context));
    }

    [Fact]
    public async Task CreateCarAsync_ShouldThrow_WhenCustomerBelongsToDifferentOrganization()
    {
        // Arrange
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        var context1 = TestDbContextFactory.CreateInMemoryContextWithTenant(org1Id);
        var context2 = TestDbContextFactory.CreateInMemoryContextWithTenant(org2Id);

        // Create customer in org1
        var customer = TestDataBuilder.CreateCustomer(org1Id);
        context1.Customers.Add(customer);
        await context1.SaveChangesAsync();

        var input = new CreateCarInput(customer.Id, "Toyota", "Camry", 2020, "ABC123", null, null);

        // Act & Assert - Try to create car in org2
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CustomerMutations.CreateCarAsync(input, context2));
    }

    [Fact]
    public async Task CreateCustomerAsync_ShouldPersistToDatabase()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateInMemoryContextWithTenant(orgId);

        var input = new CreateCustomerInput("John", "Doe", "john@example.com", "+123", null, null, null, null);

        // Act
        var result = await CustomerMutations.CreateCustomerAsync(input, context);

        // Assert - Verify persisted
        var savedCustomer = await context.Customers.FindAsync(result.Id);
        savedCustomer.Should().NotBeNull();
        savedCustomer!.FirstName.Should().Be("John");
    }
}
