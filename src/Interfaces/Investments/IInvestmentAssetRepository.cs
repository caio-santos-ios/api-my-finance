using api_finances.src.Models;

namespace api_finances.src.Interfaces
{
    public interface IInvestmentAssetRepository
    {
        Task<InvestmentAsset?> GetByIdAsync(string id);
        Task<List<InvestmentAsset>> GetAllByUserAsync(string userId);
        Task<InvestmentAsset?> CreateAsync(InvestmentAsset asset);
        Task<InvestmentAsset?> UpdateAsync(InvestmentAsset asset);
        Task<InvestmentAsset> DeleteAsync(InvestmentAsset asset);
    }
}
