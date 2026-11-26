using System.Globalization;
using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests.CreditCard;
using api_finances.src.Shared.DTOs;
using MongoDB.Driver;

namespace api_finances.src.Services
{
    public class CreditCardService(
        AppDbContext context,
        IOperationRepository operationRepository
    ) : ICreditCardService
    {
        private static readonly CultureInfo PtBr = new("pt-BR");

        public async Task<ResponseApi<List<CreditCardResponseDto>>> GetAllAsync(string userId)
        {
            try
            {
                var cards = await context.CreditCards
                    .Find(c => !c.Deleted && c.CreatedBy == userId)
                    .SortBy(c => c.Name)
                    .ToListAsync();

                var banks = await context.Banks
                    .Find(b => !b.Deleted && b.CreatedBy == userId)
                    .ToListAsync();
                var bankMap = banks.ToDictionary(b => b.Id, b => b.Name);

                var cardIds = cards.Select(c => c.Id).ToList();
                var unpaidInvoices = cardIds.Count > 0
                    ? await context.CreditCardInvoices
                        .Find(i => !i.Deleted && cardIds.Contains(i.CreditCardId) && i.Status != "paid")
                        .ToListAsync()
                    : [];

                var unpaidInvoiceIds = unpaidInvoices.Select(i => i.Id).ToHashSet();
                var unpaidTxs = unpaidInvoiceIds.Count > 0
                    ? await context.CreditCardTransactions
                        .Find(t => !t.Deleted && unpaidInvoiceIds.Contains(t.InvoiceId))
                        .ToListAsync()
                    : [];

                var usedByCard = unpaidTxs
                    .GroupBy(t => t.CreditCardId)
                    .ToDictionary(g => g.Key, g => Math.Round(g.Sum(t => t.Value), 2));

                var result = cards.Select(card =>
                {
                    decimal used = usedByCard.TryGetValue(card.Id, out decimal u) ? u : 0m;
                    decimal available = Math.Round(card.Limit - used, 2);
                    decimal usagePct = card.Limit > 0 ? Math.Round((used / card.Limit) * 100m, 1) : 0m;
                    int bestDay = card.ClosingDay >= 31 ? 1 : card.ClosingDay + 1;

                    return new CreditCardResponseDto
                    {
                        Id = card.Id,
                        Name = card.Name,
                        BankId = card.BankId,
                        BankName = !string.IsNullOrEmpty(card.BankId) && bankMap.TryGetValue(card.BankId, out string? bn) ? bn : null,
                        Brand = card.Brand,
                        Limit = card.Limit,
                        UsedLimit = used,
                        AvailableLimit = available,
                        UsagePercent = usagePct,
                        ClosingDay = card.ClosingDay,
                        DueDay = card.DueDay,
                        BestPurchaseDay = bestDay,
                        Color = string.IsNullOrWhiteSpace(card.Color) ? "#820ad1" : card.Color,
                        LastDigits = card.LastDigits,
                        CreatedAt = card.CreatedAt
                    };
                }).ToList();

                return new(result, 200, "Cartões listados com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao listar cartões de crédito: {ex.Message}");
            }
        }

        public async Task<ResponseApi<CreditCard?>> CreateAsync(string userId, CreateCreditCardRequest request)
        {
            try
            {
                var card = new CreditCard
                {
                    Name = request.Name.Trim(),
                    BankId = string.IsNullOrWhiteSpace(request.BankId) ? null : request.BankId.Trim(),
                    Brand = string.IsNullOrWhiteSpace(request.Brand) ? "Mastercard" : request.Brand.Trim(),
                    Limit = request.Limit,
                    ClosingDay = Math.Clamp(request.ClosingDay, 1, 31),
                    DueDay = Math.Clamp(request.DueDay, 1, 31),
                    Color = string.IsNullOrWhiteSpace(request.Color) ? "#820ad1" : request.Color.Trim(),
                    LastDigits = string.IsNullOrWhiteSpace(request.LastDigits) ? null : request.LastDigits.Trim(),
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Active = true,
                    Deleted = false
                };

                await context.CreditCards.InsertOneAsync(card);

                // Cria automaticamente a fatura do ciclo vigente
                var (currYear, currMonth) = GetInvoiceMonthYearForDate(DateTime.UtcNow, card.ClosingDay);
                await GetOrCreateInvoiceAsync(userId, card, currYear, currMonth);

                return new(card, 201, "Cartão de crédito criado com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao criar cartão de crédito: {ex.Message}");
            }
        }

        public async Task<ResponseApi<CreditCard?>> UpdateAsync(string userId, string id, UpdateCreditCardRequest request)
        {
            try
            {
                var card = await context.CreditCards.Find(c => c.Id == id && !c.Deleted && c.CreatedBy == userId).FirstOrDefaultAsync();
                if (card is null) return new(null, 404, "Cartão de crédito não encontrado.");

                card.Name = request.Name.Trim();
                card.BankId = string.IsNullOrWhiteSpace(request.BankId) ? null : request.BankId.Trim();
                card.Brand = string.IsNullOrWhiteSpace(request.Brand) ? "Mastercard" : request.Brand.Trim();
                card.Limit = request.Limit;
                card.ClosingDay = Math.Clamp(request.ClosingDay, 1, 31);
                card.DueDay = Math.Clamp(request.DueDay, 1, 31);
                card.Color = string.IsNullOrWhiteSpace(request.Color) ? "#820ad1" : request.Color.Trim();
                card.LastDigits = string.IsNullOrWhiteSpace(request.LastDigits) ? null : request.LastDigits.Trim();
                card.UpdatedAt = DateTime.UtcNow;
                card.UpdatedBy = userId;

                await context.CreditCards.ReplaceOneAsync(c => c.Id == card.Id, card);
                return new(card, 200, "Cartão de crédito atualizado com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao atualizar cartão de crédito: {ex.Message}");
            }
        }

        public async Task<ResponseApi<bool>> DeleteAsync(string userId, string id)
        {
            try
            {
                var card = await context.CreditCards.Find(c => c.Id == id && !c.Deleted && c.CreatedBy == userId).FirstOrDefaultAsync();
                if (card is null) return new(false, 404, "Cartão de crédito não encontrado.");

                DateTime now = DateTime.UtcNow;
                card.Deleted = true;
                card.DeletedAt = now;
                card.DeletedBy = userId;
                await context.CreditCards.ReplaceOneAsync(c => c.Id == card.Id, card);

                await context.CreditCardInvoices.UpdateManyAsync(
                    i => i.CreditCardId == id && !i.Deleted,
                    Builders<CreditCardInvoice>.Update
                        .Set(i => i.Deleted, true)
                        .Set(i => i.DeletedAt, now)
                        .Set(i => i.DeletedBy, userId));

                await context.CreditCardTransactions.UpdateManyAsync(
                    t => t.CreditCardId == id && !t.Deleted,
                    Builders<CreditCardTransaction>.Update
                        .Set(t => t.Deleted, true)
                        .Set(t => t.DeletedAt, now)
                        .Set(t => t.DeletedBy, userId));

                return new(true, 200, "Cartão de crédito excluído com sucesso.");
            }
            catch (Exception ex)
            {
                return new(false, 500, $"Erro ao excluir cartão de crédito: {ex.Message}");
            }
        }

        public async Task<ResponseApi<List<CreditCardInvoiceResponseDto>>> GetInvoicesAsync(string userId, string cardId)
        {
            try
            {
                var card = await context.CreditCards.Find(c => c.Id == cardId && !c.Deleted && c.CreatedBy == userId).FirstOrDefaultAsync();
                if (card is null) return new(null, 404, "Cartão de crédito não encontrado.");

                DateTime now = DateTime.UtcNow;
                var (currYear, currMonth) = GetInvoiceMonthYearForDate(now, card.ClosingDay);
                await GetOrCreateInvoiceAsync(userId, card, currYear, currMonth);

                var invoices = await context.CreditCardInvoices
                    .Find(i => i.CreditCardId == cardId && !i.Deleted)
                    .SortBy(i => i.Year)
                    .ThenBy(i => i.Month)
                    .ToListAsync();

                var allTxs = await context.CreditCardTransactions
                    .Find(t => t.CreditCardId == cardId && !t.Deleted)
                    .ToListAsync();

                var txByInvoice = allTxs
                    .GroupBy(t => t.InvoiceId)
                    .ToDictionary(g => g.Key, g => (Count: g.Count(), Total: Math.Round(g.Sum(x => x.Value), 2)));

                var banks = await context.Banks
                    .Find(b => !b.Deleted && b.CreatedBy == userId)
                    .ToListAsync();
                var bankMap = banks.ToDictionary(b => b.Id, b => b.Name);

                var result = new List<CreditCardInvoiceResponseDto>();
                foreach (var inv in invoices)
                {
                    var stats = txByInvoice.TryGetValue(inv.Id, out var s) ? s : (Count: 0, Total: 0m);

                    // Atualiza status e totalAmount se necessário
                    string effectiveStatus = inv.Status;
                    if (effectiveStatus == "open" && now > inv.ClosingDate)
                    {
                        effectiveStatus = "closed";
                    }

                    if (inv.TotalAmount != stats.Total || inv.Status != effectiveStatus)
                    {
                        inv.TotalAmount = stats.Total;
                        inv.Status = effectiveStatus;
                        inv.UpdatedAt = now;
                        await context.CreditCardInvoices.ReplaceOneAsync(x => x.Id == inv.Id, inv);
                    }

                    bool isFuture = inv.Year > currYear || (inv.Year == currYear && inv.Month > currMonth);
                    string shortMonth = PtBr.DateTimeFormat.GetAbbreviatedMonthName(inv.Month).TrimEnd('.');
                    shortMonth = char.ToUpper(shortMonth[0]) + shortMonth[1..];

                    result.Add(new CreditCardInvoiceResponseDto
                    {
                        Id = inv.Id,
                        CreditCardId = inv.CreditCardId,
                        Month = inv.Month,
                        Year = inv.Year,
                        Label = $"{shortMonth}/{inv.Year}",
                        ClosingDate = inv.ClosingDate,
                        DueDate = inv.DueDate,
                        Status = inv.Status,
                        IsFuture = isFuture,
                        PaidAt = inv.PaidAt,
                        PaymentBankId = inv.PaymentBankId,
                        PaymentBankName = !string.IsNullOrEmpty(inv.PaymentBankId) && bankMap.TryGetValue(inv.PaymentBankId, out string? bn) ? bn : null,
                        TotalAmount = stats.Total,
                        TransactionCount = stats.Count
                    });
                }

                return new(result, 200, "Faturas listadas com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao listar faturas: {ex.Message}");
            }
        }

        public async Task<ResponseApi<List<CreditCardTransactionResponseDto>>> GetInvoiceTransactionsAsync(string userId, string cardId, string invoiceId)
        {
            try
            {
                var card = await context.CreditCards.Find(c => c.Id == cardId && !c.Deleted && c.CreatedBy == userId).FirstOrDefaultAsync();
                if (card is null) return new(null, 404, "Cartão de crédito não encontrado.");

                var txs = await context.CreditCardTransactions
                    .Find(t => t.CreditCardId == cardId && t.InvoiceId == invoiceId && !t.Deleted)
                    .SortByDescending(t => t.Date)
                    .ThenByDescending(t => t.CreatedAt)
                    .ToListAsync();

                var categories = await context.Categories
                    .Find(c => !c.Deleted && c.CreatedBy == userId)
                    .ToListAsync();
                var catMap = categories.ToDictionary(c => c.Id, c => c.Name);

                var result = txs.Select(t => new CreditCardTransactionResponseDto
                {
                    Id = t.Id,
                    CreditCardId = t.CreditCardId,
                    InvoiceId = t.InvoiceId,
                    CategoryId = t.CategoryId,
                    CategoryName = !string.IsNullOrEmpty(t.CategoryId) && catMap.TryGetValue(t.CategoryId, out string? cn) ? cn : "Sem categoria",
                    Description = t.Description,
                    Value = t.Value,
                    Date = t.Date,
                    InstallmentNumber = t.InstallmentNumber,
                    TotalInstallments = t.TotalInstallments,
                    InstallmentGroupId = t.InstallmentGroupId
                }).ToList();

                return new(result, 200, "Lançamentos da fatura listados com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao listar lançamentos da fatura: {ex.Message}");
            }
        }

        public async Task<ResponseApi<List<CreditCardTransaction>>> CreateTransactionAsync(string userId, string cardId, CreateCreditCardTransactionRequest request)
        {
            try
            {
                var card = await context.CreditCards.Find(c => c.Id == cardId && !c.Deleted && c.CreatedBy == userId).FirstOrDefaultAsync();
                if (card is null) return new(null, 404, "Cartão de crédito não encontrado.");

                int totalInstallments = Math.Clamp(request.Installments, 1, 48);
                DateTime txDate = DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Utc);

                // Regra de fechamento: até closingDay entra no mês da compra; após closingDay entra no mês seguinte
                var (startYear, startMonth) = GetInvoiceMonthYearForDate(txDate, card.ClosingDay);

                decimal baseInstallment = Math.Floor((request.Value / totalInstallments) * 100m) / 100m;
                decimal remainder = Math.Round(request.Value - (baseInstallment * totalInstallments), 2);

                string? groupId = totalInstallments > 1 ? Guid.NewGuid().ToString("N") : null;
                List<CreditCardTransaction> created = [];
                DateTime now = DateTime.UtcNow;

                for (int i = 1; i <= totalInstallments; i++)
                {
                    DateTime targetRef = new DateTime(startYear, startMonth, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(i - 1);
                    var invoice = await GetOrCreateInvoiceAsync(userId, card, targetRef.Year, targetRef.Month);

                    decimal installmentValue = i == 1 ? baseInstallment + remainder : baseInstallment;

                    var tx = new CreditCardTransaction
                    {
                        CreditCardId = card.Id,
                        InvoiceId = invoice.Id,
                        CategoryId = string.IsNullOrWhiteSpace(request.CategoryId) ? null : request.CategoryId.Trim(),
                        Description = request.Description.Trim(),
                        Value = installmentValue,
                        Date = txDate,
                        InstallmentNumber = i,
                        TotalInstallments = totalInstallments,
                        InstallmentGroupId = groupId,
                        CreatedBy = userId,
                        CreatedAt = now,
                        UpdatedAt = now,
                        Active = true,
                        Deleted = false
                    };

                    await context.CreditCardTransactions.InsertOneAsync(tx);
                    created.Add(tx);

                    await RecalculateInvoiceTotalAsync(invoice.Id);
                }

                return new(created, 201, totalInstallments > 1
                    ? $"Compra parcelada em {totalInstallments}x lançada com sucesso!"
                    : "Compra lançada com sucesso!");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao lançar compra no cartão: {ex.Message}");
            }
        }

        public async Task<ResponseApi<CreditCardTransaction?>> UpdateTransactionAsync(string userId, string cardId, string txId, UpdateCreditCardTransactionRequest request)
        {
            try
            {
                var tx = await context.CreditCardTransactions
                    .Find(t => t.Id == txId && t.CreditCardId == cardId && !t.Deleted && t.CreatedBy == userId)
                    .FirstOrDefaultAsync();
                if (tx is null) return new(null, 404, "Lançamento não encontrado.");

                tx.Description = request.Description.Trim();
                tx.CategoryId = string.IsNullOrWhiteSpace(request.CategoryId) ? null : request.CategoryId.Trim();
                tx.Value = request.Value;
                tx.Date = DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Utc);
                tx.UpdatedAt = DateTime.UtcNow;
                tx.UpdatedBy = userId;

                await context.CreditCardTransactions.ReplaceOneAsync(t => t.Id == tx.Id, tx);
                await RecalculateInvoiceTotalAsync(tx.InvoiceId);

                return new(tx, 200, "Lançamento atualizado com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao atualizar lançamento: {ex.Message}");
            }
        }

