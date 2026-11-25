using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Shared.DTOs
{
    public class B3ImportRowDto
    {
        public string Ticker { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "STOCK"; // STOCK, FII, ETF, BDR
        public string Kind { get; set; } = "BUY"; // BUY, SELL
        public DateTime Date { get; set; }
        public string BrokerName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue { get; set; }
        public string ImportHash { get; set; } = string.Empty;
        public bool IsDuplicate { get; set; }
        public bool HasWarning { get; set; }
        public string? WarningMessage { get; set; }
    }

    public class B3PreviewResponseDto
    {
        public int TotalRows { get; set; }
        public int NewRows { get; set; }
        public int DuplicateRows { get; set; }
        public int WarningRows { get; set; }
        public List<B3ImportRowDto> Rows { get; set; } = [];
    }

    public class B3ConfirmImportRequest
    {
        [Required(ErrorMessage = "A lista de movimentações a importar é obrigatória.")]
        public List<B3ImportConfirmItemDto> Items { get; set; } = [];
    }

    public class B3ImportConfirmItemDto
    {
        public string Ticker { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "STOCK";
        public string Kind { get; set; } = "BUY";
        public DateTime Date { get; set; }
        public string BrokerName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue { get; set; }
        public string ImportHash { get; set; } = string.Empty;
    }

    public class B3ConfirmImportResponseDto
    {
        public string? BatchId { get; set; }
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> CreatedAssetTickers { get; set; } = [];
    }

    public class B3ImportBatchDto
    {
        public string BatchId { get; set; } = string.Empty;
        public DateTime ImportedAt { get; set; }
        public int TransactionCount { get; set; }
        public int AssetCount { get; set; }
        public DateTime? FirstDate { get; set; }
        public DateTime? LastDate { get; set; }
    }

    public class B3RevertImportResponseDto
    {
        public int RevertedTransactions { get; set; }
        public int RemovedAssets { get; set; }
    }
}
