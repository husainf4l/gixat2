using System.Text;
using GixatBackend.Data;
using GixatBackend.Modules.Customers.Models;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.Customers.Services;

internal sealed class CustomerExportService
{
    private readonly ApplicationDbContext _context;

    public CustomerExportService(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<byte[]> ExportCustomersToCsvAsync(CancellationToken cancellationToken = default)
    {
        var customers = await _context.Customers
            .Include(c => c.Address)
            .Include(c => c.Cars)
            .AsNoTracking()
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var csvBuilder = new StringBuilder();
        
        // CSV Header
        csvBuilder.AppendLine("First Name,Last Name,Email,Phone Number,City,Number of Cars,Created At");

        // CSV Rows
        foreach (var customer in customers)
        {
            csvBuilder.AppendLine(
                System.FormattableString.Invariant($"{EscapeCsv(customer.FirstName)},") +
                System.FormattableString.Invariant($"{EscapeCsv(customer.LastName)},") +
                System.FormattableString.Invariant($"{EscapeCsv(customer.Email ?? string.Empty)},") +
                System.FormattableString.Invariant($"{EscapeCsv(customer.PhoneNumber)},") +
                System.FormattableString.Invariant($"{EscapeCsv(customer.Address?.City ?? string.Empty)},") +
                System.FormattableString.Invariant($"{customer.Cars.Count},") +
                System.FormattableString.Invariant($"{customer.CreatedAt:yyyy-MM-dd HH:mm:ss}")
            );
        }

        return Encoding.UTF8.GetBytes(csvBuilder.ToString());
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',', StringComparison.Ordinal) || 
            value.Contains('"', StringComparison.Ordinal) || 
            value.Contains('\n', StringComparison.Ordinal))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }
}
