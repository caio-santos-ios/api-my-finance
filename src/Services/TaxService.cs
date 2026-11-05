using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;
using api_finances.src.Utils;
using ClosedXML.Excel;

namespace api_finances.src.Services
{
    public class TaxService(
        ITaxRepository repository
    ) : ITaxService
    {
        private static (DateTime start, DateTime end, DateTime prevEnd) GetYearBounds(int baseYear)
        {
            DateTime start = new(baseYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(baseYear, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc);
            DateTime prevEnd = start.AddMilliseconds(-1);
            return (start, end, prevEnd);
        }

        private static (decimal qty, decimal cost) CalculateInvestmentPosition(List<InvestmentTransaction> txs, DateTime upToDate)
        {
            var relevant = txs.Where(t => t.Date <= upToDate).OrderBy(t => t.Date).ToList();
            decimal totalQty = 0;
            decimal totalCost = 0;

            foreach (var tx in relevant)
            {
                string k = tx.Kind?.ToUpperInvariant() ?? "";
                if (k == "BUY" || k == "COMPRA")
                {
                    totalQty += tx.Quantity;
                    totalCost += tx.Amount;
                }
                else if (k == "SELL" || k == "VENDA")
                {
                    if (totalQty > 0)
                    {
                        decimal avgPrice = totalCost / totalQty;
                        totalQty -= tx.Quantity;
                        if (totalQty < 0) totalQty = 0;
                        totalCost = totalQty * avgPrice;
                    }
                }
            }

            return (totalQty, totalCost);
        }

        private async Task<TaxPreviewDto> BuildTaxPreviewInternalAsync(int baseYear, string userId)
        {
            var (start, end, prevEnd) = GetYearBounds(baseYear);
            var categories = await repository.GetCategoriesAsync(userId);
            var categoryMap = categories.ToDictionary(c => c.Id, c => c);

            var operations = await repository.GetOperationsByPeriodAsync(userId, start, end);
            var validOps = operations.Where(o => o.Type != "transfer" && string.IsNullOrEmpty(o.DestinationBankId)).ToList();

            List<TaxIncomeItemDto> taxable = [];
            List<TaxIncomeItemDto> exempt = [];
            List<TaxIncomeItemDto> exclusive = [];

            foreach (var op in validOps.Where(o => o.Type == "income" && o.Origin != "investment"))
            {
                if (categoryMap.TryGetValue(op.CategoryId, out var cat))
                {
                    var item = new TaxIncomeItemDto
                    {
                        Id = op.Id,
                        CategoryName = cat.Name,
                        CategoryId = cat.Id,
                        Description = op.Description,
                        PayerDocument = op.PayeeDocument,
                        TotalValue = op.Value,
                        Date = op.CreatedAt,
                        IrTreatment = cat.IrTreatment,
                        Source = "operation"
                    };

                    string treatment = (cat.IrTreatment ?? "").Trim().ToUpperInvariant();
                    string catNameLower = (cat.Name ?? "").Trim().ToLowerInvariant();
                    bool isLikelyTaxable = catNameLower.Contains("sal") ||
                                           catNameLower.Contains("remunera") ||
                                           catNameLower.Contains("pró-labore") ||
                                           catNameLower.Contains("pro-labore") ||
                                           catNameLower.Contains("holerite");

                    if (treatment == "TAXABLE_INCOME" || treatment == "TAXABLE" || treatment == "TRIBUTAVEL" || (string.IsNullOrEmpty(treatment) && isLikelyTaxable))
                    {
                        item.IrTreatment = "TAXABLE_INCOME";
                        taxable.Add(item);
                    }
                    else if (treatment == "EXEMPT_INCOME" || treatment == "EXEMPT" || treatment == "ISENTO") exempt.Add(item);
                    else if (treatment == "EXCLUSIVE_INCOME" || treatment == "EXCLUSIVE" || treatment == "EXCLUSIVO") exclusive.Add(item);
                }
            }

            var invTransactions = await repository.GetInvestmentTransactionsAsync(userId, end);
            var invAssets = await repository.GetInvestmentAssetsAsync(userId);
            var invAssetMap = invAssets.ToDictionary(a => a.Id, a => a);
            var yearInvTxs = invTransactions.Where(t => t.Date >= start && t.Date <= end).ToList();

            foreach (var it in yearInvTxs)
            {
                string ticker = invAssetMap.TryGetValue(it.InvestmentAssetId, out var a) ? a.Ticker : "Ativo";
                string desc = !string.IsNullOrEmpty(it.Notes) ? $"{ticker} - {it.Notes}" : $"{ticker} - Provento";
                string kind = it.Kind?.ToUpperInvariant() ?? "";

                if (kind == "DIVIDEND" || kind == "RENDIMENTO" || kind == "INCOME" || kind == "DIVIDENDOS" || kind == "RENDIMENTOS")
                {
                    exempt.Add(new TaxIncomeItemDto
                    {
                        Id = it.Id,
                        CategoryName = (kind == "DIVIDEND" || kind == "DIVIDENDOS") ? "Dividendos" : "Rendimentos FII / Cupons",
                        CategoryId = "",
                        Description = desc,
                        TotalValue = it.Amount,
                        Date = it.Date,
                        IrTreatment = "EXEMPT_INCOME",
                        Source = "investment"
                    });
                }
                else if (kind == "JCP")
                {
                    exclusive.Add(new TaxIncomeItemDto
                    {
                        Id = it.Id,
                        CategoryName = "Juros sobre Capital Próprio (JCP)",
                        CategoryId = "",
                        Description = desc,
                        TotalValue = it.Amount,
                        Date = it.Date,
                        IrTreatment = "EXCLUSIVE_INCOME",
                        Source = "investment"
                    });
                }
            }

            var deductionOps = validOps.Where(o => o.Type == "expense" && categoryMap.TryGetValue(o.CategoryId, out var cat) && cat.IrTreatment == "DEDUCTION").ToList();
            var opIds = deductionOps.Select(o => o.Id).ToList();
            var attachments = await repository.GetAttachmentsByOriginIdsAsync(opIds);
            var attachmentOriginSet = attachments.Select(att => att.ParentId).ToHashSet();

            List<TaxDeductionItemDto> deductions = [];
            foreach (var op in deductionOps)
            {
                var cat = categoryMap[op.CategoryId];
                bool hasDoc = !string.IsNullOrWhiteSpace(op.PayeeDocument);
                bool hasReceipt = attachmentOriginSet.Contains(op.Id);

                deductions.Add(new TaxDeductionItemDto
                {
                    Id = op.Id,
                    Description = op.Description,
                    CategoryName = cat.Name,
                    CategoryId = cat.Id,
                    Subtype = !string.IsNullOrEmpty(cat.IrSubtype) ? cat.IrSubtype : "other",
                    PayeeName = op.PayeeName,
                    PayeeDocument = op.PayeeDocument,
                    Amount = op.Value,
                    Date = op.CreatedAt,
                    HasDocument = hasDoc,
                    HasReceipt = hasReceipt
                });
            }

            List<TaxAssetItemDto> assetsList = [];
            var patrimonyAssets = await repository.GetAssetsAsync(userId);
            foreach (var a in patrimonyAssets)
            {
                bool heldInPrevYear = (!a.AcquisitionDate.HasValue || a.AcquisitionDate.Value <= prevEnd) &&
                                      (!a.Disposed || !a.DisposalDate.HasValue || a.DisposalDate.Value > prevEnd);
                bool heldInBaseYear = (!a.AcquisitionDate.HasValue || a.AcquisitionDate.Value <= end) &&
                                      (!a.Disposed || !a.DisposalDate.HasValue || a.DisposalDate.Value > end);

                if (heldInPrevYear || heldInBaseYear)
                {
                    decimal prevVal = heldInPrevYear ? a.PurchaseValue : 0;
                    decimal baseVal = heldInBaseYear ? a.PurchaseValue : 0;
                    bool missing = heldInBaseYear && a.PurchaseValue <= 0;

                    assetsList.Add(new TaxAssetItemDto
                    {
                        Id = a.Id,
                        Code = !string.IsNullOrWhiteSpace(a.IrAssetCode) ? a.IrAssetCode : "99",
                        Name = a.Name,
                        Type = a.Type,
                        Discrimination = !string.IsNullOrWhiteSpace(a.IrDescription) ? a.IrDescription : (a.Description ?? a.Name),
                        SituationPreviousYear = prevVal,
                        SituationBaseYear = baseVal,
                        CurrentMarketValue = a.CurrentValue,
                        AcquisitionDate = a.AcquisitionDate,
                        MissingAcquisitionValue = missing,
                        Source = "patrimony"
                    });
                }
            }

            foreach (var invAsset in invAssets)
            {
                var assetTxs = invTransactions.Where(t => t.InvestmentAssetId == invAsset.Id).ToList();
                var (prevQty, prevCost) = CalculateInvestmentPosition(assetTxs, prevEnd);
                var (baseQty, baseCost) = CalculateInvestmentPosition(assetTxs, end);

                if (prevQty > 0 || baseQty > 0)
                {
                    string code = invAsset.Type?.ToUpperInvariant() == "STOCK" ? "31" : (invAsset.Type?.ToUpperInvariant() == "FII" ? "73" : "45");
                    string discrimination = baseQty > 0
                        ? $"{baseQty} cotas/ações de {invAsset.Ticker} ({invAsset.Name}) pelo custo médio de aquisição"
                        : $"Posição zerada em {invAsset.Ticker} ({invAsset.Name}) durante o ano-base";

                    decimal marketVal = baseQty * (invAsset.ManualPrice > 0 ? invAsset.ManualPrice : (baseQty > 0 ? (baseCost / baseQty) : 0));

                    assetsList.Add(new TaxAssetItemDto
                    {
                        Id = invAsset.Id,
                        Code = code,
                        Name = $"{invAsset.Ticker} - {invAsset.Name}",
                        Type = invAsset.Type ?? "STOCK",
                        Discrimination = discrimination,
                        SituationPreviousYear = prevCost,
                        SituationBaseYear = baseCost,
                        CurrentMarketValue = marketVal,
                        AcquisitionDate = null,
                        MissingAcquisitionValue = false,
                        Source = "investment"
                    });
                }
            }

            List<TaxDebtItemDto> debtsList = [];
            var debts = await repository.GetDebtsAsync(userId);
            var debtPayments = await repository.GetDebtPaymentsAsync(userId, end);

            foreach (var debt in debts)
            {
                decimal prevBalance = 0;
                if (debt.CreatedAt <= prevEnd && (!debt.Settled || !debt.SettledDate.HasValue || debt.SettledDate.Value > prevEnd))
                {
                    var lastPayPrev = debtPayments
                        .Where(p => p.DebtId == debt.Id && p.Date <= prevEnd)
                        .OrderByDescending(p => p.Date)
                        .ThenByDescending(p => p.CreatedAt)
                        .FirstOrDefault();

                    prevBalance = lastPayPrev != null ? lastPayPrev.NewRemainingAmount : debt.TotalAmount;
                    if (prevBalance < 0) prevBalance = 0;
                }

                decimal baseBalance = 0;
                if (debt.CreatedAt <= end && (!debt.Settled || !debt.SettledDate.HasValue || debt.SettledDate.Value > end))
                {
                    var lastPayBase = debtPayments
                        .Where(p => p.DebtId == debt.Id && p.Date <= end)
                        .OrderByDescending(p => p.Date)
                        .ThenByDescending(p => p.CreatedAt)
                        .FirstOrDefault();

                    baseBalance = lastPayBase != null ? lastPayBase.NewRemainingAmount : debt.TotalAmount;
                    if (baseBalance < 0) baseBalance = 0;
                }

                if (prevBalance > 0 || baseBalance > 0)
                {
                    debtsList.Add(new TaxDebtItemDto
                    {
                        Id = debt.Id,
                        Code = "11",
                        Name = debt.Name,
                        Description = debt.Description,
                        CreditorName = debt.Name,
                        Type = debt.Type,
                        SituationPreviousYear = prevBalance,
                        SituationBaseYear = baseBalance,
                        TotalAmount = debt.TotalAmount
                    });
                }
            }

            List<TaxPendingItemDto> pendingsList = [];

            foreach (var a in assetsList.Where(x => x.MissingAcquisitionValue))
            {
                pendingsList.Add(new TaxPendingItemDto
                {
                    Type = "asset_missing_value",
                    Title = "Bem sem custo de aquisição",
                    Description = $"O bem \"{a.Name}\" está sem custo de aquisição informado.",
                    Route = "/patrimony",
                    EntityId = a.Id,
                    EntityName = a.Name,
                    Severity = "warning",
                    ActionType = "edit_asset"
                });
            }

            var undefinedCatOps = validOps
                .Where(o => categoryMap.TryGetValue(o.CategoryId, out var cat) && (string.IsNullOrEmpty(cat.IrTreatment) || cat.IrTreatment == "NONE"))
                .Select(o => o.CategoryId)
                .Distinct()
                .ToList();

            foreach (var catId in undefinedCatOps)
            {
                if (categoryMap.TryGetValue(catId, out var cat))
                {
                    pendingsList.Add(new TaxPendingItemDto
                    {
                        Type = "category_no_treatment",
                        Title = "Categoria sem tratamento de IR",
                        Description = $"A categoria \"{cat.Name}\" possui lançamentos no ano-base mas está sem tratamento de IR definido.",
                        Route = "/category",
                        EntityId = cat.Id,
                        EntityName = cat.Name,
                        Severity = "info",
                        ActionType = "edit_category"
                    });
                }
            }

            foreach (var d in deductions)
            {
                if (string.IsNullOrWhiteSpace(d.PayeeDocument))
                {
                    pendingsList.Add(new TaxPendingItemDto
                    {
                        Type = "deduction_missing_document",
                        Title = "Dedução sem CPF/CNPJ",
                        Description = $"Dedução \"{d.Description}\" ({d.Amount:C}) não possui CPF/CNPJ do prestador informado.",
                        Route = "/operation",
                        EntityId = d.Id,
                        EntityName = d.Description,
                        Severity = "warning",
                        ActionType = "edit_operation"
                    });
                }
                else if (!DocumentValidator.IsValidCpfOrCnpj(d.PayeeDocument))
                {
                    pendingsList.Add(new TaxPendingItemDto
                    {
                        Type = "deduction_invalid_document",
                        Title = "CPF/CNPJ do prestador inválido",
                        Description = $"Dedução \"{d.Description}\" possui CPF/CNPJ inválido (\"{d.PayeeDocument}\").",
                        Route = "/operation",
                        EntityId = d.Id,
                        EntityName = d.Description,
                        Severity = "warning",
                        ActionType = "edit_operation"
                    });
                }

                if (!d.HasReceipt)
                {
                    pendingsList.Add(new TaxPendingItemDto
                    {
                        Type = "deduction_missing_receipt",
                        Title = "Dedução sem comprovante anexado",
                        Description = $"Dedução \"{d.Description}\" ({d.Amount:C}) não possui comprovante/recibo anexado.",
                        Route = "/operation",
                        EntityId = d.Id,
                        EntityName = d.Description,
                        Severity = "warning",
                        ActionType = "edit_operation"
                    });
                }
            }

            return new TaxPreviewDto
            {
                BaseYear = baseYear,
                Disclaimer = "Material informativo de apoio ao preenchimento da Declaração de Ajuste Anual do IRPF. Não substitui os informes oficiais emitidos pelas fontes pagadoras nem a orientação de um profissional de contabilidade.",
                TotalTaxableIncome = taxable.Sum(x => x.TotalValue),
                TotalExemptIncome = exempt.Sum(x => x.TotalValue),
                TotalExclusiveIncome = exclusive.Sum(x => x.TotalValue),
                TotalDeductions = deductions.Sum(x => x.Amount),
                TotalAssetsCost = assetsList.Where(x => !x.MissingAcquisitionValue).Sum(x => x.SituationBaseYear),
                TotalDebtsBalance = debtsList.Sum(x => x.SituationBaseYear),
                PendingCount = pendingsList.Count,
                TaxableIncome = taxable.OrderByDescending(x => x.Date).ToList(),
                ExemptIncome = exempt.OrderByDescending(x => x.Date).ToList(),
                ExclusiveIncome = exclusive.OrderByDescending(x => x.Date).ToList(),
                Deductions = deductions.OrderByDescending(x => x.Date).ToList(),
                Assets = assetsList.OrderBy(x => x.Code).ThenBy(x => x.Name).ToList(),
                Debts = debtsList.OrderByDescending(x => x.SituationBaseYear).ToList(),
                Pendings = pendingsList
            };
        }

        public async Task<ResponseApi<TaxPreviewDto>> GetPreviewAsync(int baseYear, string userId)
        {
            try
            {
                var dto = await BuildTaxPreviewInternalAsync(baseYear, userId);
                return new(dto, 200, "Pré-visualização do IR calculada com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao calcular pré-visualização: {ex.Message}");
            }
        }

        public async Task<ResponseApi<dynamic>> GetIncomeAsync(int baseYear, string userId)
        {
            try
            {
                var prev = await BuildTaxPreviewInternalAsync(baseYear, userId);
                var result = new
                {
                    taxableTotal = prev.TotalTaxableIncome,
                    exemptTotal = prev.TotalExemptIncome,
                    exclusiveTotal = prev.TotalExclusiveIncome,
                    taxable = prev.TaxableIncome,
                    exempt = prev.ExemptIncome,
                    exclusive = prev.ExclusiveIncome
                };

                return new(result, 200, "Rendimentos obtidos com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao buscar rendimentos: {ex.Message}");
            }
        }

        public async Task<ResponseApi<dynamic>> GetDeductionsAsync(int baseYear, string userId)
        {
            try
            {
                var prev = await BuildTaxPreviewInternalAsync(baseYear, userId);
                var result = new
                {
                    total = prev.TotalDeductions,
                    items = prev.Deductions
                };

                return new(result, 200, "Deduções obtidas com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao buscar deduções: {ex.Message}");
            }
        }

        public async Task<ResponseApi<dynamic>> GetAssetsAsync(int baseYear, string userId)
        {
            try
            {
                var prev = await BuildTaxPreviewInternalAsync(baseYear, userId);
                var result = new
                {
                    totalAcquisitionCost = prev.TotalAssetsCost,
                    items = prev.Assets
                };

                return new(result, 200, "Bens e direitos obtidos com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao buscar bens e direitos: {ex.Message}");
            }
        }

        public async Task<ResponseApi<dynamic>> GetDebtsAsync(int baseYear, string userId)
        {
            try
            {
                var prev = await BuildTaxPreviewInternalAsync(baseYear, userId);
                var result = new
                {
                    totalRemaining = prev.TotalDebtsBalance,
                    items = prev.Debts
                };

                return new(result, 200, "Dívidas e ônus obtidos com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao buscar dívidas: {ex.Message}");
            }
        }

        public async Task<ResponseApi<List<TaxPendingItemDto>>> GetPendingAsync(int baseYear, string userId)
        {
            try
            {
                var prev = await BuildTaxPreviewInternalAsync(baseYear, userId);
                return new(prev.Pendings, 200, "Pendências obtidas com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao listar pendências: {ex.Message}");
            }
        }

        public async Task<byte[]> ExportSpreadsheetAsync(int baseYear, string userId)
        {
            var preview = await BuildTaxPreviewInternalAsync(baseYear, userId);
            using var workbook = new XLWorkbook();

            var wsSummary = workbook.Worksheets.Add("Resumo");
            wsSummary.Cell(1, 1).Value = $"MINHAS FINANÇAS - DECLARAÇÃO IRPF (ANO-BASE {baseYear})";
            wsSummary.Cell(1, 1).Style.Font.Bold = true;
            wsSummary.Cell(1, 1).Style.Font.FontSize = 14;

            wsSummary.Cell(3, 1).Value = preview.Disclaimer;
            wsSummary.Cell(3, 1).Style.Font.Italic = true;

            wsSummary.Cell(5, 1).Value = "Seção da Declaração";
            wsSummary.Cell(5, 2).Value = "Valor Total (R$)";
            wsSummary.Range(5, 1, 5, 2).Style.Font.Bold = true;
            wsSummary.Range(5, 1, 5, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#ec4899");
            wsSummary.Range(5, 1, 5, 2).Style.Font.FontColor = XLColor.White;

            int row = 6;
            wsSummary.Cell(row, 1).Value = "1. Rendimentos Tributáveis";
            wsSummary.Cell(row++, 2).Value = preview.TotalTaxableIncome;
            wsSummary.Cell(row, 1).Value = "2. Rendimentos Isentos e Não Tributáveis";
            wsSummary.Cell(row++, 2).Value = preview.TotalExemptIncome;
            wsSummary.Cell(row, 1).Value = "3. Rendimentos Sujeitos à Tributação Exclusiva";
            wsSummary.Cell(row++, 2).Value = preview.TotalExclusiveIncome;
            wsSummary.Cell(row, 1).Value = "4. Pagamentos Efetuados (Deduções)";
            wsSummary.Cell(row++, 2).Value = preview.TotalDeductions;
            wsSummary.Cell(row, 1).Value = "5. Bens e Direitos (Custo de Aquisição em 31/12)";
            wsSummary.Cell(row++, 2).Value = preview.TotalAssetsCost;
            wsSummary.Cell(row, 1).Value = "6. Dívidas e Ônus Reais (Saldo em 31/12)";
            wsSummary.Cell(row++, 2).Value = preview.TotalDebtsBalance;
            wsSummary.Cell(row, 1).Value = "Patrimônio Líquido Fiscal (Bens - Dívidas)";
            wsSummary.Cell(row++, 2).Value = preview.TotalAssetsCost - preview.TotalDebtsBalance;
            wsSummary.Cell(row, 1).Value = "Pendências Identificadas";
            wsSummary.Cell(row++, 2).Value = preview.PendingCount;

            wsSummary.Range(6, 2, row - 2, 2).Style.NumberFormat.Format = "R$ #,##0.00";
            wsSummary.Columns().AdjustToContents();

            var wsIncome = workbook.Worksheets.Add("Rendimentos");
            int incRow = 1;

            void RenderIncomeSection(string title, List<TaxIncomeItemDto> items, string colorHex)
            {
                wsIncome.Cell(incRow, 1).Value = title;
                wsIncome.Range(incRow, 1, incRow, 5).Merge();
                wsIncome.Cell(incRow, 1).Style.Font.Bold = true;
                wsIncome.Cell(incRow, 1).Style.Fill.BackgroundColor = XLColor.FromHtml(colorHex);
                wsIncome.Cell(incRow, 1).Style.Font.FontColor = XLColor.White;
                incRow++;

                wsIncome.Cell(incRow, 1).Value = "Data";
                wsIncome.Cell(incRow, 2).Value = "Fonte / Categoria";
                wsIncome.Cell(incRow, 3).Value = "Descrição";
                wsIncome.Cell(incRow, 4).Value = "CNPJ / CPF da Fonte";
                wsIncome.Cell(incRow, 5).Value = "Total Recebido (R$)";
                wsIncome.Range(incRow, 1, incRow, 5).Style.Font.Bold = true;
                incRow++;

                if (items.Count == 0)
                {
                    wsIncome.Cell(incRow, 1).Value = "Nenhum rendimento nesta seção.";
                    wsIncome.Range(incRow, 1, incRow, 5).Merge();
                    incRow += 2;
                    return;
                }

                int startSub = incRow;
                foreach (var it in items)
                {
                    wsIncome.Cell(incRow, 1).Value = it.Date.ToString("dd/MM/yyyy");
                    wsIncome.Cell(incRow, 2).Value = it.CategoryName;
                    wsIncome.Cell(incRow, 3).Value = it.Description ?? "—";
                    wsIncome.Cell(incRow, 4).Value = it.PayerDocument ?? "—";
                    wsIncome.Cell(incRow, 5).Value = it.TotalValue;
                    incRow++;
                }
                wsIncome.Range(startSub, 5, incRow - 1, 5).Style.NumberFormat.Format = "R$ #,##0.00";
                incRow += 2;
            }

            RenderIncomeSection("1. Rendimentos Tributáveis", preview.TaxableIncome, "#10b981");
            RenderIncomeSection("2. Rendimentos Isentos e Não Tributáveis", preview.ExemptIncome, "#3b82f6");
            RenderIncomeSection("3. Rendimentos com Tributação Exclusiva / Definitiva", preview.ExclusiveIncome, "#8b5cf6");
            wsIncome.Columns().AdjustToContents();

            var wsDeductions = workbook.Worksheets.Add("Deduções");
            wsDeductions.Cell(1, 1).Value = "Data";
            wsDeductions.Cell(1, 2).Value = "Tipo de Dedução";
            wsDeductions.Cell(1, 3).Value = "Categoria";
            wsDeductions.Cell(1, 4).Value = "Descrição";
            wsDeductions.Cell(1, 5).Value = "Prestador / Favorecido";
            wsDeductions.Cell(1, 6).Value = "CPF / CNPJ";
            wsDeductions.Cell(1, 7).Value = "Comprovante Anexado";
            wsDeductions.Cell(1, 8).Value = "Valor Dedutível (R$)";
            wsDeductions.Range(1, 1, 1, 8).Style.Font.Bold = true;
            wsDeductions.Range(1, 1, 1, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#2563eb");
            wsDeductions.Range(1, 1, 1, 8).Style.Font.FontColor = XLColor.White;

            int dRow = 2;
            foreach (var item in preview.Deductions)
            {
                wsDeductions.Cell(dRow, 1).Value = item.Date.ToString("dd/MM/yyyy");
                wsDeductions.Cell(dRow, 2).Value = item.Subtype;
                wsDeductions.Cell(dRow, 3).Value = item.CategoryName;
                wsDeductions.Cell(dRow, 4).Value = item.Description ?? "—";
                wsDeductions.Cell(dRow, 5).Value = item.PayeeName ?? "—";
                wsDeductions.Cell(dRow, 6).Value = item.PayeeDocument ?? "—";
                wsDeductions.Cell(dRow, 7).Value = item.HasReceipt ? "Sim" : "Não";
                wsDeductions.Cell(dRow, 8).Value = item.Amount;
                dRow++;
            }
            if (dRow > 2) wsDeductions.Range(2, 8, dRow - 1, 8).Style.NumberFormat.Format = "R$ #,##0.00";
            wsDeductions.Columns().AdjustToContents();

            var wsAssets = workbook.Worksheets.Add("Bens e Direitos");
            wsAssets.Cell(1, 1).Value = "Código IR";
            wsAssets.Cell(1, 2).Value = "Nome";
            wsAssets.Cell(1, 3).Value = "Tipo";
            wsAssets.Cell(1, 4).Value = "Discriminação Receita Federal";
            wsAssets.Cell(1, 5).Value = $"Situação em 31/12/{baseYear - 1} (R$)";
            wsAssets.Cell(1, 6).Value = $"Situação em 31/12/{baseYear} (R$)";
            wsAssets.Range(1, 1, 1, 6).Style.Font.Bold = true;
            wsAssets.Range(1, 1, 1, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#059669");
            wsAssets.Range(1, 1, 1, 6).Style.Font.FontColor = XLColor.White;

            int aRow = 2;
            foreach (var item in preview.Assets)
            {
                wsAssets.Cell(aRow, 1).Value = item.Code;
                wsAssets.Cell(aRow, 2).Value = item.Name;
                wsAssets.Cell(aRow, 3).Value = item.Type;
                wsAssets.Cell(aRow, 4).Value = item.Discrimination;
                wsAssets.Cell(aRow, 5).Value = item.SituationPreviousYear;
                wsAssets.Cell(aRow, 6).Value = item.SituationBaseYear;
                aRow++;
            }
            if (aRow > 2) wsAssets.Range(2, 5, aRow - 1, 6).Style.NumberFormat.Format = "R$ #,##0.00";
            wsAssets.Columns().AdjustToContents();

            var wsDebts = workbook.Worksheets.Add("Dívidas e Ônus");
            wsDebts.Cell(1, 1).Value = "Código";
            wsDebts.Cell(1, 2).Value = "Dívida / Descrição";
            wsDebts.Cell(1, 3).Value = "Credor";
            wsDebts.Cell(1, 4).Value = $"Situação em 31/12/{baseYear - 1} (R$)";
            wsDebts.Cell(1, 5).Value = $"Situação em 31/12/{baseYear} (R$)";
            wsDebts.Range(1, 1, 1, 5).Style.Font.Bold = true;
            wsDebts.Range(1, 1, 1, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#dc2626");
            wsDebts.Range(1, 1, 1, 5).Style.Font.FontColor = XLColor.White;

            int dbRow = 2;
            foreach (var item in preview.Debts)
            {
                wsDebts.Cell(dbRow, 1).Value = item.Code;
                wsDebts.Cell(dbRow, 2).Value = item.Name;
                wsDebts.Cell(dbRow, 3).Value = item.CreditorName ?? "—";
                wsDebts.Cell(dbRow, 4).Value = item.SituationPreviousYear;
                wsDebts.Cell(dbRow, 5).Value = item.SituationBaseYear;
                dbRow++;
            }
            if (dbRow > 2) wsDebts.Range(2, 4, dbRow - 1, 5).Style.NumberFormat.Format = "R$ #,##0.00";
            wsDebts.Columns().AdjustToContents();

            var wsPending = workbook.Worksheets.Add("Pendências");
            wsPending.Cell(1, 1).Value = "Tipo";
            wsPending.Cell(1, 2).Value = "Título";
            wsPending.Cell(1, 3).Value = "Descrição";
            wsPending.Range(1, 1, 1, 3).Style.Font.Bold = true;
            wsPending.Range(1, 1, 1, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#d97706");
            wsPending.Range(1, 1, 1, 3).Style.Font.FontColor = XLColor.White;

            int pRow = 2;
            foreach (var item in preview.Pendings)
            {
                wsPending.Cell(pRow, 1).Value = item.Type;
                wsPending.Cell(pRow, 2).Value = item.Title;
                wsPending.Cell(pRow, 3).Value = item.Description;
                pRow++;
            }
            wsPending.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            byte[] bytes = ms.ToArray();

            using var sha = SHA256.Create();
            string hashHex = Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();

            var snapshot = new TaxYearSnapshot
            {
                UserId = userId,
                BaseYear = baseYear,
                GeneratedAt = DateTime.UtcNow,
                Format = "xlsx",
                Payload = JsonSerializer.Serialize(preview),
                HashSha256 = hashHex,
                TotalTaxableIncome = preview.TotalTaxableIncome,
                TotalAssetsCost = preview.TotalAssetsCost,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };
            await repository.SaveSnapshotAsync(snapshot);

            return bytes;
        }

        public async Task<byte[]> ExportPdfAsync(int baseYear, string userId)
        {
            var preview = await BuildTaxPreviewInternalAsync(baseYear, userId);
            var (pdfBytes, sha256Hex) = PdfGenerator.GenerateIrpfReport(baseYear, preview);

            var snapshot = new TaxYearSnapshot
            {
                UserId = userId,
                BaseYear = baseYear,
                GeneratedAt = DateTime.UtcNow,
                Format = "pdf",
                Payload = JsonSerializer.Serialize(preview),
                HashSha256 = sha256Hex,
                TotalTaxableIncome = preview.TotalTaxableIncome,
                TotalAssetsCost = preview.TotalAssetsCost,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };
            await repository.SaveSnapshotAsync(snapshot);

            return pdfBytes;
        }

        public async Task<ResponseApi<List<TaxYearSnapshot>>> GetSnapshotsAsync(string userId, int? baseYear = null)
        {
            try
            {
                var list = await repository.GetSnapshotsAsync(userId, baseYear);
                return new(list, 200, "Snapshots listados com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao buscar snapshots: {ex.Message}");
            }
        }

        public async Task<ResponseApi<bool>> UpdateCategoryTreatmentAsync(string categoryId, UpdateCategoryIrTreatmentRequest request, string userId)
        {
            try
            {
                string[] validTreatments = ["TAXABLE_INCOME", "EXEMPT_INCOME", "EXCLUSIVE_INCOME", "DEDUCTION", "NONE"];
                string treatment = request.IrTreatment?.ToUpperInvariant() ?? "";
                if (!validTreatments.Contains(treatment))
                {
                    return new(false, 400, "Tratamento de IR inválido. Valores aceitos: TAXABLE_INCOME, EXEMPT_INCOME, EXCLUSIVE_INCOME, DEDUCTION, NONE.");
                }

                var cat = await repository.GetCategoryByIdAsync(categoryId);
                if (cat == null || cat.CreatedBy != userId) return new(false, 404, "Categoria não encontrada");

                bool ok = await repository.UpdateCategoryTreatmentAsync(categoryId, treatment, request.IrSubtype, userId);
                return new(ok, 200, "Tratamento de IR da categoria atualizado com sucesso");
            }
            catch (Exception ex)
            {
                return new(false, 500, $"Erro ao atualizar tratamento de IR: {ex.Message}");
            }
        }
    }
}
