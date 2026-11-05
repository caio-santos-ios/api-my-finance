namespace api_finances.src.Requests
{
    public class CreateGoalTransactionRequest
    {
        public string GoalId { get; set; } = string.Empty;
        public string Type { get; set; } = "deposit";
        public decimal Value { get; set; }
        public string? BankId { get; set; }
        public DateTime? Date { get; set; }
        public string? Note { get; set; }
        public string? CreatedBy { get; set; }

        public string? InvestmentAssetId { get; set; }
        public string? BrokerId { get; set; }
        public bool CreateInvestmentTransaction { get; set; } = false;
    }
}
