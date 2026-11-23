using System.Globalization;
using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;
using api_finances.src.Utils;
using MongoDB.Bson;
using MongoDB.Driver;

namespace api_finances.src.Services
{
    public class BudgetService(
        IBudgetRepository repository,
        ICategoryRepository categoryRepository,
        AppDbContext context
    ) : IBudgetService
    {
        private static (DateTime dailyStart, DateTime dailyEnd, DateTime weeklyStart, DateTime weeklyEnd, DateTime monthlyStart, DateTime monthlyEnd, DateTime yearlyStart, DateTime yearlyEnd) GetCycleDateRanges(DateTime now)
        {
            DateTime dailyStart = new(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
            DateTime dailyEnd = new(now.Year, now.Month, now.Day, 23, 59, 59, 999, DateTimeKind.Utc);

            int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime monday = now.Date.AddDays(-diff);
            DateTime weeklyStart = new(monday.Year, monday.Month, monday.Day, 0, 0, 0, DateTimeKind.Utc);
            DateTime sunday = monday.AddDays(6);
            DateTime weeklyEnd = new(sunday.Year, sunday.Month, sunday.Day, 23, 59, 59, 999, DateTimeKind.Utc);

            DateTime monthlyStart = new(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime monthlyEnd = new(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month), 23, 59, 59, 999, DateTimeKind.Utc);

            DateTime yearlyStart = new(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime yearlyEnd = new(now.Year, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc);

            return (dailyStart, dailyEnd, weeklyStart, weeklyEnd, monthlyStart, monthlyEnd, yearlyStart, yearlyEnd);
        }

        #region READ
        public async Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllRequest request)
        {
            try
            {
                var dates = GetCycleDateRanges(DateTime.UtcNow);
                PaginationUtil<Budget> pagination = new(request.QueryParams);

                BsonDocument[] pipeline = [
                    new("$match", pagination.PipelineFilter),
                    new("$sort", pagination.PipelineSort),
                    new("$skip", pagination.Skip),
                    new("$limit", pagination.Limit),

                    new("$addFields", new BsonDocument
                    {
                        { "categoryObjId", new BsonDocument("$cond", new BsonDocument
                            {
                                { "if", new BsonDocument("$in", new BsonArray { "$categoryId", new BsonArray { BsonNull.Value, "" } }) },
                                { "then", BsonNull.Value },
                                { "else", new BsonDocument("$toObjectId", "$categoryId") }
                            })
                        },
                        { "cycleStartDate", new BsonDocument("$switch", new BsonDocument
                            {
                                { "branches", new BsonArray
                                    {
                                        new BsonDocument { { "case", new BsonDocument("$eq", new BsonArray { new BsonDocument("$ifNull", new BsonArray { "$period", "monthly" }), "daily" }) }, { "then", dates.dailyStart } },
                                        new BsonDocument { { "case", new BsonDocument("$eq", new BsonArray { new BsonDocument("$ifNull", new BsonArray { "$period", "monthly" }), "weekly" }) }, { "then", dates.weeklyStart } },
                                        new BsonDocument { { "case", new BsonDocument("$eq", new BsonArray { new BsonDocument("$ifNull", new BsonArray { "$period", "monthly" }), "yearly" }) }, { "then", dates.yearlyStart } },
                                    }
                                },
                                { "default", dates.monthlyStart }
                            })
                        },
                        { "cycleEndDate", new BsonDocument("$switch", new BsonDocument
                            {
                                { "branches", new BsonArray
                                    {
                                        new BsonDocument { { "case", new BsonDocument("$eq", new BsonArray { new BsonDocument("$ifNull", new BsonArray { "$period", "monthly" }), "daily" }) }, { "then", dates.dailyEnd } },
                                        new BsonDocument { { "case", new BsonDocument("$eq", new BsonArray { new BsonDocument("$ifNull", new BsonArray { "$period", "monthly" }), "weekly" }) }, { "then", dates.weeklyEnd } },
                                        new BsonDocument { { "case", new BsonDocument("$eq", new BsonArray { new BsonDocument("$ifNull", new BsonArray { "$period", "monthly" }), "yearly" }) }, { "then", dates.yearlyEnd } },
                                    }
                                },
                                { "default", dates.monthlyEnd }
                            })
                        }
                    }),

                    MongoUtil.Lookup("categories", ["$categoryObjId"], ["$_id"], "_categories", [["deleted", false]], 1),

                    new BsonDocument("$lookup", new BsonDocument
                    {
                        { "from", "operations" },
                        { "let", new BsonDocument
                            {
                                { "catId", "$categoryId" },
                                { "user", "$createdBy" },
                                { "cStart", "$cycleStartDate" },
                                { "cEnd", "$cycleEndDate" }
                            }
                        },
                        { "pipeline", new BsonArray
                            {
                                new BsonDocument("$match", new BsonDocument
                                {
                                    { "$expr", new BsonDocument
                                        {
                                            { "$and", new BsonArray
                                                {
                                                    new BsonDocument("$eq", new BsonArray { "$categoryId", "$$catId" }),
                                                    new BsonDocument("$eq", new BsonArray { "$type", "expense" }),
                                                    new BsonDocument("$eq", new BsonArray { "$deleted", false }),
                                                    new BsonDocument("$eq", new BsonArray { "$createdBy", "$$user" }),
                                                    new BsonDocument("$gte", new BsonArray { "$createdAt", "$$cStart" }),
                                                    new BsonDocument("$lte", new BsonArray { "$createdAt", "$$cEnd" })
                                                }
                                            }
                                        }
                                    }
                                })
                            }
                        },
                        { "as", "_operations" }
                    }),

                    new("$addFields", new BsonDocument
                    {
                        {"spent", new BsonDocument("$sum", new BsonDocument("$map", new BsonDocument
                        {
                            {"input", "$_operations"},
                            {"as", "op"},
                            {"in", new BsonDocument("$convert", new BsonDocument
                            {
                                {"input", "$$op.value"},
                                {"to", "double"},
                                {"onError", 0.0},
                                {"onNull", 0.0}
                            })}
                        }))},
                    }),

                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"id", new BsonDocument("$toString", "$_id")},
                        {"name", 1},
                        {"categoryId", 1},
                        {"categoryName", MongoUtil.First("_categories.name")},
                        {"limit", 1},
                        {"period", new BsonDocument("$ifNull", new BsonArray { "$period", "monthly" })},
                        {"receiveAlert", 1},
                        {"alertPercentage", 1},
                        {"remaining", new BsonDocument("$subtract", new BsonArray
                        {
                            new BsonDocument("$toDouble", "$limit"),
                            new BsonDocument("$toDouble", "$spent"),
                        })},
                        {"spent", 1},
                        {"active", 1},
                        {"createdAt", 1}
                    }),
                    new("$sort", pagination.PipelineSort),
                ];

                List<dynamic> entities = await repository.GetAllAsync(pipeline);
                int count = await repository.GetCountDocumentsAsync(pipeline);
                PaginationApi<List<dynamic>> data = new(entities, count, pagination.PageNumber, pagination.PageSize);
                return new(data, 200, "Orçamentos listados com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }

        public async Task<ResponseApi<List<dynamic>>> GetSelectAsync(GetAllRequest request)
        {
            try
            {
                PaginationUtil<Budget> pagination = new(request.QueryParams);
                BsonDocument[] pipeline = [
                    new("$match", pagination.PipelineFilter),
                    new("$sort", pagination.PipelineSort),

                    new("$addFields", new BsonDocument
                    {
                        { "categoryObjId", new BsonDocument("$cond", new BsonDocument
                            {
                                { "if", new BsonDocument("$in", new BsonArray { "$categoryId", new BsonArray { BsonNull.Value, "" } }) },
                                { "then", BsonNull.Value },
                                { "else", new BsonDocument("$toObjectId", "$categoryId") }
                            })
                        }
                    }),

                    MongoUtil.Lookup("categories", ["$categoryObjId"], ["$_id"], "_categories", [["deleted", false]], 1),

                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"id", new BsonDocument("$toString", "$_id")},
                        {"name", 1},
                        {"categoryId", 1},
                        {"categoryName", MongoUtil.First("_categories.name")},
                        {"limit", 1},
                        {"period", new BsonDocument("$ifNull", new BsonArray { "$period", "monthly" })},
                        {"receiveAlert", 1},
                        {"alertPercentage", 1},
                        {"active", 1}
                    }),
                    new("$sort", pagination.PipelineSort),
                ];

                List<dynamic> entities = await repository.GetSelectAsync(pipeline);
                return new(entities, 200, "Orçamentos listados com sucesso");
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
                var dates = GetCycleDateRanges(DateTime.UtcNow);
                BsonDocument[] pipeline = [
                    new("$match", new BsonDocument{
                        {"_id", new ObjectId(id)},
                        {"deleted", false}
                    }),

                    new("$addFields", new BsonDocument
                    {
                        { "categoryObjId", new BsonDocument("$cond", new BsonDocument
                            {
                                { "if", new BsonDocument("$in", new BsonArray { "$categoryId", new BsonArray { BsonNull.Value, "" } }) },
                                { "then", BsonNull.Value },
                                { "else", new BsonDocument("$toObjectId", "$categoryId") }
                            })
                        },
                        { "cycleStartDate", new BsonDocument("$switch", new BsonDocument
                            {
                                { "branches", new BsonArray
                                    {
                                        new BsonDocument { { "case", new BsonDocument("$eq", new BsonArray { new BsonDocument("$ifNull", new BsonArray { "$period", "monthly" }), "daily" }) }, { "then", dates.dailyStart } },
                                        new BsonDocument { { "case", new BsonDocument("$eq", new BsonArray { new BsonDocument("$ifNull", new BsonArray { "$period", "monthly" }), "weekly" }) }, { "then", dates.weeklyStart } },
                                        new BsonDocument { { "case", new BsonDocument("$eq", new BsonArray { new BsonDocument("$ifNull", new BsonArray { "$period", "monthly" }), "yearly" }) }, { "then", dates.yearlyStart } },
                                    }
                                },
                                { "default", dates.monthlyStart }
                            })
                        },
                        { "cycleEndDate", new BsonDocument("$switch", new BsonDocument
                            {
                                { "branches", new BsonArray
                                    {
                                        new BsonDocument { { "case", new BsonDocument("$eq", new BsonArray { new BsonDocument("$ifNull", new BsonArray { "$period", "monthly" }), "daily" }) }, { "then", dates.dailyEnd } },
                                        new BsonDocument { { "case", new BsonDocument("$eq", new BsonArray { new BsonDocument("$ifNull", new BsonArray { "$period", "monthly" }), "weekly" }) }, { "then", dates.weeklyEnd } },
                                        new BsonDocument { { "case", new BsonDocument("$eq", new BsonArray { new BsonDocument("$ifNull", new BsonArray { "$period", "monthly" }), "yearly" }) }, { "then", dates.yearlyEnd } },
                                    }
                                },
                                { "default", dates.monthlyEnd }
                            })
                        }
                    }),

                    MongoUtil.Lookup("categories", ["$categoryObjId"], ["$_id"], "_categories", [["deleted", false]], 1),

                    new BsonDocument("$lookup", new BsonDocument
                    {
                        { "from", "operations" },
                        { "let", new BsonDocument
                            {
                                { "catId", "$categoryId" },
                                { "user", "$createdBy" },
                                { "cStart", "$cycleStartDate" },
                                { "cEnd", "$cycleEndDate" }
                            }
                        },
                        { "pipeline", new BsonArray
                            {
                                new BsonDocument("$match", new BsonDocument
                                {
                                    { "$expr", new BsonDocument
                                        {
                                            { "$and", new BsonArray
                                                {
                                                    new BsonDocument("$eq", new BsonArray { "$categoryId", "$$catId" }),
                                                    new BsonDocument("$eq", new BsonArray { "$type", "expense" }),
                                                    new BsonDocument("$eq", new BsonArray { "$deleted", false }),
                                                    new BsonDocument("$eq", new BsonArray { "$createdBy", "$$user" }),
                                                    new BsonDocument("$gte", new BsonArray { "$createdAt", "$$cStart" }),
                                                    new BsonDocument("$lte", new BsonArray { "$createdAt", "$$cEnd" })
                                                }
                                            }
                                        }
                                    }
                                })
                            }
                        },
                        { "as", "_operations" }
                    }),

                    new("$addFields", new BsonDocument
                    {
                        {"spent", new BsonDocument("$sum", new BsonDocument("$map", new BsonDocument
                        {
                            {"input", "$_operations"},
                            {"as", "op"},
                            {"in", new BsonDocument("$convert", new BsonDocument
                            {
                                {"input", "$$op.value"},
                                {"to", "double"},
                                {"onError", 0.0},
                                {"onNull", 0.0}
                            })}
                        }))},
                    }),

                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"id", new BsonDocument("$toString", "$_id")},
                        {"name", 1},
                        {"categoryId", 1},
                        {"categoryName", MongoUtil.First("_categories.name")},
                        {"limit", 1},
                        {"period", new BsonDocument("$ifNull", new BsonArray { "$period", "monthly" })},
                        {"receiveAlert", 1},
                        {"alertPercentage", 1},
                        {"remaining", new BsonDocument("$subtract", new BsonArray
                        {
                            new BsonDocument("$toDouble", "$limit"),
                            new BsonDocument("$toDouble", "$spent"),
                        })},
                        {"spent", 1},
                        {"active", 1},
                        {"createdAt", 1}
                    }),
                ];

                dynamic? entity = await repository.GetByIdAggregateAsync(pipeline);
                if (entity is null) return new(null, 404, "Orçamento não encontrado");
                return new(entity, 200, "Orçamento encontrado");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }

        public async Task<ResponseApi<List<dynamic>>> GetHistoryAsync(string budgetId, string? userId = null)
        {
            try
            {
                Budget? budget = await repository.GetByIdAsync(budgetId);
                if (budget is null) return new(null, 404, "Orçamento não encontrado");

                if (!string.IsNullOrEmpty(userId) && budget.CreatedBy != userId)
                {
                    return new(null, 403, "Acesso não autorizado ao orçamento informado.");
                }

                string period = string.IsNullOrWhiteSpace(budget.Period) ? "monthly" : budget.Period.ToLowerInvariant();
                DateTime now = DateTime.UtcNow;

                List<Operation> operations = await context.Operations
                    .Find(x => !x.Deleted && x.CreatedBy == budget.CreatedBy && x.CategoryId == budget.CategoryId && x.Type == "expense")
                    .ToListAsync();

                DateTime earliest = budget.CreatedAt;
                if (operations.Count > 0)
                {
                    DateTime minOp = operations.Min(x => x.CreatedAt);
                    if (minOp < earliest) earliest = minOp;
                }

                List<dynamic> history = [];
                CultureInfo ptBr = new("pt-BR");

                switch (period)
                {
                    case "daily":
                    {
                        DateTime minDate = earliest.Date;
                        DateTime curDate = now.Date;
                        DateTime targetMin = curDate.AddDays(-13);
                        if (targetMin < minDate) minDate = targetMin;

                        DateTime cursor = curDate;
                        while (cursor >= minDate)
                        {
                            DateTime start = new(cursor.Year, cursor.Month, cursor.Day, 0, 0, 0, DateTimeKind.Utc);
                            DateTime end = new(cursor.Year, cursor.Month, cursor.Day, 23, 59, 59, 999, DateTimeKind.Utc);
                            bool isCurrent = cursor == curDate;

                            decimal spent = operations.Where(o => o.CreatedAt >= start && o.CreatedAt <= end).Sum(o => o.Value);
                            decimal limit = budget.Limit;
                            decimal remaining = limit - spent;
                            decimal percentage = limit > 0 ? Math.Round((spent / limit) * 100m, 1) : 0;
                            string status = percentage > 100 ? "exceeded" : (percentage >= budget.AlertPercentage ? "warning" : "normal");

                            history.Add(new
                            {
                                cycleKey = cursor.ToString("yyyy-MM-dd"),
                                label = cursor.ToString("dd/MM/yyyy"),
                                startDate = start,
                                endDate = end,
                                isCurrent,
                                limit,
                                spent,
                                remaining,
                                percentage,
                                status
                            });

                            cursor = cursor.AddDays(-1);
                        }
                        break;
                    }

                    case "weekly":
                    {
                        int curDiff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                        DateTime curMonday = now.Date.AddDays(-curDiff);

                        int earliestDiff = (7 + (earliest.DayOfWeek - DayOfWeek.Monday)) % 7;
                        DateTime minMonday = earliest.Date.AddDays(-earliestDiff);
                        DateTime targetMinMonday = curMonday.AddDays(-7 * 7);
                        if (targetMinMonday < minMonday) minMonday = targetMinMonday;

                        DateTime cursorMonday = curMonday;
                        while (cursorMonday >= minMonday)
                        {
                            DateTime cursorSunday = cursorMonday.AddDays(6);
                            DateTime start = new(cursorMonday.Year, cursorMonday.Month, cursorMonday.Day, 0, 0, 0, DateTimeKind.Utc);
                            DateTime end = new(cursorSunday.Year, cursorSunday.Month, cursorSunday.Day, 23, 59, 59, 999, DateTimeKind.Utc);
                            bool isCurrent = cursorMonday == curMonday;

                            decimal spent = operations.Where(o => o.CreatedAt >= start && o.CreatedAt <= end).Sum(o => o.Value);
                            decimal limit = budget.Limit;
                            decimal remaining = limit - spent;
                            decimal percentage = limit > 0 ? Math.Round((spent / limit) * 100m, 1) : 0;
                            string status = percentage > 100 ? "exceeded" : (percentage >= budget.AlertPercentage ? "warning" : "normal");

                            history.Add(new
                            {
                                cycleKey = $"{cursorMonday:yyyy}-W{GetIso8601WeekOfYear(cursorMonday):D2}",
                                label = $"Semana {cursorMonday:dd/MM} a {cursorSunday:dd/MM/yyyy}",
                                startDate = start,
                                endDate = end,
                                isCurrent,
                                limit,
                                spent,
                                remaining,
                                percentage,
                                status
                            });

                            cursorMonday = cursorMonday.AddDays(-7);
                        }
                        break;
                    }

                    case "yearly":
                    {
                        int minYear = earliest.Year;
                        int curYear = now.Year;
                        if (curYear - 2 < minYear) minYear = curYear - 2;

                        for (int y = curYear; y >= minYear; y--)
                        {
                            DateTime start = new(y, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                            DateTime end = new(y, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc);
                            bool isCurrent = y == curYear;

                            decimal spent = operations.Where(o => o.CreatedAt >= start && o.CreatedAt <= end).Sum(o => o.Value);
                            decimal limit = budget.Limit;
                            decimal remaining = limit - spent;
                            decimal percentage = limit > 0 ? Math.Round((spent / limit) * 100m, 1) : 0;
                            string status = percentage > 100 ? "exceeded" : (percentage >= budget.AlertPercentage ? "warning" : "normal");

                            history.Add(new
                            {
                                cycleKey = y.ToString(),
                                label = $"Ano {y}",
                                startDate = start,
                                endDate = end,
                                isCurrent,
                                limit,
                                spent,
                                remaining,
                                percentage,
                                status
                            });
                        }
                        break;
                    }

                    default: // "monthly"
                    {
                        DateTime minMonthDate = new(earliest.Year, earliest.Month, 1);
                        DateTime curMonthDate = new(now.Year, now.Month, 1);
                        DateTime targetMinMonth = curMonthDate.AddMonths(-5);
                        if (targetMinMonth < minMonthDate) minMonthDate = targetMinMonth;

                        DateTime cursorMonth = curMonthDate;
                        while (cursorMonth >= minMonthDate)
                        {
                            int daysInMonth = DateTime.DaysInMonth(cursorMonth.Year, cursorMonth.Month);
                            DateTime start = new(cursorMonth.Year, cursorMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                            DateTime end = new(cursorMonth.Year, cursorMonth.Month, daysInMonth, 23, 59, 59, 999, DateTimeKind.Utc);
                            bool isCurrent = cursorMonth.Year == now.Year && cursorMonth.Month == now.Month;

                            string monthName = ptBr.DateTimeFormat.GetMonthName(cursorMonth.Month);
                            monthName = char.ToUpper(monthName[0]) + monthName[1..];
                            string label = $"{monthName}/{cursorMonth.Year}";

                            decimal spent = operations.Where(o => o.CreatedAt >= start && o.CreatedAt <= end).Sum(o => o.Value);
                            decimal limit = budget.Limit;
                            decimal remaining = limit - spent;
                            decimal percentage = limit > 0 ? Math.Round((spent / limit) * 100m, 1) : 0;
                            string status = percentage > 100 ? "exceeded" : (percentage >= budget.AlertPercentage ? "warning" : "normal");

                            history.Add(new
                            {
                                cycleKey = cursorMonth.ToString("yyyy-MM"),
                                label,
                                startDate = start,
                                endDate = end,
                                isCurrent,
                                limit,
                                spent,
                                remaining,
                                percentage,
                                status
                            });

                            cursorMonth = cursorMonth.AddMonths(-1);
                        }
                        break;
                    }
                }

                return new(history, 200, "Histórico de ciclos obtido com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro ao obter histórico de ciclos: {ex.Message}");
            }
        }

        private static int GetIso8601WeekOfYear(DateTime time)
        {
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }
        #endregion

        #region CREATE
        public async Task<ResponseApi<Budget?>> CreateAsync(CreateBudgetRequest request)
        {
            try
            {
                Budget budget = ObjectMapper.Map<CreateBudgetRequest, Budget>(request);
                if (string.IsNullOrWhiteSpace(budget.Period)) budget.Period = "monthly";

                Budget? response = await repository.CreateAsync(budget);
                if (response is null) return new(null, 400, "Falha ao criar orçamento.");

                return new(response, 201, "Orçamento criado com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
            }
        }
        #endregion

        #region UPDATE
        public async Task<ResponseApi<Budget?>> UpdateAsync(UpdateBudgetRequest request)
        {
            try
            {
                Budget? existed = await repository.GetByIdAsync(request.Id);
                if (existed is null) return new(null, 404, "Falha ao atualizar. Orçamento não encontrado");

                string budgetName = request.Name;
                if (string.IsNullOrWhiteSpace(budgetName))
                {
                    Category? category = await categoryRepository.GetByIdAsync(request.CategoryId);
                    budgetName = category?.Name ?? existed.Name;
                }

                existed.UpdatedAt = DateTime.UtcNow;
                existed.UpdatedBy = request.UpdatedBy;
                existed.Name = budgetName;
                existed.CategoryId = request.CategoryId;
                existed.Limit = request.Limit;
                existed.Period = string.IsNullOrWhiteSpace(request.Period) ? (existed.Period ?? "monthly") : request.Period;
                existed.ReceiveAlert = request.ReceiveAlert;
                existed.AlertPercentage = request.AlertPercentage;

                Budget? response = await repository.UpdateAsync(existed);
                if (response is null) return new(null, 400, "Falha ao atualizar");

                return new(response, 200, "Atualizado com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion

        #region DELETE
        public async Task<ResponseApi<Budget>> DeleteAsync(DeleteRequest request)
        {
            try
            {
                Budget? entity = await repository.GetByIdAsync(request.Id);
                if (entity is null) return new(null, 404, "Orçamento não encontrado");

                entity.Deleted = true;
                entity.DeletedAt = DateTime.UtcNow;
                entity.DeletedBy = request.DeletedBy;

                await repository.DeleteAsync(entity);

                return new(entity, 204, "Orçamento excluído com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion
    }
}