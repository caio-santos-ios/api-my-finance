using api_finances.src.Models;
using MongoDB.Bson;

namespace api_finances.src.Interfaces
{
    public interface IDebtRepository
    {
        Task<List<dynamic>> GetAllAsync(BsonDocument[] pipeline);
        Task<List<dynamic>> GetSelectAsync(BsonDocument[] pipeline);
        Task<dynamic?> GetByIdAggregateAsync(BsonDocument[] pipeline);
        Task<Debt?> GetByIdAsync(string id);
        Task<int> GetCountDocumentsAsync(BsonDocument[] pipeline);
        Task<Debt?> CreateAsync(Debt entity);
        Task<Debt?> UpdateAsync(Debt entity);
        Task<Debt> DeleteAsync(Debt entity);
        Task<List<Debt>> GetActiveByUserAsync(string userId);
        Task<int> GetSettledCountByUserAsync(string userId);
    }
}
