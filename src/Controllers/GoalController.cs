using System.Security.Claims;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace api_finances.src.Controllers
{
    [Route("api/goals")]
    [ApiController]
    public class GoalController(IGoalService service) : ControllerBase
    {
        private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("id")?.Value
            ?? "";

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
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            ResponseApi<dynamic> response = await service.GetSummaryAsync(UserId);
            return StatusCode(response.StatusCode, response);
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(string id)
        {
            ResponseApi<dynamic?> response = await service.GetByIdAggregateAsync(id);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateGoalRequest request)
        {
            request.CreatedBy = UserId;
            ResponseApi<Goal?> response = await service.CreateAsync(request);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateGoalRequest request)
        {
            request.UpdatedBy = UserId;
            ResponseApi<Goal?> response = await service.UpdateAsync(request);
            return StatusCode(response.StatusCode, response.Result);
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
            ResponseApi<Goal> response = await service.DeleteAsync(request);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpGet("{id}/transactions")]
        public async Task<IActionResult> GetTransactions(string id)
        {
            ResponseApi<List<dynamic>> response = await service.GetTransactionsAsync(id);
            return StatusCode(response.StatusCode, response);
        }

        [Authorize]
        [HttpPost("{id}/transactions")]
        public async Task<IActionResult> AddTransaction(string id, [FromBody] CreateGoalTransactionRequest request)
        {
            request.CreatedBy = UserId;
            ResponseApi<GoalTransaction?> response = await service.AddTransactionAsync(id, request);
            return StatusCode(response.StatusCode, response);
        }

        [Authorize]
        [HttpDelete("{id}/transactions/{transactionId}")]
        public async Task<IActionResult> DeleteTransaction(string id, string transactionId)
        {
            ResponseApi<bool> response = await service.DeleteTransactionAsync(id, transactionId, UserId);
            return StatusCode(response.StatusCode, response);
        }
    }
}
