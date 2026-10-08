

using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;

namespace api_finances.src.Interfaces
{
    public interface IUserService
    {
        Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllRequest request);
        Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id);
        Task<ResponseApi<List<User>>> GetNotByIdAsync(string id);
        Task<ResponseApi<User?>> CreateAsync(CreateUserRequest user);
        Task<ResponseApi<User?>> UpdateAsync(UpdateUserDTO user);
        Task<ResponseApi<User?>> UpdateConfirmAccountAsync(UpdateConfirmAccountDTO request);
        Task<ResponseApi<dynamic?>> UpdateFCMAsync(UpdateFCMUserDTO request);
        Task<ResponseApi<string>> ProfilePhotoAsync(ProfilePhotoDTO request);
        Task<ResponseApi<string>> RemoveProfilePhotoAsync(ProfilePhotoDTO request);
        Task<ResponseApi<User>> DeleteAsync(DeleteRequest request);
    }
}