using System.Globalization;
using System.Text.RegularExpressions;
using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests.Dashboard;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;
using api_finances.src.Utils;
using MongoDB.Bson;
using MongoDB.Driver;

namespace api_finances.src.Services
{
    public record DashboardDataResponse(
    string Id, string Name, string Type, string? CardClass,
    Dictionary<string, string>? Formats, List<object?> Data, string? Error);

    public class DashboardService(
        IDashboardRepository repository,
        IMongoDatabase db,
        AppDbContext context
    ) : IDashboardService
    {
        private static readonly HashSet<string> ForbiddenOperators = new()
        {
            "$out", "$merge", "$where", "$function", "$accumulator"
        };

        private static readonly HashSet<string> _allowedCollections = ["categories", "operations"];

        #region READ
        public async Task<ResponseApi<dynamic>> GetDashAsync(GetAllRequest request)
        {
            try
            {
                string createdBy = request.QueryParams["createdBy"];

                List<Dashboard> dashboards = await repository.GetDashAsync(createdBy);

                Dictionary<string, BsonValue> parameters = request.QueryParams
                .ToDictionary(f => $"@{f.Key}", f => ParseParameter(f.Value));
                parameters["@userId"] = createdBy;

                var tasks = dashboards.Select(async d =>
                {
                    try
                    {
                        var rows = await ExecuteAsync(d, parameters);
                        return new DashboardDataResponse(
                            d.Id!, d.Name, d.Type, d.CardClass, d.Formats,
                            rows.Select(ToPlain).ToList(), null);
                    }
                    catch (Exception ex)
                    {
                        return new DashboardDataResponse(
                            d.Id!, d.Name, d.Type, d.CardClass, d.Formats,
                            [], ex.Message);
                    }
                });

                var result = (await Task.WhenAll(tasks)).ToList();

                return new(result, 200, "Dashboard listados com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        public async Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllRequest request)
        {
            try
            {
                PaginationUtil<Dashboard> pagination = new(request.QueryParams);

                BsonDocument[] pipeline = [
                    new("$match", pagination.PipelineFilter),
                    new("$sort", pagination.PipelineSort),
                    new("$skip", pagination.Skip),
                    new("$limit", pagination.Limit),

                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"id", new BsonDocument("$toString", "$_id")},
                        {"name", 1},
                        {"type", 1},
                        {"collection", 1},
                        {"cardClass", 1},
                        {"sequence", 1},
                        {"createdAt", 1},
                    }),
                    new("$sort", pagination.PipelineSort),
                ];

                List<dynamic> entities = await repository.GetAllAsync(pipeline);
                int count = await repository.GetCountDocumentsAsync(pipeline);
                PaginationApi<List<dynamic>> data = new(entities, count, pagination.PageNumber, pagination.PageSize);

                return new(data, 200, "Dashboard listados com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        public async Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id)
        {
            try
            {
                BsonDocument[] pipeline = [
                    new("$match", new BsonDocument
                    {
                        {"_id", new ObjectId(id)},
                        {"deleted", false}
                    }),

                    new("$addFields", new BsonDocument
                    {
                        {"id", new BsonDocument("$toString", "$_id")},
                    }),

                    new("$project", new BsonDocument
                    {
                        {"_id", 0}
                    }),
                ];

                dynamic? entity = await repository.GetByIdAggregateAsync(pipeline);
                if (entity is null) return new(null, 404, "Operação não encontrado");
                return new(entity, 200, "Operação encontrado");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        public async Task<ResponseApi<dynamic?>> GetDashAppAsync(string userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                List<Operation> myOperations = await repository.GetDashAppAsync(userId);
                List<Operation> operations = myOperations.Where(x => x.CreatedAt <= endDate).ToList();
                List<Operation> operationsMonth = myOperations.Where(x => x.CreatedAt >= startDate && x.CreatedAt <= endDate).ToList();

                decimal totalIncome = operations.Where(x => x.Type == "income").Sum(x => x.Value);
                decimal totalExpense = operations.Where(x => x.Type == "expense").Sum(x => x.Value);

                decimal monthIncome = operationsMonth.Where(x => x.Type == "income").Sum(x => x.Value);
                decimal monthExpense = operationsMonth.Where(x => x.Type == "expense").Sum(x => x.Value);

                dynamic data = new
                {
                    totalAccount = totalIncome - totalExpense,
                    totalMonthAccount = monthIncome - monthExpense,

                    totalAccountIncome = operations.Where(x => x.Type == "income").Sum(x => x.Value),
                    totalAccountExpense = operations.Where(x => x.Type == "expense").Sum(x => x.Value),

                    totalAccountMonthIncome = operationsMonth.Where(x => x.Type == "income").Sum(x => x.Value),
                    totalAccountMonthExpense = operationsMonth.Where(x => x.Type == "expense").Sum(x => x.Value)
                };

                return new(data, 200, "Dashboard encontrado");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion
        #region CREATE
        public async Task<ResponseApi<Dashboard?>> CreateAsync(CreateDashboardRequest request)
        {
            try
            {
                Dashboard dashboard = ObjectMapper.Map<CreateDashboardRequest, Dashboard>(request);

                Dashboard? response = await repository.CreateAsync(dashboard);
                if (response is null) return new(null, 400, "Falha ao criar dashboard.");

                return new(response, 201, "Dashboard criado com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
            }
        }
        #endregion
        #region UPDATE
        public async Task<ResponseApi<Dashboard?>> UpdateAsync(UpdateDashboardRequest request)
        {
            try
            {
                Dashboard? existed = await repository.GetByIdAsync(request.Id);
                if (existed is null) return new(null, 404, "Dashboard não encontrado.");

                existed.Name = request.Name;
                existed.Type = request.Type;
                existed.Collection = request.Collection;
                existed.CardClass = request.CardClass;
                existed.Sequence = request.Sequence;
                existed.Query = request.Query;
                existed.Formats = request.Formats;

                Dashboard? response = await repository.UpdateAsync(existed);
                if (response is null) return new(null, 400, "Falha ao atualizar dashboard.");

                return new(response, 200, "Dashboard atualizado com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion
        #region DELETE
        public async Task<ResponseApi<Dashboard?>> DeleteAsync(DeleteRequest request)
        {
            try
            {
                Dashboard? entity = await repository.GetByIdAsync(request.Id);
                if (entity is null) return new(null, 404, "Dashboard não encontrado.");

                entity.Deleted = true;
                entity.DeletedBy = request.DeletedBy;
                entity.DeletedAt = DateTime.UtcNow;

                await repository.DeleteAsync(entity);

                return new(entity, 204, "Dashboard excluido com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion
        private async Task<List<BsonDocument>> ExecuteAsync(Dashboard dashboard, Dictionary<string, BsonValue> parameters, CancellationToken ct = default)
        {
            if (!_allowedCollections.Contains(dashboard.Collection))
                throw new InvalidOperationException("Collection não permitida.");

            var stages = ((BsonArray)ApplyParameters(ParsePipeline(dashboard.Query), parameters))
            .Select(s => s.AsBsonDocument)
            .ToList();

            foreach (var stage in stages) Validate(stage);
            stages.Add(new BsonDocument("$limit", 5000));

            var pipeline = PipelineDefinition<BsonDocument, BsonDocument>.Create(stages);
            var options = new AggregateOptions { MaxTime = TimeSpan.FromSeconds(10) };

            return await db.GetCollection<BsonDocument>(dashboard.Collection)
                .Aggregate(pipeline, options)
                .ToListAsync(ct);
        }
        private static BsonValue ApplyParameters(BsonValue value, Dictionary<string, BsonValue> parameters)
        {
            switch (value)
            {
                case BsonString s when s.Value.StartsWith('@') && parameters.TryGetValue(s.Value, out var replacement):
                    return replacement;

                case BsonDocument doc:
                    var newDoc = new BsonDocument();
                    foreach (var el in doc)
                        newDoc[el.Name] = ApplyParameters(el.Value, parameters);
                    return newDoc;

                case BsonArray arr:
                    return new BsonArray(arr.Select(v => ApplyParameters(v, parameters)));

                default:
                    return value;
            }
        }
        private static void Validate(BsonValue value)
        {
            switch (value)
            {
                case BsonDocument doc:
                    foreach (var el in doc)
                    {
                        if (ForbiddenOperators.Contains(el.Name))
                            throw new InvalidOperationException($"Operador {el.Name} não é permitido.");
                        Validate(el.Value);
                    }
                    break;

                case BsonArray arr:
                    foreach (var item in arr) Validate(item);
                    break;
            }
        }
        private static object? ToPlain(BsonValue v) => v.BsonType switch
        {
            BsonType.Document => v.AsBsonDocument.ToDictionary(e => e.Name, e => ToPlain(e.Value)),
            BsonType.Array => v.AsBsonArray.Select(ToPlain).ToList(),
            BsonType.ObjectId => v.AsObjectId.ToString(),
            BsonType.DateTime => v.ToUniversalTime(),
            BsonType.Int32 => v.AsInt32,
            BsonType.Int64 => v.AsInt64,
            BsonType.Double => v.AsDouble,
            BsonType.Decimal128 => (decimal)v.AsDecimal128,
            BsonType.Boolean => v.AsBoolean,
            BsonType.String => v.AsString,
            BsonType.Null => null,
            _ => v.ToString()
        };
        private static BsonArray ParsePipeline(string json)
        {
            var doc = BsonDocument.Parse($"{{ \"stages\": {json} }}");
            return doc["stages"].AsBsonArray;
        }
        public async Task<ResponseApi<ProjectedBalanceDto>> GetProjectedBalanceAsync(string userId, int? month = null, int? year = null)
        {
            try
            {
                DateTime now = DateTime.UtcNow;
                int targetYear = year.HasValue && year.Value >= 2000 && year.Value <= 2100 ? year.Value : now.Year;
                int targetMonth = month.HasValue && month.Value >= 1 && month.Value <= 12 ? month.Value : now.Month;
                int daysInMonth = DateTime.DaysInMonth(targetYear, targetMonth);

                bool isCurrentMonth = targetYear == now.Year && targetMonth == now.Month;
                bool isFutureMonth = targetYear > now.Year || (targetYear == now.Year && targetMonth > now.Month);
                int startDay = isCurrentMonth ? now.Day : 1;

                DateTime endOfToday = new(now.Year, now.Month, now.Day, 23, 59, 59, 999, DateTimeKind.Utc);
                DateTime startOfTargetMonth = new(targetYear, targetMonth, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime endOfTargetMonth = new(targetYear, targetMonth, daysInMonth, 23, 59, 59, 999, DateTimeKind.Utc);

                // 1. Saldo inicial (bancos ativos do usuário)
                var banks = await context.Banks
                    .Find(b => !b.Deleted && b.Active && b.CreatedBy == userId)
                    .ToListAsync();
                var activeBankIds = banks.Select(b => b.Id).ToHashSet();

                var allUserOps = await context.Operations
                    .Find(o => !o.Deleted && o.Active && o.CreatedBy == userId)
                    .ToListAsync();

                decimal currentBalance = 0m;
                foreach (var b in banks)
                {
                    decimal inc = allUserOps
                        .Where(o => o.BankId == b.Id && o.CreatedAt <= endOfToday && (o.Type == "income" || o.Type == "revenue"))
                        .Sum(o => o.Value);
                    decimal exp = allUserOps
                        .Where(o => o.BankId == b.Id && o.CreatedAt <= endOfToday && (o.Type == "expense" || o.Type == "transfer"))
                        .Sum(o => o.Value);
                    decimal inTransfers = allUserOps
                        .Where(o => o.DestinationBankId == b.Id && o.CreatedAt <= endOfToday && o.Type == "transfer")
                        .Sum(o => o.Value);
                    currentBalance += Math.Round(inc - exp + inTransfers, 2);
                }

                List<ProjectedBalancePendingItemDto> pendingItems = [];

                // Só projeta pendentes para mês atual ou futuro
                if (isCurrentMonth || isFutureMonth)
                {
                    // 2a. Operações já cadastradas com data futura dentro do mês alvo
                    DateTime futureStart = isCurrentMonth ? endOfToday.AddTicks(1) : startOfTargetMonth;
                    var futureOps = allUserOps
                        .Where(o => o.CreatedAt >= futureStart && o.CreatedAt <= endOfTargetMonth)
                        .ToList();

                    foreach (var op in futureOps)
                    {
                        if (op.Type == "income" || op.Type == "revenue")
                        {
                            pendingItems.Add(new ProjectedBalancePendingItemDto
                            {
                                Description = !string.IsNullOrWhiteSpace(op.Description) ? op.Description : "Receita agendada",
                                Value = op.Value,
                                Type = "revenue",
                                DueDay = op.CreatedAt.Day,
                                Source = "operation"
                            });
                        }
                        else if (op.Type == "expense")
                        {
                            pendingItems.Add(new ProjectedBalancePendingItemDto
                            {
                                Description = !string.IsNullOrWhiteSpace(op.Description) ? op.Description : "Despesa agendada",
                                Value = op.Value,
                                Type = "expense",
                                DueDay = op.CreatedAt.Day,
                                Source = "operation"
                            });
                        }
                    }

                    // 2b. Transações Recorrentes ativas
                    var recurringList = await context.RecurringTransactions
                        .Find(r => !r.Deleted && r.Active && r.CreatedBy == userId)
                        .ToListAsync();

                    foreach (var rec in recurringList)
                    {
                        if (rec.Value <= 0) continue;

                        bool alreadyPostedThisMonth = rec.LastPostedDate.HasValue
                            && rec.LastPostedDate.Value.Year == targetYear
                            && rec.LastPostedDate.Value.Month == targetMonth;
                        if (alreadyPostedThisMonth) continue;

                        string freq = (rec.Frequency ?? "monthly").Trim().ToLowerInvariant();
                        if (freq == "yearly" && rec.CreatedAt.Month != targetMonth) continue;

                        bool isRevenue = rec.Type.Equals("revenue", StringComparison.OrdinalIgnoreCase)
                            || rec.Type.Equals("income", StringComparison.OrdinalIgnoreCase);
                        string normalizedType = isRevenue ? "revenue" : "expense";

                        if (freq == "weekly")
                        {
                            int d = Math.Clamp(rec.DueDay, 1, daysInMonth);
                            while (d <= daysInMonth)
                            {
                                if (d >= startDay)
                                {
                                    pendingItems.Add(new ProjectedBalancePendingItemDto
                                    {
                                        Description = rec.Description,
                                        Value = rec.Value,
                                        Type = normalizedType,
                                        DueDay = d,
                                        Source = "recurring"
                                    });
                                }
                                d += 7;
                            }
                        }
                        else
                        {
                            int dueDay = Math.Clamp(rec.DueDay, 1, daysInMonth);
                            if (dueDay >= startDay)
                            {
                                pendingItems.Add(new ProjectedBalancePendingItemDto
                                {
                                    Description = rec.Description,
                                    Value = rec.Value,
                                    Type = normalizedType,
                                    DueDay = dueDay,
                                    Source = "recurring"
                                });
                            }
                        }
                    }

                    // 2c. Dívidas ativas não quitadas sem pagamento no mês alvo
                    var debts = await context.Debts
                        .Find(d => !d.Deleted && d.Active && !d.Settled && d.RemainingAmount > 0 && d.CreatedBy == userId)
                        .ToListAsync();

                    if (debts.Count > 0)
                    {
                        var debtIds = debts.Select(d => d.Id).ToList();
                        var paidDebtIds = (await context.DebtPayments
                            .Find(p => !p.Deleted && debtIds.Contains(p.DebtId) && p.Date >= startOfTargetMonth && p.Date <= endOfTargetMonth)
                            .ToListAsync())
                            .Select(p => p.DebtId)
                            .ToHashSet();

                        foreach (var debt in debts)
                        {
                            if (paidDebtIds.Contains(debt.Id)) continue;

                            int dueDay = Math.Clamp(debt.DueDay, 1, daysInMonth);
                            if (dueDay < startDay) continue;

                            decimal installment = debt.InstallmentAmount > 0
                                ? Math.Min(debt.InstallmentAmount, debt.RemainingAmount)
                                : debt.RemainingAmount;

                            if (installment <= 0) continue;

                            pendingItems.Add(new ProjectedBalancePendingItemDto
                            {
                                Description = !string.IsNullOrWhiteSpace(debt.Name) ? $"Parcela: {debt.Name}" : "Parcela de dívida",
                                Value = installment,
                                Type = "expense",
                                DueDay = dueDay,
                                Source = "debt"
                            });
                        }
                    }
                }

                pendingItems = pendingItems
                    .OrderBy(i => i.DueDay)
                    .ThenBy(i => i.Type == "revenue" ? 0 : 1)
                    .ThenBy(i => i.Description)
                    .ToList();

                // 3. Linha do tempo diária (do startDay até o último dia do mês)
                CultureInfo ptBr = new("pt-BR");
                decimal runningBalance = currentBalance;
                List<ProjectedBalanceTimelineItemDto> timeline = [];

                ProjectedBalanceLowestPointDto lowestPoint = new()
                {
                    Day = startDay,
                    Date = new DateTime(targetYear, targetMonth, startDay).ToString("yyyy-MM-dd"),
                    Balance = runningBalance,
                    IsDeficit = runningBalance < 0
                };

                for (int d = startDay; d <= daysInMonth; d++)
                {
                    var dayItems = pendingItems.Where(i => i.DueDay == d).ToList();
                    decimal dayRevenues = dayItems.Where(i => i.Type == "revenue").Sum(i => i.Value);
                    decimal dayExpenses = dayItems.Where(i => i.Type == "expense").Sum(i => i.Value);

                    runningBalance = Math.Round(runningBalance + dayRevenues - dayExpenses, 2);

                    List<string> events = dayItems.Select(i =>
                    {
                        string sign = i.Type == "revenue" ? "+" : "-";
                        return $"{i.Description} ({sign}R$ {i.Value.ToString("N2", ptBr)})";
                    }).ToList();

                    string dateStr = new DateTime(targetYear, targetMonth, d).ToString("yyyy-MM-dd");

                    timeline.Add(new ProjectedBalanceTimelineItemDto
                    {
                        Day = d,
                        Date = dateStr,
                        Balance = runningBalance,
                        Events = events
                    });

                    if (runningBalance < lowestPoint.Balance)
                    {
                        lowestPoint = new ProjectedBalanceLowestPointDto
                        {
                            Day = d,
                            Date = dateStr,
                            Balance = runningBalance,
                            IsDeficit = runningBalance < 0
                        };
                    }
                }

                decimal pendingRevenues = Math.Round(pendingItems.Where(i => i.Type == "revenue").Sum(i => i.Value), 2);
                decimal pendingExpenses = Math.Round(pendingItems.Where(i => i.Type == "expense").Sum(i => i.Value), 2);
                decimal projectedEndBalance = Math.Round(currentBalance + pendingRevenues - pendingExpenses, 2);

                var dto = new ProjectedBalanceDto
                {
                    Month = targetMonth,
                    Year = targetYear,
                    CurrentBalance = currentBalance,
                    PendingRevenues = pendingRevenues,
                    PendingExpenses = pendingExpenses,
                    ProjectedEndBalance = projectedEndBalance,
                    Status = projectedEndBalance >= 0 ? "positive" : "negative",
                    LowestPoint = lowestPoint,
                    DailyTimeline = timeline,
                    PendingItems = pendingItems
                };

                return new(dto, 200, "Saldo projetado calculado com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao calcular saldo projetado: {ex.Message}");
            }
        }

        private static BsonValue ParseParameter(string value)
        {
            if (bool.TryParse(value, out var b)) return b;
            if (Regex.IsMatch(value, @"^-?\d{1,18}$")) return long.Parse(value);
            if (Regex.IsMatch(value, @"^-?\d+\.\d+$")) return double.Parse(value, CultureInfo.InvariantCulture);
            if (Regex.IsMatch(value, @"^\d{4}-\d{2}-\d{2}") &&
                DateTime.TryParse(value, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var d))
                return new BsonDateTime(d);

            return value;
        }
    }
}
