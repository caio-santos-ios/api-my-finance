using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests
{
    public class RefreshTokenRequest : Request
    {
        [Required(ErrorMessage = "O Refresh token é obrigatório.")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}