using api_finances.src.Interfaces;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;

namespace api_finances.src.Services
{
    public class CategoryService(
        ICategoryRepository repository
    ) : ICategoryService
    {
        #region READ
        public async Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllDTO request)
        {
            try
            {
                PaginationUtil<api_finances.src.Models.Category> pagination = new(request.QueryParams);
                ResponseApi<List<dynamic>> categories = await repository.GetAllAsync(pagination);
                int count = await repository.GetCountDocumentsAsync(pagination);
                PaginationApi<List<dynamic>> data = new(categories.Data, count, pagination.PageNumber, pagination.PageSize);
                return new(data, 200, "Categorias listados com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        public async Task<ResponseApi<List<dynamic>>> GetSelectAsync(GetAllDTO request)
        {
            try
            {
                PaginationUtil<api_finances.src.Models.Category> pagination = new(request.QueryParams);
                ResponseApi<List<dynamic>> categories = await repository.GetSelectAsync(pagination);
                return new(categories.Data, 200, "Categorias listados com sucesso");
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
                ResponseApi<dynamic?> category = await repository.GetByIdAggregateAsync(id);
                if (category.Data is null) return new(null, 404, "Categoria não encontrado");
                return new(category.Data, 200, "Categoria encontrado");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion

        #region CREATE
        public async Task<ResponseApi<api_finances.src.Models.Category?>> CreateAsync(CreateCategoryRequest request)
        {
            try
            {
                long code = await repository.GetNextCode(request.CreatedBy);
                api_finances.src.Models.Category category = new()
                {
                    Name = request.Name,
                    Code = code.ToString()!.PadLeft(6, '0'),
                    CreatedBy = request.CreatedBy,
                    Type = request.Type
                };

                ResponseApi<api_finances.src.Models.Category?> response = await repository.CreateAsync(category);
                if (response.Data is null) return new(null, 400, "Falha ao criar conta.");

                return new(response.Data, 201, "Categoria criado com sucesso.");
            }
            catch
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde");
            }
        }

        #endregion

        #region UPDATE
        public async Task<ResponseApi<api_finances.src.Models.Category?>> UpdateAsync(UpdateCategoryRequest request)
        {
            try
            {
                ResponseApi<api_finances.src.Models.Category?> category = await repository.GetByIdAsync(request.Id);
                if (category.Data is null) return new(null, 404, "Falha ao atualizar");

                category.Data.UpdatedAt = DateTime.UtcNow;
                category.Data.Name = request.Name;
                category.Data.Type = request.Type;

                ResponseApi<api_finances.src.Models.Category?> response = await repository.UpdateAsync(category.Data);
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
        public async Task<ResponseApi<api_finances.src.Models.Category>> DeleteAsync(DeleteDTO request)
        {
            try
            {
                ResponseApi<api_finances.src.Models.Category> category = await repository.DeleteAsync(request);
                if (!category.IsSuccess) return new(null, 400, category.Message);
                return new(category.Data, 204, "Categoria excluído com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion        
    }
}