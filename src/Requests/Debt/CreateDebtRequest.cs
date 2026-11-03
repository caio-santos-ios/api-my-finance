using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests
{
    public class CreateDebtRequest : Request
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        public string Name { get; set; } = string.Empty;

        public string Type { get; set; } = "other";

        [Range(0.01, double.MaxValue, ErrorMessage = "O valor total deve ser maior que zero.")]
        public decimal TotalAmount { get; set; }

        public decimal RemainingAmount { get; set; }

        public decimal InstallmentAmount { get; set; }

        public int TotalInstallments { get; set; }

        public int PaidInstallments { get; set; }

        public int DueDay { get; set; } = 10;

        public string BankId { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }
}
