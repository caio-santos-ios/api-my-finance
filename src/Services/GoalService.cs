using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;
using MongoDB.Bson;

namespace api_finances.src.Services
{
    public class GoalService(
        IGoalRepository repository
    ) : IGoalService
    {
        public async Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllRequest request)
        {
            try
            {
                var queryParams = new Dictionary<string, string>(request.QueryParams);
                PaginationUtil<Goal> pagination = new(queryParams);

                BsonDocument[] pipeline = [
                    new("$match", pagination.PipelineFilter),
                    new("$sort", pagination.PipelineSort),
                    new("$skip", pagination.Skip),
                    new("$limit", pagination.Limit),
                    new("$project", new BsonDocument
                    {
                        { "_id", 0 },
                        { "id", new BsonDocument("$toString", "$_id") },
                        { "name", 1 },
                        { "targetAmount", "$target_amount" },
                        { "currentAmount", "$current_amount" },
                        { "targetDate", "$target_date" },
                        { "icon", 1 },
                        { "color", 1 },
                        { "status", 1 },
                        { "createdAt", 1 },
                        { "updatedAt", 1 }
                    })
                ];

                List<dynamic> entities = await repository.GetAllAsync(pipeline);
                int count = await repository.GetCountDocumentsAsync(pipeline);
                PaginationApi<List<dynamic>> data = new(entities, count, pagination.PageNumber, pagination.PageSize);
                return new(data, 200, "Metas listadas com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro ao listar as metas: {ex.Message}");
            }
        }

        public async Task<ResponseApi<dynamic>> GetSummaryAsync(string userId)
        {
            try
            {
                List<Goal> goals = await repository.GetRawAllAsync(userId);
                decimal totalTarget = goals.Sum(g => g.TargetAmount);
                decimal totalCurrent = goals.Sum(g => g.CurrentAmount);
                int completedCount = goals.Count(g => g.Status == "completed");
                int inProgressCount = goals.Count(g => g.Status == "in_progress");
                decimal overallProgress = totalTarget > 0 ? Math.Round((totalCurrent / totalTarget) * 100m, 2) : 0;

                var summary = new
                {
                    totalTarget,
                    totalCurrent,
                    completedCount,
                    inProgressCount,
                    totalGoals = goals.Count,
                    overallProgress
                };

                return new(summary, 200, "Resumo de metas obtido com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao calcular resumo de metas: {ex.Message}");
            }
        }

