using System.Text;
using System.Text.Json;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;
using ClosedXML.Excel;

namespace api_finances.src.Services
{
    public class TaxService(
        ITaxRepository repository
    ) : ITaxService
    {
        private static (DateTime start, DateTime end) GetYearBounds(int baseYear)
        {
            DateTime start = new(baseYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = new(baseYear, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc);
            return (start, end);
        }

        public async Task<ResponseApi<TaxPreviewDto>> GetPreviewAsync(int baseYear, string userId)
        {
            try
            {
                var (start, end) = GetYearBounds(baseYear);
                var categories = await repository.GetCategoriesAsync(userId);
                var categoryMap = categories.ToDictionary(c => c.Id, c => c);

                var operations = await repository.GetOperationsByPeriodAsync(userId, start, end);
                var validOps = operations.Where(o => o.Type != "transfer" && string.IsNullOrEmpty(o.DestinationBankId)).ToList();

                decimal taxableIncome = 0;
                decimal exemptIncome = 0;
                decimal exclusiveIncome = 0;
                decimal deductions = 0;

                foreach (var op in validOps)
                {
                    if (categoryMap.TryGetValue(op.CategoryId, out var cat))
                    {
                        if (op.Type == "income")
                        {
                            if (cat.IrTreatment == "TAXABLE_INCOME") taxableIncome += op.Value;
                            else if (cat.IrTreatment == "EXEMPT_INCOME") exemptIncome += op.Value;
                            else if (cat.IrTreatment == "EXCLUSIVE_INCOME") exclusiveIncome += op.Value;
                        }
                        else if (op.Type == "expense" && cat.IrTreatment == "DEDUCTION")
                        {
                            deductions += op.Value;
                        }
                    }
                }

                var invTransactions = await repository.GetInvestmentTransactionsAsync(userId, end);
                var yearInvTxs = invTransactions.Where(t => t.Date >= start && t.Date <= end).ToList();
                foreach (var it in yearInvTxs)
                {
                    string kind = it.Kind?.ToUpper() ?? "";
                    if (kind == "DIVIDEND" || kind == "RENDIMENTO") exemptIncome += it.Amount;
                    else if (kind == "JCP") exclusiveIncome += it.Amount;
                }

                var assets = await repository.GetAssetsAsync(userId);
                var heldAssets = assets.Where(a =>
                    (!a.AcquisitionDate.HasValue || a.AcquisitionDate.Value <= end) &&
                    (!a.Disposed || !a.DisposalDate.HasValue || a.DisposalDate.Value > end)
                ).ToList();

                decimal assetsAtAcquisitionCost = heldAssets.Where(a => a.PurchaseValue > 0).Sum(a => a.PurchaseValue);

                var invAssets = await repository.GetInvestmentAssetsAsync(userId);
                var invMap = invAssets.ToDictionary(a => a.Id, a => a);
                foreach (var invAsset in invAssets)
                {
                    var assetTxs = invTransactions.Where(t => t.InvestmentAssetId == invAsset.Id).OrderBy(t => t.Date).ToList();
                    decimal totalQty = 0;
                    decimal totalCost = 0;

                    foreach (var tx in assetTxs)
                    {
                        string k = tx.Kind?.ToUpper() ?? "";
                        if (k == "BUY")
                        {
                            totalQty += tx.Quantity;
                            totalCost += tx.Amount;
                        }
                        else if (k == "SELL")
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

                    if (totalQty > 0 && totalCost > 0)
                    {
                        assetsAtAcquisitionCost += totalCost;
                    }
                }

                var debts = await repository.GetDebtsAsync(userId);
                var debtPayments = await repository.GetDebtPaymentsAsync(userId, end);
                decimal debtsAtYearEnd = 0;

                foreach (var debt in debts.Where(d => d.CreatedAt <= end))
                {
                    decimal paid = debtPayments.Where(p => p.DebtId == debt.Id).Sum(p => p.Amount);
                    decimal remaining = debt.TotalAmount - paid;
                    if (remaining > 0) debtsAtYearEnd += remaining;
                }

                var pendingResp = await GetPendingAsync(baseYear, userId);
                int pendingCount = pendingResp.Data?.Count ?? 0;

                TaxPreviewDto dto = new()
                {
                    BaseYear = baseYear,
                    TaxableIncome = taxableIncome,
                    ExemptIncome = exemptIncome,
                    ExclusiveIncome = exclusiveIncome,
                    Deductions = deductions,
                    AssetsAtAcquisitionCost = assetsAtAcquisitionCost,
                    DebtsAtYearEnd = debtsAtYearEnd,
                    PendingCount = pendingCount
                };

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
                var (start, end) = GetYearBounds(baseYear);
                var categories = await repository.GetCategoriesAsync(userId);
                var categoryMap = categories.ToDictionary(c => c.Id, c => c);

                var operations = await repository.GetOperationsByPeriodAsync(userId, start, end);
                var validOps = operations.Where(o => o.Type == "income" && string.IsNullOrEmpty(o.DestinationBankId)).ToList();

                List<TaxIncomeItemDto> taxable = [];
                List<TaxIncomeItemDto> exempt = [];
                List<TaxIncomeItemDto> exclusive = [];

                foreach (var op in validOps)
                {
                    if (categoryMap.TryGetValue(op.CategoryId, out var cat))
                    {
                        var item = new TaxIncomeItemDto
                        {
                            Id = op.Id,
                            Description = op.Description,
                            CategoryName = cat.Name,
                            CategoryId = cat.Id,
                            Value = op.Value,
                            Date = op.CreatedAt,
                            Treatment = cat.IrTreatment,
                            Source = "operation"
                        };

                        if (cat.IrTreatment == "TAXABLE_INCOME") taxable.Add(item);
                        else if (cat.IrTreatment == "EXEMPT_INCOME") exempt.Add(item);
                        else if (cat.IrTreatment == "EXCLUSIVE_INCOME") exclusive.Add(item);
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
                    string kind = it.Kind?.ToUpper() ?? "";

                    if (kind == "DIVIDEND" || kind == "RENDIMENTO")
                    {
                        exempt.Add(new TaxIncomeItemDto
                        {
                            Id = it.Id,
                            Description = desc,
                            CategoryName = kind == "DIVIDEND" ? "Dividendos" : "Rendimentos FII",
                            CategoryId = "",
                            Value = it.Amount,
                            Date = it.Date,
                            Treatment = "EXEMPT_INCOME",
                            Source = "investment"
                        });
                    }
                    else if (kind == "JCP")
                    {
                        exclusive.Add(new TaxIncomeItemDto
                        {
                            Id = it.Id,
                            Description = desc,
                            CategoryName = "Juros sobre Capital Próprio",
                            CategoryId = "",
                            Value = it.Amount,
                            Date = it.Date,
                            Treatment = "EXCLUSIVE_INCOME",
                            Source = "investment"
                        });
                    }
                }

                var result = new
                {
                    taxableTotal = taxable.Sum(x => x.Value),
                    exemptTotal = exempt.Sum(x => x.Value),
                    exclusiveTotal = exclusive.Sum(x => x.Value),
                    taxable = taxable.OrderByDescending(x => x.Date).ToList(),
                    exempt = exempt.OrderByDescending(x => x.Date).ToList(),
                    exclusive = exclusive.OrderByDescending(x => x.Date).ToList()
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
                var (start, end) = GetYearBounds(baseYear);
                var categories = await repository.GetCategoriesAsync(userId);
                var categoryMap = categories.ToDictionary(c => c.Id, c => c);

                var operations = await repository.GetOperationsByPeriodAsync(userId, start, end);
                var deductionOps = operations.Where(o => o.Type == "expense" && categoryMap.TryGetValue(o.CategoryId, out var cat) && cat.IrTreatment == "DEDUCTION").ToList();

                var opIds = deductionOps.Select(o => o.Id).ToList();
                var attachments = await repository.GetAttachmentsByOriginIdsAsync(opIds);
                var attachmentOriginSet = attachments.Select(a => a.ParentId).ToHashSet();

                List<TaxDeductionItemDto> items = [];
                foreach (var op in deductionOps)
                {
                    var cat = categoryMap[op.CategoryId];
                    bool hasDoc = !string.IsNullOrWhiteSpace(op.PayeeDocument);
                    bool hasReceipt = attachmentOriginSet.Contains(op.Id);

                    items.Add(new TaxDeductionItemDto
                    {
                        Id = op.Id,
                        Description = op.Description,
                        CategoryName = cat.Name,
                        CategoryId = cat.Id,
                        Value = op.Value,
                        Date = op.CreatedAt,
                        PayeeName = op.PayeeName,
                        PayeeDocument = op.PayeeDocument,
                        HasDocument = hasDoc,
                        HasReceipt = hasReceipt,
                        Subtype = cat.IrSubtype
                    });
                }

                var result = new
                {
                    total = items.Sum(x => x.Value),
                    items = items.OrderByDescending(x => x.Date).ToList()
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
                var (_, end) = GetYearBounds(baseYear);
                var assets = await repository.GetAssetsAsync(userId);
                var heldAssets = assets.Where(a =>
                    (!a.AcquisitionDate.HasValue || a.AcquisitionDate.Value <= end) &&
                    (!a.Disposed || !a.DisposalDate.HasValue || a.DisposalDate.Value > end)
                ).ToList();

                List<TaxAssetItemDto> items = [];
                foreach (var a in heldAssets)
                {
                    bool missing = a.PurchaseValue <= 0;
                    items.Add(new TaxAssetItemDto
                    {
                        Id = a.Id,
                        Name = a.Name,
                        Type = a.Type,
                        IrAssetCode = a.IrAssetCode,
                        IrDescription = a.IrDescription ?? a.Description,
                        AcquisitionValue = a.PurchaseValue,
                        CurrentMarketValue = a.CurrentValue,
                        AcquisitionDate = a.AcquisitionDate,
                        MissingAcquisitionValue = missing,
                        Source = "patrimony"
                    });
                }

                var invTransactions = await repository.GetInvestmentTransactionsAsync(userId, end);
                var invAssets = await repository.GetInvestmentAssetsAsync(userId);

                foreach (var invAsset in invAssets)
                {
                    var assetTxs = invTransactions.Where(t => t.InvestmentAssetId == invAsset.Id).OrderBy(t => t.Date).ToList();
                    decimal totalQty = 0;
                    decimal totalCost = 0;

                    foreach (var tx in assetTxs)
                    {
                        string k = tx.Kind?.ToUpper() ?? "";
                        if (k == "BUY")
                        {
                            totalQty += tx.Quantity;
                            totalCost += tx.Amount;
                        }
                        else if (k == "SELL")
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

                    if (totalQty > 0)
                    {
                        items.Add(new TaxAssetItemDto
                        {
                            Id = invAsset.Id,
                            Name = $"{invAsset.Ticker} - {invAsset.Name}",
                            Type = invAsset.Type ?? "STOCK",
                            IrAssetCode = invAsset.Type?.ToUpper() == "STOCK" ? "31" : (invAsset.Type?.ToUpper() == "FII" ? "73" : "45"),
                            IrDescription = $"{totalQty} cotas/ações de {invAsset.Ticker} ({invAsset.Name}) pelo custo médio de aquisição",
                            AcquisitionValue = totalCost,
                            CurrentMarketValue = totalQty * (invAsset.ManualPrice > 0 ? invAsset.ManualPrice : (totalCost / totalQty)),
                            AcquisitionDate = null,
                            MissingAcquisitionValue = false,
                            Source = "investment"
                        });
                    }
                }

                var result = new
                {
                    totalAcquisitionCost = items.Where(x => !x.MissingAcquisitionValue).Sum(x => x.AcquisitionValue),
                    items = items.OrderBy(x => x.Name).ToList()
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
                var (_, end) = GetYearBounds(baseYear);
                var debts = await repository.GetDebtsAsync(userId);
                var debtPayments = await repository.GetDebtPaymentsAsync(userId, end);

                List<TaxDebtItemDto> items = [];
                foreach (var debt in debts.Where(d => d.CreatedAt <= end))
                {
                    decimal paid = debtPayments.Where(p => p.DebtId == debt.Id).Sum(p => p.Amount);
                    decimal remaining = debt.TotalAmount - paid;
                    if (remaining > 0)
                    {
                        items.Add(new TaxDebtItemDto
                        {
                            Id = debt.Id,
                            Name = debt.Name,
                            Type = debt.Type,
                            TotalAmount = debt.TotalAmount,
                            BalanceAtYearEnd = remaining,
                            BankName = ""
                        });
                    }
                }

                var result = new
                {
                    totalRemaining = items.Sum(x => x.BalanceAtYearEnd),
                    items = items.OrderByDescending(x => x.BalanceAtYearEnd).ToList()
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
                var (start, end) = GetYearBounds(baseYear);
                List<TaxPendingItemDto> pendings = [];

                var assets = await repository.GetAssetsAsync(userId);
                var heldAssets = assets.Where(a =>
                    (!a.AcquisitionDate.HasValue || a.AcquisitionDate.Value <= end) &&
                    (!a.Disposed || !a.DisposalDate.HasValue || a.DisposalDate.Value > end)
                ).ToList();

                foreach (var a in heldAssets)
                {
                    if (a.PurchaseValue <= 0)
                    {
                        pendings.Add(new TaxPendingItemDto
                        {
                            Type = "asset_missing_value",
                            Severity = "warning",
                            Message = $"O bem \"{a.Name}\" está sem custo de aquisição informado.",
                            EntityId = a.Id,
                            EntityName = a.Name,
                            ActionType = "edit_asset"
                        });
                    }
                }

                var categories = await repository.GetCategoriesAsync(userId);
                var categoryMap = categories.ToDictionary(c => c.Id, c => c);
                var operations = await repository.GetOperationsByPeriodAsync(userId, start, end);
                var validOps = operations.Where(o => o.Type != "transfer" && string.IsNullOrEmpty(o.DestinationBankId)).ToList();

                var opIds = validOps.Select(o => o.Id).ToList();
                var attachments = await repository.GetAttachmentsByOriginIdsAsync(opIds);
                var attachmentOriginSet = attachments.Select(a => a.ParentId).ToHashSet();

                var undefinedCatOps = validOps
                    .Where(o => categoryMap.TryGetValue(o.CategoryId, out var cat) && (string.IsNullOrEmpty(cat.IrTreatment) || cat.IrTreatment == "NONE"))
                    .Select(o => o.CategoryId)
                    .Distinct()
                    .ToList();

                foreach (var catId in undefinedCatOps)
                {
                    if (categoryMap.TryGetValue(catId, out var cat))
                    {
                        pendings.Add(new TaxPendingItemDto
                        {
                            Type = "category_no_treatment",
                            Severity = "info",
                            Message = $"A categoria \"{cat.Name}\" possui lançamentos no ano-base mas está sem tratamento de IR definido.",
                            EntityId = cat.Id,
                            EntityName = cat.Name,
                            ActionType = "edit_category"
                        });
                    }
                }

                var deductionOps = validOps.Where(o => categoryMap.TryGetValue(o.CategoryId, out var cat) && cat.IrTreatment == "DEDUCTION").ToList();
                foreach (var op in deductionOps)
                {
                    if (string.IsNullOrWhiteSpace(op.PayeeDocument))
                    {
                        pendings.Add(new TaxPendingItemDto
                        {
                            Type = "deduction_missing_document",
                            Severity = "warning",
                            Message = $"Dedução \"{op.Description}\" ({op.Value:C}) não possui CPF/CNPJ do prestador informado.",
                            EntityId = op.Id,
                            EntityName = op.Description,
                            ActionType = "edit_operation"
                        });
                    }

                    if (!attachmentOriginSet.Contains(op.Id))
                    {
                        pendings.Add(new TaxPendingItemDto
                        {
                            Type = "deduction_missing_receipt",
                            Severity = "warning",
                            Message = $"Dedução \"{op.Description}\" ({op.Value:C}) não possui comprovante/recibo anexado.",
                            EntityId = op.Id,
                            EntityName = op.Description,
                            ActionType = "edit_operation"
                        });
                    }
                }

                return new(pendings, 200, "Pendências obtidas com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Erro ao listar pendências: {ex.Message}");
            }
        }

        public async Task<byte[]> ExportSpreadsheetAsync(int baseYear, string userId)
        {
            var previewResp = await GetPreviewAsync(baseYear, userId);
            var incomeResp = await GetIncomeAsync(baseYear, userId);
            var deductionsResp = await GetDeductionsAsync(baseYear, userId);
            var assetsResp = await GetAssetsAsync(baseYear, userId);
            var debtsResp = await GetDebtsAsync(baseYear, userId);
            var pendingResp = await GetPendingAsync(baseYear, userId);

            using var workbook = new XLWorkbook();

            var wsSummary = workbook.Worksheets.Add("Resumo");
            wsSummary.Cell(1, 1).Value = $"MINHAS FINANÇAS - DECLARAÇÃO IRPF (ANO-BASE {baseYear})";
            wsSummary.Cell(1, 1).Style.Font.Bold = true;
            wsSummary.Cell(1, 1).Style.Font.FontSize = 14;

            wsSummary.Cell(3, 1).Value = "Material de apoio ao preenchimento da declaração. Não substitui a orientação de um contador.";
            wsSummary.Cell(3, 1).Style.Font.Italic = true;

            wsSummary.Cell(5, 1).Value = "Seção da Declaração";
            wsSummary.Cell(5, 2).Value = "Valor Total (R$)";
            wsSummary.Range(5, 1, 5, 2).Style.Font.Bold = true;
            wsSummary.Range(5, 1, 5, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#ec4899");
            wsSummary.Range(5, 1, 5, 2).Style.Font.FontColor = XLColor.White;

            int row = 6;
            var prev = previewResp.Data;
            wsSummary.Cell(row, 1).Value = "1. Rendimentos Tributáveis";
            wsSummary.Cell(row++, 2).Value = prev?.TaxableIncome ?? 0;
            wsSummary.Cell(row, 1).Value = "2. Rendimentos Isentos e Não Tributáveis";
            wsSummary.Cell(row++, 2).Value = prev?.ExemptIncome ?? 0;
            wsSummary.Cell(row, 1).Value = "3. Rendimentos Sujeitos à Tributação Exclusiva";
            wsSummary.Cell(row++, 2).Value = prev?.ExclusiveIncome ?? 0;
            wsSummary.Cell(row, 1).Value = "4. Pagamentos Efetuados (Deduções)";
            wsSummary.Cell(row++, 2).Value = prev?.Deductions ?? 0;
            wsSummary.Cell(row, 1).Value = "5. Bens e Direitos (Custo de Aquisição)";
            wsSummary.Cell(row++, 2).Value = prev?.AssetsAtAcquisitionCost ?? 0;
            wsSummary.Cell(row, 1).Value = "6. Dívidas e Ônus Reais (Saldo em 31/12)";
            wsSummary.Cell(row++, 2).Value = prev?.DebtsAtYearEnd ?? 0;
            wsSummary.Cell(row, 1).Value = "Pendências Identificadas";
            wsSummary.Cell(row++, 2).Value = prev?.PendingCount ?? 0;

            wsSummary.Range(6, 2, row - 2, 2).Style.NumberFormat.Format = "R$ #,##0.00";
            wsSummary.Columns().AdjustToContents();

            var wsDeductions = workbook.Worksheets.Add("Deduções");
            wsDeductions.Cell(1, 1).Value = "Data";
            wsDeductions.Cell(1, 2).Value = "Categoria";
            wsDeductions.Cell(1, 3).Value = "Descrição";
            wsDeductions.Cell(1, 4).Value = "Prestador";
            wsDeductions.Cell(1, 5).Value = "CPF / CNPJ";
            wsDeductions.Cell(1, 6).Value = "Comprovante Anexado";
            wsDeductions.Cell(1, 7).Value = "Valor (R$)";
            wsDeductions.Range(1, 1, 1, 7).Style.Font.Bold = true;
            wsDeductions.Range(1, 1, 1, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#2563eb");
            wsDeductions.Range(1, 1, 1, 7).Style.Font.FontColor = XLColor.White;

            dynamic? dData = deductionsResp.Data;
            List<TaxDeductionItemDto> dItems = (dData?.items as List<TaxDeductionItemDto>) ?? [];
            int dRow = 2;
            foreach (var item in dItems)
            {
                wsDeductions.Cell(dRow, 1).Value = item.Date.ToString("dd/MM/yyyy");
                wsDeductions.Cell(dRow, 2).Value = item.CategoryName;
                wsDeductions.Cell(dRow, 3).Value = item.Description;
                wsDeductions.Cell(dRow, 4).Value = item.PayeeName ?? "—";
                wsDeductions.Cell(dRow, 5).Value = item.PayeeDocument ?? "—";
                wsDeductions.Cell(dRow, 6).Value = item.HasReceipt ? "Sim" : "Não";
                wsDeductions.Cell(dRow, 7).Value = item.Value;
                dRow++;
            }
            if (dRow > 2) wsDeductions.Range(2, 7, dRow - 1, 7).Style.NumberFormat.Format = "R$ #,##0.00";
            wsDeductions.Columns().AdjustToContents();

            var wsAssets = workbook.Worksheets.Add("Bens e Direitos");
            wsAssets.Cell(1, 1).Value = "Código IR";
            wsAssets.Cell(1, 2).Value = "Nome";
            wsAssets.Cell(1, 3).Value = "Tipo";
            wsAssets.Cell(1, 4).Value = "Descrição para a Declaração";
            wsAssets.Cell(1, 5).Value = "Custo de Aquisição (R$)";
            wsAssets.Cell(1, 6).Value = "Valor Atual Mercado (R$)";
            wsAssets.Range(1, 1, 1, 6).Style.Font.Bold = true;
            wsAssets.Range(1, 1, 1, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#059669");
            wsAssets.Range(1, 1, 1, 6).Style.Font.FontColor = XLColor.White;

            dynamic? aData = assetsResp.Data;
            List<TaxAssetItemDto> aItems = (aData?.items as List<TaxAssetItemDto>) ?? [];
            int aRow = 2;
            foreach (var item in aItems)
            {
                wsAssets.Cell(aRow, 1).Value = item.IrAssetCode ?? "—";
                wsAssets.Cell(aRow, 2).Value = item.Name;
                wsAssets.Cell(aRow, 3).Value = item.Type;
                wsAssets.Cell(aRow, 4).Value = item.IrDescription ?? item.Name;
                wsAssets.Cell(aRow, 5).Value = item.AcquisitionValue;
                wsAssets.Cell(aRow, 6).Value = item.CurrentMarketValue;
                aRow++;
            }
            if (aRow > 2)
            {
                wsAssets.Range(2, 5, aRow - 1, 6).Style.NumberFormat.Format = "R$ #,##0.00";
            }
            wsAssets.Columns().AdjustToContents();

            var wsDebts = workbook.Worksheets.Add("Dívidas e Ônus");
            wsDebts.Cell(1, 1).Value = "Nome da Dívida";
            wsDebts.Cell(1, 2).Value = "Tipo";
            wsDebts.Cell(1, 3).Value = "Valor Original (R$)";
            wsDebts.Cell(1, 4).Value = "Saldo em 31/12 (R$)";
            wsDebts.Range(1, 1, 1, 4).Style.Font.Bold = true;
            wsDebts.Range(1, 1, 1, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#dc2626");
            wsDebts.Range(1, 1, 1, 4).Style.Font.FontColor = XLColor.White;

            dynamic? debData = debtsResp.Data;
            List<TaxDebtItemDto> debItems = (debData?.items as List<TaxDebtItemDto>) ?? [];
            int debRow = 2;
            foreach (var item in debItems)
            {
                wsDebts.Cell(debRow, 1).Value = item.Name;
                wsDebts.Cell(debRow, 2).Value = item.Type;
                wsDebts.Cell(debRow, 3).Value = item.TotalAmount;
                wsDebts.Cell(debRow, 4).Value = item.BalanceAtYearEnd;
                debRow++;
            }
            if (debRow > 2)
            {
                wsDebts.Range(2, 3, debRow - 1, 4).Style.NumberFormat.Format = "R$ #,##0.00";
            }
            wsDebts.Columns().AdjustToContents();

            var wsPend = workbook.Worksheets.Add("Pendências");
            wsPend.Cell(1, 1).Value = "Severidade";
            wsPend.Cell(1, 2).Value = "Item";
            wsPend.Cell(1, 3).Value = "Mensagem / Alerta";
            wsPend.Range(1, 1, 1, 3).Style.Font.Bold = true;
            wsPend.Range(1, 1, 1, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#d97706");
            wsPend.Range(1, 1, 1, 3).Style.Font.FontColor = XLColor.White;

            var pendList = pendingResp.Data ?? [];
            int pRow = 2;
            foreach (var p in pendList)
            {
                wsPend.Cell(pRow, 1).Value = p.Severity.ToUpper();
                wsPend.Cell(pRow, 2).Value = p.EntityName;
                wsPend.Cell(pRow, 3).Value = p.Message;
                pRow++;
            }
            wsPend.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            byte[] bytes = ms.ToArray();

            var snapshot = new TaxYearSnapshot
            {
                UserId = userId,
                BaseYear = baseYear,
                GeneratedAt = DateTime.UtcNow,
                Format = "xlsx",
                Payload = JsonSerializer.Serialize(previewResp.Data),
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };
            await repository.SaveSnapshotAsync(snapshot);

            return bytes;
        }

        public async Task<byte[]> ExportPdfAsync(int baseYear, string userId)
        {
            var previewResp = await GetPreviewAsync(baseYear, userId);
            var prev = previewResp.Data;

            var sb = new StringBuilder();
            sb.AppendLine("%PDF-1.4");
            sb.AppendLine("1 0 obj <</Type /Catalog /Pages 2 0 R>> endobj");
            sb.AppendLine("2 0 obj <</Type /Pages /Kids [3 0 R] /Count 1>> endobj");
            sb.AppendLine("3 0 obj <</Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >> endobj");
            sb.AppendLine("5 0 obj <</Type /Font /Subtype /Type1 /BaseFont /Helvetica>> endobj");

            var streamContent = new StringBuilder();
            streamContent.AppendLine("BT");
            streamContent.AppendLine("/F1 16 Tf");
            streamContent.AppendLine("50 800 Td");
            streamContent.AppendLine($"(MINHAS FINANCAS - DECLARACAO IRPF ANO-BASE {baseYear}) Tj");
            streamContent.AppendLine("/F1 10 Tf");
            streamContent.AppendLine("0 -20 Td");
            streamContent.AppendLine("(Aviso: Material de apoio ao preenchimento. Nao substitui orientacao de contador.) Tj");
            streamContent.AppendLine("/F1 12 Tf");
            streamContent.AppendLine("0 -35 Td");
            streamContent.AppendLine($"(1. Rendimentos Tributaveis: R$ {prev?.TaxableIncome:N2}) Tj");
            streamContent.AppendLine("0 -20 Td");
            streamContent.AppendLine($"(2. Rendimentos Isentos e Nao Tributaveis: R$ {prev?.ExemptIncome:N2}) Tj");
            streamContent.AppendLine("0 -20 Td");
            streamContent.AppendLine($"(3. Rendimentos Tributacao Exclusiva: R$ {prev?.ExclusiveIncome:N2}) Tj");
            streamContent.AppendLine("0 -20 Td");
            streamContent.AppendLine($"(4. Pagamentos Efetuados / Deducoes: R$ {prev?.Deductions:N2}) Tj");
            streamContent.AppendLine("0 -20 Td");
            streamContent.AppendLine($"(5. Bens e Direitos Custo Aquisicao: R$ {prev?.AssetsAtAcquisitionCost:N2}) Tj");
            streamContent.AppendLine("0 -20 Td");
            streamContent.AppendLine($"(6. Dividas e Onus Reais em 31/12: R$ {prev?.DebtsAtYearEnd:N2}) Tj");
            streamContent.AppendLine("0 -20 Td");
            streamContent.AppendLine($"(Total de Pendencias Identificadas: {prev?.PendingCount}) Tj");
            streamContent.AppendLine("0 -35 Td");
            streamContent.AppendLine($"(Relatorio gerado em: {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss} UTC) Tj");
            streamContent.AppendLine("ET");

            byte[] streamBytes = Encoding.ASCII.GetBytes(streamContent.ToString());
            sb.AppendLine($"4 0 obj <</Length {streamBytes.Length}>>");
            sb.AppendLine("stream");
            sb.Append(streamContent);
            sb.AppendLine("endstream");
            sb.AppendLine("endobj");

            sb.AppendLine("xref");
            sb.AppendLine("0 6");
            sb.AppendLine("0000000000 65535 f ");
            sb.AppendLine("0000000010 00000 n ");
            sb.AppendLine("0000000060 00000 n ");
            sb.AppendLine("0000000117 00000 n ");
            sb.AppendLine("0000000300 00000 n ");
            sb.AppendLine("0000000230 00000 n ");
            sb.AppendLine("trailer <</Size 6 /Root 1 0 R>>");
            sb.AppendLine("startxref");
            sb.AppendLine("600");
            sb.AppendLine("%%EOF");

            byte[] pdfBytes = Encoding.UTF8.GetBytes(sb.ToString());

            var snapshot = new TaxYearSnapshot
            {
                UserId = userId,
                BaseYear = baseYear,
                GeneratedAt = DateTime.UtcNow,
                Format = "pdf",
                Payload = JsonSerializer.Serialize(previewResp.Data),
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };
            await repository.SaveSnapshotAsync(snapshot);

            return pdfBytes;
        }

        public async Task<ResponseApi<List<TaxYearSnapshot>>> GetSnapshotsAsync(string userId)
        {
            try
            {
                var list = await repository.GetSnapshotsAsync(userId);
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
                var cat = await repository.GetCategoryByIdAsync(categoryId);
                if (cat == null) return new(false, 404, "Categoria não encontrada");

                bool ok = await repository.UpdateCategoryTreatmentAsync(categoryId, request.IrTreatment, request.IrSubtype, userId);
                return new(ok, 200, "Tratamento de IR da categoria atualizado com sucesso");
            }
            catch (Exception ex)
            {
                return new(false, 500, $"Erro ao atualizar tratamento de IR: {ex.Message}");
            }
        }
    }
}
