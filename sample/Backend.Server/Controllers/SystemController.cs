using Microsoft.AspNetCore.Mvc;

namespace Backend.Server.Controllers
{
    [ApiController]
    [Route("system")]
    public class SystemController : ControllerBase
    {

        [HttpGet("health")]
        public IActionResult Healthy() => Ok();
    }
}
