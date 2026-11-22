using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace api_finances.src.Repository
{
    public class BankRepository(AppDbContext context) : IBankRepository
    {
        #region READ
        public async Task<List<dynamic>> GetAllAsync(BsonDocument[] pipeline)
        {
            List<BsonDocument> results = await context.Banks.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).ToList();
        }
        public async Task<List<dynamic>> GetSelectAsync(BsonDocument[] pipeline)
        {
            List<BsonDocument> results = await context.Banks.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).ToList();
        }
        public async Task<dynamic?> GetByIdAggregateAsync(BsonDocument[] pipeline)
        {
            BsonDocument? response = await context.Banks.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
            return response is null ? null : BsonSerializer.Deserialize<dynamic>(response);
        }
        public async Task<Bank?> GetByIdAsync(string id)
        {
            return await context.Banks.Find(x => x.Id == id && !x.Deleted).FirstOrDefaultAsync();
        }
        public async Task<Bank?> GetByCodeAsync(string code)
        {
            return await context.Banks.Find(x => x.Code == code && !x.Deleted).FirstOrDefaultAsync();
        }
        public async Task<Bank?> GetFirstAsync()
        {
            return await context.Banks.Find(x => !x.Deleted).FirstOrDefaultAsync();
        }
        public async Task<long> GetNextCode(string userId)
        {
            return await context.Banks.Find(x => x.CreatedBy == userId).CountDocumentsAsync() + 1;
        }
        public async Task<int> GetCountDocumentsAsync(BsonDocument[] pipeline)
        {
            BsonDocument[] countPipeline = pipeline.Where(x => !x.Contains("$skip") && !x.Contains("$limit")).ToArray();
            List<BsonDocument> results = await context.Banks.Aggregate<BsonDocument>(countPipeline).ToListAsync();
            return results.Count;
        }
        public async Task RestoreOrphanedBanksAsync()
        {
            try
            {
                var filter = Builders<Bank>.Filter.Where(x => (x.CreatedBy == "" || x.CreatedBy == null) && !x.Deleted);
                var orphaned = await context.Banks.Find(filter).ToListAsync();
                if (orphaned.Count == 0) return;

                foreach (var b in orphaned)
                {
                    string? owner = !string.IsNullOrEmpty(b.UpdatedBy) ? b.UpdatedBy : null;
                    if (string.IsNullOrEmpty(owner))
                    {
                        var op = await context.Operations.Find(x => x.BankId == b.Id && !string.IsNullOrEmpty(x.CreatedBy)).FirstOrDefaultAsync();
                        if (op != null) owner = op.CreatedBy;
                    }
                    if (!string.IsNullOrEmpty(owner))
                    {
                        b.CreatedBy = owner;
                        await context.Banks.ReplaceOneAsync(x => x.Id == b.Id, b);
                    }
                }
            }
            catch
            {
                // Silently ignore to avoid breaking list requests
            }
        }
        #endregion
        #region CREATE
        public async Task<Bank?> CreateAsync(Bank entity)
        {
            await context.Banks.InsertOneAsync(entity);
            return entity;
        }
        #endregion
        #region UPDATE
        public async Task<Bank?> UpdateAsync(Bank entity)
        {
            await context.Banks.ReplaceOneAsync(x => x.Id == entity.Id, entity);
            return entity;
        }
        #endregion
        #region DELETE
        public async Task<Bank> DeleteAsync(Bank entity)
        {
            await context.Banks.ReplaceOneAsync(x => x.Id == entity.Id, entity);
            return entity;
        }
        #endregion
    }
}