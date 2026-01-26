// Mock ASP.NET Core MVC and WCF attributes for Roslyn testing

using System;

namespace Microsoft.AspNetCore.Mvc
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ApiControllerAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RouteAttribute : Attribute
    {
        public RouteAttribute(string template) { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class HttpGetAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class HttpPostAttribute : Attribute { }

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
}

namespace System.ServiceModel
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class ServiceContractAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class OperationContractAttribute : Attribute { }
}
