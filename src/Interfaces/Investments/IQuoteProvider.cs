namespace api_finances.src.Interfaces
{
    public record MarketAssetResult(string Symbol, string Name, string Type);
    public record QuoteResult(string Symbol, decimal Price, DateTime QuotedAt);

    public interface IQuoteProvider
    {
        Task<List<MarketAssetResult>> SearchAsync(string query, CancellationToken ct = default);
        Task<QuoteResult?> GetQuoteAsync(string symbol, CancellationToken ct = default);
    }
}
