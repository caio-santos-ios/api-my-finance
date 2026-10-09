using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace api_finances.src.Repository
{
    public class GenericTableRepository(AppDbContext context) : IGenericTableRepository
    {
        public async Task<List<dynamic>> GetSelectAsync(List<BsonDocument> pipeline)
        {
            List<BsonDocument> results = await context.GenericTables.Aggregate<BsonDocument>(pipeline).ToListAsync();
            List<dynamic> entities = results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).ToList();
            return entities;
        }

        public async Task<GenericTable?> GetByIdAsync(string id)
        {
            return await context.GenericTables.Find(x => x.Id == id && !x.Deleted).FirstOrDefaultAsync();
        }
        public async Task<long> NextCodeAsync(string table, string userId)
        {
            return await context.GenericTables.Find(x => x.Table == table && x.CreatedBy == userId && !x.Deleted).CountDocumentsAsync();
        }

        public async Task<GenericTable> CreateAsync(GenericTable entity)
        {
            await context.GenericTables.InsertOneAsync(entity);
            return entity;
        }

        public async Task<GenericTable> UpdateAsync(GenericTable entity)
        {
            await context.GenericTables.ReplaceOneAsync(x => x.Id == entity.Id && x.CreatedBy == entity.CreatedBy, entity);
            return entity;
        }
    }
}
