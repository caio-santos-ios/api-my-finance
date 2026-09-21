using System.Security.Claims;
using api_finances.src.Interfaces;
using api_finances.src.Models.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using api_finances.src.Requests;
using Microsoft.Extensions.Primitives;
using api_finances.src.Models;

namespace api_finances.src.Controllers
{
    [Route("api/banks")]
    [ApiController]
    public class BankController(IBankService service) : ControllerBase
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
        [HttpGet("select")]
        public async Task<IActionResult> GetSelect()
        {
            if (!string.IsNullOrEmpty(UserId))
            {
                Dictionary<string, StringValues> query = new(Request.Query);
                query["createdBy"] = UserId;
                Request.Query = new QueryCollection(query);
            }
            ResponseApi<List<dynamic>> response = await service.GetSelectAsync(new(Request.Query));
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
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBankRequest task)
        {
            if (task == null) return BadRequest("Dados inválidos.");
            task.CreatedBy = UserId;
            ResponseApi<Bank?> response = await service.CreateAsync(task);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateBankRequest task)
        {
            if (task == null) return BadRequest("Dados inválidos.");
            task.UpdatedBy = UserId;
            ResponseApi<Bank?> response = await service.UpdateAsync(task);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            ResponseApi<Bank> response = await service.DeleteAsync(new() { Id = id, DeletedBy = UserId });
            return StatusCode(response.StatusCode, response.Result);
        }
    }
}