using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace api_finances.src.Controllers
{
    [Route("api/dashboard")]
    [Authorize]
    [ApiController]
    public class DashboardController(IDashboardService service) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            ResponseApi<dynamic> response = await service.GetAllAsync(userId!, startDate, endDate);
            return StatusCode(response.StatusCode, response.Result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDashboardRequest request)
        {
            request.CreatedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

            ResponseApi<Dashboard?> response = await service.CreateAsync(request);
            return StatusCode(response.StatusCode, response.Result);
        }
    }
}