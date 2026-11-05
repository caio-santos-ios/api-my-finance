using api_finances.src.Models;

namespace api_finances.src.Interfaces
{
    public interface ITaxRepository
    {
        Task<List<Category>> GetCategoriesAsync(string userId);
        Task<Category?> GetCategoryByIdAsync(string categoryId);
        Task<bool> UpdateCategoryTreatmentAsync(string categoryId, string treatment, string? subtype, string userId);
        Task<List<Operation>> GetOperationsByPeriodAsync(string userId, DateTime start, DateTime end);
        Task<List<Asset>> GetAssetsAsync(string userId);
        Task<List<InvestmentAsset>> GetInvestmentAssetsAsync(string userId);
        Task<List<InvestmentTransaction>> GetInvestmentTransactionsAsync(string userId, DateTime upToDate);
        Task<List<Debt>> GetDebtsAsync(string userId);
        Task<List<DebtPayment>> GetDebtPaymentsAsync(string userId, DateTime upToDate);
        Task<List<Attachment>> GetAttachmentsByOriginIdsAsync(List<string> originIds);
        Task<TaxYearSnapshot> SaveSnapshotAsync(TaxYearSnapshot snapshot);
        Task<List<TaxYearSnapshot>> GetSnapshotsAsync(string userId, int? baseYear = null);
    }
}
