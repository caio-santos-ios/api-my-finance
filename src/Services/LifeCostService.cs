using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using MongoDB.Driver;

namespace api_finances.src.Services
{
    public class LifeCostService(
        AppDbContext context,
        ILifeCostRepository lifeCostRepository,
        IPatrimonyService patrimonyService
    ) : ILifeCostService
    {
        public async Task<ResponseApi<LifeCostSettings>> GetSettingsAsync(string userId)
        {
            try
            {
                LifeCostSettings? settings = await lifeCostRepository.GetByUserIdAsync(userId);
                if (settings is null)
                {
                    settings = new LifeCostSettings
                    {
                        UserId = userId,
                        CreatedBy = userId,
                        AverageWindowMonths = 6,
                        SafeWithdrawalRate = 0.04m,
                        EmergencyReserveTargetMonths = 6,
                        IncludeDebtPayments = false
                    };
                    await lifeCostRepository.UpsertSettingsAsync(settings);
                }

                return new(settings, 200, "Configurações obtidas com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao obter configurações: {ex.Message}");
            }
        }

        public async Task<ResponseApi<LifeCostSettings>> UpdateSettingsAsync(string userId, UpdateLifeCostSettingsRequest request)
        {
            try
            {
                LifeCostSettings? settings = await lifeCostRepository.GetByUserIdAsync(userId);
                if (settings is null)
                {
                    settings = new LifeCostSettings
                    {
                        UserId = userId,
                        CreatedBy = userId
                    };
                }

                settings.AverageWindowMonths = request.AverageWindowMonths;
                settings.SafeWithdrawalRate = request.SafeWithdrawalRate;
                settings.EmergencyReserveTargetMonths = request.EmergencyReserveTargetMonths;
                settings.IncludeDebtPayments = request.IncludeDebtPayments;

                LifeCostSettings updated = await lifeCostRepository.UpsertSettingsAsync(settings);
                return new(updated, 200, "Configurações atualizadas com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao atualizar configurações: {ex.Message}");
            }
        }

        public async Task<ResponseApi<bool>> AddAdjustmentAsync(string userId, AddMonthlyAdjustmentRequest request)
        {
            try
            {
                MonthlyAdjustment adjustment = new()
                {
                    Year = request.Year,
                    Month = request.Month,
                    Amount = request.Amount,
                    Reason = request.Reason
                };

                bool success = await lifeCostRepository.AddAdjustmentAsync(userId, adjustment);
                return new(success, 200, "Ajuste adicionado com sucesso");
            }
            catch (Exception ex)
            {
                return new(false, 500, $"Erro ao adicionar ajuste: {ex.Message}");
            }
        }

        public async Task<ResponseApi<bool>> DeleteAdjustmentAsync(string userId, string adjustmentId)
        {
            try
            {
                bool success = await lifeCostRepository.DeleteAdjustmentAsync(userId, adjustmentId);
                return new(success, 200, "Ajuste removido com sucesso");
            }
            catch (Exception ex)
            {
                return new(false, 500, $"Erro ao remover ajuste: {ex.Message}");
            }
        }

        public async Task<ResponseApi<Category>> UpdateCategoryClassificationAsync(string categoryId, UpdateCategoryClassificationRequest request)
        {
            try
            {
                Category? category = await context.Categories.Find(x => x.Id == categoryId && !x.Deleted).FirstOrDefaultAsync();
                if (category is null) return new(null, 404, "Categoria não encontrada");

                category.LifeCostClass = request.LifeCostClass.Trim().ToUpperInvariant();
                category.LifeCostRecurrence = string.IsNullOrWhiteSpace(request.LifeCostRecurrence) ? "VARIABLE" : request.LifeCostRecurrence.Trim().ToUpperInvariant();
                category.UpdatedAt = DateTime.UtcNow;

                await context.Categories.ReplaceOneAsync(x => x.Id == categoryId, category);
                return new(category, 200, "Classificação atualizada com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao classificar categoria: {ex.Message}");
            }
        }

        public async Task<ResponseApi<List<LifeCostCategoryClassificationItem>>> GetCategoriesClassificationAsync(string userId)
        {
            try
            {
                List<Category> categories = await context.Categories
                    .Find(x => x.CreatedBy == userId && !x.Deleted && (x.Type == "expense" || x.Type == "saida"))
                    .ToListAsync();

                List<LifeCostCategoryClassificationItem> items = categories.Select(c => new LifeCostCategoryClassificationItem
                {
                    Id = c.Id,
                    Name = c.Name,
                    LifeCostClass = string.IsNullOrWhiteSpace(c.LifeCostClass) ? "DISCRETIONARY" : c.LifeCostClass,
                    LifeCostRecurrence = string.IsNullOrWhiteSpace(c.LifeCostRecurrence) ? "VARIABLE" : c.LifeCostRecurrence,
                    IsUnclassified = string.IsNullOrWhiteSpace(c.LifeCostClass)
                }).OrderBy(c => c.Name).ToList();

                return new(items, 200, "Classificações obtidas com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao listar classificações: {ex.Message}");
            }
        }

        public async Task<ResponseApi<LifeCostSummaryResponse>> GetSummaryAsync(string userId)
        {
            try
            {
                LifeCostSettings? settings = await lifeCostRepository.GetByUserIdAsync(userId);
                settings ??= new LifeCostSettings { UserId = userId, CreatedBy = userId };

                List<Category> categories = await context.Categories
                    .Find(x => x.CreatedBy == userId && !x.Deleted)
                    .ToListAsync();

                Dictionary<string, Category> catMap = categories.ToDictionary(c => c.Id, c => c);
                int unclassifiedCount = categories.Count(c => (c.Type == "expense" || c.Type == "saida") && string.IsNullOrWhiteSpace(c.LifeCostClass));

                DateTime now = DateTime.UtcNow;
                int windowMonths = settings.AverageWindowMonths > 0 ? settings.AverageWindowMonths : 6;

                List<(int Year, int Month)> completeMonths = [];
                for (int i = 1; i <= windowMonths; i++)
                {
                    DateTime target = now.AddMonths(-i);
                    completeMonths.Add((target.Year, target.Month));
                }

                DateTime windowStart = now.AddMonths(-windowMonths);
                DateTime startOfWindow = new(windowStart.Year, windowStart.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime endOfCompleteMonths = new(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                List<Operation> operations = await context.Operations
                    .Find(x => x.CreatedBy == userId && !x.Deleted && (x.Type == "expense" || x.Type == "saida") && x.CreatedAt >= startOfWindow && x.CreatedAt < endOfCompleteMonths)
                    .ToListAsync();

                if (!settings.IncludeDebtPayments)
                {
                    operations = operations.Where(o => o.Origin != "debt" && o.Origin != "debt_payment" && o.Origin != "debt_amortization").ToList();
                }

                decimal sumNetMonthly = 0;
                decimal sumEssential = 0;
                int monthsWithDataCount = 0;

                foreach (var (year, month) in completeMonths)
                {
                    List<Operation> monthOps = operations
                        .Where(o => o.CreatedAt.Year == year && o.CreatedAt.Month == month)
                        .ToList();

                    decimal monthEssential = 0;
                    decimal monthDiscretionary = 0;

                    foreach (Operation op in monthOps)
                    {
                        string classification = "DISCRETIONARY";
                        if (!string.IsNullOrEmpty(op.CategoryId) && catMap.TryGetValue(op.CategoryId, out Category? cat))
                        {
                            if (!string.IsNullOrWhiteSpace(cat.LifeCostClass))
                                classification = cat.LifeCostClass.ToUpperInvariant();
                        }

                        if (classification == "ESSENTIAL") monthEssential += op.Value;
                        else if (classification == "DISCRETIONARY") monthDiscretionary += op.Value;
                    }

                    decimal monthAdjustments = settings.MonthlyAdjustments
                        .Where(a => a.Year == year && a.Month == month)
                        .Sum(a => a.Amount);

                    decimal monthNet = Math.Max(0, (monthEssential + monthDiscretionary) - monthAdjustments);
                    if (monthOps.Count > 0 || monthAdjustments > 0) monthsWithDataCount++;

                    sumNetMonthly += monthNet;
                    sumEssential += monthEssential;
                }

                int monthsConsidered = monthsWithDataCount > 0 ? monthsWithDataCount : completeMonths.Count;
                if (monthsConsidered == 0) monthsConsidered = 1;

                decimal averageMonthlyCost = Math.Round(sumNetMonthly / monthsConsidered, 2);
                decimal averageEssentialCost = Math.Round(sumEssential / monthsConsidered, 2);
                decimal averageDiscretionaryCost = Math.Max(0, averageMonthlyCost - averageEssentialCost);
                decimal projectedAnnualCost = Math.Round(averageMonthlyCost * 12m, 2);
                decimal dailyCost = Math.Round(averageMonthlyCost / 30m, 2);

                decimal liquidAssets = 0;
                decimal netPatrimony = 0;
                try
                {
                    var patrimonyResp = await patrimonyService.GetSummaryAsync(userId);
                    if (patrimonyResp?.Data != null)
                    {
                        dynamic pData = patrimonyResp.Data;
                        liquidAssets = Convert.ToDecimal(pData.totalBankBalance ?? 0);
                        netPatrimony = Convert.ToDecimal(pData.netPatrimony ?? 0);
                    }
                }
                catch { }

                decimal emergencyReserveMonths = averageEssentialCost > 0 ? Math.Round(liquidAssets / averageEssentialCost, 1) : 0;
                decimal emergencyReserveTargetAmount = Math.Round(averageEssentialCost * settings.EmergencyReserveTargetMonths, 2);
                decimal autonomyMonths = averageMonthlyCost > 0 ? Math.Round(netPatrimony / averageMonthlyCost, 1) : 0;
                decimal swr = settings.SafeWithdrawalRate > 0 ? settings.SafeWithdrawalRate : 0.04m;
                decimal financialFreedomNumber = swr > 0 ? Math.Round(projectedAnnualCost / swr, 2) : 0;
                decimal financialFreedomProgress = financialFreedomNumber > 0 ? Math.Round((netPatrimony / financialFreedomNumber) * 100m, 2) : 0;

                LifeCostSummaryResponse summary = new()
                {
                    AverageMonthlyCost = averageMonthlyCost,
                    AverageEssentialCost = averageEssentialCost,
                    AverageDiscretionaryCost = averageDiscretionaryCost,
                    ProjectedAnnualCost = projectedAnnualCost,
                    DailyCost = dailyCost,
                    EmergencyReserveMonths = emergencyReserveMonths,
                    EmergencyReserveTargetMonths = settings.EmergencyReserveTargetMonths,
                    EmergencyReserveTargetAmount = emergencyReserveTargetAmount,
                    AutonomyMonths = autonomyMonths,
                    FinancialFreedomNumber = financialFreedomNumber,
                    FinancialFreedomProgress = financialFreedomProgress,
                    LiquidAssets = liquidAssets,
                    NetPatrimony = netPatrimony,
                    MonthsConsidered = monthsConsidered,
                    ConfiguredWindowMonths = windowMonths,
                    UnclassifiedCategoriesCount = unclassifiedCount,
                    SafeWithdrawalRate = swr,
                    IncludeDebtPayments = settings.IncludeDebtPayments
                };

                return new(summary, 200, "Resumo de custo de vida obtido com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao calcular resumo de custo de vida: {ex.Message}");
            }
        }

        public async Task<ResponseApi<List<LifeCostMonthlyPoint>>> GetMonthlyAsync(string userId, int months = 12)
        {
            try
            {
                LifeCostSettings? settings = await lifeCostRepository.GetByUserIdAsync(userId);
                settings ??= new LifeCostSettings { UserId = userId, CreatedBy = userId };

                List<Category> categories = await context.Categories
                    .Find(x => x.CreatedBy == userId && !x.Deleted)
                    .ToListAsync();
                Dictionary<string, Category> catMap = categories.ToDictionary(c => c.Id, c => c);

                DateTime now = DateTime.UtcNow;
                int totalPoints = months > 0 ? months : 12;

                DateTime rangeStart = now.AddMonths(-(totalPoints - 1));
                DateTime startOfRange = new(rangeStart.Year, rangeStart.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                List<Operation> operations = await context.Operations
                    .Find(x => x.CreatedBy == userId && !x.Deleted && (x.Type == "expense" || x.Type == "saida") && x.CreatedAt >= startOfRange)
                    .ToListAsync();

                if (!settings.IncludeDebtPayments)
                {
                    operations = operations.Where(o => o.Origin != "debt" && o.Origin != "debt_payment" && o.Origin != "debt_amortization").ToList();
                }

                List<LifeCostMonthlyPoint> series = [];
                for (int i = totalPoints - 1; i >= 0; i--)
                {
                    DateTime pointDate = now.AddMonths(-i);
                    int year = pointDate.Year;
                    int month = pointDate.Month;
                    bool isPartial = year == now.Year && month == now.Month;

                    List<Operation> monthOps = operations
                        .Where(o => o.CreatedAt.Year == year && o.CreatedAt.Month == month)
                        .ToList();

                    decimal total = 0;
                    decimal essential = 0;
                    decimal discretionary = 0;
                    decimal excluded = 0;

                    foreach (Operation op in monthOps)
                    {
                        string classification = "DISCRETIONARY";
                        if (!string.IsNullOrEmpty(op.CategoryId) && catMap.TryGetValue(op.CategoryId, out Category? cat))
                        {
                            if (!string.IsNullOrWhiteSpace(cat.LifeCostClass))
                                classification = cat.LifeCostClass.ToUpperInvariant();
                        }

                        if (classification == "ESSENTIAL") essential += op.Value;
                        else if (classification == "EXCLUDED") excluded += op.Value;
                        else discretionary += op.Value;

                        total += op.Value;
                    }

                    decimal adjustments = settings.MonthlyAdjustments
                        .Where(a => a.Year == year && a.Month == month)
                        .Sum(a => a.Amount);

                    decimal net = Math.Max(0, (essential + discretionary) - adjustments);

                    series.Add(new LifeCostMonthlyPoint
                    {
                        Year = year,
                        Month = month,
                        Label = $"{month:D2}/{year}",
                        TotalExpense = total,
                        EssentialExpense = essential,
                        DiscretionaryExpense = discretionary,
                        ExcludedExpense = excluded,
                        AdjustmentsAmount = adjustments,
                        NetExpense = net,
                        IsPartial = isPartial
                    });
                }

                return new(series, 200, "Série mensal obtida com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao calcular série mensal: {ex.Message}");
            }
        }

        public async Task<ResponseApi<List<LifeCostBreakdownItem>>> GetBreakdownAsync(string userId, DateTime? start, DateTime? end)
        {
            try
            {
                DateTime now = DateTime.UtcNow;
                DateTime startDate = start ?? new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime endDate = end ?? startDate.AddMonths(1);

                List<Category> categories = await context.Categories
                    .Find(x => x.CreatedBy == userId && !x.Deleted)
                    .ToListAsync();
                Dictionary<string, Category> catMap = categories.ToDictionary(c => c.Id, c => c);

                List<Operation> operations = await context.Operations
                    .Find(x => x.CreatedBy == userId && !x.Deleted && (x.Type == "expense" || x.Type == "saida") && x.CreatedAt >= startDate && x.CreatedAt < endDate)
                    .ToListAsync();

                decimal total = operations.Sum(o => o.Value);

                List<LifeCostBreakdownItem> breakdown = operations
                    .GroupBy(o => o.CategoryId)
                    .Select(g =>
                    {
                        string catId = g.Key ?? "";
                        catMap.TryGetValue(catId, out Category? cat);
                        decimal catSum = g.Sum(x => x.Value);
                        return new LifeCostBreakdownItem
                        {
                            CategoryId = catId,
                            CategoryName = cat?.Name ?? "Sem Categoria",
                            LifeCostClass = string.IsNullOrWhiteSpace(cat?.LifeCostClass) ? "DISCRETIONARY" : cat.LifeCostClass,
                            LifeCostRecurrence = string.IsNullOrWhiteSpace(cat?.LifeCostRecurrence) ? "VARIABLE" : cat.LifeCostRecurrence,
                            TotalAmount = catSum,
                            Percentage = total > 0 ? Math.Round((catSum / total) * 100m, 2) : 0,
                            OperationsCount = g.Count()
                        };
                    })
                    .OrderByDescending(b => b.TotalAmount)
                    .ToList();

                return new(breakdown, 200, "Detalhamento obtido com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao calcular detalhamento: {ex.Message}");
            }
        }

        public async Task<ResponseApi<dynamic>> GetCompareAsync(string userId, int year1, int month1, int year2, int month2)
        {
            try
            {
                DateTime start1 = new(year1, month1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime end1 = start1.AddMonths(1);

                DateTime start2 = new(year2, month2, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime end2 = start2.AddMonths(1);

                List<Category> categories = await context.Categories
                    .Find(x => x.CreatedBy == userId && !x.Deleted)
                    .ToListAsync();
                Dictionary<string, Category> catMap = categories.ToDictionary(c => c.Id, c => c);

                List<Operation> ops1 = await context.Operations
                    .Find(x => x.CreatedBy == userId && !x.Deleted && (x.Type == "expense" || x.Type == "saida") && x.CreatedAt >= start1 && x.CreatedAt < end1)
                    .ToListAsync();

                List<Operation> ops2 = await context.Operations
                    .Find(x => x.CreatedBy == userId && !x.Deleted && (x.Type == "expense" || x.Type == "saida") && x.CreatedAt >= start2 && x.CreatedAt < end2)
                    .ToListAsync();

                decimal total1 = ops1.Sum(o => o.Value);
                decimal essential1 = ops1.Where(o => catMap.TryGetValue(o.CategoryId, out Category? c) && c.LifeCostClass == "ESSENTIAL").Sum(o => o.Value);
                decimal discretionary1 = total1 - essential1;

                decimal total2 = ops2.Sum(o => o.Value);
                decimal essential2 = ops2.Where(o => catMap.TryGetValue(o.CategoryId, out Category? c) && c.LifeCostClass == "ESSENTIAL").Sum(o => o.Value);
                decimal discretionary2 = total2 - essential2;

                decimal totalDiff = total2 - total1;
                decimal totalDiffPct = total1 > 0 ? Math.Round((totalDiff / total1) * 100m, 2) : 0;

                var result = new
                {
                    Period1 = new { Year = year1, Month = month1, Total = total1, Essential = essential1, Discretionary = discretionary1 },
                    Period2 = new { Year = year2, Month = month2, Total = total2, Essential = essential2, Discretionary = discretionary2 },
                    TotalDiff = totalDiff,
                    TotalDiffPercentage = totalDiffPct,
                    EssentialDiff = essential2 - essential1,
                    DiscretionaryDiff = discretionary2 - discretionary1
                };

                return new(result, 200, "Comparação realizada com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao comparar períodos: {ex.Message}");
            }
        }

        public async Task<ResponseApi<List<LifeCostOperationItemDTO>>> GetMonthlyOperationsAsync(string userId, int year, int month, string type)
        {
            try
            {
                LifeCostSettings? settings = await lifeCostRepository.GetByUserIdAsync(userId);
                settings ??= new LifeCostSettings { UserId = userId, CreatedBy = userId };

                DateTime start = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime end = start.AddMonths(1);

                string normType = (type ?? "net").Trim().ToLowerInvariant();

                if (normType == "adjustments" || normType == "ajustes")
                {
                    List<MonthlyAdjustment> adjustments = settings.MonthlyAdjustments
                        .Where(a => a.Year == year && a.Month == month)
                        .ToList();

                    List<LifeCostOperationItemDTO> adjItems = adjustments.Select(a => new LifeCostOperationItemDTO
                    {
                        Id = a.Id,
                        Description = string.IsNullOrWhiteSpace(a.Reason) ? "Ajuste manual abatido" : a.Reason,
                        Value = a.Amount,
                        Type = "adjustment",
                        CategoryId = string.Empty,
                        CategoryName = "Ajuste Abatido",
                        LifeCostClass = "ADJUSTMENT",
                        BankId = string.Empty,
                        BankName = "-",
                        CreatedAt = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc)
                    }).ToList();

                    return new(adjItems, 200, "Ajustes obtidos com sucesso");
                }

                List<Category> categories = await context.Categories
                    .Find(x => x.CreatedBy == userId && !x.Deleted)
                    .ToListAsync();
                Dictionary<string, Category> catMap = categories.ToDictionary(c => c.Id, c => c);

                List<Bank> banks = await context.Banks
                    .Find(b => b.CreatedBy == userId && !b.Deleted)
                    .ToListAsync();
                Dictionary<string, Bank> bankMap = banks.ToDictionary(b => b.Id, b => b);

                List<Operation> operations = await context.Operations
                    .Find(x => x.CreatedBy == userId && !x.Deleted && (x.Type == "expense" || x.Type == "saida") && x.CreatedAt >= start && x.CreatedAt < end)
                    .ToListAsync();

                if (!settings.IncludeDebtPayments)
                {
                    operations = operations.Where(o => o.Origin != "debt" && o.Origin != "debt_payment" && o.Origin != "debt_amortization").ToList();
                }

                List<LifeCostOperationItemDTO> items = [];

                foreach (Operation op in operations)
                {
                    string classification = "DISCRETIONARY";
                    string catName = "-";
                    if (!string.IsNullOrEmpty(op.CategoryId) && catMap.TryGetValue(op.CategoryId, out Category? cat))
                    {
                        catName = cat.Name;
                        if (!string.IsNullOrWhiteSpace(cat.LifeCostClass))
                            classification = cat.LifeCostClass.ToUpperInvariant();
                    }

                    bool matches = false;
                    switch (normType)
                    {
                        case "essential":
                        case "essencial":
                            matches = classification == "ESSENTIAL";
                            break;
                        case "discretionary":
                        case "discricionario":
                        case "discricionário":
                            matches = classification == "DISCRETIONARY";
                            break;
                        case "excluded":
                        case "excluido":
                        case "excluído":
                            matches = classification == "EXCLUDED";
                            break;
                        case "net":
                        case "liquido":
                        case "líquido":
                        default:
                            matches = classification != "EXCLUDED";
                            break;
                    }

                    if (matches)
                    {
                        string bankName = "-";
                        if (!string.IsNullOrEmpty(op.BankId) && bankMap.TryGetValue(op.BankId, out Bank? bank))
                        {
                            bankName = bank.Name;
                        }

                        items.Add(new LifeCostOperationItemDTO
                        {
                            Id = op.Id,
                            Description = op.Description ?? string.Empty,
                            Value = op.Value,
                            Type = op.Type ?? "expense",
                            CategoryId = op.CategoryId ?? string.Empty,
                            CategoryName = catName,
                            LifeCostClass = classification,
                            BankId = op.BankId ?? string.Empty,
                            BankName = bankName,
                            CreatedAt = op.CreatedAt
                        });
                    }
                }

                items = items.OrderByDescending(x => x.CreatedAt).ToList();

                return new(items, 200, "Operações obtidas com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao buscar operações do mês: {ex.Message}");
            }
        }
    }
}
