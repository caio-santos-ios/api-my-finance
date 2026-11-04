using System.Security.Claims;
using api_finances.src.Interfaces;
using api_finances.src.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api_finances.src.Controllers
{
    [Route("api/tax")]
    [ApiController]
    public class TaxController(ITaxService service) : ControllerBase
    {
        private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("id")?.Value
            ?? "";

        [Authorize]
        [HttpGet("{baseYear:int}/preview")]
        public async Task<IActionResult> GetPreview(int baseYear)
        {
            var response = await service.GetPreviewAsync(baseYear, UserId);
            return StatusCode(response.StatusCode, response);
        }

        [Authorize]
        [HttpGet("{baseYear:int}/income")]
        public async Task<IActionResult> GetIncome(int baseYear)
        {
            var response = await service.GetIncomeAsync(baseYear, UserId);
            return StatusCode(response.StatusCode, response);
        }

        [Authorize]
        [HttpGet("{baseYear:int}/deductions")]
        public async Task<IActionResult> GetDeductions(int baseYear)
        {
            var response = await service.GetDeductionsAsync(baseYear, UserId);
            return StatusCode(response.StatusCode, response);
        }

        [Authorize]
        [HttpGet("{baseYear:int}/assets")]
        public async Task<IActionResult> GetAssets(int baseYear)
        {
            var response = await service.GetAssetsAsync(baseYear, UserId);
            return StatusCode(response.StatusCode, response);
        }

        [Authorize]
        [HttpGet("{baseYear:int}/debts")]
        public async Task<IActionResult> GetDebts(int baseYear)
        {
            var response = await service.GetDebtsAsync(baseYear, UserId);
            return StatusCode(response.StatusCode, response);
        }

        [Authorize]
        [HttpGet("{baseYear:int}/pending")]
        public async Task<IActionResult> GetPending(int baseYear)
        {
            var response = await service.GetPendingAsync(baseYear, UserId);
            return StatusCode(response.StatusCode, response);
        }

        [Authorize]
        [HttpPost("{baseYear:int}/export")]
        public async Task<IActionResult> Export(int baseYear, [FromQuery] string format = "xlsx")
        {
            if (format?.ToLower() == "pdf")
            {
                byte[] pdfBytes = await service.ExportPdfAsync(baseYear, UserId);
                return File(pdfBytes, "application/pdf", $"IRPF_{baseYear}.pdf");
            }

            byte[] xlsxBytes = await service.ExportSpreadsheetAsync(baseYear, UserId);
            return File(xlsxBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"IRPF_{baseYear}.xlsx");
        }

        [Authorize]
        [HttpGet("snapshots")]
        public async Task<IActionResult> GetSnapshots()
        {
            var response = await service.GetSnapshotsAsync(UserId);
            return StatusCode(response.StatusCode, response);
        }

        [Authorize]
        [HttpPut("categories/{id}/treatment")]
        public async Task<IActionResult> UpdateCategoryTreatment(string id, [FromBody] UpdateCategoryIrTreatmentRequest request)
        {
            var response = await service.UpdateCategoryTreatmentAsync(id, request, UserId);
            return StatusCode(response.StatusCode, response);
        }
    }
}
