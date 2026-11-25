using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;
using ClosedXML.Excel;
using MongoDB.Driver;

namespace api_finances.src.Services
{
    public class InvestmentService(
        AppDbContext context,
        IInvestmentAssetRepository assetRepository,
        IInvestmentTransactionRepository transactionRepository,
        IOperationRepository operationRepository,
        IQuoteProvider quoteProvider
    ) : IInvestmentService
    {
        public async Task<ResponseApi<List<InvestmentAsset>>> GetAssetsAsync(string userId)
        {
            try
            {
                List<InvestmentAsset> list = await assetRepository.GetAllByUserAsync(userId);
                return new(list, 200, "Ativos listados com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao buscar ativos: {ex.Message}");
            }
        }

        public async Task<ResponseApi<InvestmentAsset?>> GetAssetByIdAsync(string id)
        {
            try
            {
                InvestmentAsset? asset = await assetRepository.GetByIdAsync(id);
                if (asset is null) return new(null, 404, "Ativo não encontrado");
                return new(asset, 200, "Ativo encontrado");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao buscar ativo: {ex.Message}");
            }
        }

        public async Task<ResponseApi<InvestmentAsset?>> CreateAssetAsync(string userId, CreateInvestmentAssetRequest request)
        {
            try
            {
                string assetType = (request.Type ?? "").Trim().ToUpperInvariant();
                string[] variableIncomeTypes = ["STOCK", "FII", "ETF", "BDR", "ACAO", "AÇÃO", "REIT"];
                if (variableIncomeTypes.Contains(assetType))
                {
                    return new(null, 400, "Ativos de renda variável (Ações, FIIs, ETFs e BDRs) não podem ser cadastrados manualmente. Utilize a busca de mercado (/investment-assets/from-market). Apenas Renda Fixa ou Outros podem ser cadastrados manualmente.");
                }

                InvestmentAsset asset = new()
                {
                    UserId = userId,
                    CreatedBy = userId,
                    Ticker = request.Ticker.Trim().ToUpperInvariant(),
                    ExternalSymbol = !string.IsNullOrWhiteSpace(request.Ticker) ? request.Ticker.Trim().ToUpperInvariant() : null,
                    Name = request.Name.Trim(),
                    Type = assetType,
                    Currency = string.IsNullOrWhiteSpace(request.Currency) ? "BRL" : request.Currency.Trim().ToUpperInvariant(),
                    Source = "MANUAL",
                    ManualPrice = request.ManualPrice,
                    MaturityDate = request.MaturityDate,
                    ContractedRate = request.ContractedRate
                };

                InvestmentAsset? created = await assetRepository.CreateAsync(asset);
                return new(created, 201, "Ativo cadastrado com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao criar ativo: {ex.Message}");
            }
        }

        public async Task<ResponseApi<List<MarketAssetSearchResultDto>>> SearchMarketAssetsAsync(string userId, string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
                {
                    return new([], 400, "O termo de busca deve conter no mínimo 2 caracteres.");
                }

                List<InvestmentAsset> userAssets = await assetRepository.GetAllByUserAsync(userId);
                HashSet<string> registeredSymbols = userAssets
                    .Select(a => (!string.IsNullOrEmpty(a.ExternalSymbol) ? a.ExternalSymbol : a.Ticker).Trim().ToUpperInvariant())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToHashSet();

                List<MarketAssetResult> marketResults = await quoteProvider.SearchAsync(query.Trim());

                List<MarketAssetSearchResultDto> results = marketResults.Select(r => new MarketAssetSearchResultDto
                {
                    Symbol = r.Symbol,
                    Name = r.Name,
                    Type = r.Type,
                    AlreadyRegistered = registeredSymbols.Contains(r.Symbol.ToUpperInvariant())
                }).ToList();

                return new(results, 200, "Ativos de mercado buscados com sucesso");
            }
            catch (Exception ex)
            {
                return new([], 500, $"Erro ao buscar ativos de mercado: {ex.Message}");
            }
        }

