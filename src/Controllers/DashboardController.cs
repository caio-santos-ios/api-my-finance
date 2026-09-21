using api_finances.src.Interfaces;
using api_finances.src.Models.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace api_finances.src.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    public class DashboardController(IDashboardService service) : ControllerBase
    {
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            ResponseApi<dynamic> response = await service.GetAllAsync(userId!, startDate, endDate);
            return StatusCode(response.StatusCode, response.Result);
        }
    }
}