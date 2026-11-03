using api_finances.src.Models;
using MongoDB.Bson;

namespace api_finances.src.Interfaces
{
    public interface IAssetRepository
    {
        Task<List<dynamic>> GetAllAsync(BsonDocument[] pipeline);
        Task<List<dynamic>> GetSelectAsync(BsonDocument[] pipeline);
        Task<dynamic?> GetByIdAggregateAsync(BsonDocument[] pipeline);
        Task<Asset?> GetByIdAsync(string id);
        Task<int> GetCountDocumentsAsync(BsonDocument[] pipeline);
        Task<Asset?> CreateAsync(Asset entity);
        Task<Asset?> UpdateAsync(Asset entity);
        Task<Asset> DeleteAsync(Asset entity);
        Task<List<Asset>> GetActiveByUserAsync(string userId);
    }
}
