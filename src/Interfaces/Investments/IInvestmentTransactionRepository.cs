using api_finances.src.Models;

namespace api_finances.src.Interfaces
{
    public interface IInvestmentTransactionRepository
    {
        Task<InvestmentTransaction?> GetByIdAsync(string id);
        Task<List<InvestmentTransaction>> GetAllByUserAsync(string userId, string? assetId = null, string? brokerId = null, string? kind = null, DateTime? start = null, DateTime? end = null);
        Task<List<InvestmentTransaction>> GetByAssetIdAsync(string assetId);
        Task<List<string>> GetExistingImportHashesAsync(string userId, IEnumerable<string> hashes);
        Task<InvestmentTransaction?> CreateAsync(InvestmentTransaction transaction);
        Task<InvestmentTransaction?> UpdateAsync(InvestmentTransaction transaction);
        Task<InvestmentTransaction> DeleteAsync(InvestmentTransaction transaction);
    }
}
