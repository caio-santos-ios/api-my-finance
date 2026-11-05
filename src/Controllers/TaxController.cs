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
        [HttpGet("preview")]
        [HttpGet("{baseYear:int}/preview")]
        public async Task<IActionResult> GetPreview(int? baseYear)
        {
            int year = baseYear ?? (DateTime.UtcNow.Year - 1);
            var response = await service.GetPreviewAsync(year, UserId);
            return StatusCode(response.StatusCode, response);
        }

        [Authorize]
        [HttpGet("income")]
        [HttpGet("{baseYear:int}/income")]
        public async Task<IActionResult> GetIncome(int? baseYear)
        {
            int year = baseYear ?? (DateTime.UtcNow.Year - 1);
            var response = await service.GetIncomeAsync(year, UserId);
            return StatusCode(response.StatusCode, response);
        }

        [Authorize]
        [HttpGet("deductions")]
        [HttpGet("{baseYear:int}/deductions")]
        public async Task<IActionResult> GetDeductions(int? baseYear)
        {
            int year = baseYear ?? (DateTime.UtcNow.Year - 1);
            var response = await service.GetDeductionsAsync(year, UserId);
            return StatusCode(response.StatusCode, response);
        }

        [Authorize]
        [HttpGet("assets")]
        [HttpGet("{baseYear:int}/assets")]
        public async Task<IActionResult> GetAssets(int? baseYear)
        {
            int year = baseYear ?? (DateTime.UtcNow.Year - 1);
            var response = await service.GetAssetsAsync(year, UserId);
            return StatusCode(response.StatusCode, response);
        }

        [Authorize]
        [HttpGet("debts")]
        [HttpGet("{baseYear:int}/debts")]
        public async Task<IActionResult> GetDebts(int? baseYear)
        {
            int year = baseYear ?? (DateTime.UtcNow.Year - 1);
            var response = await service.GetDebtsAsync(year, UserId);
            return StatusCode(response.StatusCode, response);
        }

        [Authorize]
        [HttpGet("pending")]
        [HttpGet("{baseYear:int}/pending")]
        public async Task<IActionResult> GetPending(int? baseYear)
        {
            int year = baseYear ?? (DateTime.UtcNow.Year - 1);
            var response = await service.GetPendingAsync(year, UserId);
            return StatusCode(response.StatusCode, response);
        }

        [Authorize]
        [HttpGet("export")]
        [HttpGet("{baseYear:int}/export")]
        [HttpPost("export")]
        [HttpPost("{baseYear:int}/export")]
        public async Task<IActionResult> Export(int? baseYear, [FromQuery] string format = "xlsx")
        {
            int year = baseYear ?? (DateTime.UtcNow.Year - 1);
            if (format?.ToLower() == "pdf")
            {
                byte[] pdfBytes = await service.ExportPdfAsync(year, UserId);
                return File(pdfBytes, "application/pdf", $"IRPF_{year}.pdf");
            }

            byte[] xlsxBytes = await service.ExportSpreadsheetAsync(year, UserId);
            return File(xlsxBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"IRPF_{year}.xlsx");
        }

        [Authorize]
        [HttpGet("snapshots")]
        public async Task<IActionResult> GetSnapshots([FromQuery] int? baseYear)
        {
            var response = await service.GetSnapshotsAsync(UserId, baseYear);
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
