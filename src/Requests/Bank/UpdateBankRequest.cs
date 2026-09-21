using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests
{
    public class UpdateBankRequest : Request
    {
        [Required(ErrorMessage = "O Id é obrigatório.")]
        public string Id { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "O Código é obrigatório.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "O Nome é obrigatório.")]
        public string Name { get; set; } = string.Empty;
    }
}