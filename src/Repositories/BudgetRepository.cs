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
    public class BudgetRepository(AppDbContext context) : IBudgetRepository
    {
        #region CREATE
        public async Task<ResponseApi<Budget?>> CreateAsync(Budget budget)
        {
            try
            {
                await context.Budgets.InsertOneAsync(budget);
                return new(budget, 201, "Orçamento criado com sucesso");
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        #endregion

        #region READ
        public async Task<ResponseApi<List<dynamic>>> GetAllAsync(PaginationUtil<Budget> pagination)
        {
            try
            {
                List<BsonDocument> pipeline = new()
                {
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

                    // new BsonDocument("$lookup", new BsonDocument
                    // {
                    //     { "from", "operations" },
                    //     { "let", new BsonDocument
                    //         {
                    //             { "catId", "$categoryId" },
                    //             { "user", "$createdBy" }
                    //         }
                    //     },
                    //     { "pipeline", new BsonArray
                    //         {
                    //             new BsonDocument("$match", new BsonDocument
                    //             {
                    //                 { "$expr", new BsonDocument
                    //                     {
                    //                         { "$and", new BsonArray
                    //                             {
                    //                                 new BsonDocument("$eq", new BsonArray { "$categoryId", "$$catId" }),
                    //                                 new BsonDocument("$eq", new BsonArray { "$type", "expense" }),
                    //                                 new BsonDocument("$eq", new BsonArray { "$deleted", false }),
                    //                                 new BsonDocument("$eq", new BsonArray { "$createdBy", "$$user" })
                    //                             }
                    //                         }
                    //                     }
                    //                 }
                    //             })
                    //         }
                    //     },
                    //     { "as", "_operations" }
                    // }),

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
                };

                List<BsonDocument> results = await context.Budgets.Aggregate<BsonDocument>(pipeline).ToListAsync();
                List<dynamic> list = results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).ToList();
                return new(list);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }

        public async Task<ResponseApi<List<dynamic>>> GetSelectAsync(PaginationUtil<Budget> pagination)
        {
            try
            {
                List<BsonDocument> pipeline = new()
                {
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
                };

                List<BsonDocument> results = await context.Budgets.Aggregate<BsonDocument>(pipeline).ToListAsync();
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

                BsonDocument? response = await context.Budgets.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
                dynamic? result = response is null ? null : BsonSerializer.Deserialize<dynamic>(response);
                return result is null ? new(null, 404, "Orçamento não encontrado") : new(result);
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }

        public async Task<ResponseApi<Budget?>> GetByIdAsync(string id)
        {
            try
            {
                Budget? budget = await context.Budgets.Find(x => x.Id == id && !x.Deleted).FirstOrDefaultAsync();
                return new(budget);
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }

        public async Task<int> GetCountDocumentsAsync(PaginationUtil<Budget> pagination)
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
                }),
                new("$sort", pagination.PipelineSort),
            };

            List<BsonDocument> results = await context.Budgets.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).Count();
        }
        #endregion

        #region UPDATE
        public async Task<ResponseApi<Budget?>> UpdateAsync(Budget budget)
        {
            try
            {
                await context.Budgets.ReplaceOneAsync(x => x.Id == budget.Id, budget);
                return new(budget, 200, "Orçamento atualizado com sucesso");
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        #endregion

        #region DELETE
        public async Task<ResponseApi<Budget>> DeleteAsync(DeleteDTO request)
        {
            try
            {
                Budget? budget = await context.Budgets.Find(x => x.Id == request.Id && !x.Deleted).FirstOrDefaultAsync();
                if (budget is null) return new(null, 404, "Orçamento não encontrado");

                budget.Deleted = true;
                budget.DeletedAt = DateTime.UtcNow;
                budget.DeletedBy = request.DeletedBy;

                await context.Budgets.ReplaceOneAsync(x => x.Id == budget.Id, budget);

                return new(budget, 204, "Orçamento excluído com sucesso");
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        #endregion
    }
}
