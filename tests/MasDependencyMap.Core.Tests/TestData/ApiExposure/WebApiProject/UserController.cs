using Microsoft.AspNetCore.Mvc;

namespace WebApiProject;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    [HttpGet]  // Endpoint 1
    public IActionResult GetAllUsers() => Ok(new[] { "user1", "user2" });

    [HttpGet("{id}")]  // Endpoint 2
    public IActionResult GetUser(int id) => Ok($"User {id}");

    [HttpPost]  // Endpoint 3
    public IActionResult CreateUser() => Ok();
}
