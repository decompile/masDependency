using SampleMonolith.Services;

namespace SampleMonolith.UI;

/// <summary>
/// UI controller for customer operations.
/// </summary>
public class CustomerController
{
    private readonly CustomerService _customerService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerController"/> class.
    /// </summary>
    public CustomerController(CustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>
    /// Handles customer creation request.
    /// </summary>
    public void CreateCustomer(string name, string email)
    {
        _customerService.CreateCustomer(name, email);
        Console.WriteLine($"Customer created: {name}");
    }
}
