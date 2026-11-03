using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using MongoDB.Driver;

namespace api_finances.src.Repository;

public class AssetValuationRepository(AppDbContext context) : IAssetValuationRepository
{
    public async Task<List<AssetValuation>> GetByAssetIdAsync(string assetId)
    {
        return await context.AssetValuations
            .Find(x => x.AssetId == assetId && !x.Deleted)
            .SortByDescending(x => x.Date)
            .ToListAsync();
    }

    public async Task<AssetValuation?> CreateAsync(AssetValuation entity)
    {
        await context.AssetValuations.InsertOneAsync(entity);
        return entity;
    }

    public async Task<AssetValuation?> UpdateAsync(AssetValuation entity)
    {
        await context.AssetValuations.ReplaceOneAsync(x => x.Id == entity.Id, entity);
        return entity;
    }

    public async Task<AssetValuation> DeleteAsync(AssetValuation entity)
    {
        await context.AssetValuations.ReplaceOneAsync(x => x.Id == entity.Id, entity);
        return entity;
    }

    public async Task<AssetValuation?> GetByIdAsync(string id)
    {
        return await context.AssetValuations.Find(x => x.Id == id && !x.Deleted).FirstOrDefaultAsync();
    }
}
