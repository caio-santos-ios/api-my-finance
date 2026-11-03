using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests.Asset
{
    public class DisposeAssetRequest
    {
        public DateTime? DisposalDate { get; set; } = DateTime.UtcNow;

        [Range(0, double.MaxValue, ErrorMessage = "O valor deve ser maior ou igual a zero.")]
        public decimal? DisposalValue { get; set; }

        public string DisposalReason { get; set; } = "Venda";

        public string? BankId { get; set; }

        public bool CreateIncomeOperation { get; set; } = false;
    }
}
