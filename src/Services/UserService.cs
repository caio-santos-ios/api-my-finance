using api_finances.src.Helpers;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;
using MongoDB.Bson;

namespace api_finances.src.Services
{
    public class UserService(
        IUserRepository repository,
        UploadHelper uploadHelper,
        MailHelper mailHelper
    ) : IUserService
    {
        #region READ
        public async Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllRequest request)
        {
            try
            {
                PaginationUtil<User> pagination = new(request.QueryParams);
                BsonDocument[] pipeline = [
                    new("$match", pagination.PipelineFilter),
                    new("$sort", pagination.PipelineSort),
                    new("$skip", pagination.Skip),
                    new("$limit", pagination.Limit),

                    new("$addFields", new BsonDocument
                    {
                        {"id", new BsonDocument("$toString", "$_id")},
                    }),

                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"password", 0},
                    }),
                    new("$sort", pagination.PipelineSort),
                ];

                List<dynamic> entities = await repository.GetAllAsync(pipeline);
                int count = await repository.GetCountDocumentsAsync(pipeline);
                PaginationApi<List<dynamic>> data = new(entities, count, pagination.PageNumber, pagination.PageSize);
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
                BsonDocument[] pipeline = [
                    new("$match", new BsonDocument{
                        {"_id", new ObjectId(id)},
                        {"deleted", false}
                    }),

                    new("$addFields", new BsonDocument
                    {
                        {"id", new BsonDocument("$toString", "$_id")},
                    }),

                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"password", 0},
                    }),
                ];

                dynamic? entity = await repository.GetByIdAggregateAsync(pipeline);
                if (entity is null) return new(null, 404, "Usuário não encontrado");
                return new(entity, 200, "Usuário encontrado");
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
                List<User> users = await repository.GetNotByIdAsync(id);
                return new(users, 200, "Usuário encontrado");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion

        #region CREATE
        public async Task<ResponseApi<User?>> CreateAsync(CreateUserRequest request)
        {
            try
            {
                User? existed = await repository.GetByEmailAsync(request.Email);
                if (existed is not null) return new(null, 400, "E-mail inválido, tente usar outro");

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

                User? response = await repository.CreateAsync(user);
                if (response is null) return new(null, 400, "Falha ao criar conta.");

                await mailHelper.SendMail(request.Email, "Código de Confirmação", $"Seu código de confirmação: {access.CodeAccess}");

                return new(null, 201, "Usuário criado com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
            }
        }
        #endregion

        #region UPDATE
        public async Task<ResponseApi<User?>> UpdateAsync(UpdateUserDTO request)
        {
            try
            {
                User? user = await repository.GetByIdAsync(request.Id);
                if (user is null) return new(null, 404, "Falha ao atualizar");

                if (!string.IsNullOrEmpty(request.Password))
                {
                    user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
                }

                user.UpdatedAt = DateTime.UtcNow;
                user.Email = request.Email;
                user.Name = request.Name;
                user.Phone = request.Phone;

                User? response = await repository.UpdateAsync(user);
                if (response is null) return new(null, 400, "Falha ao atualizar");

                return new(response, 200, "Atualizado com sucesso");
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
                User? user = await repository.GetByIdAsync(request.Id);
                if (user is null) return new(null, 404, "Falha ao atualizar");

                user.UpdatedAt = DateTime.UtcNow;
                user.TokenFCM = request.FCM;

                User? response = await repository.UpdateAsync(user);
                if (response is null) return new(null, 400, "Falha ao atualizar");

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
                User? user = await repository.GetByCodeAsync(request.Code);
                if (user is null)
                {
                    User? existed = await repository.GetByEmailAsync(request.Email);
                    if (existed is not null)
                    {
                        dynamic access = Util.GenerateCodeAccess();
                        existed.CodeAccess = access.CodeAccess;
                        existed.CodeAccessExpiration = access.CodeAccessExpiration;
                        existed.ValidatedAccess = false;

                        await repository.UpdateAsync(existed);

                        await mailHelper.SendMail(request.Email, "Código de Confirmação", $"Seu código de confirmação: {access.CodeAccess}");
                        return new(null, 404, "Código inválido, foi enviado um novo para o seu e-mail");
                    }
                    return new(null, 404, "Código inválido");
                }

                user.UpdatedAt = DateTime.UtcNow;
                user.CodeAccessExpiration = null;
                user.CodeAccess = "";
                user.ValidatedAccess = true;

                User? response = await repository.UpdateAsync(user);
                if (response is null) return new(null, 400, "Falha ao confirmar conta");

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
                User? user = await repository.GetByIdAsync(request.Id);
                if (user is null) return new(null, 404, "Falha ao salvar foto de perfil");
                if (request.Photo is null) return new(null, 404, "Falha ao salvar foto de perfil");

                string uri = await uploadHelper.SaveFileAsync(request.Photo, "users");
                user.Photo = uri;
                User? response = await repository.UpdateAsync(user);
                if (response is null) return new(null, 400, "Falha ao salvar foto de perfil");

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
                User? user = await repository.GetByIdAsync(request.Id);
                if (user is null) return new(null, 404, "Falha ao remover foto de perfil");

                user.Photo = "";
                User? response = await repository.UpdateAsync(user);
                if (response is null) return new(null, 400, "Falha ao remover foto de perfil");

                return new("", 200, "Foto de perfil removida com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion

        #region DELETE
        public async Task<ResponseApi<User>> DeleteAsync(DeleteRequest request)
        {
            try
            {
                User? entity = await repository.GetByIdAsync(request.Id);
                if (entity is null) return new(null, 404, "Usuário não encontrado");

                entity.Deleted = true;
                entity.DeletedAt = DateTime.UtcNow;
                entity.DeletedBy = request.DeletedBy;

                await repository.DeleteAsync(entity);

                return new(entity, 204, "Usuário excluído com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion
    }
}