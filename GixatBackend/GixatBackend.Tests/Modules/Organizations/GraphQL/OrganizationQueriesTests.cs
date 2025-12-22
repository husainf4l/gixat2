using FluentAssertions;
using GixatBackend.Modules.Organizations.GraphQL;
using GixatBackend.Modules.Common.Services.Tenant;
using GixatBackend.Tests.Helpers;
using GixatBackend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GixatBackend.Tests.Modules.Organizations.GraphQL;

public class OrganizationQueriesTests : MultiTenancyTestBase
{
    [Fact]
    public async Task GetMyOrganizationAsync_ShouldReturnOnlyUserOrganization()
    {
        // Arrange
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        var contextOrg1 = TestDbContextFactory.CreateInMemoryContextWithTenant(org1Id);
        var contextOrg2 = TestDbContextFactory.CreateInMemoryContextWithTenant(org2Id);

        var org1 = CreateTestOrganization("Organization 1");
        org1.Id = org1Id;
        var org2 = CreateTestOrganization("Organization 2");
        org2.Id = org2Id;

        contextOrg1.Organizations.Add(org1);
        await contextOrg1.SaveChangesAsync();

        contextOrg2.Organizations.Add(org2);
        await contextOrg2.SaveChangesAsync();

        var user1 = CreateTestUser(org1Id, "user1@test.com");
        contextOrg1.Users.Add(user1);
        await contextOrg1.SaveChangesAsync();

        var user2 = CreateTestUser(org2Id, "user2@test.com");
        contextOrg2.Users.Add(user2);
        await contextOrg2.SaveChangesAsync();

        var tenantServiceMock1 = new Mock<ITenantService>();
        tenantServiceMock1.Setup(x => x.OrganizationId).Returns(org1Id);

        var tenantServiceMock2 = new Mock<ITenantService>();
        tenantServiceMock2.Setup(x => x.OrganizationId).Returns(org2Id);

        // Act
        var resultOrg1 = await OrganizationQueries.GetMyOrganizationAsync(contextOrg1, tenantServiceMock1.Object);
        var resultOrg2 = await OrganizationQueries.GetMyOrganizationAsync(contextOrg2, tenantServiceMock2.Object);

        // Assert
        resultOrg1.Should().NotBeNull();
        resultOrg1!.Id.Should().Be(org1Id);
        resultOrg1.Name.Should().Be("Organization 1");

        resultOrg2.Should().NotBeNull();
        resultOrg2!.Id.Should().Be(org2Id);
        resultOrg2.Name.Should().Be("Organization 2");
    }

    [Fact]
    public async Task GetOrganizationByIdAsync_ShouldReturnNull_WhenAccessingAnotherOrganization()
    {
        // Arrange
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        var contextOrg1 = TestDbContextFactory.CreateInMemoryContextWithTenant(org1Id);
        var contextOrg2 = TestDbContextFactory.CreateInMemoryContextWithTenant(org2Id);

        var org1 = CreateTestOrganization("Organization 1");
        org1.Id = org1Id;
        contextOrg1.Organizations.Add(org1);
        await contextOrg1.SaveChangesAsync();

        // Act - Try to access org1 from org2 context
        var result = await OrganizationQueries.GetOrganizationByIdAsync(org1Id, contextOrg2);

        // Assert
        result.Should().BeNull("Should not access another organization's data");
    }
}
