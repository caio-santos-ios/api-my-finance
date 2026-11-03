using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests
{
    public class UpdateAssetRequest : Request
    {
        [Required(ErrorMessage = "O id é obrigatório.")]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome é obrigatório.")]
        public string Name { get; set; } = string.Empty;

        public string Type { get; set; } = "other";

        [Range(0, double.MaxValue, ErrorMessage = "O valor de compra deve ser maior ou igual a zero.")]
        public decimal PurchaseValue { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "O valor atual deve ser maior ou igual a zero.")]
        public decimal CurrentValue { get; set; }

        public DateTime? AcquisitionDate { get; set; }

        public string Description { get; set; } = string.Empty;
    }
}
