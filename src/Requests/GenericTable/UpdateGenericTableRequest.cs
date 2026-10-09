using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests.GenericTable
{
    public class UpdateGenericTableRequest : Request
    {
        [Required(ErrorMessage = "O Id é obrigatório.")]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "O Nome é obrigatório.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "A Tabela é obrigatória.")]
        public string Table { get; set; } = string.Empty;
    }
}