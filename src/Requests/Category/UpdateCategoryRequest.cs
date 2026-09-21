using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests
{
    public class UpdateCategoryRequest : Request
    {
        [Required(ErrorMessage = "O Id é obrigatório.")]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "O Nome é obrigatório.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "O Tipo é obrigatório.")]
        public string Type { get; set; } = string.Empty;
    }
}