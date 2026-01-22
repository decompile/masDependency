using SampleMonolith.Core.Domain;

namespace SampleMonolith.Infrastructure.Data;

/// <summary>
/// Repository interface for customer data access.
/// </summary>
public interface ICustomerRepository
{
    /// <summary>
    /// Gets a customer by ID.
    /// </summary>
    Customer? GetById(int id);

    /// <summary>
    /// Finds a customer by email.
    /// </summary>
    Customer? FindByEmail(string email);

    /// <summary>
    /// Saves a customer.
    /// </summary>
    void Save(Customer customer);
}
