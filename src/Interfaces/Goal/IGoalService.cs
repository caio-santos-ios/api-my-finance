using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;

namespace api_finances.src.Interfaces
{
    public interface IGoalService
    {
        Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllRequest request);
        Task<ResponseApi<dynamic>> GetSummaryAsync(string userId);
        Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id);
        Task<ResponseApi<Goal?>> GetByIdAsync(string id);
        Task<ResponseApi<Goal?>> CreateAsync(CreateGoalRequest request);
        Task<ResponseApi<Goal?>> UpdateAsync(UpdateGoalRequest request);
        Task<ResponseApi<Goal>> DeleteAsync(DeleteRequest request);
        Task<ResponseApi<List<dynamic>>> GetTransactionsAsync(string goalId);
        Task<ResponseApi<GoalTransaction?>> AddTransactionAsync(string goalId, CreateGoalTransactionRequest request);
        Task<ResponseApi<bool>> DeleteTransactionAsync(string goalId, string transactionId, string userId);
    }
}
