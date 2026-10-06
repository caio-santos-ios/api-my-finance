using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;

namespace api_finances.src.Interfaces
{
    public interface IImportationRepository
    {
        Task<ResponseApi<List<dynamic>>> GetAllAsync(PaginationUtil<Importation> pagination);
        Task<ResponseApi<List<dynamic>>> GetSelectAsync(PaginationUtil<Importation> pagination);
        Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id);
        Task<ResponseApi<Importation?>> GetByIdAsync(string id);
        Task<int> GetCountDocumentsAsync(PaginationUtil<Importation> pagination);
        Task<ResponseApi<Importation?>> CreateAsync(Importation entity);
        Task<ResponseApi<Importation?>> UpdateAsync(Importation request);
        Task<ResponseApi<Importation>> DeleteAsync(DeleteRequest request);
    }
}