using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;
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
    }
}
