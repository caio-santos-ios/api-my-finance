using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests
{
    public class PayDebtInstallmentRequest : Request
    {
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
        public decimal Amount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Os juros não podem ser negativos.")]
        public decimal Interest { get; set; } = 0;

        public string BankId { get; set; } = string.Empty;

        public bool CreateExpenseOperation { get; set; } = true;

        public DateTime? PaymentDate { get; set; }
    }
}
