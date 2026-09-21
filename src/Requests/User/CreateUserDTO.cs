using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests
{
    public class CreateUserDTO : Request
    {
        [Required(ErrorMessage = "O Nome é obrigatório.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "O E-mail é obrigatório.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A Senha é obrigatória.")]
        public string Password { get; set; } = string.Empty;
        public bool Admin { get; set; } = false;
    }
}