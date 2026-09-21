using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests
{
    public class UpdateUserDTO : Request
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "O Nome é obrigatório.")]
        [Display(Order = 1)]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "O E-mail é obrigatório.")]
        [Display(Order = 2)]
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;    
        public bool Admin { get; set; } = false;
    }
}