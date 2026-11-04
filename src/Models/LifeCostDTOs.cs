namespace api_finances.src.Models
{
    public class LifeCostSummaryResponse
    {
        public decimal AverageMonthlyCost { get; set; }
        public decimal AverageEssentialCost { get; set; }
        public decimal AverageDiscretionaryCost { get; set; }
        public decimal ProjectedAnnualCost { get; set; }
        public decimal DailyCost { get; set; }
        public decimal EmergencyReserveMonths { get; set; }
        public int EmergencyReserveTargetMonths { get; set; }
        public decimal EmergencyReserveTargetAmount { get; set; }
        public decimal AutonomyMonths { get; set; }
        public decimal FinancialFreedomNumber { get; set; }
        public decimal FinancialFreedomProgress { get; set; }
        public decimal LiquidAssets { get; set; }
        public decimal NetPatrimony { get; set; }
        public int MonthsConsidered { get; set; }
        public int ConfiguredWindowMonths { get; set; }
        public int UnclassifiedCategoriesCount { get; set; }
        public decimal SafeWithdrawalRate { get; set; }
        public bool IncludeDebtPayments { get; set; }
    }

    public class LifeCostMonthlyPoint
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal TotalExpense { get; set; }
        public decimal EssentialExpense { get; set; }
        public decimal DiscretionaryExpense { get; set; }
        public decimal ExcludedExpense { get; set; }
        public decimal AdjustmentsAmount { get; set; }
        public decimal NetExpense { get; set; }
        public bool IsPartial { get; set; }
    }

    public class LifeCostBreakdownItem
    {
        public string CategoryId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string LifeCostClass { get; set; } = "DISCRETIONARY";
        public string LifeCostRecurrence { get; set; } = "VARIABLE";
        public decimal TotalAmount { get; set; }
        public decimal Percentage { get; set; }
        public int OperationsCount { get; set; }
    }

    public class LifeCostCategoryClassificationItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string LifeCostClass { get; set; } = "DISCRETIONARY";
        public string LifeCostRecurrence { get; set; } = "VARIABLE";
        public bool IsUnclassified { get; set; }
    }
}
