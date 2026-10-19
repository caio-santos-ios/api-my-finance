using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace api_finances.src.Repository
{
    public class OperationRepository(AppDbContext context) : IOperationRepository
    {
        #region READ
        public async Task<List<dynamic>> GetAllAsync(BsonDocument[] pipeline)
        {
            List<BsonDocument> results = await context.Operations.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).ToList();
        }
        public async Task<List<dynamic>> GetSelectAsync(BsonDocument[] pipeline)
        {
            List<BsonDocument> results = await context.Operations.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).ToList();
        }
        public async Task<dynamic?> GetByIdAggregateAsync(BsonDocument[] pipeline)
        {
            BsonDocument? response = await context.Operations.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
            return response is null ? null : BsonSerializer.Deserialize<dynamic>(response);
        }
        public async Task<Operation?> GetByIdAsync(string id)
        {
            return await context.Operations.Find(x => x.Id == id && !x.Deleted).FirstOrDefaultAsync();
        }
        public async Task<long> GetNextCode(string userId)
        {
            return await context.Operations.Find(x => x.CreatedBy == userId).CountDocumentsAsync() + 1;
        }
        public async Task<Operation?> GetByOriginIdAsync(string originId)
        {
            return await context.Operations.Find(x => !x.Deleted && x.OriginId == originId).FirstOrDefaultAsync();
        }
        public async Task<int> GetCountDocumentsAsync(BsonDocument[] pipeline)
        {
            BsonDocument[] countPipeline = pipeline.Where(x => !x.Contains("$skip") && !x.Contains("$limit")).ToArray();
            List<BsonDocument> results = await context.Operations.Aggregate<BsonDocument>(countPipeline).ToListAsync();
            return results.Count;
        }
        #endregion
        #region CREATE
        public async Task<Operation?> CreateAsync(Operation entity)
        {
            await context.Operations.InsertOneAsync(entity);
            return entity;
        }
        #endregion
        #region UPDATE
        public async Task<Operation?> UpdateAsync(Operation entity)
        {
            await context.Operations.ReplaceOneAsync(x => x.Id == entity.Id, entity);
            return entity;
        }
        #endregion
        #region DELETE
        public async Task<Operation> DeleteAsync(Operation entity)
        {
            await context.Operations.ReplaceOneAsync(x => x.Id == entity.Id, entity);
            return entity;
        }
        #endregion
    }
}