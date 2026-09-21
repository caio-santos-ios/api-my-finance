using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests
{
    public class LoginRequest : Request
    {
        [Required(ErrorMessage = "O E-mail é obrigatório.")]
        [Display(Order = 1)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A Senha é obrigatório.")]
        [Display(Order = 2)]
        public string Password { get; set; } = string.Empty;
    }
}