using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests
{
    public class UpdateInvestmentTransactionRequest : Request
    {
        [Required(ErrorMessage = "O id é obrigatório.")]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "O ativo de investimento é obrigatório.")]
        public string InvestmentAssetId { get; set; } = string.Empty;

        [Required(ErrorMessage = "A corretora/banco é obrigatória.")]
        public string BrokerId { get; set; } = string.Empty;

        [Required(ErrorMessage = "O tipo da movimentação é obrigatório.")]
        public string Kind { get; set; } = "BUY";

        public DateTime Date { get; set; } = DateTime.UtcNow;

        public decimal Quantity { get; set; } = 0;

        public decimal UnitPrice { get; set; } = 0;

        public decimal Fees { get; set; } = 0;

        public decimal Amount { get; set; } = 0;

        public string Notes { get; set; } = string.Empty;

        public bool CreateOperation { get; set; } = false;
    }
}
