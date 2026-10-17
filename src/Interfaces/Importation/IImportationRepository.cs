using api_finances.src.Models;
using MongoDB.Bson;

namespace api_finances.src.Interfaces
{
    public interface IImportationRepository
    {
        Task<List<dynamic>> GetAllAsync(BsonDocument[] pipeline);
        Task<List<dynamic>> GetSelectAsync(BsonDocument[] pipeline);
        Task<dynamic?> GetByIdAggregateAsync(BsonDocument[] pipeline);
        Task<Importation?> GetByIdAsync(string id);
        Task<int> GetCountDocumentsAsync(BsonDocument[] pipeline);
        Task<Importation?> CreateAsync(Importation entity);
        Task<Importation?> UpdateAsync(Importation entity);
        Task<Importation> DeleteAsync(Importation entity);
    }
}