using api_finances.src.Models.Base;
using api_finances.src.Requests;

namespace api_finances.src.Interfaces
{
    public interface IAuthService
    {
        Task<ResponseApi<dynamic?>> CreateAsync(CreateUserRequest request);
        Task<ResponseApi<dynamic?>> LoginAsync(LoginRequest request);
        Task<ResponseApi<dynamic?>> NewCodeAsync(NewCodeRequest request);
        Task<ResponseApi<dynamic?>> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<ResponseApi<dynamic?>> ResetPasswordAsync(ResetPasswordRequest request);
        Task<ResponseApi<dynamic?>> CleanIncorrectPasswordAsync(CleanIncorrectPasswordRequest request);
    }
}