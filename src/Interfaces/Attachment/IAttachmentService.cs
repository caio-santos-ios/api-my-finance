using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;

namespace api_finances.src.Interfaces
{
    public interface IAttachmentService
    {
        Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllDTO request);
        Task<ResponseApi<List<dynamic>>> GetSelectAsync(GetAllDTO request);
        Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id);
        Task<ResponseApi<Attachment?>> CreateAsync(CreateAttachmentRequest request);
        Task<ResponseApi<Attachment?>> UpdateAsync(UpdateAttachmentRequest request);
        Task<ResponseApi<Attachment>> DeleteAsync(DeleteDTO request);
    }
}