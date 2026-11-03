using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace api_finances.src.Repository;

public class RecurringTransactionRepository(AppDbContext context) : IRecurringTransactionRepository
{
    public async Task<List<dynamic>> GetAllAsync(BsonDocument[] pipeline)
    {
        List<BsonDocument> results = await context.RecurringTransactions.Aggregate<BsonDocument>(pipeline).ToListAsync();
        return results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).ToList();
    }

    public async Task<dynamic?> GetByIdAggregateAsync(BsonDocument[] pipeline)
    {
        BsonDocument? response = await context.RecurringTransactions.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
        return response is null ? null : BsonSerializer.Deserialize<dynamic>(response);
    }

    public async Task<RecurringTransaction?> GetByIdAsync(string id)
    {
        return await context.RecurringTransactions.Find(x => x.Id == id && !x.Deleted).FirstOrDefaultAsync();
    }

    public async Task<int> GetCountDocumentsAsync(BsonDocument[] pipeline)
    {
        BsonDocument[] countPipeline = pipeline.Where(x => !x.Contains("$skip") && !x.Contains("$limit")).ToArray();
        List<BsonDocument> results = await context.RecurringTransactions.Aggregate<BsonDocument>(countPipeline).ToListAsync();
        return results.Count;
    }

    public async Task<RecurringTransaction?> CreateAsync(RecurringTransaction entity)
    {
        await context.RecurringTransactions.InsertOneAsync(entity);
        return entity;
    }

    public async Task<RecurringTransaction?> UpdateAsync(RecurringTransaction entity)
    {
        await context.RecurringTransactions.ReplaceOneAsync(x => x.Id == entity.Id, entity);
        return entity;
    }

    public async Task<RecurringTransaction> DeleteAsync(RecurringTransaction entity)
    {
        await context.RecurringTransactions.ReplaceOneAsync(x => x.Id == entity.Id, entity);
        return entity;
    }

    public async Task<List<RecurringTransaction>> GetActiveByUserAsync(string userId)
    {
        return await context.RecurringTransactions
            .Find(x => x.CreatedBy == userId && !x.Deleted)
            .ToListAsync();
    }
}
