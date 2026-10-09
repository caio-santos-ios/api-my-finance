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
    public class BankRepository(AppDbContext context) : IBankRepository
    {
        #region CREATE
        public async Task<ResponseApi<Bank?>> CreateAsync(Bank bank)
        {
            try
            {
                await context.Banks.InsertOneAsync(bank);
                return new(bank, 201, "Banco criado com sucesso");
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        #endregion
        #region READ
        public async Task<ResponseApi<List<dynamic>>> GetAllAsync(PaginationUtil<Bank> pagination)
        {
            try
            {
                List<BsonDocument> pipeline = new()
                {
                    new("$match", pagination.PipelineFilter),
                    new("$sort", pagination.PipelineSort),
                    new("$skip", pagination.Skip),
                    new("$limit", pagination.Limit),

                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"id", new BsonDocument("$toString", "$_id")},
                        {"name", 1},
                        {"code", 1},
                        {"active", 1},
                        {"createdAt", 1}
                    }),
                    new("$sort", pagination.PipelineSort),
                };

                List<BsonDocument> results = await context.Banks.Aggregate<BsonDocument>(pipeline).ToListAsync();
                List<dynamic> list = results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).ToList();
                return new(list);
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        public async Task<ResponseApi<List<dynamic>>> GetSelectAsync(PaginationUtil<Bank> pagination)
        {
            try
            {
                List<BsonDocument> pipeline = new()
                {
                    new("$match", pagination.PipelineFilter),
                    new("$sort", pagination.PipelineSort),

                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"id", new BsonDocument("$toString", "$_id")},
                        {"name", 1},
                        {"code", 1},
                        {"active", 1}
                    }),
                    new("$sort", pagination.PipelineSort),
                };

                List<BsonDocument> results = await context.Banks.Aggregate<BsonDocument>(pipeline).ToListAsync();
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

                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"id", new BsonDocument("$toString", "$_id")},
                        {"name", 1},
                        {"code", 1}
                    }),
                ];

                BsonDocument? response = await context.Banks.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
                dynamic? result = response is null ? null : BsonSerializer.Deserialize<dynamic>(response);
                return result is null ? new(null, 404, "Banco não encontrado") : new(result);
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        public async Task<ResponseApi<Bank?>> GetByIdAsync(string id)
        {
            try
            {
                Bank? bank = await context.Banks.Find(x => x.Id == id && !x.Deleted).FirstOrDefaultAsync();
                return new(bank);
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        public async Task<Bank?> GetByCodeAsync(string code)
        {
            Bank? bank = await context.Banks.Find(x => x.Code == code && !x.Deleted).FirstOrDefaultAsync();
            return bank;
        }
        public async Task<Bank?> GetFirstAsync()
        {
            Bank? bank = await context.Banks.Find(x => !x.Deleted).FirstOrDefaultAsync();
            return bank;
        }
        public async Task<long> GetNextCode(string userId)
        {
            return await context.Banks.Find(x => x.CreatedBy == userId).CountDocumentsAsync() + 1;
        }
        public async Task<int> GetCountDocumentsAsync(PaginationUtil<Bank> pagination)
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

            List<BsonDocument> results = await context.Banks.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).Count();
        }
        #endregion
        #region UPDATE
        public async Task<ResponseApi<Bank?>> UpdateAsync(Bank bank)
        {
            try
            {
                await context.Banks.ReplaceOneAsync(x => x.Id == bank.Id, bank);
                return new(bank, 200, "Banco atualizado com sucesso");
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        #endregion
        #region DELETE
        public async Task<ResponseApi<Bank>> DeleteAsync(DeleteRequest request)
        {
            try
            {
                Bank? bank = await context.Banks.Find(x => x.Id == request.Id && !x.Deleted).FirstOrDefaultAsync();
                if (bank is null) return new(null, 404, "Banco não encontrado");

                bank.Deleted = true;
                bank.DeletedAt = DateTime.UtcNow;
                bank.DeletedBy = request.DeletedBy;

                await context.Banks.ReplaceOneAsync(x => x.Id == bank.Id, bank);

                return new(bank, 204, "Banco excluído com sucesso");
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        #endregion
    }
}