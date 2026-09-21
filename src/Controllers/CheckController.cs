using Microsoft.AspNetCore.Mvc;

namespace api_finances.src.Controllers
{
    [Route("api/check")]
    [ApiController]
    public class CheckController : ControllerBase
    {        
        [HttpGet]
        public IActionResult Check()
        {
            return StatusCode(200, new { message = "API OK" });
        }
    }
}