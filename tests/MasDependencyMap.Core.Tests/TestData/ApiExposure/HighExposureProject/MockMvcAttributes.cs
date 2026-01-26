// Mock ASP.NET Core MVC attributes for Roslyn testing
// These have the same fully qualified names as the real attributes
// so the ExternalApiDetector can detect them using semantic analysis

using System;

namespace Microsoft.AspNetCore.Mvc;

[AttributeUsage(AttributeTargets.Class)]
public class ApiControllerAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RouteAttribute : Attribute
{
    public RouteAttribute(string template) { }
}

[AttributeUsage(AttributeTargets.Method)]
public class HttpGetAttribute : Attribute
{
    public HttpGetAttribute() { }
    public HttpGetAttribute(string template) { }
}

[AttributeUsage(AttributeTargets.Method)]
public class HttpPostAttribute : Attribute
{
    public HttpPostAttribute() { }
    public HttpPostAttribute(string template) { }
}

[AttributeUsage(AttributeTargets.Method)]
public class HttpPutAttribute : Attribute
{
    public HttpPutAttribute() { }
    public HttpPutAttribute(string template) { }
}

[AttributeUsage(AttributeTargets.Method)]
public class HttpDeleteAttribute : Attribute
{
    public HttpDeleteAttribute() { }
    public HttpDeleteAttribute(string template) { }
}

[AttributeUsage(AttributeTargets.Method)]
public class HttpPatchAttribute : Attribute { }

public abstract class ControllerBase
{
    protected IActionResult Ok() => new OkResult();
    protected IActionResult Ok(object value) => new OkObjectResult(value);
}

public interface IActionResult { }
public class OkResult : IActionResult { }
public class OkObjectResult : IActionResult
{
    public OkObjectResult(object value) { }
}
