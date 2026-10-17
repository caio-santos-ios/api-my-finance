using api_finances.src.Models;
using MongoDB.Bson;

namespace api_finances.src.Interfaces
{
    public interface IBudgetRepository
    {
        Task<List<dynamic>> GetAllAsync(BsonDocument[] pipeline);
        Task<List<dynamic>> GetSelectAsync(BsonDocument[] pipeline);
        Task<dynamic?> GetByIdAggregateAsync(BsonDocument[] pipeline);
        Task<Budget?> GetByIdAsync(string id);
        Task<int> GetCountDocumentsAsync(BsonDocument[] pipeline);
        Task<Budget?> CreateAsync(Budget entity);
        Task<Budget?> UpdateAsync(Budget entity);
        Task<Budget> DeleteAsync(Budget entity);
    }
}
