namespace api_finances.src.Requests
{
    public class UpdateGoalRequest
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public DateTime? TargetDate { get; set; }
        public string? Icon { get; set; }
        public string? Color { get; set; }
        public string? Status { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
