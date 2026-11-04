using System.Security.Claims;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api_finances.src.Controllers
{
    [Route("api/life-cost")]
    [ApiController]
    public class LifeCostController(ILifeCostService service) : ControllerBase
    {
        private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

        [Authorize]
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            ResponseApi<LifeCostSummaryResponse> response = await service.GetSummaryAsync(UserId);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpGet("monthly")]
        public async Task<IActionResult> GetMonthly([FromQuery] int months = 12)
        {
            ResponseApi<List<LifeCostMonthlyPoint>> response = await service.GetMonthlyAsync(UserId, months);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpGet("breakdown")]
        public async Task<IActionResult> GetBreakdown([FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            ResponseApi<List<LifeCostBreakdownItem>> response = await service.GetBreakdownAsync(UserId, start, end);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpGet("compare")]
        public async Task<IActionResult> GetCompare([FromQuery] int year1, [FromQuery] int month1, [FromQuery] int year2, [FromQuery] int month2)
        {
            ResponseApi<dynamic> response = await service.GetCompareAsync(UserId, year1, month1, year2, month2);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            ResponseApi<LifeCostSettings> response = await service.GetSettingsAsync(UserId);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpPut("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateLifeCostSettingsRequest request)
        {
            ResponseApi<LifeCostSettings> response = await service.UpdateSettingsAsync(UserId, request);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpPost("adjustments")]
        public async Task<IActionResult> AddAdjustment([FromBody] AddMonthlyAdjustmentRequest request)
        {
            ResponseApi<bool> response = await service.AddAdjustmentAsync(UserId, request);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpDelete("adjustments/{id}")]
        public async Task<IActionResult> DeleteAdjustment(string id)
        {
            ResponseApi<bool> response = await service.DeleteAdjustmentAsync(UserId, id);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpPut("categories/{id}/classification")]
        public async Task<IActionResult> UpdateCategoryClassification(string id, [FromBody] UpdateCategoryClassificationRequest request)
        {
            ResponseApi<Category> response = await service.UpdateCategoryClassificationAsync(id, request);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpGet("categories/classification")]
        public async Task<IActionResult> GetCategoriesClassification()
        {
            ResponseApi<List<LifeCostCategoryClassificationItem>> response = await service.GetCategoriesClassificationAsync(UserId);
            return StatusCode(response.StatusCode, response.Result);
        }
    }
}
