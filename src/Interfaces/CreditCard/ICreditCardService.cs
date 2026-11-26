using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests.CreditCard;
using api_finances.src.Shared.DTOs;

namespace api_finances.src.Interfaces
{
    public interface ICreditCardService
    {
        Task<ResponseApi<List<CreditCardResponseDto>>> GetAllAsync(string userId);
        Task<ResponseApi<CreditCard?>> CreateAsync(string userId, CreateCreditCardRequest request);
        Task<ResponseApi<CreditCard?>> UpdateAsync(string userId, string id, UpdateCreditCardRequest request);
        Task<ResponseApi<bool>> DeleteAsync(string userId, string id);
        Task<ResponseApi<List<CreditCardInvoiceResponseDto>>> GetInvoicesAsync(string userId, string cardId);
        Task<ResponseApi<List<CreditCardTransactionResponseDto>>> GetInvoiceTransactionsAsync(string userId, string cardId, string invoiceId);
        Task<ResponseApi<List<CreditCardTransaction>>> CreateTransactionAsync(string userId, string cardId, CreateCreditCardTransactionRequest request);
        Task<ResponseApi<CreditCardTransaction?>> UpdateTransactionAsync(string userId, string cardId, string txId, UpdateCreditCardTransactionRequest request);
        Task<ResponseApi<bool>> DeleteTransactionAsync(string userId, string cardId, string txId, bool deleteGroup = false);
        Task<ResponseApi<CreditCardInvoice?>> PayInvoiceAsync(string userId, string cardId, string invoiceId, PayCreditCardInvoiceRequest request);
    }
}
