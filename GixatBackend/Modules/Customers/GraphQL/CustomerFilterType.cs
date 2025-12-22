using HotChocolate.Data.Filters;
using GixatBackend.Modules.Customers.Models;

namespace GixatBackend.Modules.Customers.GraphQL;

internal sealed class CustomerFilterType : FilterInputType<Customer>
{
    protected override void Configure(IFilterInputTypeDescriptor<Customer> descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        
        descriptor.BindFieldsExplicitly();
        
        descriptor.Field(c => c.Id);
        descriptor.Field(c => c.FirstName);
        descriptor.Field(c => c.LastName);
        descriptor.Field(c => c.Email);
        descriptor.Field(c => c.PhoneNumber);
        descriptor.Field(c => c.CreatedAt);
        descriptor.Field(c => c.UpdatedAt);
        
        // Allow filtering by address city
        descriptor.Field(c => c.Address);
        
        // Allow filtering by whether customer has cars
        descriptor.Field(c => c.Cars)
            .Description("Filter customers who have cars");
    }
}
