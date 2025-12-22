using FluentAssertions;
using GixatBackend.Modules.Common.Lookup.GraphQL;
using GixatBackend.Modules.Common.Lookup.Models;
using GixatBackend.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Tests.Modules.Common.Lookup.GraphQL;

public class LookupQueriesTests
{
    [Fact]
    public async Task GetLookupItems_ShouldReturnAllActiveLookupItems()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();

        var item1 = new LookupItem
        {
            Id = Guid.NewGuid(),
            Category = "CarMake",
            Value = "Toyota",
            IsActive = true
        };

        var item2 = new LookupItem
        {
            Id = Guid.NewGuid(),
            Category = "CarMake",
            Value = "Honda",
            IsActive = true
        };

        var item3 = new LookupItem
        {
            Id = Guid.NewGuid(),
            Category = "CarMake",
            Value = "Ford",
            IsActive = false
        };

        context.LookupItems.AddRange(item1, item2, item3);
        await context.SaveChangesAsync();

        // Act
        var results = await LookupQueries.GetLookupItems(context).ToListAsync();

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(i => i.Value == "Toyota");
        results.Should().Contain(i => i.Value == "Honda");
        results.Should().NotContain(i => i.Value == "Ford");
    }

    [Fact]
    public async Task GetLookupItemsByCategory_ShouldFilterByCategory()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();

        var carMake = new LookupItem
        {
            Id = Guid.NewGuid(),
            Category = "CarMake",
            Value = "Toyota",
            IsActive = true
        };

        var serviceType = new LookupItem
        {
            Id = Guid.NewGuid(),
            Category = "ServiceType",
            Value = "Oil Change",
            IsActive = true
        };

        context.LookupItems.AddRange(carMake, serviceType);
        await context.SaveChangesAsync();

        // Act
        var carMakes = await LookupQueries.GetLookupItemsByCategory(context, "CarMake").ToListAsync();
        var serviceTypes = await LookupQueries.GetLookupItemsByCategory(context, "ServiceType").ToListAsync();

        // Assert
        carMakes.Should().HaveCount(1);
        carMakes[0].Value.Should().Be("Toyota");

        serviceTypes.Should().HaveCount(1);
        serviceTypes[0].Value.Should().Be("Oil Change");
    }

    [Fact]
    public async Task GetLookupItemsByCategory_ShouldReturnHierarchicalData()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();

        var parent = new LookupItem
        {
            Id = Guid.NewGuid(),
            Category = "ServiceType",
            Value = "Maintenance",
            IsActive = true
        };

        var child = new LookupItem
        {
            Id = Guid.NewGuid(),
            Category = "ServiceType",
            Value = "Oil Change",
            ParentId = parent.Id,
            IsActive = true
        };

        context.LookupItems.AddRange(parent, child);
        await context.SaveChangesAsync();

        // Act
        var results = await LookupQueries.GetLookupItemsByCategory(context, "ServiceType").ToListAsync();

        // Assert
        results.Should().HaveCount(2);
        var childItem = results.FirstOrDefault(i => i.Value == "Oil Change");
        childItem.Should().NotBeNull();
        childItem!.ParentId.Should().Be(parent.Id);
    }
}
