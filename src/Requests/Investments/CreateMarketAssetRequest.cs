using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests
{
    public class CreateMarketAssetRequest : Request
    {
        [Required(ErrorMessage = "O símbolo (ticker) do ativo é obrigatório.")]
        public string Symbol { get; set; } = string.Empty;

        [Required(ErrorMessage = "O tipo do ativo é obrigatório.")]
        public string Type { get; set; } = "STOCK";

        public string? Name { get; set; }
    }
}
