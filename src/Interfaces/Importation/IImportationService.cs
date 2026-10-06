using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;

namespace api_finances.src.Interfaces
{
    public interface IImportationService
    {
        Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllRequest request);
        Task<ResponseApi<List<dynamic>>> GetSelectAsync(GetAllRequest request);
        Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id);
        Task<ResponseApi<Importation?>> CreateAsync(CreateImportationRequest request);
        Task<ResponseApi<Importation?>> UpdateAsync(UpdateImportationRequest request);
        Task<ResponseApi<Importation>> DeleteAsync(DeleteRequest request);
    }
}