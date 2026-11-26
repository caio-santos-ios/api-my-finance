namespace api_finances.src.Shared.DTOs
{
    public class ProjectedBalanceLowestPointDto
    {
        public int Day { get; set; }
        public string Date { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public bool IsDeficit { get; set; }
    }

    public class ProjectedBalanceTimelineItemDto
    {
        public int Day { get; set; }
        public string Date { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public List<string> Events { get; set; } = [];
    }

    public class ProjectedBalancePendingItemDto
    {
        public string Description { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string Type { get; set; } = "expense"; // "revenue" | "expense"
        public int DueDay { get; set; }
        public string Source { get; set; } = string.Empty; // "recurring" | "debt" | "operation"
    }

    public class ProjectedBalanceDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal PendingRevenues { get; set; }
        public decimal PendingExpenses { get; set; }
        public decimal ProjectedEndBalance { get; set; }
        public string Status { get; set; } = "positive"; // "positive" | "negative"
        public ProjectedBalanceLowestPointDto LowestPoint { get; set; } = new();
        public List<ProjectedBalanceTimelineItemDto> DailyTimeline { get; set; } = [];
        public List<ProjectedBalancePendingItemDto> PendingItems { get; set; } = [];
    }
}
