using api_finances.src.Models;

namespace api_finances.src.Interfaces;

public interface IDebtPaymentRepository
{
    Task<List<DebtPayment>> GetByDebtIdAsync(string debtId);
    Task<DebtPayment?> CreateAsync(DebtPayment entity);
    Task<DebtPayment?> UpdateAsync(DebtPayment entity);
    Task<DebtPayment> DeleteAsync(DebtPayment entity);
    Task<DebtPayment?> GetByIdAsync(string id);
}
