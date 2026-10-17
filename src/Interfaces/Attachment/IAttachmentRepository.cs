using api_finances.src.Models;
using MongoDB.Bson;

namespace api_finances.src.Interfaces
{
    public interface IAttachmentRepository
    {
        Task<List<dynamic>> GetAllAsync(BsonDocument[] pipeline);
        Task<List<dynamic>> GetSelectAsync(BsonDocument[] pipeline);
        Task<dynamic?> GetByIdAggregateAsync(BsonDocument[] pipeline);
        Task<Attachment?> GetByIdAsync(string id);
        Task<List<Attachment>> GetByParentIdAsync(string parentId, string parent);
        Task<int> GetCountDocumentsAsync(BsonDocument[] pipeline);
        Task<Attachment?> CreateAsync(Attachment entity);
        Task<Attachment?> UpdateAsync(Attachment entity);
        Task<Attachment> DeleteAsync(Attachment entity);
    }
}