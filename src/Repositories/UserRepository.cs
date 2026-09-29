using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace api_finances.src.Repository
{
    public class UserRepository(AppDbContext context) : IUserRepository
    {
        #region CREATE
        public async Task<ResponseApi<User?>> CreateAsync(User user)
        {
            try
            {
                await context.Users.InsertOneAsync(user);
                return new(user, 201, "Usuário criado com sucesso");
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        #endregion
        #region READ
        public async Task<ResponseApi<List<dynamic>>> GetAllAsync(PaginationUtil<User> pagination)
        {
            try
            {
                List<BsonDocument> pipeline = new()
                {
                    new("$match", pagination.PipelineFilter),
                    new("$sort", pagination.PipelineSort),
                    new("$skip", pagination.Skip),
                    new("$limit", pagination.Limit),

                    new("$addFields", new BsonDocument
                    {
                        {"id", new BsonDocument("$toString", "$_id")},                        
                    }),

                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"password", 0},
                    }),
                    new("$sort", pagination.PipelineSort),
                };

                List<BsonDocument> results = await context.Users.Aggregate<BsonDocument>(pipeline).ToListAsync();
                List<dynamic> list = results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).ToList();
                return new(list);
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        public async Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id)
        {
            try
            {
                BsonDocument[] pipeline = [
                    new("$match", new BsonDocument{
                        {"_id", new ObjectId(id)},
                        {"deleted", false}
                    }),

                    new("$addFields", new BsonDocument
                    {
                        {"id", new BsonDocument("$toString", "$_id")},
                    }),

                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"password", 0},                       
                    }),
                ];

                BsonDocument? response = await context.Users.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
                dynamic? result = response is null ? null : BsonSerializer.Deserialize<dynamic>(response);
                return result is null ? new(null, 404, "Usuário não encontrado") : new(result);
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        public async Task<ResponseApi<User?>> GetByIdAsync(string id)
        {
            try
            {
                User? user = await context.Users.Find(x => x.Id == id && !x.Deleted).FirstOrDefaultAsync();
                return new(user);
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        public async Task<ResponseApi<List<User>>> GetNotByIdAsync(string id)
        {
            try
            {
                List<User> users = [];
                if (string.IsNullOrEmpty(id))
                {
                    users = await context.Users.Find(x => x.Admin && !x.Deleted).ToListAsync();
                    return new(users);
                }

                users = await context.Users.Find(x => x.Id != id && x.Admin && !x.Deleted).ToListAsync();
                return new(users);
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        public async Task<ResponseApi<User?>> GetByEmailAsync(string email)
        {
            try
            {
                User? user = await context.Users.Find(x => x.Email == email && !x.Deleted).FirstOrDefaultAsync();
                return new(user);
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        public async Task<ResponseApi<User?>> GetByCodeAsync(string code)
        {
            try
            {
                User? user = await context.Users.Find(x => x.CodeAccess == code && !x.Deleted).FirstOrDefaultAsync();
                return new(user);
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        public async Task<int> GetCountDocumentsAsync(PaginationUtil<User> pagination)
        {
            List<BsonDocument> pipeline = new()
            {
                new("$match", pagination.PipelineFilter),
                new("$sort", pagination.PipelineSort),
                new("$addFields", new BsonDocument
                {
                    {"id", new BsonDocument("$toString", "$_id")},
                }),
                new("$project", new BsonDocument
                {
                    {"_id", 0},
                    {"password", 0},
                    {"role", 0},
                    {"blocked", 0},
                    {"codeAccess", 0},
                    {"validatedAccess", 0}
                }),
                new("$sort", pagination.PipelineSort),
            };

            List<BsonDocument> results = await context.Users.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).Count();
        }
        #endregion
        #region UPDATE
        public async Task<ResponseApi<User?>> UpdateAsync(User user)
        {
            try
            {
                await context.Users.ReplaceOneAsync(x => x.Id == user.Id, user);
                user.Password = "";
                return new(user, 201, "Usuário atualizado com sucesso");
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        #endregion
        #region DELETE
        public async Task<ResponseApi<User>> DeleteAsync(DeleteDTO request)
        {
            try
            {
                User? user = await context.Users.Find(x => x.Id == request.Id && !x.Deleted).FirstOrDefaultAsync();
                if (user is null) return new(null, 404, "Usuário não encontrado");

                user.Deleted = true;
                user.DeletedAt = DateTime.UtcNow;
                user.DeletedBy = request.DeletedBy;

                await context.Users.ReplaceOneAsync(x => x.Id == user.Id, user);

                return new(user, 204, "Usuário excluído com sucesso");
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        #endregion
    }
}