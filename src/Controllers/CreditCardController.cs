using System.Security.Claims;
using api_finances.src.Interfaces;
using api_finances.src.Requests.CreditCard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api_finances.src.Controllers
{
    [Route("api/credit-cards")]
    [Authorize]
    [ApiController]
    public class CreditCardController(ICreditCardService service) : ControllerBase
    {
        private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await service.GetAllAsync(UserId);
            return StatusCode(response.StatusCode, response.Result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCreditCardRequest request)
        {
            var response = await service.CreateAsync(UserId, request);
            return StatusCode(response.StatusCode, response.Result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateCreditCardRequest request)
        {
            var response = await service.UpdateAsync(UserId, id, request);
            return StatusCode(response.StatusCode, response.Result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var response = await service.DeleteAsync(UserId, id);
            return StatusCode(response.StatusCode, response.Result);
        }

        [HttpGet("{id}/invoices")]
        public async Task<IActionResult> GetInvoices(string id)
        {
            var response = await service.GetInvoicesAsync(UserId, id);
            return StatusCode(response.StatusCode, response.Result);
        }

        [HttpGet("{id}/invoices/{invoiceId}/transactions")]
        public async Task<IActionResult> GetInvoiceTransactions(string id, string invoiceId)
        {
            var response = await service.GetInvoiceTransactionsAsync(UserId, id, invoiceId);
            return StatusCode(response.StatusCode, response.Result);
        }

        [HttpPost("{id}/transactions")]
        public async Task<IActionResult> CreateTransaction(string id, [FromBody] CreateCreditCardTransactionRequest request)
        {
            var response = await service.CreateTransactionAsync(UserId, id, request);
            return StatusCode(response.StatusCode, response.Result);
        }

        [HttpPut("{id}/transactions/{txId}")]
        public async Task<IActionResult> UpdateTransaction(string id, string txId, [FromBody] UpdateCreditCardTransactionRequest request)
        {
            var response = await service.UpdateTransactionAsync(UserId, id, txId, request);
            return StatusCode(response.StatusCode, response.Result);
        }

        [HttpDelete("{id}/transactions/{txId}")]
        public async Task<IActionResult> DeleteTransaction(string id, string txId, [FromQuery] bool deleteGroup = false)
        {
            var response = await service.DeleteTransactionAsync(UserId, id, txId, deleteGroup);
            return StatusCode(response.StatusCode, response.Result);
        }

        [HttpPost("{id}/invoices/{invoiceId}/pay")]
        public async Task<IActionResult> PayInvoice(string id, string invoiceId, [FromBody] PayCreditCardInvoiceRequest request)
        {
            var response = await service.PayInvoiceAsync(UserId, id, invoiceId, request);
            return StatusCode(response.StatusCode, response.Result);
        }
    }
}
