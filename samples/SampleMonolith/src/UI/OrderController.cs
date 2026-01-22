using SampleMonolith.Services;

namespace SampleMonolith.UI;

/// <summary>
/// UI controller for order operations.
/// </summary>
public class OrderController
{
    private readonly OrderService _orderService;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderController"/> class.
    /// </summary>
    public OrderController(OrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Handles order creation request.
    /// </summary>
    public void CreateOrder(int customerId, decimal amount)
    {
        _orderService.CreateOrder(customerId, amount);
        Console.WriteLine($"Order created for customer {customerId}: ${amount}");
    }
}
