using api_finances.src.Models;
using MongoDB.Bson;

namespace api_finances.src.Interfaces
{
    public interface IOperationRepository
    {
        Task<List<dynamic>> GetAllAsync(BsonDocument[] pipeline);
        Task<List<dynamic>> GetSelectAsync(BsonDocument[] pipeline);
        Task<dynamic?> GetByIdAggregateAsync(BsonDocument[] pipeline);
        Task<Operation?> GetByIdAsync(string id);
        Task<Operation?> GetByOriginIdAsync(string originId);
        Task<long> GetNextCode(string userId);
        Task<int> GetCountDocumentsAsync(BsonDocument[] pipeline);
        Task<Operation?> CreateAsync(Operation entity);
        Task<Operation?> UpdateAsync(Operation entity);
        Task<Operation> DeleteAsync(Operation entity);
    }
}