        public async Task<ResponseApi<bool>> DeleteTransactionAsync(string userId, string cardId, string txId, bool deleteGroup = false)
        {
            try
            {
                var tx = await context.CreditCardTransactions
                    .Find(t => t.Id == txId && t.CreditCardId == cardId && !t.Deleted && t.CreatedBy == userId)
                    .FirstOrDefaultAsync();
                if (tx is null) return new(false, 404, "Lançamento não encontrado.");

                DateTime now = DateTime.UtcNow;
                HashSet<string> affectedInvoiceIds = [tx.InvoiceId];

                if (deleteGroup && !string.IsNullOrEmpty(tx.InstallmentGroupId))
                {
                    var groupTxs = await context.CreditCardTransactions
                        .Find(t => t.InstallmentGroupId == tx.InstallmentGroupId && t.CreditCardId == cardId && !t.Deleted)
                        .ToListAsync();

                    foreach (var gt in groupTxs) affectedInvoiceIds.Add(gt.InvoiceId);

                    await context.CreditCardTransactions.UpdateManyAsync(
                        t => t.InstallmentGroupId == tx.InstallmentGroupId && t.CreditCardId == cardId && !t.Deleted,
                        Builders<CreditCardTransaction>.Update
                            .Set(t => t.Deleted, true)
                            .Set(t => t.DeletedAt, now)
                            .Set(t => t.DeletedBy, userId));
                }
                else
                {
                    tx.Deleted = true;
                    tx.DeletedAt = now;
                    tx.DeletedBy = userId;
                    await context.CreditCardTransactions.ReplaceOneAsync(t => t.Id == tx.Id, tx);
                }

                foreach (var invId in affectedInvoiceIds)
                {
                    await RecalculateInvoiceTotalAsync(invId);
                }

                return new(true, 200, "Lançamento excluído com sucesso.");
            }
            catch (Exception ex)
            {
                return new(false, 500, $"Erro ao excluir lançamento: {ex.Message}");
            }
        }

