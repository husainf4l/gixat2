using GixatBackend.Modules.Users.Models;
using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Common.Services;
using GixatBackend.Modules.Common.Services.Tenant;
using GixatBackend.Modules.Customers.Models;
using GixatBackend.Modules.Common.Lookup.Models;
using GixatBackend.Modules.Sessions.Models;
using GixatBackend.Modules.JobCards.Models;
using GixatBackend.Modules.Invites.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Data;

[SuppressMessage("Performance", "CA1515:Consider making public types internal", Justification = "Required for EF Core and testing")]
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
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
    public DbSet<JobCardMedia> JobCardMedias { get; set; }
    public DbSet<JobItemMedia> JobItemMedias { get; set; }
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

        // Unique constraints for business keys per organization
        builder.Entity<Car>()
            .HasIndex(c => new { c.LicensePlate, c.OrganizationId })
            .IsUnique()
            .HasDatabaseName("IX_Cars_LicensePlate_OrgId");

        builder.Entity<Car>()
            .HasIndex(c => new { c.VIN, c.OrganizationId })
            .IsUnique()
            .HasFilter("\"VIN\" IS NOT NULL")
            .HasDatabaseName("IX_Cars_VIN_OrgId");

        builder.Entity<Customer>()
            .HasIndex(c => new { c.Email, c.OrganizationId })
            .IsUnique()
            .HasFilter("\"Email\" IS NOT NULL")
            .HasDatabaseName("IX_Customers_Email_OrgId");

        builder.Entity<Customer>()
            .HasIndex(c => new { c.PhoneNumber, c.OrganizationId })
            .IsUnique()
            .HasDatabaseName("IX_Customers_Phone_OrgId");

        builder.Entity<LookupItem>()
            .HasOne(l => l.Parent)
            .WithMany(l => l.Children)
            .HasForeignKey(l => l.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Add indexes for fast filtering on LookupItems
        builder.Entity<LookupItem>()
            .HasIndex(l => new { l.Category, l.IsActive, l.ParentId });

        builder.Entity<LookupItem>()
            .HasIndex(l => new { l.ParentId, l.IsActive });

        // Performance indexes for customer queries (DataLoaders)
        // Index for GarageSessions by CustomerId and CreatedAt (for lastSessionDate and totalVisits)
        builder.Entity<GarageSession>()
            .HasIndex(s => new { s.CustomerId, s.CreatedAt });

        // Index for JobCards by CustomerId and Status (for totalSpent and activeJobCards)
        builder.Entity<JobCard>()
            .HasIndex(j => new { j.CustomerId, j.Status });

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

        // JobCardMedia composite key
        builder.Entity<JobCardMedia>()
            .HasKey(jm => new { jm.JobCardId, jm.MediaId });

        builder.Entity<JobCardMedia>()
            .HasOne(jm => jm.JobCard)
            .WithMany(j => j.Media)
            .HasForeignKey(jm => jm.JobCardId);

        builder.Entity<JobCardMedia>()
            .HasOne(jm => jm.Media)
            .WithMany()
            .HasForeignKey(jm => jm.MediaId);

        // JobItemMedia composite key
        builder.Entity<JobItemMedia>()
            .HasKey(jim => new { jim.JobItemId, jim.MediaId });

        builder.Entity<JobItemMedia>()
            .HasOne(jim => jim.JobItem)
            .WithMany(ji => ji.Media)
            .HasForeignKey(jim => jim.JobItemId);

        builder.Entity<JobItemMedia>()
            .HasOne(jim => jim.Media)
            .WithMany()
            .HasForeignKey(jim => jim.MediaId);

        // JobCard -> AssignedTechnician relationship
        builder.Entity<JobCard>()
            .HasOne(j => j.AssignedTechnician)
            .WithMany()
            .HasForeignKey(j => j.AssignedTechnicianId)
            .OnDelete(DeleteBehavior.SetNull);

        // JobItem -> AssignedTechnician relationship
        builder.Entity<JobItem>()
            .HasOne(ji => ji.AssignedTechnician)
            .WithMany()
            .HasForeignKey(ji => ji.AssignedTechnicianId)
            .OnDelete(DeleteBehavior.SetNull);

        // Global Query Filters for Multi-Tenancy
        // IMPORTANT: Use _tenantService.OrganizationId directly in the lambda expression
        // so it's evaluated at query time, not at model creation time
        
        // Parent entities with OrganizationId
        builder.Entity<Customer>().HasQueryFilter(c => c.OrganizationId == _tenantService.OrganizationId);
        builder.Entity<Car>().HasQueryFilter(c => c.OrganizationId == _tenantService.OrganizationId);
        builder.Entity<GarageSession>().HasQueryFilter(s => s.OrganizationId == _tenantService.OrganizationId);
        builder.Entity<JobCard>().HasQueryFilter(j => j.OrganizationId == _tenantService.OrganizationId);
        builder.Entity<ApplicationUser>().HasQueryFilter(u => u.OrganizationId == _tenantService.OrganizationId);
        builder.Entity<UserInvite>().HasQueryFilter(i => i.OrganizationId == _tenantService.OrganizationId);
        
        // Child entities - filter through parent navigation properties
        builder.Entity<SessionMedia>().HasQueryFilter(sm => sm.Session!.OrganizationId == _tenantService.OrganizationId);
        builder.Entity<SessionLog>().HasQueryFilter(sl => sl.Session!.OrganizationId == _tenantService.OrganizationId);
        builder.Entity<JobItem>().HasQueryFilter(ji => ji.JobCard!.OrganizationId == _tenantService.OrganizationId);
        builder.Entity<JobCardMedia>().HasQueryFilter(jcm => jcm.JobCard!.OrganizationId == _tenantService.OrganizationId);
        builder.Entity<Account>().HasQueryFilter(a => a.User!.OrganizationId == _tenantService.OrganizationId);
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
                    // Do NOT auto-assign organization to ApplicationUser - they set it explicitly during registration
                    if (entry.Entity is ApplicationUser)
                        continue;
                    
                    entry.Entity.OrganizationId = organizationId.Value;
                }
            }
        }
        else
        {
            // Check if there are any entities that require an organization
            // Exclude ApplicationUser as they can exist without an organization initially
            var entitiesRequiringOrg = ChangeTracker.Entries<IMustHaveOrganization>()
                .Where(e => e.State == EntityState.Added && e.Entity is not ApplicationUser)
                .ToList();
            
            if (entitiesRequiringOrg.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Cannot create entities that require an organization when user is not associated with an organization. " +
                    $"Entities: {string.Join(", ", entitiesRequiringOrg.Select(e => e.Entity.GetType().Name))}");
            }
        }

        // NOTE: Removed customer detachment as it was causing related entities (like Cars) to also be detached
        // Database triggers handle the denormalized fields update without EF tracking conflicts
        // foreach (var entry in ChangeTracker.Entries<Customer>())
        // {
        //     if (entry.State == EntityState.Unchanged)
        //     {
        //         entry.State = EntityState.Detached;
        //     }
        // }

        return base.SaveChangesAsync(cancellationToken);
    }
}
