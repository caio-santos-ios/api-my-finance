using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using MongoDB.Driver;

namespace api_finances.src.Works
{
    public class QuotesUpdateWork(
        IServiceProvider serviceProvider,
        ILogger<QuotesUpdateWork> logger
    ) : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(30);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("QuotesUpdateWork inicializado com intervalo de {Interval} minutos.", CheckInterval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (IsMarketOpenNow())
                    {
                        await UpdateMarketQuotesAsync(stoppingToken);
                    }
                    else
                    {
                        logger.LogInformation("Mercado fechado no momento (fora da janela Seg-Sex 10:00-17:30 horário de Brasília). Nenhuma cotação atualizada.");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erro no background worker de atualização de cotações.");
                }

                try
                {
                    await Task.Delay(CheckInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private static bool IsMarketOpenNow()
        {
            DateTime nowUtc = DateTime.UtcNow;
            DateTime nowBrasilia = ConvertToBrasiliaTime(nowUtc);

            if (nowBrasilia.DayOfWeek == DayOfWeek.Saturday || nowBrasilia.DayOfWeek == DayOfWeek.Sunday)
                return false;

            TimeSpan timeOfDay = nowBrasilia.TimeOfDay;
            TimeSpan openTime = new(10, 0, 0);
            TimeSpan closeTime = new(17, 30, 0);

            return timeOfDay >= openTime && timeOfDay <= closeTime;
        }

        private static DateTime ConvertToBrasiliaTime(DateTime utcTime)
        {
            try
            {
                TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz);
            }
            catch
            {
                try
                {
                    TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
                    return TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz);
                }
                catch
                {
                    return utcTime.AddHours(-3);
                }
            }
        }

        private async Task UpdateMarketQuotesAsync(CancellationToken ct)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var quoteProvider = scope.ServiceProvider.GetRequiredService<IQuoteProvider>();

            logger.LogInformation("Iniciando rotina de atualização periódica de cotações...");

            List<InvestmentAsset> marketAssets = await context.InvestmentAssets
                .Find(a => !a.Deleted && (!string.IsNullOrEmpty(a.ExternalSymbol) || a.Source == "MARKET_API"))
                .ToListAsync(ct);

            if (marketAssets.Count == 0)
            {
                logger.LogInformation("Nenhum ativo de mercado cadastrado para atualizar cotações.");
                return;
            }

            var symbols = marketAssets
                .Select(a => !string.IsNullOrEmpty(a.ExternalSymbol) ? a.ExternalSymbol : a.Ticker)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim().ToUpperInvariant())
                .Distinct()
                .ToList();

            logger.LogInformation("Total de {Count} símbolos de mercado para atualizar cotações: {Symbols}", symbols.Count, string.Join(", ", symbols));

            int successCount = 0;
            int errorCount = 0;

            foreach (var symbol in symbols)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    var quote = await quoteProvider.GetQuoteAsync(symbol, ct);
                    if (quote != null && quote.Price > 0)
                    {
                        var quoteFilter = Builders<PriceQuote>.Filter.Eq(x => x.Symbol, quote.Symbol);
                        var quoteUpdate = Builders<PriceQuote>.Update
                            .Set(x => x.Price, quote.Price)
                            .Set(x => x.QuotedAt, quote.QuotedAt)
                            .Set(x => x.FetchedAt, DateTime.UtcNow)
                            .Set(x => x.Provider, "brapi")
                            .SetOnInsert(x => x.Symbol, quote.Symbol);

                        await context.PriceQuotes.UpdateOneAsync(quoteFilter, quoteUpdate, new UpdateOptions { IsUpsert = true }, ct);

                        var assetFilter = Builders<InvestmentAsset>.Filter.Eq(a => a.Deleted, false) &
                                         (Builders<InvestmentAsset>.Filter.Eq(a => a.ExternalSymbol, quote.Symbol) |
                                          Builders<InvestmentAsset>.Filter.Eq(a => a.Ticker, quote.Symbol));

                        var assetUpdate = Builders<InvestmentAsset>.Update
                            .Set(a => a.ManualPrice, quote.Price)
                            .Set(a => a.LastQuoteAt, quote.QuotedAt)
                            .Set(a => a.UpdatedAt, DateTime.UtcNow);

                        await context.InvestmentAssets.UpdateManyAsync(assetFilter, assetUpdate, cancellationToken: ct);

                        successCount++;
                    }
                    else
                    {
                        logger.LogWarning("Não foi possível obter cotação para o símbolo {Symbol}. Mantendo último preço.", symbol);
                        errorCount++;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Falha ao atualizar cotação do ativo {Symbol}. Mantendo último preço sem interromper.", symbol);
                    errorCount++;
                }
            }

            logger.LogInformation("Atualização de cotações concluída. Sucessos: {Success}, Falhas: {Errors}", successCount, errorCount);
        }
    }
}
