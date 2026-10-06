using api_finances.src.Models.Base;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;

namespace api_finances.src.Interfaces
{
    public interface ICategoryRepository
    {
        Task<ResponseApi<List<dynamic>>> GetAllAsync(PaginationUtil<api_finances.src.Models.Category> pagination);
        Task<ResponseApi<List<dynamic>>> GetSelectAsync(PaginationUtil<api_finances.src.Models.Category> pagination);
        Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id);
        Task<ResponseApi<api_finances.src.Models.Category?>> GetByIdAsync(string id);
        Task<long> GetNextCode(string userId);
        Task<int> GetCountDocumentsAsync(PaginationUtil<api_finances.src.Models.Category> pagination);
        Task<ResponseApi<api_finances.src.Models.Category?>> CreateAsync(api_finances.src.Models.Category user);
        Task<ResponseApi<api_finances.src.Models.Category?>> UpdateAsync(api_finances.src.Models.Category request);
        Task<ResponseApi<api_finances.src.Models.Category>> DeleteAsync(DeleteRequest request);
    }
}