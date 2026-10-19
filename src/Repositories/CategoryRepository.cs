using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace api_finances.src.Repository
{
    public class CategoryRepository(AppDbContext context) : ICategoryRepository
    {
        #region READ
        public async Task<List<dynamic>> GetAllAsync(BsonDocument[] pipeline)
        {
            List<BsonDocument> results = await context.Categories.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).ToList();
        }
        public async Task<List<dynamic>> GetSelectAsync(BsonDocument[] pipeline)
        {
            List<BsonDocument> results = await context.Categories.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).ToList();
        }
        public async Task<dynamic?> GetByIdAggregateAsync(BsonDocument[] pipeline)
        {
            BsonDocument? response = await context.Categories.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
            return response is null ? null : BsonSerializer.Deserialize<dynamic>(response);
        }
        public async Task<Category?> GetByIdAsync(string id)
        {
            return await context.Categories.Find(x => x.Id == id && !x.Deleted).FirstOrDefaultAsync();
        }
        public async Task<long> GetNextCode(string userId)
        {
            return await context.Categories.Find(x => x.CreatedBy == userId).CountDocumentsAsync() + 1;
        }
        public async Task<int> GetCountDocumentsAsync(BsonDocument[] pipeline)
        {
            BsonDocument[] countPipeline = pipeline.Where(x => !x.Contains("$skip") && !x.Contains("$limit")).ToArray();
            List<BsonDocument> results = await context.Categories.Aggregate<BsonDocument>(countPipeline).ToListAsync();
            return results.Count;
        }
        #endregion
        #region CREATE
        public async Task<Category?> CreateAsync(Category entity)
        {
            await context.Categories.InsertOneAsync(entity);
            return entity;
        }
        #endregion
        #region UPDATE
        public async Task<Category?> UpdateAsync(Category entity)
        {
            await context.Categories.ReplaceOneAsync(x => x.Id == entity.Id, entity);
            return entity;
        }
        #endregion
        #region DELETE
        public async Task<Category> DeleteAsync(Category entity)
        {
            await context.Categories.ReplaceOneAsync(x => x.Id == entity.Id, entity);
            return entity;
        }
        #endregion
    }
}