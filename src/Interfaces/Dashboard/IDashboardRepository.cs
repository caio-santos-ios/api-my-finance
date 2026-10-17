using api_finances.src.Models;
using MongoDB.Bson;

namespace api_finances.src.Interfaces
{
    public interface IDashboardRepository
    {
        Task<List<Dashboard>> GetDashAsync(string userId);
        Task<List<dynamic>> GetAllAsync(BsonDocument[] pipeline);
        Task<dynamic?> GetByIdAggregateAsync(BsonDocument[] pipeline);
        Task<int> GetCountDocumentsAsync(BsonDocument[] pipeline);
        Task<List<Operation>> GetDashAppAsync(string userId);
        Task<Dashboard?> GetByIdAsync(string id);
        Task<Dashboard?> CreateAsync(Dashboard entity);
        Task<Dashboard?> UpdateAsync(Dashboard entity);
        Task<Dashboard> DeleteAsync(Dashboard entity);
    }
}