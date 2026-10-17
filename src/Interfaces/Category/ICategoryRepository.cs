using api_finances.src.Models;
using MongoDB.Bson;

namespace api_finances.src.Interfaces
{
    public interface ICategoryRepository
    {
        Task<List<dynamic>> GetAllAsync(BsonDocument[] pipeline);
        Task<List<dynamic>> GetSelectAsync(BsonDocument[] pipeline);
        Task<dynamic?> GetByIdAggregateAsync(BsonDocument[] pipeline);
        Task<Category?> GetByIdAsync(string id);
        Task<long> GetNextCode(string userId);
        Task<int> GetCountDocumentsAsync(BsonDocument[] pipeline);
        Task<Category?> CreateAsync(Category entity);
        Task<Category?> UpdateAsync(Category entity);
        Task<Category> DeleteAsync(Category entity);
    }
}