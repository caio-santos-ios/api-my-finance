using api_finances.src.Models;
using MongoDB.Bson;

namespace api_finances.src.Interfaces
{
    public interface IUserRepository
    {
        Task<List<dynamic>> GetAllAsync(BsonDocument[] pipeline);
        Task<dynamic?> GetByIdAggregateAsync(BsonDocument[] pipeline);
        Task<User?> GetByIdAsync(string id);
        Task<List<User>> GetNotByIdAsync(string id);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByCodeAsync(string code);
        Task<int> GetCountDocumentsAsync(BsonDocument[] pipeline);
        Task<User?> CreateAsync(User entity);
        Task<User?> UpdateAsync(User entity);
        Task<User> DeleteAsync(User entity);
    }
}