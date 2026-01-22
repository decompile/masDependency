using SampleMonolith.Core.Domain;
using SampleMonolith.Infrastructure.Data;

namespace SampleMonolith.Services;

/// <summary>
/// Business logic service for order operations.
/// </summary>
public class OrderService
{
    private readonly OrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderService"/> class.
    /// </summary>
    public OrderService(OrderRepository orderRepository, ICustomerRepository customerRepository)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
    }

    /// <summary>
    /// Creates a new order for a customer.
    /// </summary>
    public void CreateOrder(int customerId, decimal totalAmount)
    {
        var customer = _customerRepository.GetById(customerId);
        if (customer == null)
        {
            throw new InvalidOperationException($"Customer {customerId} not found");
        }

        var order = new Order
        {
            CustomerId = customerId,
            TotalAmount = totalAmount,
            OrderDate = DateTime.UtcNow
        };

        _orderRepository.Save(order);
    }
}
