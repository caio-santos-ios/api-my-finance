using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;

namespace api_finances.src.Interfaces
{
    public interface ICategoryService
    {
        Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllDTO request);
        Task<ResponseApi<List<dynamic>>> GetSelectAsync(GetAllDTO request);
        Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id);
        Task<ResponseApi<api_finances.src.Models.Category?>> CreateAsync(CreateCategoryRequest user);
        Task<ResponseApi<api_finances.src.Models.Category?>> UpdateAsync(UpdateCategoryRequest user);
        Task<ResponseApi<api_finances.src.Models.Category>> DeleteAsync(DeleteDTO request);
    }
}