namespace api_finances.src.Shared.DTOs
{
    public class TaxPreviewDto
    {
        public int BaseYear { get; set; }
        public decimal TaxableIncome { get; set; }
        public decimal ExemptIncome { get; set; }
        public decimal ExclusiveIncome { get; set; }
        public decimal Deductions { get; set; }
        public decimal AssetsAtAcquisitionCost { get; set; }
        public decimal DebtsAtYearEnd { get; set; }
        public int PendingCount { get; set; }
    }

    public class TaxPendingItemDto
    {
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = "warning";
        public string Message { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
    }

    public class TaxIncomeItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public DateTime Date { get; set; }
        public string Treatment { get; set; } = string.Empty;
        public string Source { get; set; } = "operation";
    }

    public class TaxDeductionItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public DateTime Date { get; set; }
        public string? PayeeName { get; set; }
        public string? PayeeDocument { get; set; }
        public bool HasDocument { get; set; }
        public bool HasReceipt { get; set; }
        public string? Subtype { get; set; }
    }

    public class TaxAssetItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? IrAssetCode { get; set; }
        public string? IrDescription { get; set; }
        public decimal AcquisitionValue { get; set; }
        public decimal CurrentMarketValue { get; set; }
        public DateTime? AcquisitionDate { get; set; }
        public bool MissingAcquisitionValue { get; set; }
        public string Source { get; set; } = "asset";
    }

    public class TaxDebtItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal BalanceAtYearEnd { get; set; }
        public string? BankName { get; set; }
    }
}
