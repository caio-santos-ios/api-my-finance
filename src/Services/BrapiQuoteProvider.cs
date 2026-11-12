using System.Net.Http.Headers;
using System.Text.Json;
using api_finances.src.Interfaces;

namespace api_finances.src.Services
{
    public class BrapiQuoteProvider : IQuoteProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _token;
        private readonly ILogger<BrapiQuoteProvider> _logger;

        public BrapiQuoteProvider(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<BrapiQuoteProvider> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            if (_httpClient.BaseAddress == null)
            {
                string baseUrl = configuration["Brapi:BaseUrl"] ?? "https://brapi.dev/api/";
                if (!baseUrl.EndsWith('/')) baseUrl += "/";
                _httpClient.BaseAddress = new Uri(baseUrl);
            }

            _token = configuration["Brapi:Token"] 
                ?? Environment.GetEnvironmentVariable("BRAPI_TOKEN") 
                ?? string.Empty;

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MyFinances/1.0");

            if (!string.IsNullOrWhiteSpace(_token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            }
        }

        public async Task<List<MarketAssetResult>> SearchAsync(string query, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                return [];

            try
            {
                string cleanQuery = query.Trim();
                string endpoint = $"quote/list?search={Uri.EscapeDataString(cleanQuery)}";
                if (!string.IsNullOrWhiteSpace(_token))
                {
                    endpoint += $"&token={Uri.EscapeDataString(_token)}";
                }

                using var response = await _httpClient.GetAsync(endpoint, ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Falha ao buscar ativos na brapi ({StatusCode}): {Query}", response.StatusCode, cleanQuery);
                    return [];
                }

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

                List<MarketAssetResult> results = [];
                var root = doc.RootElement;

                if (root.TryGetProperty("stocks", out var stocksElem) && stocksElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in stocksElem.EnumerateArray())
                    {
                        string symbol = GetStringProperty(item, "stock") ?? GetStringProperty(item, "symbol") ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(symbol)) continue;

                        string name = GetStringProperty(item, "name") ?? symbol;
                        string rawType = GetStringProperty(item, "type") ?? string.Empty;
                        string mappedType = MapAssetType(rawType, symbol);

                        results.Add(new MarketAssetResult(
                            Symbol: symbol.ToUpperInvariant().Trim(),
                            Name: name.Trim(),
                            Type: mappedType
                        ));
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro inesperado ao consultar brapi SearchAsync para query '{Query}'.", query);
                return [];
            }
        }

        public async Task<QuoteResult?> GetQuoteAsync(string symbol, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return null;

            try
            {
                string cleanSymbol = symbol.Trim().ToUpperInvariant();
                string endpoint = $"quote/{Uri.EscapeDataString(cleanSymbol)}";
                if (!string.IsNullOrWhiteSpace(_token))
                {
                    endpoint += $"?token={Uri.EscapeDataString(_token)}";
                }

                using var response = await _httpClient.GetAsync(endpoint, ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Falha ao buscar cotação na brapi ({StatusCode}): {Symbol}", response.StatusCode, cleanSymbol);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

                var root = doc.RootElement;
                if (!root.TryGetProperty("results", out var resultsElem) || resultsElem.ValueKind != JsonValueKind.Array)
                {
                    return null;
                }

                var firstResult = resultsElem.EnumerateArray().FirstOrDefault();
                if (firstResult.ValueKind == JsonValueKind.Undefined)
                {
                    return null;
                }

                decimal price = 0;
                if (firstResult.TryGetProperty("regularMarketPrice", out var priceElem) && priceElem.ValueKind == JsonValueKind.Number)
                {
                    price = priceElem.GetDecimal();
                }
                else if (firstResult.TryGetProperty("close", out var closeElem) && closeElem.ValueKind == JsonValueKind.Number)
                {
                    price = closeElem.GetDecimal();
                }
                else if (firstResult.TryGetProperty("regularMarketPreviousClose", out var prevCloseElem) && prevCloseElem.ValueKind == JsonValueKind.Number)
                {
                    price = prevCloseElem.GetDecimal();
                }

                DateTime quotedAt = DateTime.UtcNow;
                if (firstResult.TryGetProperty("regularMarketTime", out var timeElem))
                {
                    if (timeElem.ValueKind == JsonValueKind.String && DateTime.TryParse(timeElem.GetString(), out var parsedDate))
                    {
                        quotedAt = parsedDate.ToUniversalTime();
                    }
                    else if (timeElem.ValueKind == JsonValueKind.Number && timeElem.TryGetInt64(out long unixSecs))
                    {
                        quotedAt = unixSecs > 1000000000000L
                            ? DateTimeOffset.FromUnixTimeMilliseconds(unixSecs).UtcDateTime
                            : DateTimeOffset.FromUnixTimeSeconds(unixSecs).UtcDateTime;
                    }
                }

                string returnedSymbol = GetStringProperty(firstResult, "symbol") ?? cleanSymbol;

                return new QuoteResult(
                    Symbol: returnedSymbol.ToUpperInvariant().Trim(),
                    Price: price,
                    QuotedAt: quotedAt
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro inesperado ao buscar cotação na brapi para symbol '{Symbol}'.", symbol);
                return null;
            }
        }

        private static string? GetStringProperty(JsonElement elem, string propName)
        {
            if (elem.TryGetProperty(propName, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                return prop.GetString();
            }
            return null;
        }

        private static string MapAssetType(string rawType, string symbol)
        {
            string clean = rawType.Trim().ToLowerInvariant();
            if (clean.Contains("fund") || clean.Contains("fii") || clean.Contains("imobili"))
                return "FII";
            if (clean.Contains("bdr"))
                return "BDR";
            if (clean.Contains("etf"))
                return "ETF";
            if (clean.Contains("stock") || clean.Contains("acao") || clean.Contains("ação"))
                return "STOCK";

            string sym = symbol.Trim().ToUpperInvariant();
            if (sym.EndsWith("11B") || (sym.EndsWith("11") && !sym.StartsWith("BOVA") && !sym.StartsWith("SMAL") && !sym.StartsWith("IVVB")))
                return "FII";
            if (sym.EndsWith("34") || sym.EndsWith("35") || sym.EndsWith("33"))
                return "BDR";
            if (sym.StartsWith("BOVA") || sym.StartsWith("SMAL") || sym.StartsWith("IVVB") || sym.StartsWith("HASH") || sym.StartsWith("XINA"))
                return "ETF";

            return "STOCK";
        }
    }
}
