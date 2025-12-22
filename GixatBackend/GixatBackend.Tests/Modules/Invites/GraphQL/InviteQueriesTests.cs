using FluentAssertions;
using GixatBackend.Modules.Invites.GraphQL;
using GixatBackend.Modules.Invites.Models;
using GixatBackend.Modules.Users.Enums;
using GixatBackend.Tests.Helpers;
using GixatBackend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Tests.Modules.Invites.GraphQL;

public class InviteQueriesTests : MultiTenancyTestBase
{
    [Fact]
    public async Task GetInvites_ShouldReturnOnlyOrganizationInvites()
    {
        // Arrange
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        var contextOrg1 = TestDbContextFactory.CreateInMemoryContextWithTenant(org1Id);
        var contextOrg2 = TestDbContextFactory.CreateInMemoryContextWithTenant(org2Id);

        var invite1 = new UserInvite
        {
            Id = Guid.NewGuid(),
            Email = "user1@test.com",
            Role = "OrgUser",
            InviteCode = Guid.NewGuid().ToString(),
            OrganizationId = org1Id,
            CreatedAt = DateTime.UtcNow
        };

        var invite2 = new UserInvite
        {
            Id = Guid.NewGuid(),
            Email = "user2@test.com",
            Role = "OrgManager",
            InviteCode = Guid.NewGuid().ToString(),
            OrganizationId = org1Id,
            CreatedAt = DateTime.UtcNow
        };

        var invite3 = new UserInvite
        {
            Id = Guid.NewGuid(),
            Email = "user3@test.com",
            Role = "OrgUser",
            InviteCode = Guid.NewGuid().ToString(),
            OrganizationId = org2Id,
            CreatedAt = DateTime.UtcNow
        };

        contextOrg1.UserInvites.AddRange(invite1, invite2);
        await contextOrg1.SaveChangesAsync();

        contextOrg2.UserInvites.Add(invite3);
        await contextOrg2.SaveChangesAsync();

        // Act
        var org1Invites = await InviteQueries.GetInvites(contextOrg1).ToListAsync();
        var org2Invites = await InviteQueries.GetInvites(contextOrg2).ToListAsync();

        // Assert
        org1Invites.Should().HaveCount(2);
        org1Invites.Should().Contain(i => i.Email == "user1@test.com");
        org1Invites.Should().Contain(i => i.Email == "user2@test.com");

        org2Invites.Should().HaveCount(1);
        org2Invites.Should().Contain(i => i.Email == "user3@test.com");
    }

    [Fact]
    public async Task GetInviteByCode_ShouldReturnNull_WhenInviteBelongsToAnotherOrganization()
    {
        // Arrange
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        var contextOrg1 = TestDbContextFactory.CreateInMemoryContextWithTenant(org1Id);
        var contextOrg2 = TestDbContextFactory.CreateInMemoryContextWithTenant(org2Id);

        var inviteCode = Guid.NewGuid().ToString();
        var invite = new UserInvite
        {
            Id = Guid.NewGuid(),
            Email = "user@test.com",
            Role = "OrgUser",
            InviteCode = inviteCode,
            OrganizationId = org1Id,
            CreatedAt = DateTime.UtcNow
        };

        contextOrg1.UserInvites.Add(invite);
        await contextOrg1.SaveChangesAsync();

        // Act
        var resultOrg1 = await InviteQueries.GetInviteByCodeAsync(inviteCode, contextOrg1);
        var resultOrg2 = await InviteQueries.GetInviteByCodeAsync(inviteCode, contextOrg2);

        // Assert
        resultOrg1.Should().NotBeNull();
        resultOrg2.Should().BeNull("Invite belongs to another organization");
    }
}
