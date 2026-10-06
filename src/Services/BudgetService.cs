using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;

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
                ResponseApi<List<dynamic>> budgets = await repository.GetAllAsync(pagination);
                int count = await repository.GetCountDocumentsAsync(pagination);
                PaginationApi<List<dynamic>> data = new(budgets.Data, count, pagination.PageNumber, pagination.PageSize);
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
                ResponseApi<List<dynamic>> budgets = await repository.GetSelectAsync(pagination);
                return new(budgets.Data, 200, "Orçamentos listados com sucesso");
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
                ResponseApi<dynamic?> budget = await repository.GetByIdAggregateAsync(id);
                if (budget.Data is null) return new(null, 404, "Orçamento não encontrado");
                return new(budget.Data, 200, "Orçamento encontrado");
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
                string budgetName = request.Name;
                if (string.IsNullOrWhiteSpace(budgetName))
                {
                    ResponseApi<Category?> category = await categoryRepository.GetByIdAsync(request.CategoryId);
                    budgetName = category?.Data?.Name ?? "Orçamento";
                }

                Budget budget = new()
                {
                    Name = budgetName,
                    CategoryId = request.CategoryId,
                    Limit = request.Limit,
                    ReceiveAlert = request.ReceiveAlert,
                    AlertPercentage = request.AlertPercentage,
                    CreatedBy = request.CreatedBy
                };

                ResponseApi<Budget?> response = await repository.CreateAsync(budget);
                if (response.Data is null) return new(null, 400, "Falha ao criar orçamento.");

                return new(response.Data, 201, "Orçamento criado com sucesso.");
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        #endregion

        #region UPDATE
        public async Task<ResponseApi<Budget?>> UpdateAsync(UpdateBudgetRequest request)
        {
            try
            {
                ResponseApi<Budget?> budget = await repository.GetByIdAsync(request.Id);
                if (budget.Data is null) return new(null, 404, "Falha ao atualizar. Orçamento não encontrado");

                string budgetName = request.Name;
                if (string.IsNullOrWhiteSpace(budgetName))
                {
                    ResponseApi<Category?> category = await categoryRepository.GetByIdAsync(request.CategoryId);
                    budgetName = category?.Data?.Name ?? budget.Data.Name;
                }

                budget.Data.UpdatedAt = DateTime.UtcNow;
                budget.Data.UpdatedBy = request.UpdatedBy;
                budget.Data.Name = budgetName;
                budget.Data.CategoryId = request.CategoryId;
                budget.Data.Limit = request.Limit;
                budget.Data.ReceiveAlert = request.ReceiveAlert;
                budget.Data.AlertPercentage = request.AlertPercentage;

                ResponseApi<Budget?> response = await repository.UpdateAsync(budget.Data);
                if (!response.IsSuccess) return new(null, 400, "Falha ao atualizar");

                return new(response.Data, 200, "Atualizado com sucesso");
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
                ResponseApi<Budget> budget = await repository.DeleteAsync(request);
                if (!budget.IsSuccess) return new(null, 400, budget.Message);
                return new(budget.Data, 204, "Orçamento excluído com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion
    }
}
