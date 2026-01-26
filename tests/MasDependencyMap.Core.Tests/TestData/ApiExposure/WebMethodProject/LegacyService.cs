using System.Web.Services;

namespace WebMethodProject;

public class LegacyService
{
    [WebMethod]  // ASMX Endpoint 1
    public string GetData(int id)
    {
        return $"Data {id}";
    }

    [WebMethod]  // ASMX Endpoint 2
    public void SaveData(string data)
    {
        // Save logic
    }

    [WebMethod]  // ASMX Endpoint 3
    public string[] ListData()
    {
        return new[] { "Item1", "Item2", "Item3" };
    }

    // Not a WebMethod - should not be counted
    public string HelperMethod()
    {
        return "Helper";
    }
}
