using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests
{
    public class UpdateInvestmentAssetRequest : Request
    {
        [Required(ErrorMessage = "O id é obrigatório.")]
        public string Id { get; set; } = string.Empty;

        public string Ticker { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome do ativo é obrigatório.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "O tipo do ativo é obrigatório.")]
        public string Type { get; set; } = "STOCK";

        public string Currency { get; set; } = "BRL";

        public decimal ManualPrice { get; set; } = 0;

        public DateTime? MaturityDate { get; set; }

        public decimal? ContractedRate { get; set; }
    }
}
