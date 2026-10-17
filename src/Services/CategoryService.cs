using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;
using MongoDB.Bson;

namespace api_finances.src.Services
{
    public class CategoryService(
        ICategoryRepository repository
    ) : ICategoryService
    {
        #region READ
        public async Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllRequest request)
        {
            try
            {
                PaginationUtil<Category> pagination = new(request.QueryParams);
                BsonDocument[] pipeline = [
                    new("$match", pagination.PipelineFilter),
                    new("$sort", pagination.PipelineSort),
                    new("$skip", pagination.Skip),
                    new("$limit", pagination.Limit),
                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"id", new BsonDocument("$toString", "$_id")},
                        {"name", 1},
                        {"code", 1},
                        {"type", 1},
                        {"active", 1},
                        {"createdAt", 1}
                    }),
                    new("$sort", pagination.PipelineSort),
                ];

                List<dynamic> entities = await repository.GetAllAsync(pipeline);
                int count = await repository.GetCountDocumentsAsync(pipeline);
                PaginationApi<List<dynamic>> data = new(entities, count, pagination.PageNumber, pagination.PageSize);
                return new(data, 200, "Categorias listados com sucesso");
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
                PaginationUtil<Category> pagination = new(request.QueryParams);
                BsonDocument[] pipeline = [
                    new("$match", pagination.PipelineFilter),
                    new("$sort", pagination.PipelineSort),
                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"id", new BsonDocument("$toString", "$_id")},
                        {"name", 1},
                        {"code", 1},
                        {"type", 1},
                        {"active", 1}
                    }),
                    new("$sort", pagination.PipelineSort),
                ];

                List<dynamic> entities = await repository.GetSelectAsync(pipeline);
                return new(entities, 200, "Categorias listados com sucesso");
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
                BsonDocument[] pipeline = [
                    new("$match", new BsonDocument{
                        {"_id", new ObjectId(id)},
                        {"deleted", false}
                    }),
                    new("$addFields", new BsonDocument
                    {
                        {"id", new BsonDocument("$toString", "$_id")}
                    }),
                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                    }),
                ];

                dynamic? entity = await repository.GetByIdAggregateAsync(pipeline);
                if (entity is null) return new(null, 404, "Categoria não encontrado");
                return new(entity, 200, "Categoria encontrado");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion

        #region CREATE
        public async Task<ResponseApi<Category?>> CreateAsync(CreateCategoryRequest request)
        {
            try
            {
                long code = await repository.GetNextCode(request.CreatedBy);
                Category category = new()
                {
                    Name = request.Name,
                    Code = code.ToString()!.PadLeft(6, '0'),
                    CreatedBy = request.CreatedBy,
                    Type = request.Type
                };

                Category? response = await repository.CreateAsync(category);
                if (response is null) return new(null, 400, "Falha ao criar conta.");

                return new(response, 201, "Categoria criado com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
            }
        }
        #endregion

        #region UPDATE
        public async Task<ResponseApi<Category?>> UpdateAsync(UpdateCategoryRequest request)
        {
            try
            {
                Category? existed = await repository.GetByIdAsync(request.Id);
                if (existed is null) return new(null, 404, "Falha ao atualizar");

                existed.UpdatedAt = DateTime.UtcNow;
                existed.Name = request.Name;
                existed.Type = request.Type;

                Category? response = await repository.UpdateAsync(existed);
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
        public async Task<ResponseApi<Category>> DeleteAsync(DeleteRequest request)
        {
            try
            {
                Category? entity = await repository.GetByIdAsync(request.Id);
                if (entity is null) return new(null, 404, "Categoria não encontrado");

                entity.Deleted = true;
                entity.DeletedAt = DateTime.UtcNow;
                entity.DeletedBy = request.DeletedBy;

                await repository.DeleteAsync(entity);

                return new(entity, 204, "Categoria excluído com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion
    }
}