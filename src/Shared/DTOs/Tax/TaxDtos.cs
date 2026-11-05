namespace api_finances.src.Shared.DTOs
{
    public class TaxPreviewDto
    {
        public int BaseYear { get; set; }
        public string Disclaimer { get; set; } = "Material informativo de apoio ao preenchimento da Declaração de Ajuste Anual do IRPF. Não substitui os informes oficiais emitidos pelas fontes pagadoras nem a orientação de um profissional de contabilidade.";
        public decimal TotalTaxableIncome { get; set; }
        public decimal TotalExemptIncome { get; set; }
        public decimal TotalExclusiveIncome { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TotalAssetsCost { get; set; }
        public decimal TotalDebtsBalance { get; set; }
        public int PendingCount { get; set; }

        public List<TaxPendingItemDto> Pendings { get; set; } = [];
        public List<TaxIncomeItemDto> TaxableIncome { get; set; } = [];
        public List<TaxIncomeItemDto> ExemptIncome { get; set; } = [];
        public List<TaxIncomeItemDto> ExclusiveIncome { get; set; } = [];
        public List<TaxDeductionItemDto> Deductions { get; set; } = [];
        public List<TaxAssetItemDto> Assets { get; set; } = [];
        public List<TaxDebtItemDto> Debts { get; set; } = [];
    }

    public class TaxPendingItemDto
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Message { get => Description; set => Description = value; }
        public string? Route { get; set; }
        public string? EntityId { get; set; }
        public string? EntityName { get; set; }
        public string Severity { get; set; } = "warning";
        public string? ActionType { get; set; }
    }

    public class TaxIncomeItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? PayerDocument { get; set; }
        public decimal TotalValue { get; set; }
        public decimal Value { get => TotalValue; set => TotalValue = value; }
        public DateTime Date { get; set; }
        public string IrTreatment { get; set; } = string.Empty;
        public string Treatment { get => IrTreatment; set => IrTreatment = value; }
        public string Source { get; set; } = "operation";
    }

    public class TaxDeductionItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public string Subtype { get; set; } = "other";
        public string? PayeeName { get; set; }
        public string? PayeeDocument { get; set; }
        public decimal Amount { get; set; }
        public decimal Value { get => Amount; set => Amount = value; }
        public DateTime Date { get; set; }
        public bool HasDocument { get; set; }
        public bool HasReceipt { get; set; }
    }

    public class TaxAssetItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = "99";
        public string? IrAssetCode { get => Code; set => Code = value ?? "99"; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Discrimination { get; set; } = string.Empty;
        public string? IrDescription { get => Discrimination; set => Discrimination = value ?? string.Empty; }
        public decimal SituationPreviousYear { get; set; }
        public decimal SituationBaseYear { get; set; }
        public decimal AcquisitionValue { get => SituationBaseYear; set => SituationBaseYear = value; }
        public decimal CurrentMarketValue { get; set; }
        public DateTime? AcquisitionDate { get; set; }
        public bool MissingAcquisitionValue { get; set; }
        public string Source { get; set; } = "asset";
    }

    public class TaxDebtItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = "11";
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CreditorName { get; set; }
        public string? BankName { get => CreditorName; set => CreditorName = value; }
        public string Type { get; set; } = string.Empty;
        public decimal SituationPreviousYear { get; set; }
        public decimal SituationBaseYear { get; set; }
        public decimal BalanceAtYearEnd { get => SituationBaseYear; set => SituationBaseYear = value; }
        public decimal TotalAmount { get; set; }
    }
}
