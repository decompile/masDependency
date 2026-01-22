using SampleMonolith.Common;
using SampleMonolith.Core.Domain;
using SampleMonolith.Infrastructure.Data;

namespace SampleMonolith.Services;

/// <summary>
/// Business logic service for customer operations.
/// </summary>
public class CustomerService
{
    private readonly ICustomerRepository _repository;
    private readonly DateTimeProvider _dateTimeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerService"/> class.
    /// </summary>
    public CustomerService(ICustomerRepository repository, DateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    public void CreateCustomer(string name, string email)
    {
        if (email.IsNullOrWhiteSpace())
        {
            throw new ArgumentException("Email is required", nameof(email));
        }

        var customer = new Customer
        {
            Name = name,
            Email = email
        };

        _repository.Save(customer);
    }
}
