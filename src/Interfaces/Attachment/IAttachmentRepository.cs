using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;

namespace api_finances.src.Interfaces
{
    public interface IAttachmentRepository
    {
        Task<ResponseApi<List<dynamic>>> GetAllAsync(PaginationUtil<Attachment> pagination);
        Task<ResponseApi<List<dynamic>>> GetSelectAsync(PaginationUtil<Attachment> pagination);
        Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id);
        Task<ResponseApi<Attachment?>> GetByIdAsync(string id);
        Task<int> GetCountDocumentsAsync(PaginationUtil<Attachment> pagination);
        Task<ResponseApi<Attachment?>> CreateAsync(Attachment entity);
        Task<ResponseApi<Attachment?>> UpdateAsync(Attachment request);
        Task<ResponseApi<Attachment>> DeleteAsync(DeleteDTO request);
    }
}