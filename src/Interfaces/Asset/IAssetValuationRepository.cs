using api_finances.src.Models;

namespace api_finances.src.Interfaces;

public interface IAssetValuationRepository
{
    Task<List<AssetValuation>> GetByAssetIdAsync(string assetId);
    Task<AssetValuation?> CreateAsync(AssetValuation entity);
    Task<AssetValuation?> UpdateAsync(AssetValuation entity);
    Task<AssetValuation> DeleteAsync(AssetValuation entity);
    Task<AssetValuation?> GetByIdAsync(string id);
}
