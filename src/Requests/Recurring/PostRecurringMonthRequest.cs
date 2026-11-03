namespace api_finances.src.Requests.Recurring;

public class PostRecurringMonthRequest
{
    public string? BankId { get; set; }
    public DateTime? Date { get; set; }
}
