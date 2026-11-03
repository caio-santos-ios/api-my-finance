using System.Security.Claims;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests.Recurring;
using api_finances.src.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace api_finances.src.Controllers;

[Route("api/recurrings")]
[ApiController]
public class RecurringTransactionController(IRecurringTransactionService service) : ControllerBase
{
    private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll()
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

    [Authorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(string id)
    {
        ResponseApi<dynamic?> response = await service.GetByIdAggregateAsync(id);
        return StatusCode(response.StatusCode, response.Result);
    }

    [Authorize]
    [HttpGet("cost-of-living")]
    public async Task<IActionResult> GetCostOfLiving()
    {
        ResponseApi<dynamic> response = await service.GetCostOfLivingSummaryAsync(UserId);
        return StatusCode(response.StatusCode, response.Result);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecurringRequest request)
    {
        ResponseApi<RecurringTransaction?> response = await service.CreateAsync(request);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateRecurringRequest request)
    {
        request.Id = id;
        ResponseApi<RecurringTransaction?> response = await service.UpdateAsync(request);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        DeleteRequest request = new()
        {
            Id = id,
            DeletedBy = UserId
        };
        ResponseApi<RecurringTransaction> response = await service.DeleteAsync(request);
        return StatusCode(response.StatusCode, response);
    }

    [Authorize]
    [HttpPost("{id}/post-month")]
    public async Task<IActionResult> PostMonth(string id, [FromBody] PostRecurringMonthRequest request)
    {
        ResponseApi<bool> response = await service.PostMonthAsync(id, request);
        return StatusCode(response.StatusCode, response);
    }
}
