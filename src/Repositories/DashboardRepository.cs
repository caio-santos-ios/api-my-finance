using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace api_finances.src.Repository
{
    public class DashboardRepository(AppDbContext context) : IDashboardRepository
    {
        #region READ
        public async Task<List<Dashboard>> GetDashAsync(string userId)
        {
            List<Dashboard> entities = await context.Dashboards.Find(x => !x.Deleted && x.Active && x.CreatedBy == userId).ToListAsync();
            return entities.OrderBy(x => x.Sequence).ToList();
        }
        public async Task<List<dynamic>> GetAllAsync(BsonDocument[] pipeline)
        {
            List<BsonDocument> results = await context.Dashboards.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).ToList();
        }
        public async Task<dynamic?> GetByIdAggregateAsync(BsonDocument[] pipeline)
        {
            BsonDocument? response = await context.Dashboards.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
            return response is null ? null : BsonSerializer.Deserialize<dynamic>(response);
        }
        public async Task<Dashboard?> GetByIdAsync(string id)
        {
            return await context.Dashboards.Find(x => !x.Deleted && x.Id == id).FirstOrDefaultAsync();
        }
        public async Task<List<Operation>> GetDashAppAsync(string userId)
        {
            return await context.Operations.Find(x => !x.Deleted && x.CreatedBy == userId).ToListAsync();
        }
        public async Task<int> GetCountDocumentsAsync(BsonDocument[] pipeline)
        {
            List<BsonDocument> results = await context.Dashboards.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).Count();
        }
        #endregion
        #region CREATE
        public async Task<Dashboard?> CreateAsync(Dashboard entity)
        {
            await context.Dashboards.InsertOneAsync(entity);
            return entity;
        }
        #endregion
        #region UPDATE
        public async Task<Dashboard?> UpdateAsync(Dashboard entity)
        {
            await context.Dashboards.ReplaceOneAsync(x => x.Id == entity.Id, entity);
            return entity;
        }
        #endregion
        #region DELETE
        public async Task<Dashboard> DeleteAsync(Dashboard entity)
        {
            await context.Dashboards.ReplaceOneAsync(x => x.Id == entity.Id, entity);
            return entity;
        }
        #endregion
    }
}