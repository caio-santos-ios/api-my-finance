using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;

namespace api_finances.src.Interfaces
{
    public interface IOperationService
    {
        Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllDTO request);
        Task<ResponseApi<List<dynamic>>> GetSelectAsync(GetAllDTO request);
        Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id);
        Task<ResponseApi<Models.Operation?>> CreateAsync(CreateOperationRequest request);
        Task<ResponseApi<Models.Operation?>> UpdateAsync(UpdateOperationRequest request);
        Task<ResponseApi<Models.Operation>> DeleteAsync(DeleteDTO request);
    }
}