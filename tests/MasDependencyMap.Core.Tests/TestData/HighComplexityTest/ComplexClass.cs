namespace HighComplexityTest;

/// <summary>
/// Test class with high cyclomatic complexity for integration testing.
/// Contains methods with extensive branching and decision points.
/// </summary>
public class ComplexClass
{
    // Complex method with multiple branching: CC = 15
    // 1 base + 3 if + 4 case + 1 for + 2 while + 1 && + 1 || + 1 catch + 1 ternary = 15
    public int VeryComplexMethod(int input, string mode)
    {
        int result = 0;

        // +1 for if
        if (input < 0)
        {
            return -1;
        }

        // +1 for if
        if (mode == "fast")
        {
            // +1 for for loop
            for (int i = 0; i < input; i++)
            {
                result += i;
            }
        }
        // +1 for else if
        else if (mode == "slow")
        {
            // +1 for while
            while (input > 0)
            {
                // +1 for if with && operator (+1 for &&)
                if (result > 100 && input % 2 == 0)
                {
                    break;
                }
                result += input--;
            }
        }

        // +4 for switch (4 cases)
        switch (result)
        {
            case 0:
                return 0;
            case 1:
                return 1;
            case 2:
                return 2;
            default:
                break;
        }

        try
        {
            // +1 for || operator
            result = (result > 0 || input < 0) ? result * 2 : 0; // +1 for ternary
        }
        catch (Exception)  // +1 for catch
        {
            result = -1;
        }

        return result;
    }

    // Another complex method: CC = 10
    // 1 base + 2 if + 3 case + 1 foreach + 1 do-while + 1 && + 1 || = 10
    public bool ComplexValidation(List<int> values, int threshold)
    {
        // +1 for if with || (+1 for ||)
        if (values == null || values.Count == 0)
        {
            return false;
        }

        bool isValid = true;

        // +1 for foreach
        foreach (var value in values)
        {
            // +1 for if with &&  (+1 for &&)
            if (value > threshold && value % 2 == 0)
            {
                isValid = false;
                break;
            }
        }

        int counter = 0;
        // +1 for do-while
        do
        {
            counter++;
        } while (counter < values.Count);

        // +3 for switch (3 cases)
        switch (counter)
        {
            case 0:
                return false;
            case 1:
                return true;
            default:
                return isValid;
        }
    }
}

// Expected totals for HighComplexityTest project:
// - VeryComplexMethod: CC = 15
// - ComplexValidation: CC = 10
// Total methods: 2
// Total complexity: 15 + 10 = 25
// Average complexity: 25 / 2 = 12.5
// Expected normalized score: ~54 (medium-high range: 34-66)
