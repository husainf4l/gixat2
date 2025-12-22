using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Customers.Models;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class CustomerStatistics
{
    public int TotalCustomers { get; set; }
    public int CustomersThisMonth { get; set; }
    public int ActiveCustomers { get; set; }
    public decimal TotalRevenue { get; set; }
}