        public async Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id)
        {
            try
            {
                if (!ObjectId.TryParse(id, out var objectId))
                {
                    return new(null, 400, "ID inválido");
                }

                BsonDocument[] pipeline = [
                    new("$match", new BsonDocument { { "_id", objectId }, { "deleted", false } }),
                    new("$project", new BsonDocument
                    {
                        { "_id", 0 },
                        { "id", new BsonDocument("$toString", "$_id") },
                        { "name", 1 },
                        { "targetAmount", "$target_amount" },
                        { "currentAmount", "$current_amount" },
                        { "targetDate", "$target_date" },
                        { "icon", 1 },
                        { "color", 1 },
                        { "status", 1 },
                        { "createdAt", 1 },
                        { "updatedAt", 1 }
                    })
                ];

                dynamic? entity = await repository.GetByIdAggregateAsync(pipeline);
                if (entity == null) return new(null, 404, "Meta não encontrada");

                return new(entity, 200, "Meta recuperada com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao buscar meta: {ex.Message}");
            }
        }

        public async Task<ResponseApi<Goal?>> GetByIdAsync(string id)
        {
            try
            {
                Goal? goal = await repository.GetByIdAsync(id);
                if (goal == null) return new(null, 404, "Meta não encontrada");
                return new(goal, 200, "Meta recuperada com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao buscar meta: {ex.Message}");
            }
        }

        public async Task<ResponseApi<Goal?>> CreateAsync(CreateGoalRequest request)
        {
            try
            {
                decimal initial = request.InitialAmount ?? 0;
                string status = (request.TargetAmount > 0 && initial >= request.TargetAmount) ? "completed" : "in_progress";

                Goal entity = new()
                {
                    Name = request.Name,
                    TargetAmount = request.TargetAmount,
                    CurrentAmount = initial,
                    TargetDate = request.TargetDate,
                    Icon = request.Icon ?? "bullseye",
                    Color = request.Color ?? "#ec4899",
                    Status = status,
                    CreatedBy = request.CreatedBy ?? "",
                    CreatedAt = DateTime.UtcNow
                };

                Goal? created = await repository.CreateAsync(entity);

                if (created != null && initial > 0)
                {
                    GoalTransaction tx = new()
                    {
                        GoalId = created.Id,
                        Type = "deposit",
                        Value = initial,
                        Date = DateTime.UtcNow,
                        Note = "Valor inicial",
                        CreatedBy = request.CreatedBy ?? "",
                        CreatedAt = DateTime.UtcNow
                    };
                    await repository.CreateTransactionAsync(tx);
                }

                return new(created, 201, "Meta criada com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao criar meta: {ex.Message}");
            }
        }

        public async Task<ResponseApi<Goal?>> UpdateAsync(UpdateGoalRequest request)
        {
            try
            {
                Goal? existing = await repository.GetByIdAsync(request.Id);
                if (existing == null) return new(null, 404, "Meta não encontrada");

                existing.Name = request.Name;
                existing.TargetAmount = request.TargetAmount;
                existing.TargetDate = request.TargetDate;
                if (!string.IsNullOrEmpty(request.Icon)) existing.Icon = request.Icon;
                if (!string.IsNullOrEmpty(request.Color)) existing.Color = request.Color;

                if (existing.TargetAmount > 0 && existing.CurrentAmount >= existing.TargetAmount)
                {
                    existing.Status = "completed";
                }
                else if (!string.IsNullOrEmpty(request.Status))
                {
                    existing.Status = request.Status;
                }
                else
                {
                    existing.Status = "in_progress";
                }

                existing.UpdatedBy = request.UpdatedBy ?? "";
                existing.UpdatedAt = DateTime.UtcNow;

                Goal? updated = await repository.UpdateAsync(existing);
                return new(updated, 200, "Meta atualizada com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao atualizar meta: {ex.Message}");
            }
        }

        public async Task<ResponseApi<Goal>> DeleteAsync(DeleteRequest request)
        {
            try
            {
                Goal? existing = await repository.GetByIdAsync(request.Id);
                if (existing == null) return new(null, 404, "Meta não encontrada");

                existing.Deleted = true;
                existing.DeletedBy = request.DeletedBy;
                existing.DeletedAt = DateTime.UtcNow;

                Goal deleted = await repository.DeleteAsync(existing);
                return new(deleted, 200, "Meta removida com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao excluir meta: {ex.Message}");
            }
        }

        public async Task<ResponseApi<List<dynamic>>> GetTransactionsAsync(string goalId)
        {
            try
            {
                List<GoalTransaction> transactions = await repository.GetTransactionsAsync(goalId);
                List<dynamic> result = transactions.Select(t => new
                {
                    id = t.Id,
                    goalId = t.GoalId,
                    type = t.Type,
                    value = t.Value,
                    bankId = t.BankId,
                    date = t.Date,
                    note = t.Note,
                    createdAt = t.CreatedAt
                }).Cast<dynamic>().ToList();

                return new(result, 200, "Transações de meta listadas com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao listar transações da meta: {ex.Message}");
            }
        }

        public async Task<ResponseApi<GoalTransaction?>> AddTransactionAsync(string goalId, CreateGoalTransactionRequest request)
        {
            try
            {
                Goal? goal = await repository.GetByIdAsync(goalId);
                if (goal == null) return new(null, 404, "Meta não encontrada");

                decimal value = Math.Abs(request.Value);
                if (request.Type == "withdraw")
                {
                    goal.CurrentAmount -= value;
                    if (goal.CurrentAmount < 0) goal.CurrentAmount = 0;
                }
                else
                {
                    goal.CurrentAmount += value;
                }

                if (goal.TargetAmount > 0 && goal.CurrentAmount >= goal.TargetAmount)
                {
                    goal.Status = "completed";
                }
                else
                {
                    goal.Status = "in_progress";
                }

                goal.UpdatedAt = DateTime.UtcNow;
                goal.UpdatedBy = request.CreatedBy ?? "";
                await repository.UpdateAsync(goal);

                GoalTransaction transaction = new()
                {
                    GoalId = goalId,
                    Type = request.Type ?? "deposit",
                    Value = value,
                    BankId = request.BankId,
                    Date = request.Date ?? DateTime.UtcNow,
                    Note = request.Note,
                    CreatedBy = request.CreatedBy ?? "",
                    CreatedAt = DateTime.UtcNow
                };

                GoalTransaction created = await repository.CreateTransactionAsync(transaction);
                return new(created, 201, "Aporte registrado com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao registrar aporte: {ex.Message}");
            }
        }

        public async Task<ResponseApi<bool>> DeleteTransactionAsync(string goalId, string transactionId, string userId)
        {
            try
            {
                Goal? goal = await repository.GetByIdAsync(goalId);
                if (goal == null) return new(false, 404, "Meta não encontrada");

                GoalTransaction? transaction = await repository.GetTransactionByIdAsync(transactionId);
                if (transaction == null || transaction.GoalId != goalId) return new(false, 404, "Transação não encontrada");

                if (transaction.Type == "deposit")
                {
                    goal.CurrentAmount -= transaction.Value;
                    if (goal.CurrentAmount < 0) goal.CurrentAmount = 0;
                }
                else if (transaction.Type == "withdraw")
                {
                    goal.CurrentAmount += transaction.Value;
                }

                if (goal.TargetAmount > 0 && goal.CurrentAmount >= goal.TargetAmount)
                {
                    goal.Status = "completed";
                }
                else
                {
                    goal.Status = "in_progress";
                }

                goal.UpdatedAt = DateTime.UtcNow;
                goal.UpdatedBy = userId;
                await repository.UpdateAsync(goal);

                transaction.Deleted = true;
                transaction.DeletedBy = userId;
                transaction.DeletedAt = DateTime.UtcNow;
                await repository.DeleteTransactionAsync(transaction);

                return new(true, 200, "Aporte removido com sucesso");
            }
            catch (Exception ex)
            {
                return new(false, 500, $"Erro ao remover aporte: {ex.Message}");
            }
        }
    }
}
