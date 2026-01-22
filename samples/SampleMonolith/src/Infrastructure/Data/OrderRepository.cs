using SampleMonolith.Core.Domain;

namespace SampleMonolith.Infrastructure.Data;

/// <summary>
/// In-memory order repository implementation.
/// </summary>
public class OrderRepository
{
    private readonly List<Order> _orders = new();

    /// <summary>
    /// Gets an order by ID.
    /// </summary>
    public Order? GetById(int id)
    {
        return _orders.FirstOrDefault(o => o.Id == id);
    }

    /// <summary>
    /// Gets all orders for a customer.
    /// </summary>
    public IEnumerable<Order> GetByCustomerId(int customerId)
    {
        return _orders.Where(o => o.CustomerId == customerId);
    }

    /// <summary>
    /// Saves an order.
    /// </summary>
    public void Save(Order order)
    {
        _orders.Add(order);
    }
}
