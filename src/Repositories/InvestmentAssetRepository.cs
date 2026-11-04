using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using MongoDB.Driver;

namespace api_finances.src.Repository
{
    public class InvestmentAssetRepository(AppDbContext context) : IInvestmentAssetRepository
    {
        public async Task<InvestmentAsset?> GetByIdAsync(string id)
        {
            return await context.InvestmentAssets.Find(x => x.Id == id && !x.Deleted).FirstOrDefaultAsync();
        }

        public async Task<List<InvestmentAsset>> GetAllByUserAsync(string userId)
        {
            return await context.InvestmentAssets
                .Find(x => (x.UserId == userId || x.CreatedBy == userId) && !x.Deleted)
                .SortBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<InvestmentAsset?> CreateAsync(InvestmentAsset asset)
        {
            asset.CreatedAt = DateTime.UtcNow;
            asset.UpdatedAt = DateTime.UtcNow;
            await context.InvestmentAssets.InsertOneAsync(asset);
            return asset;
        }

        public async Task<InvestmentAsset?> UpdateAsync(InvestmentAsset asset)
        {
            asset.UpdatedAt = DateTime.UtcNow;
            await context.InvestmentAssets.ReplaceOneAsync(x => x.Id == asset.Id, asset);
            return asset;
        }

        public async Task<InvestmentAsset> DeleteAsync(InvestmentAsset asset)
        {
            asset.Deleted = true;
            asset.DeletedAt = DateTime.UtcNow;
            await context.InvestmentAssets.ReplaceOneAsync(x => x.Id == asset.Id, asset);
            return asset;
        }
    }
}
