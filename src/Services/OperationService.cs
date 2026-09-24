using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;
using api_finances.src.Utils;

namespace api_finances.src.Services
{
    public class OperationService(
        IOperationRepository repository
    ) : IOperationService
    {
        #region READ
        public async Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllDTO request)
        {
            try
            {
                PaginationUtil<Operation> pagination = new(request.QueryParams);
                ResponseApi<List<dynamic>> operations = await repository.GetAllAsync(pagination);
                int count = await repository.GetCountDocumentsAsync(pagination);
                PaginationApi<List<dynamic>> data = new(operations.Data, count, pagination.PageNumber, pagination.PageSize);
                return new(data, 200, "Operações listados com sucesso");
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
                PaginationUtil<Operation> pagination = new(request.QueryParams);
                ResponseApi<List<dynamic>> operations = await repository.GetSelectAsync(pagination);
                return new(operations.Data, 200, "Operações listados com sucesso");
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
                ResponseApi<dynamic?> operation = await repository.GetByIdAggregateAsync(id);
                if (operation.Data is null) return new(null, 404, "Operação não encontrado");
                return new(operation.Data, 200, "Operação encontrado");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion

        #region CREATE
        public async Task<ResponseApi<Operation?>> CreateAsync(CreateOperationRequest request)
        {
            try
            {
                if (request.Type != "transfer" && string.IsNullOrWhiteSpace(request.CategoryId))
                    return new(null, 400, "A Categoria é obrigatória.");

                if (request.Type == "transfer" && string.IsNullOrWhiteSpace(request.DestinationBankId))
                    return new(null, 400, "O Banco de destino é obrigatório.");

                Operation operation = ObjectMapper.Map<CreateOperationRequest, Operation>(request);

                ResponseApi<Operation?> response = await repository.CreateAsync(operation);
                if (response.Data is null) return new(null, 400, "Falha ao criar conta.");

                return new(response.Data, 201, "Operação criado com sucesso.");
            }
            catch
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde");
            }
        }

        #endregion

        #region UPDATE
        public async Task<ResponseApi<Operation?>> UpdateAsync(UpdateOperationRequest request)
        {
            try
            {
                if (request.Type != "transfer" && string.IsNullOrWhiteSpace(request.CategoryId))
                    return new(null, 400, "A Categoria é obrigatória.");

                if (request.Type == "transfer" && string.IsNullOrWhiteSpace(request.DestinationBankId))
                    return new(null, 400, "O Banco de destino é obrigatório.");

                ResponseApi<Operation?> existed = await repository.GetByIdAsync(request.Id);
                if (existed.Data is null) return new(null, 404, "Falha ao atualizar");

                existed.Data.UpdatedAt = DateTime.UtcNow;
                existed.Data.UpdatedBy = request.UpdatedBy;
                existed.Data.Value = request.Value;
                existed.Data.CategoryId = request.CategoryId;
                existed.Data.BankId = request.BankId;
                existed.Data.DestinationBankId = request.DestinationBankId;
                existed.Data.Description = request.Description;
                existed.Data.Type = request.Type;
                existed.Data.Repeat = request.Repeat;

                ResponseApi<Operation?> response = await repository.UpdateAsync(existed.Data);
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
        public async Task<ResponseApi<Operation>> DeleteAsync(DeleteDTO request)
        {
            try
            {
                ResponseApi<Operation> operation = await repository.DeleteAsync(request);
                if (!operation.IsSuccess) return new(null, 400, operation.Message);
                return new(operation.Data, 204, "Operação excluído com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion        
    }
}