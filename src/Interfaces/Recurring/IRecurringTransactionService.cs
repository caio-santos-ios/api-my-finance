using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests.Recurring;
using api_finances.src.Shared.DTOs;

namespace api_finances.src.Interfaces;

public interface IRecurringTransactionService
{
    Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllRequest request);
    Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id);
    Task<ResponseApi<RecurringTransaction?>> CreateAsync(CreateRecurringRequest request);
    Task<ResponseApi<RecurringTransaction?>> UpdateAsync(UpdateRecurringRequest request);
    Task<ResponseApi<RecurringTransaction>> DeleteAsync(DeleteRequest request);
    Task<ResponseApi<bool>> PostMonthAsync(string id, PostRecurringMonthRequest request);
    Task<ResponseApi<dynamic>> GetCostOfLivingSummaryAsync(string userId);
}
