namespace api_finances.src.Models
{
    public class InvestmentPositionDTO
    {
        public string InvestmentAssetId { get; set; } = string.Empty;
        public string Ticker { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "STOCK";
        public decimal Quantity { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal LastPrice { get; set; }
        public decimal TotalInvested { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal UnrealizedProfit { get; set; }
        public decimal ProfitPercent { get; set; }
        public decimal IncomeReceived { get; set; }
        public decimal TotalReturn { get; set; }
        public decimal TotalReturnPercent { get; set; }
        public bool HasQuote { get; set; }
        public bool IsEstimated { get; set; }
        public List<string> BrokerNames { get; set; } = [];
    }

    public class PortfolioResponse
    {
        public decimal TotalInvested { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal UnrealizedProfit { get; set; }
        public decimal UnrealizedProfitPercent { get; set; }
        public decimal IncomeReceived { get; set; }
        public decimal TotalReturn { get; set; }
        public decimal TotalReturnPercent { get; set; }
        public List<InvestmentPositionDTO> Positions { get; set; } = [];
    }

    public class InvestmentTransactionResponse
    {
        public string Id { get; set; } = string.Empty;
        public string InvestmentAssetId { get; set; } = string.Empty;
        public string AssetTicker { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public string BrokerId { get; set; } = string.Empty;
        public string BrokerName { get; set; } = string.Empty;
        public string Kind { get; set; } = "BUY";
        public DateTime Date { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Fees { get; set; }
        public decimal Amount { get; set; }
        public string Notes { get; set; } = string.Empty;
        public decimal? RealizedProfit { get; set; }
    }
}
