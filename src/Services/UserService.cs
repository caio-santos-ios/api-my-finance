using api_finances.src.Helpers;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;

namespace api_finances.src.Services
{
    public class UserService(
        IUserRepository repository,
        UploadHelper uploadHelper,
        MailHelper mailHelper
    ) : IUserService
    {
        #region READ
        public async Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllDTO request)
        {
            try
            {
                PaginationUtil<User> pagination = new(request.QueryParams);
                ResponseApi<List<dynamic>> users = await repository.GetAllAsync(pagination);
                int count = await repository.GetCountDocumentsAsync(pagination);
                PaginationApi<List<dynamic>> data = new(users.Data, count, pagination.PageNumber, pagination.PageSize);
                return new(data, 200, "Usuários listados com sucesso");
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
                ResponseApi<dynamic?> user = await repository.GetByIdAggregateAsync(id);
                if (user.Data is null) return new(null, 404, "Usuário não encontrado");
                return new(user.Data, 200, "Usuário encontrado");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        public async Task<ResponseApi<List<User>>> GetNotByIdAsync(string id)
        {
            try
            {
                ResponseApi<List<User>> users = await repository.GetNotByIdAsync(id);
                if (users.Data is null) return new(null, 404, "Usuário não encontrado");
                return new(users.Data, 200, "Usuário encontrado");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion

        #region CREATE
        public async Task<ResponseApi<User?>> CreateAsync(CreateUserDTO request)
        {
            try
            {
                ResponseApi<User?> existed = await repository.GetByEmailAsync(request.Email);
                if (existed.Data is not null) return new(null, 400, "E-mail inválido, tente usar outro");

                dynamic access = Util.GenerateCodeAccess();

                User user = new()
                {
                    Email = request.Email,
                    Name = request.Name,
                    Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    CodeAccess = access.CodeAccess,
                    CodeAccessExpiration = access.CodeAccessExpiration,
                    ValidatedAccess = false,
                    Admin = request.Admin,
                };

                ResponseApi<User?> response = await repository.CreateAsync(user);
                if (response.Data is null) return new(null, 400, "Falha ao criar conta.");

                await mailHelper.SendMail(request.Email, "Código de Confirmação", $"Seu código de confirmação: {access.CodeAccess}");

                return new(null, 201, "Usuário criado com sucesso.");
            }
            catch
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde");
            }
        }

        #endregion

        #region UPDATE
        public async Task<ResponseApi<User?>> UpdateAsync(UpdateUserDTO request)
        {
            try
            {
                ResponseApi<User?> user = await repository.GetByIdAsync(request.Id);
                if (user.Data is null) return new(null, 404, "Falha ao atualizar");

                if (!string.IsNullOrEmpty(request.Password))
                {
                    user.Data.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
                }

                user.Data.UpdatedAt = DateTime.UtcNow;
                user.Data.Email = request.Email;
                user.Data.Name = request.Name;

                ResponseApi<User?> response = await repository.UpdateAsync(user.Data);
                if (!response.IsSuccess) return new(null, 400, "Falha ao atualizar");

                return new(response.Data, 200, "Atualizado com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        public async Task<ResponseApi<dynamic?>> UpdateFCMAsync(UpdateFCMUserDTO request)
        {
            try
            {
                ResponseApi<User?> user = await repository.GetByIdAsync(request.Id);
                if (user.Data is null) return new(null, 404, "Falha ao atualizar");

                user.Data.UpdatedAt = DateTime.UtcNow;
                user.Data.TokenFCM = request.FCM;

                ResponseApi<User?> response = await repository.UpdateAsync(user.Data);
                if (!response.IsSuccess) return new(null, 400, "Falha ao atualizar");

                return new(null, 200, "Atualizado com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        public async Task<ResponseApi<User?>> UpdateConfirmAccountAsync(UpdateConfirmAccountDTO request)
        {
            try
            {
                ResponseApi<User?> user = await repository.GetByCodeAsync(request.Code);
                if (user.Data is null)
                {
                    ResponseApi<User?> existed = await repository.GetByEmailAsync(request.Email);
                    if (existed.Data is not null)
                    {
                        dynamic access = Util.GenerateCodeAccess();
                        existed.Data.CodeAccess = access.CodeAccess;
                        existed.Data.CodeAccessExpiration = access.CodeAccessExpiration;
                        existed.Data.ValidatedAccess = false;

                        await repository.UpdateAsync(existed.Data);

                        await mailHelper.SendMail(request.Email, "Código de Confirmação", $"Seu código de confirmação: {access.CodeAccess}");
                        return new(null, 404, "Código inválido, foi enviado um novo para o seu e-mail");
                    }
                    return new(null, 404, "Código inválido");
                }

                user.Data.UpdatedAt = DateTime.UtcNow;
                user.Data.CodeAccessExpiration = null;
                user.Data.CodeAccess = "";
                user.Data.ValidatedAccess = true;

                ResponseApi<User?> response = await repository.UpdateAsync(user.Data);
                if (!response.IsSuccess) return new(null, 400, "Falha ao confirmar conta");

                return new(null, 200, "Conta confirmada com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        public async Task<ResponseApi<string>> ProfilePhotoAsync(ProfilePhotoDTO request)
        {
            try
            {
                ResponseApi<User?> user = await repository.GetByIdAsync(request.Id);
                if (user.Data is null) return new(null, 404, "Falha ao salvar foto de perfil");
                if (request.Photo is null) return new(null, 404, "Falha ao salvar foto de perfil");

                string uri = await uploadHelper.SaveFileAsync(request.Photo);
                user.Data.Photo = uri;
                ResponseApi<User?> response = await repository.UpdateAsync(user.Data);
                if (!response.IsSuccess) return new(null, 400, "Falha ao salvar foto de perfil");

                return new(uri, 200, "Foto de perfil salva com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        public async Task<ResponseApi<string>> RemoveProfilePhotoAsync(ProfilePhotoDTO request)
        {
            try
            {
                ResponseApi<User?> user = await repository.GetByIdAsync(request.Id);
                if (user.Data is null) return new(null, 404, "Falha ao remover foto de perfil");

                user.Data.Photo = "";
                ResponseApi<User?> response = await repository.UpdateAsync(user.Data);
                if (!response.IsSuccess) return new(null, 400, "Falha ao remover foto de perfil");

                return new("", 200, "Foto de perfil removida com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion

        #region DELETE
        public async Task<ResponseApi<User>> DeleteAsync(DeleteDTO request)
        {
            try
            {
                ResponseApi<User> user = await repository.DeleteAsync(request);
                if (!user.IsSuccess) return new(null, 400, user.Message);
                return new(user.Data, 204, "Usuário excluído com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion        
    }
}