using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests
{
    public class PayDebtInstallmentRequest : Request
    {
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
        public decimal Amount { get; set; }

        public string BankId { get; set; } = string.Empty;

        public bool CreateExpenseOperation { get; set; } = true;

        public DateTime? PaymentDate { get; set; }
    }
}
