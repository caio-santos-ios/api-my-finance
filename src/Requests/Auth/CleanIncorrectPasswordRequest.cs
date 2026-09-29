using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests
{
    public class CleanIncorrectPasswordRequest : Request
    {
        [Required(ErrorMessage = "O Id do Usuário é obrigatório.")]
        [Display(Order = 1)]
        public string UserId { get; set; } = string.Empty;
    }
}