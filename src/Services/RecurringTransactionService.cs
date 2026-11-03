using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests.Recurring;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;
using api_finances.src.Utils;
using MongoDB.Bson;

namespace api_finances.src.Services;

public class RecurringTransactionService(
    IRecurringTransactionRepository repository,
    IOperationRepository operationRepository
) : IRecurringTransactionService
{
    public async Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllRequest request)
    {
        try
        {
            PaginationUtil<RecurringTransaction> pagination = new(request.QueryParams);
            BsonDocument[] pipeline = [
                new("$match", pagination.PipelineFilter),
                new("$sort", pagination.PipelineSort),
                new("$skip", pagination.Skip),
                new("$limit", pagination.Limit),

                new("$addFields", new BsonDocument
                {
                    { "bankObjId", new BsonDocument("$cond", new BsonDocument
                        {
                            { "if", new BsonDocument("$in", new BsonArray { "$bank_id", new BsonArray { BsonNull.Value, "" } }) },
                            { "then", BsonNull.Value },
                            { "else", new BsonDocument("$toObjectId", "$bank_id") }
                        })
                    },
                    { "catObjId", new BsonDocument("$cond", new BsonDocument
                        {
                            { "if", new BsonDocument("$in", new BsonArray { "$category_id", new BsonArray { BsonNull.Value, "" } }) },
                            { "then", BsonNull.Value },
                            { "else", new BsonDocument("$toObjectId", "$category_id") }
                        })
                    }
                }),

                MongoUtil.Lookup("banks", ["$bankObjId"], ["$_id"], "_banks", [["deleted", false]], 1),
                MongoUtil.Lookup("categories", ["$catObjId"], ["$_id"], "_categories", [["deleted", false]], 1),

                new("$project", new BsonDocument
                {
                    {"_id", 0},
                    {"id", new BsonDocument("$toString", "$_id")},
                    {"description", 1},
                    {"type", 1},
                    {"value", 1},
                    {"categoryId", "$category_id"},
                    {"categoryName", MongoUtil.First("_categories.name")},
                    {"bankId", "$bank_id"},
                    {"bankName", MongoUtil.First("_banks.name")},
                    {"frequency", 1},
                    {"dueDay", "$due_day"},
                    {"autoPost", "$auto_post"},
                    {"lastPostedDate", "$last_posted_date"},
                    {"active", 1},
                    {"createdAt", 1}
                })
            ];

            List<dynamic> response = await repository.GetAllAsync(pipeline);
            int totalCount = await repository.GetCountDocumentsAsync(pipeline);

            return new(new PaginationApi<List<dynamic>>(response, totalCount, pagination.PageNumber, pagination.PageSize), 200);
        }
        catch (Exception ex)
        {
            return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
        }
    }

    public async Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id)
    {
        try
        {
            BsonDocument[] pipeline = [
                new("$match", new BsonDocument
                {
                    { "_id", new ObjectId(id) },
                    { "deleted", false }
                }),
                new("$addFields", new BsonDocument
                {
                    { "bankObjId", new BsonDocument("$cond", new BsonDocument
                        {
                            { "if", new BsonDocument("$in", new BsonArray { "$bank_id", new BsonArray { BsonNull.Value, "" } }) },
                            { "then", BsonNull.Value },
                            { "else", new BsonDocument("$toObjectId", "$bank_id") }
                        })
                    },
                    { "catObjId", new BsonDocument("$cond", new BsonDocument
                        {
                            { "if", new BsonDocument("$in", new BsonArray { "$category_id", new BsonArray { BsonNull.Value, "" } }) },
                            { "then", BsonNull.Value },
                            { "else", new BsonDocument("$toObjectId", "$category_id") }
                        })
                    }
                }),
                MongoUtil.Lookup("banks", ["$bankObjId"], ["$_id"], "_banks", [["deleted", false]], 1),
                MongoUtil.Lookup("categories", ["$catObjId"], ["$_id"], "_categories", [["deleted", false]], 1),
                new("$project", new BsonDocument
                {
                    {"_id", 0},
                    {"id", new BsonDocument("$toString", "$_id")},
                    {"description", 1},
                    {"type", 1},
                    {"value", 1},
                    {"categoryId", "$category_id"},
                    {"categoryName", MongoUtil.First("_categories.name")},
                    {"bankId", "$bank_id"},
                    {"bankName", MongoUtil.First("_banks.name")},
                    {"frequency", 1},
                    {"dueDay", "$due_day"},
                    {"autoPost", "$auto_post"},
                    {"lastPostedDate", "$last_posted_date"},
                    {"active", 1},
                    {"createdAt", 1}
                })
            ];

            dynamic? response = await repository.GetByIdAggregateAsync(pipeline);
            if (response is null) return new(null, 404, "Recorrência não encontrada.");

            return new(response, 200);
        }
        catch (Exception ex)
        {
            return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
        }
    }

    public async Task<ResponseApi<RecurringTransaction?>> CreateAsync(CreateRecurringRequest request)
    {
        try
        {
            RecurringTransaction entity = new()
            {
                Description = request.Description,
                Type = request.Type,
                Value = request.Value,
                CategoryId = request.CategoryId,
                BankId = request.BankId,
                Frequency = request.Frequency,
                DueDay = request.DueDay,
                AutoPost = request.AutoPost
            };

            RecurringTransaction? response = await repository.CreateAsync(entity);
            if (response is null) return new(null, 400, "Falha ao criar recorrência.");

            return new(response, 201, "Recorrência criada com sucesso.");
        }
        catch (Exception ex)
        {
            return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
        }
    }

    public async Task<ResponseApi<RecurringTransaction?>> UpdateAsync(UpdateRecurringRequest request)
    {
        try
        {
            RecurringTransaction? existed = await repository.GetByIdAsync(request.Id);
            if (existed is null) return new(null, 404, "Falha ao atualizar. Recorrência não encontrada.");

            existed.Description = request.Description;
            existed.Type = request.Type;
            existed.Value = request.Value;
            existed.CategoryId = request.CategoryId;
            existed.BankId = request.BankId;
            existed.Frequency = request.Frequency;
            existed.DueDay = request.DueDay;
            existed.AutoPost = request.AutoPost;
            existed.UpdatedAt = DateTime.UtcNow;

            RecurringTransaction? response = await repository.UpdateAsync(existed);
            if (response is null) return new(null, 400, "Falha ao atualizar recorrência.");

            return new(response, 200, "Recorrência atualizada com sucesso.");
        }
        catch (Exception ex)
        {
            return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
        }
    }

    public async Task<ResponseApi<RecurringTransaction>> DeleteAsync(DeleteRequest request)
    {
        try
        {
            RecurringTransaction? existed = await repository.GetByIdAsync(request.Id);
            if (existed is null) return new(null, 404, "Falha ao excluir. Recorrência não encontrada.");

            existed.Deleted = true;
            existed.DeletedAt = DateTime.UtcNow;
            existed.DeletedBy = request.DeletedBy;

            RecurringTransaction response = await repository.DeleteAsync(existed);
            return new(response, 200, "Recorrência excluída com sucesso.");
        }
        catch (Exception ex)
        {
            return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
        }
    }

    public async Task<ResponseApi<bool>> PostMonthAsync(string id, PostRecurringMonthRequest request)
    {
        try
        {
            RecurringTransaction? recurring = await repository.GetByIdAsync(id);
            if (recurring is null) return new(false, 404, "Recorrência não encontrada.");

            string? targetBankId = !string.IsNullOrEmpty(request.BankId) ? request.BankId : recurring.BankId;
            DateTime targetDate = request.Date ?? DateTime.UtcNow;

            Operation op = new()
            {
                Description = recurring.Description,
                Value = recurring.Value,
                Type = recurring.Type,
                BankId = targetBankId ?? string.Empty,
                CategoryId = recurring.CategoryId ?? string.Empty,
                Origin = "recurring",
                OriginId = recurring.Id,
                CreatedAt = targetDate,
                CreatedBy = recurring.CreatedBy
            };

            await operationRepository.CreateAsync(op);

            recurring.LastPostedDate = DateTime.UtcNow;
            recurring.UpdatedAt = DateTime.UtcNow;
            await repository.UpdateAsync(recurring);

            return new(true, 200, "Lançamento da recorrência realizado com sucesso.");
        }
        catch (Exception ex)
        {
            return new(false, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
        }
    }

    public async Task<ResponseApi<dynamic>> GetCostOfLivingSummaryAsync(string userId)
    {
        try
        {
            List<RecurringTransaction> all = await repository.GetActiveByUserAsync(userId);

            decimal totalIncome = all.Where(x => x.Type == "income").Sum(x => x.Value);
            decimal totalExpense = all.Where(x => x.Type == "expense").Sum(x => x.Value);
            decimal netFixedBalance = totalIncome - totalExpense;

            var summary = new
            {
                totalIncomeRecurrings = totalIncome,
                totalExpenseRecurrings = totalExpense,
                netFixedBalance = netFixedBalance,
                totalActiveRecurrings = all.Count,
                incomeCount = all.Count(x => x.Type == "income"),
                expenseCount = all.Count(x => x.Type == "expense")
            };

            return new(summary, 200);
        }
        catch (Exception ex)
        {
            return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
        }
    }
}
