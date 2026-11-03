using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;

namespace api_finances.src.Interfaces
{
    public interface IDebtService
    {
        Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllRequest request);
        Task<ResponseApi<List<dynamic>>> GetSelectAsync(GetAllRequest request);
        Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id);
        Task<ResponseApi<Debt?>> CreateAsync(CreateDebtRequest request);
        Task<ResponseApi<Debt?>> UpdateAsync(UpdateDebtRequest request);
        Task<ResponseApi<Debt>> DeleteAsync(DeleteRequest request);
        Task<ResponseApi<Debt?>> PayInstallmentAsync(string id, PayDebtInstallmentRequest request);
        Task<ResponseApi<List<DebtPayment>>> GetPaymentsAsync(string debtId);
        Task<ResponseApi<DebtPayment?>> AddPaymentAsync(string debtId, Requests.Debt.AddDebtPaymentRequest request);
        Task<ResponseApi<DebtPayment?>> UpdatePaymentAsync(string debtId, string paymentId, Requests.Debt.UpdateDebtPaymentRequest request);
        Task<ResponseApi<bool>> DeletePaymentAsync(string debtId, string paymentId, string userId);
    }
}
