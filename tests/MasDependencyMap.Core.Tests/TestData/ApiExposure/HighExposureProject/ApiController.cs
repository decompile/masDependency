using Microsoft.AspNetCore.Mvc;

namespace HighExposureProject;

[ApiController]
[Route("api/[controller]")]
public class BigApiController : ControllerBase
{
    [HttpGet("1")] public IActionResult Get1() => Ok();
    [HttpGet("2")] public IActionResult Get2() => Ok();
    [HttpGet("3")] public IActionResult Get3() => Ok();
    [HttpGet("4")] public IActionResult Get4() => Ok();
    [HttpGet("5")] public IActionResult Get5() => Ok();
    [HttpGet("6")] public IActionResult Get6() => Ok();
    [HttpGet("7")] public IActionResult Get7() => Ok();
    [HttpGet("8")] public IActionResult Get8() => Ok();
    [HttpGet("9")] public IActionResult Get9() => Ok();
    [HttpGet("10")] public IActionResult Get10() => Ok();
    [HttpPost("1")] public IActionResult Post1() => Ok();
    [HttpPost("2")] public IActionResult Post2() => Ok();
    [HttpPost("3")] public IActionResult Post3() => Ok();
    [HttpPost("4")] public IActionResult Post4() => Ok();
    [HttpPost("5")] public IActionResult Post5() => Ok();
    [HttpPut("1")] public IActionResult Put1() => Ok();
    [HttpPut("2")] public IActionResult Put2() => Ok();
    [HttpDelete("1")] public IActionResult Delete1() => Ok();
    [HttpDelete("2")] public IActionResult Delete2() => Ok();
    [HttpDelete("3")] public IActionResult Delete3() => Ok();
}
