using api_finances.src.Models;
using MongoDB.Bson;

namespace api_finances.src.Interfaces
{
    public interface IBankRepository
    {
        Task<List<dynamic>> GetAllAsync(BsonDocument[] pipeline);
        Task<List<dynamic>> GetSelectAsync(BsonDocument[] pipeline);
        Task<dynamic?> GetByIdAggregateAsync(BsonDocument[] pipeline);
        Task<Bank?> GetByIdAsync(string id);
        Task<Bank?> GetByCodeAsync(string code);
        Task<Bank?> GetFirstAsync();
        Task<long> GetNextCode(string userId);
        Task<int> GetCountDocumentsAsync(BsonDocument[] pipeline);
        Task<Bank?> CreateAsync(Bank entity);
        Task<Bank?> UpdateAsync(Bank entity);
        Task<Bank> DeleteAsync(Bank entity);
    }
}