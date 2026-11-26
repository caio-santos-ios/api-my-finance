using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests.CreditCard
{
    public class CreateCreditCardRequest
    {
        [Required(ErrorMessage = "O nome do cartão é obrigatório.")]
        public string Name { get; set; } = string.Empty;

        public string? BankId { get; set; }

        public string Brand { get; set; } = "Mastercard";

        [Range(0.01, double.MaxValue, ErrorMessage = "O limite deve ser maior que zero.")]
        public decimal Limit { get; set; }

        [Range(1, 31, ErrorMessage = "O dia de fechamento deve estar entre 1 e 31.")]
        public int ClosingDay { get; set; } = 1;

        [Range(1, 31, ErrorMessage = "O dia de vencimento deve estar entre 1 e 31.")]
        public int DueDay { get; set; } = 10;

        public string Color { get; set; } = "#820ad1";

        public string? LastDigits { get; set; }
    }

    public class UpdateCreditCardRequest : CreateCreditCardRequest
    {
    }

    public class CreateCreditCardTransactionRequest
    {
        public string? CategoryId { get; set; }

        [Required(ErrorMessage = "A descrição da compra é obrigatória.")]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
        public decimal Value { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;

        [Range(1, 48, ErrorMessage = "O número de parcelas deve estar entre 1 e 48.")]
        public int Installments { get; set; } = 1;
    }

    public class UpdateCreditCardTransactionRequest
    {
        public string? CategoryId { get; set; }

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
        public decimal Value { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;
    }

    public class PayCreditCardInvoiceRequest
    {
        [Required(ErrorMessage = "A conta bancária de pagamento é obrigatória.")]
        public string PaymentBankId { get; set; } = string.Empty;

        public DateTime? PaidAt { get; set; }

        public decimal? Value { get; set; }
    }
}
