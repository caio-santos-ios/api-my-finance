using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace api_finances.src.Repository
{
    public class UserRepository(AppDbContext context) : IUserRepository
    {
        #region READ
        public async Task<List<dynamic>> GetAllAsync(BsonDocument[] pipeline)
        {
            List<BsonDocument> results = await context.Users.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).ToList();
        }
        public async Task<dynamic?> GetByIdAggregateAsync(BsonDocument[] pipeline)
        {
            BsonDocument? response = await context.Users.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
            return response is null ? null : BsonSerializer.Deserialize<dynamic>(response);
        }
        public async Task<User?> GetByIdAsync(string id)
        {
            return await context.Users.Find(x => x.Id == id && !x.Deleted).FirstOrDefaultAsync();
        }
        public async Task<List<User>> GetNotByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return await context.Users.Find(x => x.Admin && !x.Deleted).ToListAsync();
            }

            return await context.Users.Find(x => x.Id != id && x.Admin && !x.Deleted).ToListAsync();
        }
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await context.Users.Find(x => x.Email == email && !x.Deleted).FirstOrDefaultAsync();
        }
        public async Task<User?> GetByCodeAsync(string code)
        {
            return await context.Users.Find(x => x.CodeAccess == code && !x.Deleted).FirstOrDefaultAsync();
        }
        public async Task<int> GetCountDocumentsAsync(BsonDocument[] pipeline)
        {
            BsonDocument[] countPipeline = pipeline.Where(x => !x.Contains("$skip") && !x.Contains("$limit")).ToArray();
            List<BsonDocument> results = await context.Users.Aggregate<BsonDocument>(countPipeline).ToListAsync();
            return results.Count;
        }
        #endregion
        #region CREATE
        public async Task<User?> CreateAsync(User entity)
        {
            await context.Users.InsertOneAsync(entity);
            return entity;
        }
        #endregion
        #region UPDATE
        public async Task<User?> UpdateAsync(User entity)
        {
            await context.Users.ReplaceOneAsync(x => x.Id == entity.Id, entity);
            entity.Password = "";
            return entity;
        }
        #endregion
        #region DELETE
        public async Task<User> DeleteAsync(User entity)
        {
            await context.Users.ReplaceOneAsync(x => x.Id == entity.Id, entity);
            return entity;
        }
        #endregion
    }
}