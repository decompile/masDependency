namespace SampleMonolith.Core.Services;

/// <summary>
/// Validates customer data.
/// </summary>
public interface ICustomerValidator
{
    /// <summary>
    /// Validates a customer email.
    /// </summary>
    bool ValidateEmail(string email);
}
