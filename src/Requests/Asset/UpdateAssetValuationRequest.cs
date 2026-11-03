using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests.Asset;

public class UpdateAssetValuationRequest
{
    [Required]
    public string Type { get; set; } = "appreciation";

    public decimal Amount { get; set; }

    public decimal? NewValue { get; set; }

    public DateTime? Date { get; set; }

    public string Note { get; set; } = string.Empty;

    public string UpdatedBy { get; set; } = string.Empty;
}
