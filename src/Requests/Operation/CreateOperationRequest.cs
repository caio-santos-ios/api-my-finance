using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests
{
    public class CreateOperationRequest : Request
    {
        [Required(ErrorMessage = "O Valor é obrigatório.")]
        public decimal Value { get; set; }

        public string CategoryId { get; set; } = string.Empty;

        public string BankId { get; set; } = string.Empty;

        public string DestinationBankId { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
        public string ParentId { get; set; } = string.Empty;

        [Required(ErrorMessage = "O Tipo é obrigatório.")]
        public string Type { get; set; } = string.Empty;
        public bool Repeat { get; set; } = false;
        public string Origin { get; set; } = string.Empty;
        public string OriginId { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}