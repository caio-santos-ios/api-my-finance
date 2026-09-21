using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests
{
    public class ForgotPasswordRequest : Request
    {
        [Required(ErrorMessage = "O E-mail é obrigatório.")]
        [Display(Order = 1)]
        public string Email { get; set; } = string.Empty;
    }
}