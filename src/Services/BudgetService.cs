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
    public class BudgetService(
        IBudgetRepository repository,
        ICategoryRepository categoryRepository
    ) : IBudgetService
    {
        #region READ
        public async Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllRequest request)
        {
            try
            {
                PaginationUtil<Budget> pagination = new(request.QueryParams);
                BsonDocument[] pipeline = [
                    new("$match", pagination.PipelineFilter),
                    new("$sort", pagination.PipelineSort),
                    new("$skip", pagination.Skip),
                    new("$limit", pagination.Limit),

                    MongoUtil.Lookup("operations", ["$categoryId"], ["$categoryId"], "_operations", [["deleted", false]]),

                    new("$addFields", new BsonDocument
                    {
                        { "categoryObjId", new BsonDocument("$cond", new BsonDocument
                            {
                                { "if", new BsonDocument("$in", new BsonArray { "$categoryId", new BsonArray { BsonNull.Value, "" } }) },
                                { "then", BsonNull.Value },
                                { "else", new BsonDocument("$toObjectId", "$categoryId") }
                            })
                        }
                    }),

                    MongoUtil.Lookup("categories", ["$categoryObjId"], ["$_id"], "_categories", [["deleted", false]], 1),

                    new("$addFields", new BsonDocument
                    {
                        {"spent", new BsonDocument("$sum", new BsonDocument("$map", new BsonDocument
                        {
                            {"input", "$_operations"},
                            {"as", "op"},
                            {"in", new BsonDocument("$convert", new BsonDocument
                            {
                                {"input", "$$op.value"},
                                {"to", "double"},
                                {"onError", 0.0},
                                {"onNull", 0.0}
                            })}
                        }))},
                    }),

                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"id", new BsonDocument("$toString", "$_id")},
                        {"name", 1},
                        {"categoryId", 1},
                        {"categoryName", MongoUtil.First("_categories.name")},
                        {"limit", 1},
                        {"receiveAlert", 1},
                        {"alertPercentage", 1},
                        {"remaining", new BsonDocument("$subtract", new BsonArray
                        {
                            new BsonDocument("$toDouble", "$limit"),
                            new BsonDocument("$toDouble", "$spent"),
                        })},
                        {"spent", 1},
                        {"active", 1},
                        {"createdAt", 1}
                    }),
                    new("$sort", pagination.PipelineSort),
                ];

                List<dynamic> entities = await repository.GetAllAsync(pipeline);
                int count = await repository.GetCountDocumentsAsync(pipeline);
                PaginationApi<List<dynamic>> data = new(entities, count, pagination.PageNumber, pagination.PageSize);
                return new(data, 200, "Orçamentos listados com sucesso");
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
                PaginationUtil<Budget> pagination = new(request.QueryParams);
                BsonDocument[] pipeline = [
                    new("$match", pagination.PipelineFilter),
                    new("$sort", pagination.PipelineSort),

                    new("$addFields", new BsonDocument
                    {
                        { "categoryObjId", new BsonDocument("$cond", new BsonDocument
                            {
                                { "if", new BsonDocument("$in", new BsonArray { "$categoryId", new BsonArray { BsonNull.Value, "" } }) },
                                { "then", BsonNull.Value },
                                { "else", new BsonDocument("$toObjectId", "$categoryId") }
                            })
                        }
                    }),

                    MongoUtil.Lookup("categories", ["$categoryObjId"], ["$_id"], "_categories", [["deleted", false]], 1),

                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"id", new BsonDocument("$toString", "$_id")},
                        {"name", 1},
                        {"categoryId", 1},
                        {"categoryName", MongoUtil.First("_categories.name")},
                        {"limit", 1},
                        {"receiveAlert", 1},
                        {"alertPercentage", 1},
                        {"active", 1}
                    }),
                    new("$sort", pagination.PipelineSort),
                ];

                List<dynamic> entities = await repository.GetSelectAsync(pipeline);
                return new(entities, 200, "Orçamentos listados com sucesso");
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

                    new("$addFields", new BsonDocument
                    {
                        { "categoryObjId", new BsonDocument("$cond", new BsonDocument
                            {
                                { "if", new BsonDocument("$in", new BsonArray { "$categoryId", new BsonArray { BsonNull.Value, "" } }) },
                                { "then", BsonNull.Value },
                                { "else", new BsonDocument("$toObjectId", "$categoryId") }
                            })
                        }
                    }),

                    MongoUtil.Lookup("categories", ["$categoryObjId"], ["$_id"], "_categories", [["deleted", false]], 1),

                    new BsonDocument("$lookup", new BsonDocument
                    {
                        { "from", "operations" },
                        { "let", new BsonDocument
                            {
                                { "catId", "$categoryId" },
                                { "user", "$createdBy" }
                            }
                        },
                        { "pipeline", new BsonArray
                            {
                                new BsonDocument("$match", new BsonDocument
                                {
                                    { "$expr", new BsonDocument
                                        {
                                            { "$and", new BsonArray
                                                {
                                                    new BsonDocument("$eq", new BsonArray { "$categoryId", "$$catId" }),
                                                    new BsonDocument("$eq", new BsonArray { "$type", "expense" }),
                                                    new BsonDocument("$eq", new BsonArray { "$deleted", false }),
                                                    new BsonDocument("$eq", new BsonArray { "$createdBy", "$$user" })
                                                }
                                            }
                                        }
                                    }
                                })
                            }
                        },
                        { "as", "_operations" }
                    }),

                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"id", new BsonDocument("$toString", "$_id")},
                        {"name", 1},
                        {"categoryId", 1},
                        {"categoryName", MongoUtil.First("_categories.name")},
                        {"limit", 1},
                        {"receiveAlert", 1},
                        {"alertPercentage", 1},
                        {"spent", new BsonDocument("$sum", "$_operations.value")},
                        {"remaining", new BsonDocument("$subtract", new BsonArray
                        {
                            "$limit",
                            new BsonDocument("$sum", "$_operations.value")
                        })},
                        {"active", 1},
                        {"createdAt", 1}
                    }),
                ];

                dynamic? entity = await repository.GetByIdAggregateAsync(pipeline);
                if (entity is null) return new(null, 404, "Orçamento não encontrado");
                return new(entity, 200, "Orçamento encontrado");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion

        #region CREATE
        public async Task<ResponseApi<Budget?>> CreateAsync(CreateBudgetRequest request)
        {
            try
            {
                Budget budget = ObjectMapper.Map<CreateBudgetRequest, Budget>(request);
                Budget? response = await repository.CreateAsync(budget);
                if (response is null) return new(null, 400, "Falha ao criar orçamento.");

                return new(response, 201, "Orçamento criado com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
            }
        }
        #endregion

        #region UPDATE
        public async Task<ResponseApi<Budget?>> UpdateAsync(UpdateBudgetRequest request)
        {
            try
            {
                Budget? existed = await repository.GetByIdAsync(request.Id);
                if (existed is null) return new(null, 404, "Falha ao atualizar. Orçamento não encontrado");

                string budgetName = request.Name;
                if (string.IsNullOrWhiteSpace(budgetName))
                {
                    Category? category = await categoryRepository.GetByIdAsync(request.CategoryId);
                    budgetName = category?.Name ?? existed.Name;
                }

                existed.UpdatedAt = DateTime.UtcNow;
                existed.UpdatedBy = request.UpdatedBy;
                existed.Name = budgetName;
                existed.CategoryId = request.CategoryId;
                existed.Limit = request.Limit;
                existed.ReceiveAlert = request.ReceiveAlert;
                existed.AlertPercentage = request.AlertPercentage;

                Budget? response = await repository.UpdateAsync(existed);
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
        public async Task<ResponseApi<Budget>> DeleteAsync(DeleteRequest request)
        {
            try
            {
                Budget? entity = await repository.GetByIdAsync(request.Id);
                if (entity is null) return new(null, 404, "Orçamento não encontrado");

                entity.Deleted = true;
                entity.DeletedAt = DateTime.UtcNow;
                entity.DeletedBy = request.DeletedBy;

                await repository.DeleteAsync(entity);

                return new(entity, 204, "Orçamento excluído com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion
    }
}
