using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests.GenericTable;
using api_finances.src.Shared.DTOs;

namespace api_finances.src.Interfaces
{
    public interface IGenericTableService
    {
        Task<ResponseApi<List<dynamic>>> GetSelectAsync(GetAllRequest request);
        Task<ResponseApi<GenericTable?>> CreateAsync(CreateGenericTableRequest request);
        Task<ResponseApi<GenericTable?>> UpdateAsync(UpdateGenericTableRequest request);
        Task<ResponseApi<dynamic?>> DeleteAsync(string id, string userId);
    }
}