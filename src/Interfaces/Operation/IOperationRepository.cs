using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;

namespace api_finances.src.Interfaces
{
    public interface IOperationRepository
    {
        Task<ResponseApi<List<dynamic>>> GetAllAsync(PaginationUtil<Operation> pagination);
        Task<ResponseApi<List<dynamic>>> GetSelectAsync(PaginationUtil<Operation> pagination);
        Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id);
        Task<ResponseApi<Operation?>> GetByIdAsync(string id);
        Task<long> GetNextCode(string userId);
        Task<int> GetCountDocumentsAsync(PaginationUtil<Operation> pagination);
        Task<ResponseApi<Operation?>> CreateAsync(Operation entity);
        Task<ResponseApi<Operation?>> UpdateAsync(Operation request);
        Task<ResponseApi<Operation>> DeleteAsync(DeleteDTO request);
    }
}