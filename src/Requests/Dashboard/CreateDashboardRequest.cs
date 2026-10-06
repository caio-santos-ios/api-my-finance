using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests.Dashboard
{
    public class CreateDashboardRequest : Request
    {
        [Required(ErrorMessage = "O Nome é obrigatório.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "O Tipo é obrigatório.")]
        public string Type { get; set; } = string.Empty;

        [Required(ErrorMessage = "A Collection é obrigatória.")]
        public string Collection { get; set; } = string.Empty;

        [Required(ErrorMessage = "A Query é obrigatória.")]
        public string Query { get; set; } = string.Empty;
    }
}