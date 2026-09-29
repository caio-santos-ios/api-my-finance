using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests
{
    public class UpdateImportationRequest : Request
    {
        [Required(ErrorMessage = "O Id é obrigatório.")]
        public string Id { get; set; } = string.Empty;
        public IFormFile? File { get; set; }
    }
}