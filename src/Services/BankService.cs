using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;
using api_finances.src.Utils;

namespace api_finances.src.Services
{
    public class BankService(
        IBankRepository repository
    ) : IBankService
    {
        #region READ
        public async Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllRequest request)
        {
            try
            {
                PaginationUtil<Bank> pagination = new(request.QueryParams);
                ResponseApi<List<dynamic>> categories = await repository.GetAllAsync(pagination);
                int count = await repository.GetCountDocumentsAsync(pagination);
                PaginationApi<List<dynamic>> data = new(categories.Data, count, pagination.PageNumber, pagination.PageSize);
                return new(data, 200, "Bancos listados com sucesso");
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
                PaginationUtil<Bank> pagination = new(request.QueryParams);
                ResponseApi<List<dynamic>> categories = await repository.GetSelectAsync(pagination);
                return new(categories.Data, 200, "Bancos listados com sucesso");
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
                ResponseApi<dynamic?> banks = await repository.GetByIdAggregateAsync(id);
                if (banks.Data is null) return new(null, 404, "Banco não encontrado");
                return new(banks.Data, 200, "Banco encontrado");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion

        #region CREATE
        public async Task<ResponseApi<Bank?>> CreateAsync(CreateBankRequest request)
        {
            try
            {
                Bank banks = ObjectMapper.Map<CreateBankRequest, Bank>(request);

                ResponseApi<Bank?> response = await repository.CreateAsync(banks);
                if (response.Data is null) return new(null, 400, "Falha ao criar conta.");

                return new(response.Data, 201, "Banco criado com sucesso.");
            }
            catch
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde");
            }
        }

        #endregion

        #region UPDATE
        public async Task<ResponseApi<Bank?>> UpdateAsync(UpdateBankRequest request)
        {
            try
            {
                ResponseApi<Bank?> existed = await repository.GetByIdAsync(request.Id);
                if (existed.Data is null) return new(null, 404, "Falha ao atualizar");

                Bank banks = ObjectMapper.Map<UpdateBankRequest, Bank>(request);
                banks.UpdatedAt = DateTime.UtcNow;

                ResponseApi<Bank?> response = await repository.UpdateAsync(banks);
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
        public async Task<ResponseApi<Bank>> DeleteAsync(DeleteRequest request)
        {
            try
            {
                ResponseApi<Bank> banks = await repository.DeleteAsync(request);
                if (!banks.IsSuccess) return new(null, 400, banks.Message);
                return new(banks.Data, 204, "Banco excluído com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion        
    }
}