        public async Task<ResponseApi<CreditCardInvoice?>> PayInvoiceAsync(string userId, string cardId, string invoiceId, PayCreditCardInvoiceRequest request)
        {
            try
            {
                var card = await context.CreditCards.Find(c => c.Id == cardId && !c.Deleted && c.CreatedBy == userId).FirstOrDefaultAsync();
                if (card is null) return new(null, 404, "Cartão de crédito não encontrado.");

                var invoice = await context.CreditCardInvoices
                    .Find(i => i.Id == invoiceId && i.CreditCardId == cardId && !i.Deleted)
                    .FirstOrDefaultAsync();
                if (invoice is null) return new(null, 404, "Fatura não encontrada.");

                if (invoice.Status == "paid")
                    return new(null, 400, "Esta fatura já foi paga.");

                await RecalculateInvoiceTotalAsync(invoice.Id);
                invoice = await context.CreditCardInvoices.Find(i => i.Id == invoiceId).FirstAsync();

                decimal amountToPay = request.Value.HasValue && request.Value.Value > 0
                    ? request.Value.Value
                    : invoice.TotalAmount;

                DateTime paidAt = request.PaidAt ?? DateTime.UtcNow;

                invoice.Status = "paid";
                invoice.PaidAt = paidAt;
                invoice.PaymentBankId = request.PaymentBankId;
                invoice.UpdatedAt = DateTime.UtcNow;
                invoice.UpdatedBy = userId;
                await context.CreditCardInvoices.ReplaceOneAsync(i => i.Id == invoice.Id, invoice);

                if (amountToPay > 0 && !string.IsNullOrWhiteSpace(request.PaymentBankId))
                {
                    // Tenta localizar uma categoria de cartão de crédito do usuário (opcional)
                    var ccCategory = await context.Categories
                        .Find(c => !c.Deleted && c.CreatedBy == userId && c.Type == "expense" &&
                                   (c.Name.ToLower().Contains("cartão") || c.Name.ToLower().Contains("cartao")))
                        .FirstOrDefaultAsync();

                    Operation op = new()
                    {
                        Description = $"Fatura {card.Name} ({invoice.Month:D2}/{invoice.Year})",
                        Value = amountToPay,
                        Type = "expense",
                        BankId = request.PaymentBankId,
                        CategoryId = ccCategory?.Id ?? string.Empty,
                        Origin = "credit_card_invoice",
                        OriginId = invoice.Id,
                        CreatedAt = paidAt,
                        CreatedBy = userId,
                        Active = true,
                        Deleted = false
                    };
                    await operationRepository.CreateAsync(op);
                }

                return new(invoice, 200, "Fatura paga com sucesso! O limite foi liberado.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao pagar fatura: {ex.Message}");
            }
        }

