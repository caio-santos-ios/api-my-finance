using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests
{
    public class ResetPasswordRequest : Request
    {
        [Required(ErrorMessage = "O Código é obrigatório.")]
        [Display(Order = 1)]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "A Senha é obrigatória.")]
        [Display(Order = 2)]
        public string Password { get; set; } = string.Empty;
    }
}