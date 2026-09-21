using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;

namespace api_finances.src.Interfaces
{
    public interface IBankRepository
    {
        Task<ResponseApi<List<dynamic>>> GetAllAsync(PaginationUtil<Bank> pagination);
        Task<ResponseApi<List<dynamic>>> GetSelectAsync(PaginationUtil<Bank> pagination);
        Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id);
        Task<ResponseApi<Bank?>> GetByIdAsync(string id);
        Task<long> GetNextCode(string userId);
        Task<int> GetCountDocumentsAsync(PaginationUtil<Bank> pagination);
        Task<ResponseApi<Bank?>> CreateAsync(Bank entity);
        Task<ResponseApi<Bank?>> UpdateAsync(Bank request);
        Task<ResponseApi<Bank>> DeleteAsync(DeleteDTO request);
    }
}