using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;

namespace api_finances.src.Interfaces
{
    public interface IBankService
    {
        Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllRequest request);
        Task<ResponseApi<List<dynamic>>> GetSelectAsync(GetAllRequest request);
        Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id);
        Task<ResponseApi<Bank?>> CreateAsync(CreateBankRequest request);
        Task<ResponseApi<Bank?>> UpdateAsync(UpdateBankRequest request);
        Task<ResponseApi<Bank>> DeleteAsync(DeleteRequest request);
    }
}