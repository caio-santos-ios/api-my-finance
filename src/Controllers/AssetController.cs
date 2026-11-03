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
    [Route("api/assets")]
    [ApiController]
    public class AssetController(IAssetService service) : ControllerBase
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
        public async Task<IActionResult> Create([FromBody] CreateAssetRequest request)
        {
            request.CreatedBy = UserId;
            ResponseApi<Asset?> response = await service.CreateAsync(request);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateAssetRequest request)
        {
            request.UpdatedBy = UserId;
            ResponseApi<Asset?> response = await service.UpdateAsync(request);
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
            ResponseApi<Asset> response = await service.DeleteAsync(request);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpGet("{id}/valuations")]
        public async Task<IActionResult> GetValuations(string id)
        {
            ResponseApi<List<AssetValuation>> response = await service.GetValuationsAsync(id);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpPost("{id}/valuations")]
        public async Task<IActionResult> AddValuation(string id, [FromBody] Requests.Asset.AddAssetValuationRequest request)
        {
            request.CreatedBy = UserId;
            ResponseApi<AssetValuation?> response = await service.AddValuationAsync(id, request);
            return StatusCode(response.StatusCode, response);
        }

        [Authorize]
        [HttpPut("{id}/valuations/{valuationId}")]
        public async Task<IActionResult> UpdateValuation(string id, string valuationId, [FromBody] Requests.Asset.UpdateAssetValuationRequest request)
        {
            request.UpdatedBy = UserId;
            ResponseApi<AssetValuation?> response = await service.UpdateValuationAsync(id, valuationId, request);
            return StatusCode(response.StatusCode, response);
        }

        [Authorize]
        [HttpDelete("{id}/valuations/{valuationId}")]
        public async Task<IActionResult> DeleteValuation(string id, string valuationId)
        {
            ResponseApi<bool> response = await service.DeleteValuationAsync(id, valuationId, UserId);
            return StatusCode(response.StatusCode, response);
        }
    }
}
