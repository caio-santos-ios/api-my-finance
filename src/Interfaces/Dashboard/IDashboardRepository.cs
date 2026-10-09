using api_finances.src.Models;
using MongoDB.Bson;

namespace api_finances.src.Interfaces
{
    public interface IDashboardRepository
    {
        Task<List<Dashboard>> GetDashAsync(string userId);
        Task<List<dynamic>> GetAllAsync(List<BsonDocument> pipeline);
        Task<dynamic?> GetByIdAggregateAsync(List<BsonDocument> pipeline);
        Task<int> GetCountDocumentsAsync(List<BsonDocument> pipeline);
        Task<Dashboard?> GetByIdAsync(string id);
        Task<Dashboard?> CreateAsync(Dashboard entity);
        Task<Dashboard?> UpdateAsync(Dashboard entity);
    }
}