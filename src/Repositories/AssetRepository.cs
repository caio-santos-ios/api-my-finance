using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace api_finances.src.Repository
{
    public class AssetRepository(AppDbContext context) : IAssetRepository
    {
        public async Task<List<dynamic>> GetAllAsync(BsonDocument[] pipeline)
        {
            List<BsonDocument> results = await context.Assets.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).ToList();
        }

        public async Task<List<dynamic>> GetSelectAsync(BsonDocument[] pipeline)
        {
            List<BsonDocument> results = await context.Assets.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).ToList();
        }

        public async Task<dynamic?> GetByIdAggregateAsync(BsonDocument[] pipeline)
        {
            BsonDocument? response = await context.Assets.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
            return response is null ? null : BsonSerializer.Deserialize<dynamic>(response);
        }

        public async Task<Asset?> GetByIdAsync(string id)
        {
            return await context.Assets.Find(x => x.Id == id && !x.Deleted).FirstOrDefaultAsync();
        }

        public async Task<int> GetCountDocumentsAsync(BsonDocument[] pipeline)
        {
            BsonDocument[] countPipeline = pipeline.Where(x => !x.Contains("$skip") && !x.Contains("$limit")).ToArray();
            List<BsonDocument> results = await context.Assets.Aggregate<BsonDocument>(countPipeline).ToListAsync();
            return results.Count;
        }

        public async Task<Asset?> CreateAsync(Asset entity)
        {
            await context.Assets.InsertOneAsync(entity);
            return entity;
        }

        public async Task<Asset?> UpdateAsync(Asset entity)
        {
            await context.Assets.ReplaceOneAsync(x => x.Id == entity.Id, entity);
            return entity;
        }

        public async Task<Asset> DeleteAsync(Asset entity)
        {
            await context.Assets.ReplaceOneAsync(x => x.Id == entity.Id, entity);
            return entity;
        }

        public async Task<List<Asset>> GetActiveByUserAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return await context.Assets.Find(x => !x.Deleted && !x.Disposed).ToListAsync();
            }
            return await context.Assets.Find(x => !x.Deleted && !x.Disposed && (x.CreatedBy == userId || x.CreatedBy == null || x.CreatedBy == "")).ToListAsync();
        }

        public async Task<int> GetDisposedCountByUserAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return (int)await context.Assets.CountDocumentsAsync(x => !x.Deleted && x.Disposed);
            }
            return (int)await context.Assets.CountDocumentsAsync(x => !x.Deleted && x.Disposed && (x.CreatedBy == userId || x.CreatedBy == null || x.CreatedBy == ""));
        }
    }
}
