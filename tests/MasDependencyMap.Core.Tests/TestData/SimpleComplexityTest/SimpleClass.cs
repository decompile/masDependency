namespace SimpleComplexityTest;

/// <summary>
/// Test class with low cyclomatic complexity for integration testing.
/// Contains methods with known complexity values for validation.
/// </summary>
public class SimpleClass
{
    // Constructor with no branching: CC = 1
    public SimpleClass()
    {
        Value = 0;
    }

    public int Value { get; set; }

    // Simple method with no branching: CC = 1
    public int GetValue()
    {
        return Value;
    }

    // Method with one if statement: CC = 2 (1 base + 1 if)
    public int SimpleIfMethod(int x)
    {
        if (x > 0)
        {
            return x * 2;
        }
        return 0;
    }

    // Method with one for loop: CC = 2 (1 base + 1 for)
    public int SimpleLoopMethod(int max)
    {
        int sum = 0;
        for (int i = 0; i < max; i++)
        {
            sum += i;
        }
        return sum;
    }
}

// Expected totals for SimpleComplexityTest project:
// - Constructor: CC = 1
// - GetValue: CC = 1
// - Property getter/setter: CC = 1 each = 2
// - SimpleIfMethod: CC = 2
// - SimpleLoopMethod: CC = 2
// Total methods/executables: 6
// Total complexity: 1 + 1 + 2 + 2 + 2 = 8
// Average complexity: 8 / 6 ≈ 1.33
// Expected normalized score: ~6.3 (low complexity range: 0-33)
