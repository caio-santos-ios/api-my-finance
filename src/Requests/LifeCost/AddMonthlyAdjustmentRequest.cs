using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests
{
    public class AddMonthlyAdjustmentRequest : Request
    {
        [Required(ErrorMessage = "O ano é obrigatório.")]
        [Range(2000, 2100, ErrorMessage = "Ano inválido.")]
        public int Year { get; set; }

        [Required(ErrorMessage = "O mês é obrigatório.")]
        [Range(1, 12, ErrorMessage = "Mês inválido (1 a 12).")]
        public int Month { get; set; }

        [Required(ErrorMessage = "O valor é obrigatório.")]
        [Range(0.01, 1000000000, ErrorMessage = "O valor deve ser maior que zero.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "O motivo é obrigatório.")]
        public string Reason { get; set; } = string.Empty;
    }
}
