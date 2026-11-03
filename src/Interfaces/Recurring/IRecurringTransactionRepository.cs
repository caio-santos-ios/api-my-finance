using api_finances.src.Models;
using MongoDB.Bson;

namespace api_finances.src.Interfaces;

public interface IRecurringTransactionRepository
{
    Task<List<dynamic>> GetAllAsync(BsonDocument[] pipeline);
    Task<dynamic?> GetByIdAggregateAsync(BsonDocument[] pipeline);
    Task<RecurringTransaction?> GetByIdAsync(string id);
    Task<int> GetCountDocumentsAsync(BsonDocument[] pipeline);
    Task<RecurringTransaction?> CreateAsync(RecurringTransaction entity);
    Task<RecurringTransaction?> UpdateAsync(RecurringTransaction entity);
    Task<RecurringTransaction> DeleteAsync(RecurringTransaction entity);
    Task<List<RecurringTransaction>> GetActiveByUserAsync(string userId);
}
