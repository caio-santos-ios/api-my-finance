using api_finances.src.Models;
using MongoDB.Bson;

namespace api_finances.src.Interfaces
{
    public interface IGoalRepository
    {
        Task<List<dynamic>> GetAllAsync(BsonDocument[] pipeline);
        Task<List<Goal>> GetRawAllAsync(string userId);
        Task<Goal?> GetByIdAsync(string id);
        Task<dynamic?> GetByIdAggregateAsync(BsonDocument[] pipeline);
        Task<int> GetCountDocumentsAsync(BsonDocument[] pipeline);
        Task<Goal?> CreateAsync(Goal entity);
        Task<Goal?> UpdateAsync(Goal entity);
        Task<Goal> DeleteAsync(Goal entity);
        Task<List<GoalTransaction>> GetTransactionsAsync(string goalId);
        Task<GoalTransaction?> GetTransactionByIdAsync(string id);
        Task<GoalTransaction> CreateTransactionAsync(GoalTransaction entity);
        Task<GoalTransaction> DeleteTransactionAsync(GoalTransaction entity);
    }
}
