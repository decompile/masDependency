namespace SampleMonolith.Core.Domain;

/// <summary>
/// Represents an order entity.
/// </summary>
public class Order
{
    /// <summary>
    /// Gets or sets the order ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the customer ID.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the order total amount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the order date.
    /// </summary>
    public DateTime OrderDate { get; set; }
}
