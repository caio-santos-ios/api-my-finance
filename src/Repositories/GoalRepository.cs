using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace api_finances.src.Repository
{
    public class GoalRepository(AppDbContext context) : IGoalRepository
    {
        public async Task<List<dynamic>> GetAllAsync(BsonDocument[] pipeline)
        {
            List<BsonDocument> results = await context.Goals.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).ToList();
        }

        public async Task<List<Goal>> GetRawAllAsync(string userId)
        {
            return await context.Goals.Find(x => x.CreatedBy == userId && !x.Deleted).ToListAsync();
        }

        public async Task<dynamic?> GetByIdAggregateAsync(BsonDocument[] pipeline)
        {
            BsonDocument? response = await context.Goals.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
            return response is null ? null : BsonSerializer.Deserialize<dynamic>(response);
        }

        public async Task<Goal?> GetByIdAsync(string id)
        {
            return await context.Goals.Find(x => x.Id == id && !x.Deleted).FirstOrDefaultAsync();
        }

        public async Task<int> GetCountDocumentsAsync(BsonDocument[] pipeline)
        {
            BsonDocument[] countPipeline = pipeline.Where(x => !x.Contains("$skip") && !x.Contains("$limit")).ToArray();
            List<BsonDocument> results = await context.Goals.Aggregate<BsonDocument>(countPipeline).ToListAsync();
            return results.Count;
        }

        public async Task<Goal?> CreateAsync(Goal entity)
        {
            await context.Goals.InsertOneAsync(entity);
            return entity;
        }

        public async Task<Goal?> UpdateAsync(Goal entity)
        {
            await context.Goals.ReplaceOneAsync(x => x.Id == entity.Id, entity);
            return entity;
        }

        public async Task<Goal> DeleteAsync(Goal entity)
        {
            await context.Goals.ReplaceOneAsync(x => x.Id == entity.Id, entity);
            return entity;
        }

        public async Task<List<GoalTransaction>> GetTransactionsAsync(string goalId)
        {
            return await context.GoalTransactions
                .Find(x => x.GoalId == goalId && !x.Deleted)
                .SortByDescending(x => x.Date)
                .ToListAsync();
        }

        public async Task<GoalTransaction?> GetTransactionByIdAsync(string id)
        {
            return await context.GoalTransactions.Find(x => x.Id == id && !x.Deleted).FirstOrDefaultAsync();
        }

        public async Task<GoalTransaction> CreateTransactionAsync(GoalTransaction entity)
        {
            await context.GoalTransactions.InsertOneAsync(entity);
            return entity;
        }

        public async Task<GoalTransaction> DeleteTransactionAsync(GoalTransaction entity)
        {
            await context.GoalTransactions.ReplaceOneAsync(x => x.Id == entity.Id, entity);
            return entity;
        }
    }
}
