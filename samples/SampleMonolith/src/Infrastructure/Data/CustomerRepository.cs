using SampleMonolith.Core.Domain;

namespace SampleMonolith.Infrastructure.Data;

/// <summary>
/// In-memory customer repository implementation.
/// </summary>
public class CustomerRepository : ICustomerRepository
{
    private readonly List<Customer> _customers = new();

    /// <inheritdoc/>
    public Customer? GetById(int id)
    {
        return _customers.FirstOrDefault(c => c.Id == id);
    }

    /// <inheritdoc/>
    public Customer? FindByEmail(string email)
    {
        return _customers.FirstOrDefault(c => c.Email == email);
    }

    /// <inheritdoc/>
    public void Save(Customer customer)
    {
        _customers.Add(customer);
    }
}
