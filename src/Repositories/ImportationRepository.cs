using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace api_finances.src.Repository
{
    public class ImportationRepository(AppDbContext context) : IImportationRepository
    {
        #region READ
        public async Task<List<dynamic>> GetAllAsync(BsonDocument[] pipeline)
        {
            List<BsonDocument> results = await context.Importations.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).ToList();
        }
        public async Task<List<dynamic>> GetSelectAsync(BsonDocument[] pipeline)
        {
            List<BsonDocument> results = await context.Importations.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).ToList();
        }
        public async Task<dynamic?> GetByIdAggregateAsync(BsonDocument[] pipeline)
        {
            BsonDocument? response = await context.Importations.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
            return response is null ? null : BsonSerializer.Deserialize<dynamic>(response);
        }
        public async Task<Importation?> GetByIdAsync(string id)
        {
            return await context.Importations.Find(x => x.Id == id && !x.Deleted).FirstOrDefaultAsync();
        }
        public async Task<int> GetCountDocumentsAsync(BsonDocument[] pipeline)
        {
            List<BsonDocument> results = await context.Importations.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).Count();
        }
        #endregion
        #region CREATE
        public async Task<Importation?> CreateAsync(Importation entity)
        {
            await context.Importations.InsertOneAsync(entity);
            return entity;
        }
        #endregion
        #region UPDATE
        public async Task<Importation?> UpdateAsync(Importation entity)
        {
            await context.Importations.ReplaceOneAsync(x => x.Id == entity.Id, entity);
            return entity;
        }
        #endregion
        #region DELETE
        public async Task<Importation> DeleteAsync(Importation entity)
        {
            await context.Importations.ReplaceOneAsync(x => x.Id == entity.Id, entity);
            return entity;
        }
        #endregion
    }
}