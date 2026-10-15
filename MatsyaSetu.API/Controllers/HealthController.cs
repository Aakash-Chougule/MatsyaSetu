using Microsoft.AspNetCore.Mvc;

namespace MatsyaSetu.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("MatsyaSetu API is running");
        }
    }
}