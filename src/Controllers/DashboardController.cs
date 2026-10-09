using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;

namespace api_finances.src.Controllers
{
    [Route("api/dashboard")]
    [Authorize]
    [ApiController]
    public class DashboardController(IDashboardService service) : ControllerBase
    {
        private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (!string.IsNullOrEmpty(UserId))
            {
                Dictionary<string, StringValues> query = new(Request.Query);
                query["createdBy"] = UserId;
                Request.Query = new QueryCollection(query);
            }

            ResponseApi<PaginationApi<List<dynamic>>> response = await service.GetAllAsync(new(Request.Query));
            return StatusCode(response.StatusCode, response.Result);
        }

        [HttpGet("dash")]
        public async Task<IActionResult> GetDash()
        {
            if (!string.IsNullOrEmpty(UserId))
            {
                Dictionary<string, StringValues> query = new(Request.Query);
                query["createdBy"] = UserId;
                Request.Query = new QueryCollection(query);
            }
            ResponseApi<dynamic> response = await service.GetDashAsync(new(Request.Query));
            return StatusCode(response.StatusCode, response.Result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(string id)
        {
            ResponseApi<dynamic?> response = await service.GetByIdAggregateAsync(id);
            return StatusCode(response.StatusCode, response.Result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDashboardRequest request)
        {
            request.CreatedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

            ResponseApi<Dashboard?> response = await service.CreateAsync(request);
            return StatusCode(response.StatusCode, response.Result);
        }
        
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateDashboardRequest request)
        {
            request.UpdatedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

            ResponseApi<Dashboard?> response = await service.UpdateAsync(request);
            return StatusCode(response.StatusCode, response.Result);
        }
    }
}