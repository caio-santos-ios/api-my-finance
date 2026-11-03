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
    [Route("api/debts")]
    [ApiController]
    public class DebtController(IDebtService service) : ControllerBase
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
        public async Task<IActionResult> Create([FromBody] CreateDebtRequest request)
        {
            request.CreatedBy = UserId;
            ResponseApi<Debt?> response = await service.CreateAsync(request);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateDebtRequest request)
        {
            request.UpdatedBy = UserId;
            ResponseApi<Debt?> response = await service.UpdateAsync(request);
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
            ResponseApi<Debt> response = await service.DeleteAsync(request);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpPost("{id}/pay-installment")]
        public async Task<IActionResult> PayInstallment(string id, [FromBody] PayDebtInstallmentRequest request)
        {
            request.CreatedBy = UserId;
            ResponseApi<Debt?> response = await service.PayInstallmentAsync(id, request);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpGet("{id}/payments")]
        public async Task<IActionResult> GetPayments(string id)
        {
            ResponseApi<List<DebtPayment>> response = await service.GetPaymentsAsync(id);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpPost("{id}/payments")]
        public async Task<IActionResult> AddPayment(string id, [FromBody] Requests.Debt.AddDebtPaymentRequest request)
        {
            request.CreatedBy = UserId;
            ResponseApi<DebtPayment?> response = await service.AddPaymentAsync(id, request);
            return StatusCode(response.StatusCode, response);
        }

        [Authorize]
        [HttpPut("{id}/payments/{paymentId}")]
        public async Task<IActionResult> UpdatePayment(string id, string paymentId, [FromBody] Requests.Debt.UpdateDebtPaymentRequest request)
        {
            request.UpdatedBy = UserId;
            ResponseApi<DebtPayment?> response = await service.UpdatePaymentAsync(id, paymentId, request);
            return StatusCode(response.StatusCode, response);
        }

        [Authorize]
        [HttpDelete("{id}/payments/{paymentId}")]
        public async Task<IActionResult> DeletePayment(string id, string paymentId)
        {
            ResponseApi<bool> response = await service.DeletePaymentAsync(id, paymentId, UserId);
            return StatusCode(response.StatusCode, response);
        }
    }
}
