using Microsoft.AspNetCore.Mvc;

namespace MixedApiProject;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    [HttpGet]  // WebAPI Endpoint 1
    public IActionResult GetProducts() => Ok(new[] { "Product1", "Product2" });

    [HttpPost]  // WebAPI Endpoint 2
    public IActionResult CreateProduct() => Ok();
}
