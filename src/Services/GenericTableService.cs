using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests.GenericTable;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;
using api_finances.src.Utils;
using MongoDB.Bson;

namespace api_finances.src.Services
{
    public class GenericTableService(
        IGenericTableRepository repository
    ) : IGenericTableService
    {
        #region READ
        public async Task<ResponseApi<List<dynamic>>> GetSelectAsync(GetAllRequest request)
        {
            try
            {
                PaginationUtil<GenericTable> pagination = new(request.QueryParams);
                List<BsonDocument> pipeline = [
                    new("$match", pagination.PipelineFilter),
                    new("$project", new BsonDocument {
                        {"_id", 0},
                        {"id", MongoUtil.ToString("$_id")},
                        {"code", 1},
                        {"name", 1},
                        {"table", 1}
                    })
                ];

                List<dynamic> entities = await repository.GetSelectAsync(pipeline);
                return new(entities, 200, "Registros listados com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion

        #region CREATE
        public async Task<ResponseApi<GenericTable?>> CreateAsync(CreateGenericTableRequest request)
        {
            try
            {
                GenericTable entity = ObjectMapper.Map<CreateGenericTableRequest, GenericTable>(request);
                entity.CreatedBy = request.CreatedBy;
                entity.CreatedBy = request.CreatedBy;
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.Deleted = false;
                entity.Name = request.Name;
                long code = await repository.NextCodeAsync(request.Table, request.CreatedBy);

                entity.Code = code.ToString().PadLeft(4, '0');

                GenericTable? response = await repository.CreateAsync(entity);
                if (response is null) return new(null, 400, "Falha ao salvar registro.");

                return new(response, 201, "Registro criado com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion

        #region UPDATE
        public async Task<ResponseApi<GenericTable?>> UpdateAsync(UpdateGenericTableRequest request)
        {
            try
            {
                GenericTable? existed = await repository.GetByIdAsync(request.Id);
                if (existed is null) return new(null, 404, "Falha ao atualizar");

                existed.Name = request.Name;
                existed.UpdatedAt = DateTime.UtcNow;
                existed.UpdatedBy = request.UpdatedBy;

                GenericTable? response = await repository.UpdateAsync(existed);
                if (response is null) return new(null, 400, "Falha ao atualizar");

                return new(response, 200, "Atualizado com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion

        #region DELETE
        public async Task<ResponseApi<dynamic?>> DeleteAsync(string id, string userId)
        {
            try
            {
                GenericTable? existed = await repository.GetByIdAsync(id);
                if (existed is null || existed.CreatedBy != userId) return new(null, 404, "Registro não encontrado");

                existed.Deleted = true;
                existed.UpdatedAt = DateTime.UtcNow;
                existed.UpdatedBy = userId;

                await repository.UpdateAsync(existed);
                return new(null, 200, "Registro excluído com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion        
    }
}