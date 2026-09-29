using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests
{
    public class UpdateBudgetRequest : Request
    {
        [Required(ErrorMessage = "O Id é obrigatório.")]
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "A categoria é obrigatória.")]
        public string CategoryId { get; set; } = string.Empty;

        [Required(ErrorMessage = "O limite é obrigatório.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O limite deve ser maior que zero.")]
        public decimal Limit { get; set; }

        public bool ReceiveAlert { get; set; } = false;

        public int AlertPercentage { get; set; } = 80;
    }
}
