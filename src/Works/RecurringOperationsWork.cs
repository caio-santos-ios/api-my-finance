using api_finances.src.Infraestructure;
using api_finances.src.Models;
using MongoDB.Driver;

namespace api_finances.src.Works
{
    public class RecurringOperationsWork(IServiceProvider serviceProvider, ILogger<RecurringOperationsWork> _logger) : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(120);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessRecurringOperations(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro no background worker de operações recorrentes.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }

        private async Task ProcessRecurringOperations(CancellationToken ct)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await CreateRecurringOperationsAsync(context, ct);
        }

        private async Task CreateRecurringOperationsAsync(AppDbContext context, CancellationToken ct)
        {
            List<Operation> operations = await context.Operations
                .Find(x => !x.Deleted && x.Active && x.Repeat && x.Recurrence != "" && x.Origin != "recurrence")
                .ToListAsync(ct);

            if (operations.Count == 0) return;

            DateTime today = DateTime.UtcNow.Date;

            foreach (Operation operation in operations)
            {
                try
                {
                    DateTime? nextDate = GetNextDate(operation.CreatedAt.Date, operation.Recurrence);
                    if (nextDate is null) continue;

                    while (nextDate.Value <= today)
                    {
                        string originId = $"{operation.Id}-{nextDate.Value:yyyy-MM-dd}";

                        bool exists = await context.Operations
                            .Find(x => !x.Deleted && x.Origin == "recurrence" && x.OriginId == originId)
                            .AnyAsync(ct);

                        if (!exists)
                        {
                            Operation newOperation = new()
                            {
                                CategoryId = operation.CategoryId,
                                BankId = operation.BankId,
                                DestinationBankId = operation.DestinationBankId,
                                Description = operation.Description,
                                Value = operation.Value,
                                Type = operation.Type,
                                Origin = "recurrence",
                                OriginId = originId,
                                Repeat = false,
                                Recurrence = string.Empty,
                                Active = true,
                                CreatedAt = nextDate.Value,
                                UpdatedAt = DateTime.UtcNow,
                                CreatedBy = operation.CreatedBy,
                                UpdatedBy = operation.CreatedBy
                            };

                            await context.Operations.InsertOneAsync(newOperation, cancellationToken: ct);
                        }

                        nextDate = GetNextDate(nextDate.Value, operation.Recurrence);
                        if (nextDate is null) break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao processar operação recorrente {Id}", operation.Id);
                }
            }
        }

        private static DateTime? GetNextDate(DateTime baseDate, string recurrence)
        {
            return recurrence.ToLowerInvariant() switch
            {
                "weekly" => baseDate.AddDays(7),
                "monthly" => baseDate.AddMonths(1),
                "yearly" => baseDate.AddYears(1),
                _ => null
            };
        }
    }
}
