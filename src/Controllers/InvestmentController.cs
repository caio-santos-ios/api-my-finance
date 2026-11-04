using System.Security.Claims;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api_finances.src.Controllers
{
    [Route("api/investments")]
    [ApiController]
    public class InvestmentController(IInvestmentService service) : ControllerBase
    {
        private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

        [Authorize]
        [HttpGet("investment-assets")]
        public async Task<IActionResult> GetAssets()
        {
            ResponseApi<List<InvestmentAsset>> response = await service.GetAssetsAsync(UserId);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpGet("investment-assets/{id}")]
        public async Task<IActionResult> GetAssetById(string id)
        {
            ResponseApi<InvestmentAsset?> response = await service.GetAssetByIdAsync(id);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpPost("investment-assets")]
        public async Task<IActionResult> CreateAsset([FromBody] CreateInvestmentAssetRequest request)
        {
            ResponseApi<InvestmentAsset?> response = await service.CreateAssetAsync(UserId, request);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpPut("investment-assets/{id}")]
        public async Task<IActionResult> UpdateAsset(string id, [FromBody] UpdateInvestmentAssetRequest request)
        {
            request.Id = id;
            ResponseApi<InvestmentAsset?> response = await service.UpdateAssetAsync(UserId, request);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpDelete("investment-assets/{id}")]
        public async Task<IActionResult> DeleteAsset(string id)
        {
            ResponseApi<bool> response = await service.DeleteAssetAsync(UserId, id);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions([FromQuery] string? assetId, [FromQuery] string? brokerId, [FromQuery] string? kind, [FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            ResponseApi<List<InvestmentTransactionResponse>> response = await service.GetTransactionsAsync(UserId, assetId, brokerId, kind, start, end);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpPost("transactions")]
        public async Task<IActionResult> CreateTransaction([FromBody] CreateInvestmentTransactionRequest request)
        {
            ResponseApi<InvestmentTransaction?> response = await service.CreateTransactionAsync(UserId, request);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpPut("transactions/{id}")]
        public async Task<IActionResult> UpdateTransaction(string id, [FromBody] UpdateInvestmentTransactionRequest request)
        {
            request.Id = id;
            ResponseApi<InvestmentTransaction?> response = await service.UpdateTransactionAsync(UserId, request);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpDelete("transactions/{id}")]
        public async Task<IActionResult> DeleteTransaction(string id)
        {
            ResponseApi<bool> response = await service.DeleteTransactionAsync(UserId, id);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpGet("portfolio")]
        public async Task<IActionResult> GetPortfolio()
        {
            ResponseApi<PortfolioResponse> response = await service.GetPortfolioAsync(UserId);
            return StatusCode(response.StatusCode, response.Result);
        }
    }
}
