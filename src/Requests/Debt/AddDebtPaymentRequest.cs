using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests.Debt;

public class AddDebtPaymentRequest
{
    [Required]
    public string Type { get; set; } = "installment";

    [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal Amount { get; set; }

    public string? BankId { get; set; }

    public DateTime? PaymentDate { get; set; }

    public bool CreateExpenseOperation { get; set; } = false;

    public int InstallmentsCount { get; set; } = 1;

    public string Note { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;
}
