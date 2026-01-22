namespace SampleMonolith.Core.Domain;

/// <summary>
/// Represents a customer entity.
/// </summary>
public class Customer
{
    /// <summary>
    /// Gets or sets the customer ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}
