using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests.Recurring;

public class UpdateRecurringRequest
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "A descrição é obrigatória.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "O tipo é obrigatório.")]
    public string Type { get; set; } = "expense";

    [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal Value { get; set; }

    public string? CategoryId { get; set; }

    public string? BankId { get; set; }

    public string Frequency { get; set; } = "monthly";

    [Range(1, 31, ErrorMessage = "O dia de vencimento deve estar entre 1 e 31.")]
    public int DueDay { get; set; } = 1;

    public bool AutoPost { get; set; } = false;
}
