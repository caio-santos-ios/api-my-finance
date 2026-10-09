using System.Security.Claims;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests.GenericTable;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api_finances.src.Controllers
{
    [Route("api/generic-tables")]
    [ApiController]
    [Authorize]
    public class GenericTableController(IGenericTableService service) : ControllerBase
    {
        private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

        [HttpGet("select")]
        public async Task<IActionResult> GetAll()
        {
            var dict = Request.Query.ToDictionary(k => k.Key, v => v.Value);
            dict["createdBy"] = UserId;

            ResponseApi<List<dynamic>> response = await service.GetSelectAsync(new(new QueryCollection(dict)));
            return StatusCode(response.StatusCode, response.Result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateGenericTableRequest request)
        {
            if (request == null) return BadRequest("Dados inválidos.");
            request.CreatedBy = UserId;

            ResponseApi<GenericTable?> response = await service.CreateAsync(request);
            return StatusCode(response.StatusCode, response.Result);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateGenericTableRequest request)
        {
            if (request == null) return BadRequest("Dados inválidos.");
            request.UpdatedBy = UserId;

            ResponseApi<GenericTable?> response = await service.UpdateAsync(request);
            return StatusCode(response.StatusCode, response.Result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            ResponseApi<dynamic?> response = await service.DeleteAsync(id, UserId);
            return StatusCode(response.StatusCode, response.Result);
        }
    }
}
