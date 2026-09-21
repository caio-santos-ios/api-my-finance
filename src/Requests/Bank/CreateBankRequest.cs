using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests
{
    public class CreateBankRequest : Request
    {
        [Required(ErrorMessage = "O Código é obrigatório.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "O Nome é obrigatório.")]
        public string Name { get; set; } = string.Empty;
    }
}