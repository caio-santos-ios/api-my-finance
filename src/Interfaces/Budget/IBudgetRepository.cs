using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;

namespace api_finances.src.Interfaces
{
    public interface IBudgetRepository
    {
        Task<ResponseApi<List<dynamic>>> GetAllAsync(PaginationUtil<Budget> pagination);
        Task<ResponseApi<List<dynamic>>> GetSelectAsync(PaginationUtil<Budget> pagination);
        Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id);
        Task<ResponseApi<Budget?>> GetByIdAsync(string id);
        Task<int> GetCountDocumentsAsync(PaginationUtil<Budget> pagination);
        Task<ResponseApi<Budget?>> CreateAsync(Budget budget);
        Task<ResponseApi<Budget?>> UpdateAsync(Budget budget);
        Task<ResponseApi<Budget>> DeleteAsync(DeleteRequest request);
    }
}
