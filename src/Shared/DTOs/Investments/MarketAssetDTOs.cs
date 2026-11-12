using api_finances.src.Models;

namespace api_finances.src.Shared.DTOs
{
    public class MarketAssetSearchResultDto
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "STOCK";
        public bool AlreadyRegistered { get; set; }
    }

    public class MarketAssetCreateResultDto
    {
        public bool AlreadyExisted { get; set; }
        public InvestmentAsset? Asset { get; set; }
        public decimal? CurrentPrice { get; set; }
        public DateTime? LastQuoteAt { get; set; }
    }
}
