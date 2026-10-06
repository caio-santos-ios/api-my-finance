using api_finances.src.Helpers;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;
using api_finances.src.Utils;

namespace api_finances.src.Services
{
    public class AttachmentService(
        IAttachmentRepository repository,
        UploadHelper uploadHelper
    ) : IAttachmentService
    {
        #region READ
        public async Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllRequest request)
        {
            try
            {
                PaginationUtil<Attachment> pagination = new(request.QueryParams);
                ResponseApi<List<dynamic>> attachments = await repository.GetAllAsync(pagination);
                int count = await repository.GetCountDocumentsAsync(pagination);
                PaginationApi<List<dynamic>> data = new(attachments.Data, count, pagination.PageNumber, pagination.PageSize);
                return new(data, 200, "Operações listados com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        public async Task<ResponseApi<List<dynamic>>> GetSelectAsync(GetAllRequest request)
        {
            try
            {
                PaginationUtil<Attachment> pagination = new(request.QueryParams);
                ResponseApi<List<dynamic>> attachments = await repository.GetSelectAsync(pagination);
                return new(attachments.Data, 200, "Operações listados com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        public async Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id)
        {
            try
            {
                ResponseApi<dynamic?> attachment = await repository.GetByIdAggregateAsync(id);
                if (attachment.Data is null) return new(null, 404, "Anexo não encontrado");
                return new(attachment.Data, 200, "Anexo encontrado");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion

        #region CREATE
        public async Task<ResponseApi<Attachment?>> CreateAsync(CreateAttachmentRequest request)
        {
            try
            {
                if (request.File is null) return new(null, 400, "Falha ao salvar anexo");
                Attachment attachment = ObjectMapper.Map<CreateAttachmentRequest, Attachment>(request);

                string uri = await uploadHelper.SaveFileAsync(request.File!, request.Parent);
                attachment.Uri = uri;

                ResponseApi<Attachment?> response = await repository.CreateAsync(attachment);
                if (response.Data is null) return new(null, 400, "Falha ao salvar anexo.");

                return new(response.Data, 201, "Anexo criado com sucesso.");
            }
            catch
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde");
            }
        }

        #endregion

        #region UPDATE
        public async Task<ResponseApi<Attachment?>> UpdateAsync(UpdateAttachmentRequest request)
        {
            try
            {
                ResponseApi<Attachment?> existed = await repository.GetByIdAsync(request.Id);
                if (existed.Data is null) return new(null, 404, "Falha ao atualizar");

                existed.Data.UpdatedAt = DateTime.UtcNow;
                existed.Data.UpdatedBy = request.UpdatedBy;

                ResponseApi<Attachment?> response = await repository.UpdateAsync(existed.Data);
                if (!response.IsSuccess) return new(null, 400, "Falha ao atualizar");

                return new(response.Data, 200, "Atualizado com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion

        #region DELETE
        public async Task<ResponseApi<Attachment>> DeleteAsync(DeleteRequest request)
        {
            try
            {
                ResponseApi<Attachment> attachment = await repository.DeleteAsync(request);
                if (!attachment.IsSuccess) return new(null, 400, attachment.Message);
                return new(attachment.Data, 204, "Anexo excluído com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion        
    }
}