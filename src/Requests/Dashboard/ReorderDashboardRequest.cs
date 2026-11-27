namespace api_finances.src.Requests.Dashboard
{
    public class ReorderDashboardRequest
    {
        public List<ReorderDashboardItem> Items { get; set; } = [];
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class ReorderDashboardItem
    {
        public string Id { get; set; } = string.Empty;
        public int Sequence { get; set; }
    }
}
