using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;
using api_finances.src.Utils;
using MongoDB.Bson;

namespace api_finances.src.Services
{
    public class BankService(
        IBankRepository repository
    ) : IBankService
    {
        #region READ
        public async Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllRequest request)
        {
            try
            {
                PaginationUtil<Bank> pagination = new(request.QueryParams);
                BsonDocument[] pipeline = [
                    new("$match", pagination.PipelineFilter),
                    new("$sort", pagination.PipelineSort),
                    new("$skip", pagination.Skip),
                    new("$limit", pagination.Limit),

                    new("$addFields", new BsonDocument
                    {
                        {"id", new BsonDocument("$toString", "$_id")},
                    }),

                    MongoUtil.Lookup("operations", ["$id"], ["$bankId"], "_operations", [["deleted", false]]),

                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"id", 1},
                        {"name", 1},
                        {"code", 1},
                        {"active", 1},
                        {"createdAt", 1},
                        {"balance", new BsonDocument("$round", new BsonArray
                            {
                                new BsonDocument("$sum", new BsonDocument("$map", new BsonDocument
                                {
                                    {"input", "$_operations"},
                                    {"as", "op"},
                                    {"in", new BsonDocument("$cond", new BsonArray
                                    {
                                        new BsonDocument("$eq", new BsonArray { "$$op.type", "income" }),
                                        new BsonDocument("$convert", new BsonDocument
                                        {
                                            {"input", "$$op.value"},
                                            {"to", "double"},
                                            {"onError", 0},
                                            {"onNull", 0}
                                        }),
                                        new BsonDocument("$multiply", new BsonArray
                                        {
                                            -1,
                                            new BsonDocument("$convert", new BsonDocument
                                            {
                                                {"input", "$$op.value"},
                                                {"to", "double"},
                                                {"onError", 0},
                                                {"onNull", 0}
                                            })
                                        })
                                    })}
                                })),
                                2
                            })},
                    }),
                    new("$sort", pagination.PipelineSort),
                ];

                List<dynamic> entities = await repository.GetAllAsync(pipeline);
                int count = await repository.GetCountDocumentsAsync(pipeline);
                PaginationApi<List<dynamic>> data = new(entities, count, pagination.PageNumber, pagination.PageSize);
                return new(data, 200, "Bancos listados com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        public async Task<ResponseApi<List<dynamic>>> GetSelectAsync(GetAllRequest request)
        {
            try
            {
                PaginationUtil<Bank> pagination = new(request.QueryParams);
                BsonDocument[] pipeline = [
                    new("$match", pagination.PipelineFilter),
                    new("$sort", pagination.PipelineSort),

                    new("$addFields", new BsonDocument
                    {
                        {"id", new BsonDocument("$toString", "$_id")},
                    }),

                    MongoUtil.Lookup("operations", ["$id"], ["$bankId"], "_operations", [["deleted", false]]),

                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"id", 1},
                        {"name", 1},
                        {"code", 1},
                        {"active", 1},
                        {"createdAt", 1},
                        {"balance", new BsonDocument("$round", new BsonArray
                            {
                                new BsonDocument("$sum", new BsonDocument("$map", new BsonDocument
                                {
                                    {"input", "$_operations"},
                                    {"as", "op"},
                                    {"in", new BsonDocument("$cond", new BsonArray
                                    {
                                        new BsonDocument("$eq", new BsonArray { "$$op.type", "income" }),
                                        new BsonDocument("$convert", new BsonDocument
                                        {
                                            {"input", "$$op.value"},
                                            {"to", "double"},
                                            {"onError", 0},
                                            {"onNull", 0}
                                        }),
                                        new BsonDocument("$multiply", new BsonArray
                                        {
                                            -1,
                                            new BsonDocument("$convert", new BsonDocument
                                            {
                                                {"input", "$$op.value"},
                                                {"to", "double"},
                                                {"onError", 0},
                                                {"onNull", 0}
                                            })
                                        })
                                    })}
                                })),
                                2
                            })},
                    }),
                    new("$sort", pagination.PipelineSort),
                ];
                List<dynamic> entities = await repository.GetSelectAsync(pipeline);
                return new(entities, 200, "Bancos listados com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
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
                dynamic? entity = await repository.GetByIdAggregateAsync(pipeline);
                if (entity is null) return new(null, 404, "Banco não encontrado");
                return new(entity, 200, "Banco encontrado");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion

        #region CREATE
        public async Task<ResponseApi<Bank?>> CreateAsync(CreateBankRequest request)
        {
            try
            {
                Bank banks = ObjectMapper.Map<CreateBankRequest, Bank>(request);

                Bank? response = await repository.CreateAsync(banks);
                if (response is null) return new(null, 400, "Falha ao criar conta.");

                return new(response, 201, "Banco criado com sucesso.");
            }
            catch(Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
            }
        }

        #endregion

        #region UPDATE
        public async Task<ResponseApi<Bank?>> UpdateAsync(UpdateBankRequest request)
        {
            try
            {
                Bank? existed = await repository.GetByIdAsync(request.Id);
                if (existed is null) return new(null, 404, "Falha ao atualizar");

                Bank banks = ObjectMapper.Map<UpdateBankRequest, Bank>(request);
                banks.UpdatedAt = DateTime.UtcNow;

                Bank? response = await repository.UpdateAsync(banks);
                if (response is null) return new(null, 400, "Falha ao atualizar");

                return new(response, 200, "Atualizado com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion

        #region DELETE
        public async Task<ResponseApi<Bank>> DeleteAsync(DeleteRequest request)
        {
            try
            {
                Bank? entity = await repository.GetByIdAsync(request.Id);
                if (entity is null) return new(null, 404, "Falha ao atualizar");
                
                entity.Deleted = true;
                entity.DeletedAt = DateTime.UtcNow;
                entity.DeletedBy = request.DeletedBy;

                await repository.DeleteAsync(entity);

                return new(entity, 204, "Banco excluído com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion        
    }
}