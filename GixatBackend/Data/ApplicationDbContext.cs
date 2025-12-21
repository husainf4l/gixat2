using GixatBackend.Modules.Users.Models;
using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Common.Services;
using GixatBackend.Modules.Customers.Models;
using GixatBackend.Modules.Lookup.Models;
using GixatBackend.Modules.Sessions.Models;
using GixatBackend.Modules.JobCards.Models;
using GixatBackend.Modules.Invites.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Data;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by DI")]
internal sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ITenantService _tenantService;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantService tenantService)
        : base(options)
    {
        _tenantService = tenantService;
    }

    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<AppMedia> Medias { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Car> Cars { get; set; }
    public DbSet<LookupItem> LookupItems { get; set; }
    public DbSet<GarageSession> GarageSessions { get; set; }
    public DbSet<SessionMedia> SessionMedias { get; set; }
    public DbSet<SessionLog> SessionLogs { get; set; }
    public DbSet<JobCard> JobCards { get; set; }
    public DbSet<JobItem> JobItems { get; set; }
    public DbSet<UserInvite> UserInvites { get; set; }
    public DbSet<Account> Accounts { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        base.OnModelCreating(builder);

        var organizationId = _tenantService.OrganizationId;

        builder.Entity<Organization>()
            .HasMany(o => o.Users)
            .WithOne(u => u.Organization)
            .HasForeignKey(u => u.OrganizationId);
            
        builder.Entity<Organization>()
            .HasMany(o => o.Customers)
            .WithOne(c => c.Organization)
            .HasForeignKey(c => c.OrganizationId);

        builder.Entity<Customer>()
            .HasMany(c => c.Cars)
            .WithOne(car => car.Customer)
            .HasForeignKey(car => car.CustomerId);

        builder.Entity<LookupItem>()
            .HasOne(l => l.Parent)
            .WithMany(l => l.Children)
            .HasForeignKey(l => l.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<GarageSession>()
            .HasMany(s => s.Media)
            .WithOne(sm => sm.Session)
            .HasForeignKey(sm => sm.SessionId);

        builder.Entity<GarageSession>()
            .HasMany(s => s.Logs)
            .WithOne(sl => sl.Session)
            .HasForeignKey(sl => sl.SessionId);

        builder.Entity<JobCard>()
            .HasMany(j => j.Items)
            .WithOne(i => i.JobCard)
            .HasForeignKey(i => i.JobCardId);

        builder.Entity<Account>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Account>()
            .HasIndex(a => new { a.Provider, a.ProviderAccountId })
            .IsUnique();

        // Global Query Filters for Multi-Tenancy
        if (organizationId.HasValue)
        {
            builder.Entity<Customer>().HasQueryFilter(c => c.OrganizationId == organizationId.Value);
            builder.Entity<Car>().HasQueryFilter(c => c.OrganizationId == organizationId.Value);
            builder.Entity<GarageSession>().HasQueryFilter(s => s.OrganizationId == organizationId.Value);
            builder.Entity<JobCard>().HasQueryFilter(j => j.OrganizationId == organizationId.Value);
            builder.Entity<ApplicationUser>().HasQueryFilter(u => u.OrganizationId == organizationId.Value);
            builder.Entity<UserInvite>().HasQueryFilter(i => i.OrganizationId == organizationId.Value);
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var organizationId = _tenantService.OrganizationId;

        if (organizationId.HasValue)
        {
            foreach (var entry in ChangeTracker.Entries<IMustHaveOrganization>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.OrganizationId = organizationId.Value;
                }
            }
        }
        else
        {
            // Check if there are any entities that require an organization
            var entitiesRequiringOrg = ChangeTracker.Entries<IMustHaveOrganization>()
                .Where(e => e.State == EntityState.Added)
                .ToList();
            
            if (entitiesRequiringOrg.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Cannot create entities that require an organization when user is not associated with an organization. " +
                    $"Entities: {string.Join(", ", entitiesRequiringOrg.Select(e => e.Entity.GetType().Name))}");
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
