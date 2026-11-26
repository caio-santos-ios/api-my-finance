namespace api_finances.src.Shared.DTOs
{
    public class CreditCardResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? BankId { get; set; }
        public string? BankName { get; set; }
        public string Brand { get; set; } = "Mastercard";
        public decimal Limit { get; set; }
        public decimal UsedLimit { get; set; }
        public decimal AvailableLimit { get; set; }
        public decimal UsagePercent { get; set; }
        public int ClosingDay { get; set; }
        public int DueDay { get; set; }
        public int BestPurchaseDay { get; set; }
        public string Color { get; set; } = "#820ad1";
        public string? LastDigits { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreditCardInvoiceResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string CreditCardId { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public string Label { get; set; } = string.Empty;
        public DateTime ClosingDate { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = "open"; // "open", "closed", "paid"
        public bool IsFuture { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? PaymentBankId { get; set; }
        public string? PaymentBankName { get; set; }
        public decimal TotalAmount { get; set; }
        public int TransactionCount { get; set; }
    }

    public class CreditCardTransactionResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string CreditCardId { get; set; } = string.Empty;
        public string InvoiceId { get; set; } = string.Empty;
        public string? CategoryId { get; set; }
        public string CategoryName { get; set; } = "Sem categoria";
        public string Description { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public DateTime Date { get; set; }
        public int InstallmentNumber { get; set; }
        public int TotalInstallments { get; set; }
        public string? InstallmentGroupId { get; set; }
    }
}
