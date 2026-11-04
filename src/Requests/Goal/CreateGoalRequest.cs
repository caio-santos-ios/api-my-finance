namespace api_finances.src.Requests
{
    public class CreateGoalRequest
    {
        public string Name { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public decimal? InitialAmount { get; set; }
        public DateTime? TargetDate { get; set; }
        public string? Icon { get; set; }
        public string? Color { get; set; }
        public string? CreatedBy { get; set; }
    }
}