        public async Task<ResponseApi<MarketAssetCreateResultDto>> CreateFromMarketAsync(string userId, CreateMarketAssetRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Symbol))
                    return new(null, 400, "O símbolo (ticker) é obrigatório.");

                string cleanSymbol = request.Symbol.Trim().ToUpperInvariant();

                List<InvestmentAsset> userAssets = await assetRepository.GetAllByUserAsync(userId);
                InvestmentAsset? existing = userAssets.FirstOrDefault(a =>
                    (!string.IsNullOrEmpty(a.ExternalSymbol) && a.ExternalSymbol.Equals(cleanSymbol, StringComparison.OrdinalIgnoreCase)) ||
                    a.Ticker.Equals(cleanSymbol, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    var quoteInDb = await context.PriceQuotes.Find(q => q.Symbol == cleanSymbol).FirstOrDefaultAsync();
                    return new(new MarketAssetCreateResultDto
                    {
                        AlreadyExisted = true,
                        Asset = existing,
                        CurrentPrice = quoteInDb?.Price > 0 ? quoteInDb.Price : existing.ManualPrice,
                        LastQuoteAt = quoteInDb?.QuotedAt ?? existing.LastQuoteAt
                    }, 200, "Ativo já cadastrado na sua carteira.");
                }

                var quote = await quoteProvider.GetQuoteAsync(cleanSymbol);

                string name = !string.IsNullOrWhiteSpace(request.Name) ? request.Name.Trim() : cleanSymbol;
                string type = !string.IsNullOrWhiteSpace(request.Type) ? request.Type.Trim().ToUpperInvariant() : "STOCK";

                if (name.Equals(cleanSymbol, StringComparison.OrdinalIgnoreCase))
                {
                    var search = await quoteProvider.SearchAsync(cleanSymbol);
                    var matched = search.FirstOrDefault(s => s.Symbol.Equals(cleanSymbol, StringComparison.OrdinalIgnoreCase));
                    if (matched != null)
                    {
                        if (!string.IsNullOrWhiteSpace(matched.Name)) name = matched.Name;
                        if (string.IsNullOrWhiteSpace(request.Type) || request.Type == "STOCK") type = matched.Type;
                    }
                }

                decimal price = quote?.Price ?? 0;
                DateTime? quotedAt = quote?.QuotedAt;

                InvestmentAsset asset = new()
                {
                    UserId = userId,
                    CreatedBy = userId,
                    Ticker = cleanSymbol,
                    ExternalSymbol = cleanSymbol,
                    Name = name,
                    Type = type,
                    Currency = "BRL",
                    Source = "MARKET_API",
                    ManualPrice = price,
                    LastQuoteAt = quotedAt,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                InvestmentAsset? created = await assetRepository.CreateAsync(asset);

                if (quote != null && quote.Price > 0)
                {
                    var filter = Builders<PriceQuote>.Filter.Eq(x => x.Symbol, cleanSymbol);
                    var update = Builders<PriceQuote>.Update
                        .Set(x => x.Price, quote.Price)
                        .Set(x => x.QuotedAt, quote.QuotedAt)
                        .Set(x => x.FetchedAt, DateTime.UtcNow)
                        .Set(x => x.Provider, "brapi")
                        .SetOnInsert(x => x.Symbol, cleanSymbol);

                    await context.PriceQuotes.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
                }

                return new(new MarketAssetCreateResultDto
                {
                    AlreadyExisted = false,
                    Asset = created,
                    CurrentPrice = price > 0 ? price : null,
                    LastQuoteAt = quotedAt
                }, 201, "Ativo de mercado criado com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao criar ativo de mercado: {ex.Message}");
            }
        }

        public async Task<ResponseApi<InvestmentAsset?>> UpdateAssetAsync(string userId, UpdateInvestmentAssetRequest request)
        {
            try
            {
                InvestmentAsset? existed = await assetRepository.GetByIdAsync(request.Id);
                if (existed is null || existed.UserId != userId && existed.CreatedBy != userId)
                    return new(null, 404, "Ativo não encontrado");

                existed.Ticker = request.Ticker.Trim().ToUpperInvariant();
                existed.Name = request.Name.Trim();
                existed.Type = request.Type.Trim().ToUpperInvariant();
                existed.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "BRL" : request.Currency.Trim().ToUpperInvariant();
                existed.ManualPrice = request.ManualPrice;
                existed.MaturityDate = request.MaturityDate;
                existed.ContractedRate = request.ContractedRate;

                InvestmentAsset? updated = await assetRepository.UpdateAsync(existed);
                return new(updated, 200, "Ativo atualizado com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao atualizar ativo: {ex.Message}");
            }
        }

        public async Task<ResponseApi<bool>> DeleteAssetAsync(string userId, string id)
        {
            try
            {
                InvestmentAsset? existed = await assetRepository.GetByIdAsync(id);
                if (existed is null || existed.UserId != userId && existed.CreatedBy != userId)
                    return new(false, 404, "Ativo não encontrado");

                List<InvestmentTransaction> txs = await transactionRepository.GetByAssetIdAsync(id);
                if (txs.Count > 0)
                    return new(false, 400, "Não é possível excluir um ativo que possui movimentações registradas");

                await assetRepository.DeleteAsync(existed);
                return new(true, 200, "Ativo excluído com sucesso");
            }
            catch (Exception ex)
            {
                return new(false, 500, $"Erro ao excluir ativo: {ex.Message}");
            }
        }

        public async Task<ResponseApi<List<InvestmentTransactionResponse>>> GetTransactionsAsync(string userId, string? assetId = null, string? brokerId = null, string? kind = null, DateTime? start = null, DateTime? end = null)
        {
            try
            {
                List<InvestmentTransaction> transactions = await transactionRepository.GetAllByUserAsync(userId, assetId, brokerId, kind, start, end);
                List<InvestmentAsset> assets = await assetRepository.GetAllByUserAsync(userId);
                Dictionary<string, InvestmentAsset> assetMap = assets.ToDictionary(a => a.Id, a => a);

                List<Bank> banks = await context.Banks.Find(b => b.CreatedBy == userId && !b.Deleted).ToListAsync();
                Dictionary<string, Bank> bankMap = banks.ToDictionary(b => b.Id, b => b);

                List<InvestmentTransactionResponse> response = [];

                foreach (InvestmentTransaction tx in transactions)
                {
                    assetMap.TryGetValue(tx.InvestmentAssetId, out InvestmentAsset? asset);
                    bankMap.TryGetValue(tx.BrokerId, out Bank? bank);

                    response.Add(new InvestmentTransactionResponse
                    {
                        Id = tx.Id,
                        InvestmentAssetId = tx.InvestmentAssetId,
                        AssetTicker = asset?.Ticker ?? "",
                        AssetName = asset?.Name ?? "Ativo Desconhecido",
                        AssetType = asset?.Type ?? "STOCK",
                        BrokerId = tx.BrokerId,
                        BrokerName = bank?.Name ?? "Corretora",
                        Kind = tx.Kind,
                        Date = tx.Date,
                        Quantity = tx.Quantity,
                        UnitPrice = tx.UnitPrice,
                        Fees = tx.Fees,
                        Amount = tx.Amount,
                        Notes = tx.Notes
                    });
                }

                return new(response, 200, "Movimentações obtidas com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao buscar movimentações: {ex.Message}");
            }
        }

        public async Task<ResponseApi<InvestmentTransaction?>> CreateTransactionAsync(string userId, CreateInvestmentTransactionRequest request)
        {
            try
            {
                InvestmentAsset? asset = await assetRepository.GetByIdAsync(request.InvestmentAssetId);
                if (asset is null) return new(null, 404, "Ativo não encontrado");

                string kind = request.Kind.Trim().ToUpperInvariant();
                decimal qty = request.Quantity;
                decimal unitPrice = request.UnitPrice;
                decimal fees = request.Fees;
                decimal amount = request.Amount;

                if (kind == "BUY" || kind == "SELL")
                {
                    if (qty <= 0) return new(null, 400, "Quantidade deve ser maior que zero");
                    if (unitPrice < 0) return new(null, 400, "Preço unitário deve ser maior ou igual a zero");
                    if (amount <= 0)
                    {
                        amount = kind == "BUY" ? (qty * unitPrice) + fees : Math.Max(0, (qty * unitPrice) - fees);
                    }
                }
                else
                {
                    if (amount <= 0) return new(null, 400, "Valor do provento deve ser maior que zero");
                }

                List<InvestmentTransaction> existingTxs = await transactionRepository.GetByAssetIdAsync(asset.Id);

                InvestmentTransaction newTx = new()
                {
                    UserId = userId,
                    CreatedBy = userId,
                    InvestmentAssetId = asset.Id,
                    BrokerId = request.BrokerId,
                    Kind = kind,
                    Date = request.Date,
                    Quantity = qty,
                    UnitPrice = unitPrice,
                    Fees = fees,
                    Amount = amount,
                    Notes = request.Notes,
                    CreateOperation = request.CreateOperation
                };

                List<InvestmentTransaction> simulated = new(existingTxs) { newTx };
                if (!ValidateQuantityBalance(simulated))
                {
                    return new(null, 400, "Operação inválida: a quantidade vendida é superior ao saldo disponível na data especificada");
                }

                InvestmentTransaction? created = await transactionRepository.CreateAsync(newTx);

                if (created != null && request.CreateOperation && !string.IsNullOrEmpty(request.BrokerId))
                {
                    await SyncLinkedOperationAsync(userId, created, asset);
                }

                return new(created, 201, "Movimentação registrada com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao registrar movimentação: {ex.Message}");
            }
        }

        public async Task<ResponseApi<InvestmentTransaction?>> UpdateTransactionAsync(string userId, UpdateInvestmentTransactionRequest request)
        {
            try
            {
                InvestmentTransaction? tx = await transactionRepository.GetByIdAsync(request.Id);
                if (tx is null || tx.UserId != userId && tx.CreatedBy != userId)
                    return new(null, 404, "Movimentação não encontrada");

                InvestmentAsset? asset = await assetRepository.GetByIdAsync(request.InvestmentAssetId);
                if (asset is null) return new(null, 404, "Ativo não encontrado");

                string kind = request.Kind.Trim().ToUpperInvariant();
                decimal qty = request.Quantity;
                decimal unitPrice = request.UnitPrice;
                decimal fees = request.Fees;
                decimal amount = request.Amount;

                if (kind == "BUY" || kind == "SELL")
                {
                    if (qty <= 0) return new(null, 400, "Quantidade deve ser maior que zero");
                    if (unitPrice < 0) return new(null, 400, "Preço unitário deve ser maior ou igual a zero");
                    if (amount <= 0)
                    {
                        amount = kind == "BUY" ? (qty * unitPrice) + fees : Math.Max(0, (qty * unitPrice) - fees);
                    }
                }

                List<InvestmentTransaction> existingTxs = await transactionRepository.GetByAssetIdAsync(asset.Id);
                List<InvestmentTransaction> simulated = existingTxs.Where(t => t.Id != tx.Id).ToList();

                InvestmentTransaction updatedCandidate = new()
                {
                    Id = tx.Id,
                    UserId = userId,
                    InvestmentAssetId = asset.Id,
                    BrokerId = request.BrokerId,
                    Kind = kind,
                    Date = request.Date,
                    Quantity = qty,
                    UnitPrice = unitPrice,
                    Fees = fees,
                    Amount = amount,
                    CreatedAt = tx.CreatedAt
                };
                simulated.Add(updatedCandidate);

                if (!ValidateQuantityBalance(simulated))
                {
                    return new(null, 400, "Edição inválida: resultaria em saldo negativo em vendas posteriores");
                }

                tx.InvestmentAssetId = asset.Id;
                tx.BrokerId = request.BrokerId;
                tx.Kind = kind;
                tx.Date = request.Date;
                tx.Quantity = qty;
                tx.UnitPrice = unitPrice;
                tx.Fees = fees;
                tx.Amount = amount;
                tx.Notes = request.Notes;
                tx.CreateOperation = request.CreateOperation;

                InvestmentTransaction? updated = await transactionRepository.UpdateAsync(tx);

                if (updated != null && !string.IsNullOrEmpty(tx.OperationId))
                {
                    await RemoveLinkedOperationAsync(tx.OperationId);
                    tx.OperationId = null;
                }

                if (updated != null && request.CreateOperation && !string.IsNullOrEmpty(request.BrokerId))
                {
                    await SyncLinkedOperationAsync(userId, updated, asset);
                }

                return new(updated, 200, "Movimentação atualizada com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao atualizar movimentação: {ex.Message}");
            }
        }

        public async Task<ResponseApi<bool>> DeleteTransactionAsync(string userId, string id)
        {
            try
            {
                InvestmentTransaction? tx = await transactionRepository.GetByIdAsync(id);
                if (tx is null || tx.UserId != userId && tx.CreatedBy != userId)
                    return new(false, 404, "Movimentação não encontrada");

                List<InvestmentTransaction> existingTxs = await transactionRepository.GetByAssetIdAsync(tx.InvestmentAssetId);
                List<InvestmentTransaction> simulated = existingTxs.Where(t => t.Id != tx.Id).ToList();

                if (!ValidateQuantityBalance(simulated))
                {
                    return new(false, 400, "Exclusão inválida: deixaria uma venda posterior sem saldo de cotas");
                }

                if (!string.IsNullOrEmpty(tx.OperationId))
                {
                    await RemoveLinkedOperationAsync(tx.OperationId);
                }

                await transactionRepository.DeleteAsync(tx);
                return new(true, 200, "Movimentação excluída com sucesso");
            }
            catch (Exception ex)
            {
                return new(false, 500, $"Erro ao excluir movimentação: {ex.Message}");
            }
        }

        public async Task<ResponseApi<PortfolioResponse>> GetPortfolioAsync(string userId)
        {
            try
            {
                List<InvestmentAsset> assets = await assetRepository.GetAllByUserAsync(userId);
                List<InvestmentTransaction> allTxs = await transactionRepository.GetAllByUserAsync(userId);

                List<Bank> banks = await context.Banks.Find(b => b.CreatedBy == userId && !b.Deleted).ToListAsync();
                Dictionary<string, Bank> bankMap = banks.ToDictionary(b => b.Id, b => b);

                List<PriceQuote> quotes = await context.PriceQuotes.Find(_ => true).ToListAsync();
                Dictionary<string, PriceQuote> quoteMap = quotes
                    .Where(q => !string.IsNullOrEmpty(q.Symbol))
                    .GroupBy(q => q.Symbol.ToUpperInvariant())
                    .ToDictionary(g => g.Key, g => g.First());

                List<InvestmentPositionDTO> positions = [];
                decimal portfolioInvested = 0;
                decimal portfolioCurrentValue = 0;
                decimal portfolioIncome = 0;

                foreach (InvestmentAsset asset in assets)
                {
                    List<InvestmentTransaction> assetTxs = allTxs
                        .Where(t => t.InvestmentAssetId == asset.Id)
                        .OrderBy(t => t.Date)
                        .ThenBy(t => t.CreatedAt)
                        .ToList();

                    if (assetTxs.Count == 0) continue;

                    decimal currentQty = 0;
                    decimal averagePrice = 0;
                    decimal totalBoughtQty = 0;
                    decimal totalBoughtCost = 0;
                    decimal incomeReceived = 0;
                    HashSet<string> brokerIds = [];

                    foreach (InvestmentTransaction tx in assetTxs)
                    {
                        if (!string.IsNullOrEmpty(tx.BrokerId)) brokerIds.Add(tx.BrokerId);

                        if (tx.Kind == "BUY")
                        {
                            decimal txCost = (tx.Quantity * tx.UnitPrice) + tx.Fees;
                            decimal prevCost = currentQty * averagePrice;
                            currentQty += tx.Quantity;
                            averagePrice = currentQty > 0 ? (prevCost + txCost) / currentQty : 0;
                            totalBoughtQty += tx.Quantity;
                            totalBoughtCost += txCost;
                        }
                        else if (tx.Kind == "SELL")
                        {
                            currentQty -= tx.Quantity;
                            if (currentQty <= 0)
                            {
                                currentQty = 0;
                                averagePrice = 0;
                            }
                        }
                        else if (tx.Kind == "DIVIDEND" || tx.Kind == "JCP" || tx.Kind == "INCOME")
                        {
                            incomeReceived += tx.Amount;
                        }
                    }

                    if (currentQty <= 0 && incomeReceived <= 0) continue;

                    decimal totalInvested = Math.Round(currentQty * averagePrice, 2);
                    decimal lastPrice = averagePrice;
                    bool hasQuote = false;
                    bool isEstimated = false;

                    string symbolKey = (!string.IsNullOrEmpty(asset.ExternalSymbol) ? asset.ExternalSymbol : asset.Ticker).Trim().ToUpperInvariant();
                    quoteMap.TryGetValue(symbolKey, out var pq);

                    if (pq != null && pq.Price > 0)
                    {
                        lastPrice = pq.Price;
                        hasQuote = true;
                    }
                    else if (asset.ManualPrice > 0)
                    {
                        lastPrice = asset.ManualPrice;
                        hasQuote = true;
                    }
                    else if (asset.Type == "FIXED_INCOME" && asset.ContractedRate.HasValue && asset.ContractedRate.Value > 0)
                    {
                        DateTime firstBuy = assetTxs.Where(t => t.Kind == "BUY").Select(t => t.Date).DefaultIfEmpty(DateTime.UtcNow).Min();
                        double days = Math.Max(0, (DateTime.UtcNow - firstBuy).TotalDays);
                        double rate = (double)asset.ContractedRate.Value;
                        double factor = Math.Pow(1.0 + rate, days / 365.0);
                        lastPrice = Math.Round(averagePrice * (decimal)factor, 4);
                        isEstimated = true;
                        hasQuote = true;
                    }

                    decimal currentValue = Math.Round(currentQty * lastPrice, 2);
                    decimal unrealizedProfit = currentValue - totalInvested;
                    decimal profitPercent = totalInvested > 0 ? Math.Round((unrealizedProfit / totalInvested) * 100m, 2) : 0;
                    decimal totalReturn = unrealizedProfit + incomeReceived;
                    decimal totalReturnPercent = totalInvested > 0 ? Math.Round((totalReturn / totalInvested) * 100m, 2) : 0;

                    List<string> brokerNames = brokerIds
                        .Where(id => bankMap.ContainsKey(id))
                        .Select(id => bankMap[id].Name)
                        .ToList();

                    positions.Add(new InvestmentPositionDTO
                    {
                        InvestmentAssetId = asset.Id,
                        Ticker = asset.Ticker,
                        Name = asset.Name,
                        Type = asset.Type,
                        Quantity = currentQty,
                        AveragePrice = Math.Round(averagePrice, 4),
                        LastPrice = lastPrice,
                        TotalInvested = totalInvested,
                        CurrentValue = currentValue,
                        UnrealizedProfit = unrealizedProfit,
                        ProfitPercent = profitPercent,
                        IncomeReceived = incomeReceived,
                        TotalReturn = totalReturn,
                        TotalReturnPercent = totalReturnPercent,
                        HasQuote = hasQuote,
                        IsEstimated = isEstimated,
                        BrokerNames = brokerNames
                    });

                    portfolioInvested += totalInvested;
                    portfolioCurrentValue += currentValue;
                    portfolioIncome += incomeReceived;
                }

                decimal portfolioProfit = portfolioCurrentValue - portfolioInvested;
                decimal portfolioProfitPct = portfolioInvested > 0 ? Math.Round((portfolioProfit / portfolioInvested) * 100m, 2) : 0;
                decimal portfolioTotalReturn = portfolioProfit + portfolioIncome;
                decimal portfolioTotalReturnPct = portfolioInvested > 0 ? Math.Round((portfolioTotalReturn / portfolioInvested) * 100m, 2) : 0;

                PortfolioResponse portfolio = new()
                {
                    TotalInvested = portfolioInvested,
                    CurrentValue = portfolioCurrentValue,
                    UnrealizedProfit = portfolioProfit,
                    UnrealizedProfitPercent = portfolioProfitPct,
                    IncomeReceived = portfolioIncome,
                    TotalReturn = portfolioTotalReturn,
                    TotalReturnPercent = portfolioTotalReturnPct,
                    Positions = positions.OrderByDescending(p => p.CurrentValue).ToList()
                };

                return new(portfolio, 200, "Carteira obtida com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao calcular carteira de investimentos: {ex.Message}");
            }
        }

        private static bool ValidateQuantityBalance(List<InvestmentTransaction> transactions)
        {
            List<InvestmentTransaction> ordered = transactions.OrderBy(t => t.Date).ThenBy(t => t.CreatedAt).ToList();
            decimal running = 0;
            foreach (InvestmentTransaction tx in ordered)
            {
                if (tx.Kind == "BUY") running += tx.Quantity;
                else if (tx.Kind == "SELL")
                {
                    running -= tx.Quantity;
                    if (running < 0) return false;
                }
            }
            return true;
        }

        private async Task SyncLinkedOperationAsync(string userId, InvestmentTransaction tx, InvestmentAsset asset)
        {
            try
            {
                string type = tx.Kind == "BUY" ? "expense" : "income";
                string descPrefix = tx.Kind == "BUY" ? "Compra" : tx.Kind == "SELL" ? "Venda" : "Provento";
                string label = !string.IsNullOrWhiteSpace(asset.Ticker) ? asset.Ticker : asset.Name;

                Operation op = new()
                {
                    CreatedBy = userId,
                    BankId = tx.BrokerId,
                    Type = type,
                    Value = tx.Amount,
                    Description = $"{descPrefix} de Investimento ({label})",
                    Origin = "investment",
                    OriginId = tx.Id,
                    CreatedAt = tx.Date
                };

                Operation? created = await operationRepository.CreateAsync(op);
                if (created != null)
                {
                    tx.OperationId = created.Id;
                    await transactionRepository.UpdateAsync(tx);
                }
            }
            catch { }
        }

        private async Task RemoveLinkedOperationAsync(string operationId)
        {
            try
            {
                Operation? op = await operationRepository.GetByIdAsync(operationId);
                if (op != null)
                {
                    await operationRepository.DeleteAsync(op);
                }
            }
            catch { }
        }

        public async Task<ResponseApi<B3PreviewResponseDto>> PreviewB3ImportAsync(string userId, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return new(null, 400, "Arquivo inválido ou vazio.");

                string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext != ".csv" && ext != ".xlsx" && ext != ".xls")
                    return new(null, 400, "Formato de arquivo não suportado. Envie uma planilha da B3 em formato .csv ou .xlsx.");

                var rawRows = await ParseB3FileAsync(file, ext);
                if (rawRows.Count == 0)
                    return new(null, 400, "Nenhuma linha válida de movimentação foi encontrada no arquivo da B3.");

                // Linhas idênticas na planilha são operações legítimas distintas (ex.: duas compras iguais no mesmo dia).
                // Cada ocorrência recebe um hash próprio (1ª = hash base, n-ésima = hash(base#n)),
                // mantendo a idempotência ao reimportar o mesmo arquivo.
                var occurrences = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var raw in rawRows)
                {
                    int n = occurrences.TryGetValue(raw.ImportHash, out int c) ? c + 1 : 1;
                    occurrences[raw.ImportHash] = n;
                    if (n > 1) raw.ImportHash = ComputeSha256($"{raw.ImportHash}#{n}");
                }

                var hashes = rawRows.Select(r => r.ImportHash).Distinct().ToList();
                var existingHashes = await transactionRepository.GetExistingImportHashesAsync(userId, hashes);
                var existingSet = new HashSet<string>(existingHashes, StringComparer.OrdinalIgnoreCase);

                List<B3ImportRowDto> rows = [];

                foreach (var raw in rawRows)
                {
                    bool isDuplicate = false;
                    string? warningMsg = raw.WarningMessage;

                    if (existingSet.Contains(raw.ImportHash))
                    {
                        isDuplicate = true;
                        warningMsg = "Movimentação já cadastrada no sistema.";
                    }

                    rows.Add(new B3ImportRowDto
                    {
                        Ticker = raw.Ticker,
                        Name = raw.Name,
                        Type = raw.Type,
                        Kind = raw.Kind,
                        Date = raw.Date,
                        BrokerName = raw.BrokerName,
                        Quantity = raw.Quantity,
                        UnitPrice = raw.UnitPrice,
                        TotalValue = raw.TotalValue,
                        ImportHash = raw.ImportHash,
                        IsDuplicate = isDuplicate,
                        HasWarning = raw.HasWarning,
                        WarningMessage = warningMsg
                    });
                }

                int total = rows.Count;
                int duplicates = rows.Count(r => r.IsDuplicate);
                int warnings = rows.Count(r => r.HasWarning && !r.IsDuplicate);

                var result = new B3PreviewResponseDto
                {
                    TotalRows = total,
                    NewRows = total - duplicates,
                    DuplicateRows = duplicates,
                    WarningRows = warnings,
                    Rows = rows
                };

                return new(result, 200, "Planilha processada com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao processar planilha da B3: {ex.Message}");
            }
        }

        public async Task<ResponseApi<B3ConfirmImportResponseDto>> ConfirmB3ImportAsync(string userId, B3ConfirmImportRequest request)
        {
            try
            {
                if (request?.Items == null || request.Items.Count == 0)
                    return new(null, 400, "Nenhuma movimentação selecionada para importação.");

                var distinctItems = request.Items
                    .GroupBy(x => x.ImportHash, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();

                var hashes = distinctItems.Select(x => x.ImportHash).ToList();
                var existingHashes = await transactionRepository.GetExistingImportHashesAsync(userId, hashes);
                var existingSet = new HashSet<string>(existingHashes, StringComparer.OrdinalIgnoreCase);

                var itemsToImport = distinctItems.Where(x => !existingSet.Contains(x.ImportHash)).ToList();
                int skipped = request.Items.Count - itemsToImport.Count;

                if (itemsToImport.Count == 0)
                {
                    return new(new B3ConfirmImportResponseDto
                    {
                        ImportedCount = 0,
                        SkippedCount = skipped,
                        CreatedAssetTickers = []
                    }, 200, "Todas as movimentações selecionadas já haviam sido importadas anteriormente.");
                }

                List<InvestmentAsset> userAssets = await assetRepository.GetAllByUserAsync(userId);
                var assetMap = new Dictionary<string, InvestmentAsset>(StringComparer.OrdinalIgnoreCase);
                foreach (var a in userAssets)
                {
                    if (!string.IsNullOrWhiteSpace(a.Ticker)) assetMap[a.Ticker] = a;
                    if (!string.IsNullOrWhiteSpace(a.ExternalSymbol)) assetMap[a.ExternalSymbol] = a;
                }

                List<Bank> userBanks = await context.Banks.Find(b => b.CreatedBy == userId && !b.Deleted).ToListAsync();
                List<string> createdTickers = [];
                List<InvestmentTransaction> transactions = [];
                string batchId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();

                foreach (var item in itemsToImport)
                {
                    string cleanTicker = item.Ticker.Trim().ToUpperInvariant();

                    if (!assetMap.TryGetValue(cleanTicker, out InvestmentAsset? asset))
                    {
                        string assetType = !string.IsNullOrWhiteSpace(item.Type) ? item.Type.Trim().ToUpperInvariant() : InferAssetType(cleanTicker);
                        bool isTrade = item.Kind.Equals("BUY", StringComparison.OrdinalIgnoreCase) || item.Kind.Equals("SELL", StringComparison.OrdinalIgnoreCase);

                        asset = new InvestmentAsset
                        {
                            UserId = userId,
                            CreatedBy = userId,
                            Ticker = cleanTicker,
                            ExternalSymbol = cleanTicker,
                            Name = !string.IsNullOrWhiteSpace(item.Name) ? item.Name.Trim() : cleanTicker,
                            Type = assetType,
                            Currency = "BRL",
                            Source = assetType == "FIXED_INCOME" ? "MANUAL" : "MARKET_API",
                            ManualPrice = isTrade ? item.UnitPrice : 0,
                            ImportBatchId = batchId,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        await assetRepository.CreateAsync(asset);
                        assetMap[cleanTicker] = asset;
                        createdTickers.Add(cleanTicker);
                    }

                    Bank broker = await ResolveBrokerAsync(userId, userBanks, item.BrokerName);

                    decimal amount = item.TotalValue > 0
                        ? item.TotalValue
                        : Math.Round(item.Quantity * item.UnitPrice, 2);

                    transactions.Add(new InvestmentTransaction
                    {
                        UserId = userId,
                        CreatedBy = userId,
                        InvestmentAssetId = asset.Id,
                        BrokerId = broker.Id,
                        Kind = item.Kind.Trim().ToUpperInvariant(),
                        Date = DateTime.SpecifyKind(item.Date.Date, DateTimeKind.Utc),
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Amount = amount,
                        Fees = 0,
                        Notes = "Importado da B3",
                        ImportHash = item.ImportHash,
                        ImportBatchId = batchId,
                        CreateOperation = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                if (transactions.Count > 0)
                {
                    await context.InvestmentTransactions.InsertManyAsync(transactions);
                }

                var response = new B3ConfirmImportResponseDto
                {
                    BatchId = batchId,
                    ImportedCount = transactions.Count,
                    SkippedCount = skipped,
                    CreatedAssetTickers = createdTickers.Distinct().ToList()
                };

                return new(response, 201, $"{transactions.Count} movimentação(ões) importada(s) com sucesso!");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao confirmar importação da B3: {ex.Message}");
            }
        }

        public async Task<ResponseApi<List<B3ImportBatchDto>>> GetB3ImportBatchesAsync(string userId)
        {
            try
            {
                var txs = await context.InvestmentTransactions
                    .Find(x => x.UserId == userId && !x.Deleted && x.ImportBatchId != null && x.ImportBatchId != "")
                    .ToListAsync();

                var assetCounts = (await context.InvestmentAssets
                    .Find(x => x.UserId == userId && !x.Deleted && x.ImportBatchId != null && x.ImportBatchId != "")
                    .ToListAsync())
                    .GroupBy(a => a.ImportBatchId!)
                    .ToDictionary(g => g.Key, g => g.Count());

                var batches = txs
                    .GroupBy(t => t.ImportBatchId!)
                    .Select(g => new B3ImportBatchDto
                    {
                        BatchId = g.Key,
                        ImportedAt = g.Min(t => t.CreatedAt),
                        TransactionCount = g.Count(),
                        AssetCount = assetCounts.TryGetValue(g.Key, out int ac) ? ac : 0,
                        FirstDate = g.Min(t => t.Date),
                        LastDate = g.Max(t => t.Date)
                    })
                    .OrderByDescending(b => b.ImportedAt)
                    .ToList();

                return new(batches, 200, "Importações listadas com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao listar importações: {ex.Message}");
            }
        }

        public async Task<ResponseApi<B3RevertImportResponseDto>> RevertB3ImportAsync(string userId, string batchId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(batchId))
                    return new(null, 400, "Importação inválida.");

                DateTime now = DateTime.UtcNow;

                var txResult = await context.InvestmentTransactions.UpdateManyAsync(
                    x => x.UserId == userId && !x.Deleted && x.ImportBatchId == batchId,
                    Builders<InvestmentTransaction>.Update
                        .Set(x => x.Deleted, true)
                        .Set(x => x.DeletedAt, now)
                        .Set(x => x.DeletedBy, userId));

                if (txResult.ModifiedCount == 0)
                    return new(null, 404, "Importação não encontrada ou já revertida.");

                // Remove os ativos criados por esta importação que não possuem mais movimentações ativas
                var batchAssets = await context.InvestmentAssets
                    .Find(x => x.UserId == userId && !x.Deleted && x.ImportBatchId == batchId)
                    .ToListAsync();

                int removedAssets = 0;
                foreach (var asset in batchAssets)
                {
                    bool hasActive = await context.InvestmentTransactions
                        .Find(x => x.InvestmentAssetId == asset.Id && !x.Deleted)
                        .AnyAsync();
                    if (hasActive) continue;

                    await context.InvestmentAssets.UpdateOneAsync(
                        x => x.Id == asset.Id,
                        Builders<InvestmentAsset>.Update
                            .Set(x => x.Deleted, true)
                            .Set(x => x.DeletedAt, now)
                            .Set(x => x.DeletedBy, userId));
                    removedAssets++;
                }

                var result = new B3RevertImportResponseDto
                {
                    RevertedTransactions = (int)txResult.ModifiedCount,
                    RemovedAssets = removedAssets
                };

                return new(result, 200, $"Importação revertida: {result.RevertedTransactions} movimentação(ões) removida(s).");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao reverter importação: {ex.Message}");
            }
        }

        private async Task<Bank> ResolveBrokerAsync(string userId, List<Bank> userBanks, string brokerName)
        {
            var match = FindOrMatchBroker(userBanks, brokerName);
            if (match != null) return match;

            string cleanName = !string.IsNullOrWhiteSpace(brokerName) ? brokerName.Trim() : "B3";
            Bank newBank = new()
            {
                Name = cleanName,
                Code = "",
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Deleted = false
            };

            await context.Banks.InsertOneAsync(newBank);
            userBanks.Add(newBank);
            return newBank;
        }

        private static Bank? FindOrMatchBroker(List<Bank> userBanks, string brokerName)
        {
            if (string.IsNullOrWhiteSpace(brokerName)) return userBanks.FirstOrDefault();

            string clean = brokerName.Trim();

            var exact = userBanks.FirstOrDefault(b => b.Name.Trim().Equals(clean, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;

            var contains = userBanks.FirstOrDefault(b =>
                clean.Contains(b.Name.Trim(), StringComparison.OrdinalIgnoreCase) ||
                b.Name.Trim().Contains(clean, StringComparison.OrdinalIgnoreCase));
            if (contains != null) return contains;

            var keywords = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "NU", ["NU INVEST", "NUBANK", "EASYNVEST", "NU"] },
                { "XP", ["XP INVESTIMENTOS", "XP"] },
                { "CLEAR", ["CLEAR"] },
                { "RICO", ["RICO"] },
                { "INTER", ["INTER", "BANCO INTER"] },
                { "BTG", ["BTG", "BTG PACTUAL"] },
                { "ITAU", ["ITAU", "ITAÚ"] },
                { "BRADESCO", ["BRADESCO", "AGORA", "ÁGORA"] },
                { "SANTANDER", ["SANTANDER"] },
                { "GENIAL", ["GENIAL"] },
                { "TORO", ["TORO"] },
                { "C6", ["C6", "C6 BANK"] }
            };

            foreach (var (_, words) in keywords)
            {
                if (words.Any(w => clean.Contains(w, StringComparison.OrdinalIgnoreCase)))
                {
                    var match = userBanks.FirstOrDefault(b =>
                        words.Any(w => b.Name.Contains(w, StringComparison.OrdinalIgnoreCase)));
                    if (match != null) return match;
                }
            }

            return null;
        }

        private static async Task<List<B3RawParsedItem>> ParseB3FileAsync(IFormFile file, string ext)
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            if (ext == ".csv")
            {
                byte[] bytes = stream.ToArray();
                string content;
                try
                {
                    var utf8Strict = new UTF8Encoding(false, true);
                    content = utf8Strict.GetString(bytes);
                }
                catch
                {
                    content = Encoding.GetEncoding("ISO-8859-1").GetString(bytes);
                }

                return ParseCsvContent(content);
            }
            else
            {
                return ParseExcelStream(stream);
            }
        }

        private static List<B3RawParsedItem> ParseCsvContent(string content)
        {
            List<B3RawParsedItem> list = [];
            var lines = content.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2) return list;

            string headerLine = lines[0];
            char delimiter = headerLine.Count(c => c == ';') > headerLine.Count(c => c == ',') ? ';' : ',';

            var headers = ParseCsvLine(headerLine, delimiter);
            var colMap = MapHeaderColumns(headers);

            if (!colMap.ContainsKey("produto") || !colMap.ContainsKey("data"))
            {
                for (int i = 1; i < Math.Min(lines.Length, 5); i++)
                {
                    var tryHeaders = ParseCsvLine(lines[i], delimiter);
                    var tryMap = MapHeaderColumns(tryHeaders);
                    if (tryMap.ContainsKey("produto") && tryMap.ContainsKey("data"))
                    {
                        colMap = tryMap;
                        lines = lines.Skip(i).ToArray();
                        break;
                    }
                }
            }

            for (int i = 1; i < lines.Length; i++)
            {
                var fields = ParseCsvLine(lines[i], delimiter);
                if (fields.Count == 0 || fields.All(string.IsNullOrWhiteSpace)) continue;

                var item = ExtractItemFromRow(fields, colMap);
                if (item != null) list.Add(item);
            }

            return list;
        }

        private static List<B3RawParsedItem> ParseExcelStream(Stream stream)
        {
            List<B3RawParsedItem> list = [];
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null) return list;

            var firstRow = worksheet.FirstRowUsed();
            var lastRow = worksheet.LastRowUsed();
            if (firstRow == null || lastRow == null) return list;

            int headerRowNum = -1;
            Dictionary<string, int> colMap = [];

            for (int r = firstRow.RowNumber(); r <= Math.Min(firstRow.RowNumber() + 10, lastRow.RowNumber()); r++)
            {
                var row = worksheet.Row(r);
                List<string> rowHeaders = [];
                for (int c = 1; c <= row.LastCellUsed()?.Address.ColumnNumber; c++)
                {
                    rowHeaders.Add(row.Cell(c).GetString().Trim());
                }

                var map = MapHeaderColumns(rowHeaders);
                if (map.ContainsKey("produto") && map.ContainsKey("data"))
                {
                    headerRowNum = r;
                    colMap = map;
                    break;
                }
            }

            if (headerRowNum == -1) return list;

            for (int r = headerRowNum + 1; r <= lastRow.RowNumber(); r++)
            {
                var row = worksheet.Row(r);
                if (row.IsEmpty()) continue;

                List<string> fields = [];
                for (int c = 1; c <= Math.Max(10, colMap.Values.DefaultIfEmpty(0).Max() + 1); c++)
                {
                    var cell = row.Cell(c);
                    if (cell.DataType == XLDataType.DateTime)
                    {
                        fields.Add(cell.GetDateTime().ToString("dd/MM/yyyy"));
                    }
                    else
                    {
                        fields.Add(cell.GetString().Trim());
                    }
                }

                var item = ExtractItemFromRow(fields, colMap);
                if (item != null) list.Add(item);
            }

            return list;
        }

        private static Dictionary<string, int> MapHeaderColumns(List<string> headers)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < headers.Count; i++)
            {
                string norm = NormalizeText(headers[i]);

                if (norm.Contains("entrada") || norm.Contains("saida") || norm.Contains("direcao") || norm.Contains("tipo"))
                {
                    if (!map.ContainsKey("tipo")) map["tipo"] = i;
                }
                else if (norm.Contains("data"))
                {
                    if (!map.ContainsKey("data")) map["data"] = i;
                }
                else if (norm.Contains("movimentacao") || norm.Contains("operacao"))
                {
                    if (!map.ContainsKey("movimentacao")) map["movimentacao"] = i;
                }
                else if (norm.Contains("produto") || norm.Contains("ativo") || norm.Contains("codigo") || norm.Contains("ticker"))
                {
                    if (!map.ContainsKey("produto")) map["produto"] = i;
                }
                else if (norm.Contains("instituicao") || norm.Contains("corretora") || norm.Contains("agente"))
                {
                    if (!map.ContainsKey("instituicao")) map["instituicao"] = i;
                }
                else if (norm.Contains("quantidade") || norm.Contains("qtd"))
                {
                    if (!map.ContainsKey("quantidade")) map["quantidade"] = i;
                }
                else if (norm.Contains("preco unitario") || norm.Contains("preco"))
                {
                    if (!map.ContainsKey("preco")) map["preco"] = i;
                }
                else if (norm.Contains("valor da operacao") || norm.Contains("valor operacao") || norm.Contains("valor total") || norm.Contains("total"))
                {
                    if (!map.ContainsKey("total")) map["total"] = i;
                }
            }

            return map;
        }

        private static B3RawParsedItem? ExtractItemFromRow(List<string> fields, Dictionary<string, int> colMap)
        {
            string GetField(string key)
            {
                if (colMap.TryGetValue(key, out int idx) && idx >= 0 && idx < fields.Count)
                    return fields[idx].Trim();
                return "";
            }

            string rawTipo = GetField("tipo");
            string rawData = GetField("data");
            string rawMov = GetField("movimentacao");
            string rawProduto = GetField("produto");
            string rawInstituicao = GetField("instituicao");
            string rawQtd = GetField("quantidade");
            string rawPreco = GetField("preco");
            string rawTotal = GetField("total");

            if (string.IsNullOrWhiteSpace(rawProduto) || string.IsNullOrWhiteSpace(rawData))
                return null;

            string normTipo = NormalizeText(rawTipo);
            string normMov = NormalizeText(rawMov);
            string kind = "BUY";

            if (normMov.Contains("dividendo"))
            {
                kind = "DIVIDEND";
            }
            else if (normMov.Contains("juros sobre capital") || normMov.Contains("jcp"))
            {
                kind = "JCP";
            }
            else if (normMov.Contains("rendimento") || normMov.Contains("cupom") || normMov.Contains("leilao de fracao"))
            {
                kind = "INCOME";
            }
            else if (normMov.Contains("venda") || normTipo.Contains("debito") || normTipo.Contains("venda") || normTipo.Contains("saida"))
            {
                kind = "SELL";
            }
            else
            {
                kind = "BUY";
            }

            bool isIncomeKind = kind is "DIVIDEND" or "JCP" or "INCOME";

            DateTime date;
            string[] dateFormats = ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "dd/MM/yyyy HH:mm:ss", "yyyy-MM-ddTHH:mm:ss"];
            if (!DateTime.TryParseExact(rawData, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date) &&
                !DateTime.TryParse(rawData, new CultureInfo("pt-BR"), DateTimeStyles.None, out date))
            {
                return null;
            }
            date = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);

            string ticker;
            string name;
            string type;
            string normProd = NormalizeText(rawProduto);

            if (!rawProduto.Contains('-') && normProd.StartsWith("tesouro"))
            {
                ticker = rawProduto.Trim().ToUpperInvariant();
                name = rawProduto.Trim();
                type = "FIXED_INCOME";
            }
            else
            {
                string[] prodParts = rawProduto.Split(['-'], 2);
                ticker = prodParts[0].Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(ticker)) return null;

                if (ticker.Contains(' '))
                {
                    ticker = ticker.Split([' '], StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                }

                name = prodParts.Length > 1 ? prodParts[1].Trim() : ticker;
                if (string.IsNullOrWhiteSpace(name)) name = ticker;

                type = InferAssetType(ticker);
            }

            string brokerName = !string.IsNullOrWhiteSpace(rawInstituicao) ? rawInstituicao.Trim() : "B3";

            decimal quantity = ParseDecimal(rawQtd);
            decimal unitPrice = (rawPreco == "-" || rawPreco == "--" || string.IsNullOrWhiteSpace(rawPreco)) ? 0 : ParseDecimal(rawPreco);
            decimal totalValue = (rawTotal == "-" || rawTotal == "--" || string.IsNullOrWhiteSpace(rawTotal))
                ? Math.Round(quantity * unitPrice, 2)
                : ParseDecimal(rawTotal);

            if (totalValue <= 0 && quantity > 0 && unitPrice > 0)
            {
                totalValue = Math.Round(quantity * unitPrice, 2);
            }

            if (isIncomeKind)
            {
                if (totalValue <= 0 && quantity <= 0) return null;
                if (unitPrice <= 0 && totalValue > 0 && quantity > 0)
                {
                    unitPrice = Math.Round(totalValue / quantity, 4);
                }
            }
            else
            {
                if (quantity <= 0) return null;
            }

            bool hasWarning = false;
            string? warningMessage = null;

            if (isIncomeKind)
            {
                if (totalValue <= 0)
                {
                    hasWarning = true;
                    warningMessage = "Valor do provento zerado na planilha da B3.";
                }
            }
            else if (rawPreco == "-" || rawPreco == "--" || string.IsNullOrWhiteSpace(rawPreco))
            {
                unitPrice = 0;
                hasWarning = true;
                warningMessage = "Preço unitário não informado na planilha da B3 (R$ 0,00).";
            }
            else if (unitPrice <= 0)
            {
                hasWarning = true;
                warningMessage = "Preço unitário zerado.";
            }

            string qtyStr = quantity.ToString("0.########", CultureInfo.InvariantCulture);
            string priceStr = unitPrice.ToString("0.########", CultureInfo.InvariantCulture);
            string rawHashInput = $"{kind}|{date:yyyy-MM-dd}|{ticker}|{brokerName.ToUpperInvariant()}|{qtyStr}|{priceStr}";
            string importHash = ComputeSha256(rawHashInput);

            return new B3RawParsedItem
            {
                Ticker = ticker,
                Name = name,
                Type = type,
                Kind = kind,
                Date = date,
                BrokerName = brokerName,
                Quantity = quantity,
                UnitPrice = unitPrice,
                TotalValue = totalValue,
                ImportHash = importHash,
                HasWarning = hasWarning,
                WarningMessage = warningMessage
            };
        }

        private static List<string> ParseCsvLine(string line, char delimiter)
        {
            List<string> fields = [];
            bool inQuotes = false;
            StringBuilder current = new();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == delimiter && !inQuotes)
                {
                    fields.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            fields.Add(current.ToString().Trim());
            return fields;
        }

        private static decimal ParseDecimal(string? val)
        {
            if (string.IsNullOrWhiteSpace(val)) return 0;
            val = val.Trim();
            if (val == "-" || val == "--") return 0;

            val = val.Replace("R$", "").Trim();

            if (val.Contains('.') && val.Contains(','))
            {
                if (val.LastIndexOf(',') > val.LastIndexOf('.'))
                    val = val.Replace(".", "").Replace(",", ".");
                else
                    val = val.Replace(",", "");
            }
            else if (val.Contains(','))
            {
                val = val.Replace(',', '.');
            }

            if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                return result;

            return 0;
        }

        private static string ComputeSha256(string input)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToUpperInvariant();
        }

        private static string InferAssetType(string ticker)
        {
            if (string.IsNullOrWhiteSpace(ticker)) return "STOCK";
            ticker = ticker.Trim().ToUpperInvariant();

            if (ticker.EndsWith("34") || ticker.EndsWith("35"))
                return "BDR";

            if (ticker.EndsWith("11"))
            {
                string[] knownEtfs = ["BOVA11", "SMAL11", "IVVB11", "HASH11", "BBOV11", "XBOV11", "SPXI11", "ECOO11", "GOLD11", "DIVO11", "FIND11", "GOVE11", "MATB11", "ISUS11", "PIBB11", "BRAX11", "GENB11", "ACWI11", "NASD11", "TECK11", "DNAI11", "MILL11", "SHOT11", "YDRO11", "URET11", "ALUG11", "WRLD11", "BBSD11", "ESGE11"];
                if (knownEtfs.Contains(ticker))
                    return "ETF";

                return "FII";
            }

            if (ticker.EndsWith("3") || ticker.EndsWith("4") || ticker.EndsWith("5") || ticker.EndsWith("6"))
                return "STOCK";

            return "STOCK";
        }

        private static string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            string normalized = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            StringBuilder sb = new();
            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString();
        }

        private class B3RawParsedItem
        {
            public string Ticker { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = "STOCK";
            public string Kind { get; set; } = "BUY";
            public DateTime Date { get; set; }
            public string BrokerName { get; set; } = string.Empty;
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal TotalValue { get; set; }
            public string ImportHash { get; set; } = string.Empty;
            public bool HasWarning { get; set; }
            public string? WarningMessage { get; set; }
        }
    }
}