        private static (int Year, int Month) GetInvoiceMonthYearForDate(DateTime date, int closingDay)
        {
            int maxDayInMonth = DateTime.DaysInMonth(date.Year, date.Month);
            int effectiveClosingDay = Math.Min(closingDay, maxDayInMonth);

            if (date.Day <= effectiveClosingDay)
            {
                return (date.Year, date.Month);
            }

            DateTime nextMonth = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
            return (nextMonth.Year, nextMonth.Month);
        }

        private async Task<CreditCardInvoice> GetOrCreateInvoiceAsync(string userId, CreditCard card, int year, int month)
        {
            var existing = await context.CreditCardInvoices
                .Find(i => i.CreditCardId == card.Id && i.Year == year && i.Month == month && !i.Deleted)
                .FirstOrDefaultAsync();
            if (existing != null) return existing;

            int closingDay = Math.Clamp(card.ClosingDay, 1, DateTime.DaysInMonth(year, month));
            DateTime closingDate = new(year, month, closingDay, 23, 59, 59, DateTimeKind.Utc);

            // Se o dia de vencimento é menor que o de fechamento, vence no mês seguinte
            DateTime dueRef = card.DueDay < card.ClosingDay
                ? new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1)
                : new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            int dueDay = Math.Clamp(card.DueDay, 1, DateTime.DaysInMonth(dueRef.Year, dueRef.Month));
            DateTime dueDate = new(dueRef.Year, dueRef.Month, dueDay, 23, 59, 59, DateTimeKind.Utc);

            var invoice = new CreditCardInvoice
            {
                CreditCardId = card.Id,
                Month = month,
                Year = year,
                ClosingDate = closingDate,
                DueDate = dueDate,
                Status = DateTime.UtcNow > closingDate ? "closed" : "open",
                TotalAmount = 0,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Active = true,
                Deleted = false
            };

            await context.CreditCardInvoices.InsertOneAsync(invoice);
            return invoice;
        }

        private async Task RecalculateInvoiceTotalAsync(string invoiceId)
        {
            var txs = await context.CreditCardTransactions
                .Find(t => t.InvoiceId == invoiceId && !t.Deleted)
                .ToListAsync();
            decimal total = Math.Round(txs.Sum(t => t.Value), 2);

            await context.CreditCardInvoices.UpdateOneAsync(
                i => i.Id == invoiceId,
                Builders<CreditCardInvoice>.Update
                    .Set(i => i.TotalAmount, total)
                    .Set(i => i.UpdatedAt, DateTime.UtcNow));
        }
    }
}
