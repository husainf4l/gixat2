using GixatBackend.Modules.Customers.Models;
using GixatBackend.Modules.JobCards.Models;
using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Modules.Sessions.Enums;
using GixatBackend.Modules.Sessions.Models;
using GixatBackend.Modules.Users.Enums;
using GixatBackend.Modules.Users.Models;

namespace GixatBackend.Tests.Helpers;

/// <summary>
/// Builder for creating test data entities
/// </summary>
public static class TestDataBuilder
{
    public static Organization CreateOrganization(string name = "Test Organization")
    {
        return new Organization
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static ApplicationUser CreateUser(
        string email = "test@example.com",
        Guid? organizationId = null,
        UserType userType = UserType.Organizational)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            FullName = "Test User",
            UserType = userType,
            OrganizationId = organizationId,
            EmailConfirmed = true
        };
    }

    public static Customer CreateCustomer(
        Guid organizationId,
        string name = "Test Customer",
        string? email = "customer@example.com",
        string? phone = "1234567890")
    {
        var nameParts = name.Split(' ', 2);
        return new Customer
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            FirstName = nameParts[0],
            LastName = nameParts.Length > 1 ? nameParts[1] : "",
            Email = email,
            PhoneNumber = phone ?? "",
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Car CreateCar(
        Guid customerId,
        Guid organizationId,
        string make = "Toyota",
        string model = "Camry",
        string? plateNumber = "ABC123")
    {
        return new Car
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            OrganizationId = organizationId,
            Make = make,
            Model = model,
            Year = 2020,
            LicensePlate = plateNumber ?? "",
            CreatedAt = DateTime.UtcNow
        };
    }

    public static GarageSession CreateSession(
        Guid customerId,
        Guid carId,
        Guid organizationId,
        SessionStatus status = SessionStatus.CustomerRequest)
    {
        return new GarageSession
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CarId = carId,
            OrganizationId = organizationId,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static JobCard CreateJobCard(
        Guid sessionId,
        Guid customerId,
        Guid carId,
        Guid organizationId,
        string? jobNumber = null)
    {
        return new JobCard
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            CustomerId = customerId,
            CarId = carId,
            OrganizationId = organizationId,
            Status = JobCardStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static JobItem CreateJobItem(
        Guid jobCardId,
        string description = "Test Service",
        decimal estimatedLaborCost = 50m,
        decimal estimatedPartsCost = 50m)
    {
        return new JobItem
        {
            Id = Guid.NewGuid(),
            JobCardId = jobCardId,
            Description = description,
            EstimatedLaborCost = estimatedLaborCost,
            EstimatedPartsCost = estimatedPartsCost,
            Status = JobItemStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }
}
