using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace api_finances.src.Repository
{
    public class AttachmentRepository(AppDbContext context) : IAttachmentRepository
    {
        #region READ
        public async Task<List<dynamic>> GetAllAsync(BsonDocument[] pipeline)
        {
            List<BsonDocument> results = await context.Attachments.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).ToList();
        }
        public async Task<List<dynamic>> GetSelectAsync(BsonDocument[] pipeline)
        {
            List<BsonDocument> results = await context.Attachments.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).ToList();
        }
        public async Task<dynamic?> GetByIdAggregateAsync(BsonDocument[] pipeline)
        {
            BsonDocument? response = await context.Attachments.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
            return response is null ? null : BsonSerializer.Deserialize<dynamic>(response);
        }
        public async Task<Attachment?> GetByIdAsync(string id)
        {
            return await context.Attachments.Find(x => x.Id == id && !x.Deleted).FirstOrDefaultAsync();
        }
        public async Task<List<Attachment>> GetByParentIdAsync(string parentId, string parent)
        {
            return await context.Attachments.Find(x => x.ParentId == parentId && x.Parent == parent && !x.Deleted).ToListAsync();
        }
        public async Task<int> GetCountDocumentsAsync(BsonDocument[] pipeline)
        {
            List<BsonDocument> results = await context.Attachments.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).Count();
        }
        #endregion
        #region CREATE
        public async Task<Attachment?> CreateAsync(Attachment entity)
        {
            await context.Attachments.InsertOneAsync(entity);
            return entity;
        }
        #endregion
        #region UPDATE
        public async Task<Attachment?> UpdateAsync(Attachment entity)
        {
            await context.Attachments.ReplaceOneAsync(x => x.Id == entity.Id, entity);
            return entity;
        }
        #endregion
        #region DELETE
        public async Task<Attachment> DeleteAsync(Attachment entity)
        {
            await context.Attachments.ReplaceOneAsync(x => x.Id == entity.Id, entity);
            return entity;
        }
        #endregion
    }
}