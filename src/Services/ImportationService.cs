using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using api_finances.src.Helpers;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;
using api_finances.src.Utils;
using ClosedXML.Excel;

namespace api_finances.src.Services
{
    public class ImportationService(
        IImportationRepository repository,
        IBankRepository bankRepository,
        IOperationRepository operationRepository
    ) : IImportationService
    {
        #region READ
        public async Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllRequest request)
        {
            try
            {
                PaginationUtil<Importation> pagination = new(request.QueryParams);
                ResponseApi<List<dynamic>> attachments = await repository.GetAllAsync(pagination);
                int count = await repository.GetCountDocumentsAsync(pagination);
                PaginationApi<List<dynamic>> data = new(attachments.Data, count, pagination.PageNumber, pagination.PageSize);
                return new(data, 200, "Importações listados com sucesso");
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
                PaginationUtil<Importation> pagination = new(request.QueryParams);
                ResponseApi<List<dynamic>> attachments = await repository.GetSelectAsync(pagination);
                return new(attachments.Data, 200, "Importações listados com sucesso");
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
                ResponseApi<dynamic?> attachment = await repository.GetByIdAggregateAsync(id);
                if (attachment.Data is null) return new(null, 404, "Anexo não encontrado");
                return new(attachment.Data, 200, "Anexo encontrado");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion

        #region CREATE
        public async Task<ResponseApi<Importation?>> CreateAsync(CreateImportationRequest request)
        {
            try
            {
                if (request.File is null) return new(null, 400, "Falha ao salvar anexo");
                Importation attachment = ObjectMapper.Map<CreateImportationRequest, Importation>(request);
                if (request.File == null || request.File.Length == 0)
                {
                    return new(null, 400, "Nenhum arquivo de planilha foi enviado.");
                }

                string extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();

                List<Dictionary<string, string>> rowsList = [];
                List<string> columnHeaders = [];

                if (extension != ".xlsx") return new(null, 400, "Arquivo deve ser xlsx");

                using var stream = new MemoryStream();
                await request.File.CopyToAsync(stream);
                stream.Position = 0;

                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    return new(null, 400, "A planilha não contém nenhuma aba.");
                }

                var range = worksheet.RangeUsed();
                if (range == null)
                {
                    return new(null, 400, "A planilha está vazia.");
                }

                int firstRow = range.FirstRow().RowNumber();
                int lastRow = range.LastRow().RowNumber();
                int firstColumn = range.FirstColumn().ColumnNumber();
                int lastColumn = range.LastColumn().ColumnNumber();
                Bank? bank = await bankRepository.GetByCodeAsync(request.Bank);
                List<string> myTranferName = (request.Name ?? "")
                    .Split(";", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(x => x.ToUpper())
                    .ToList();

                switch (request.Bank)
                {
                    case "nubank-j":
                    case "nubank":
                        await repository.CreateAsync(new()
                        {
                            Name = "Importação",
                            CreatedBy = request.CreatedBy,
                        });

                        for (int row = 2; row <= lastRow; row++)
                        {
                            string date = worksheet.Cell(row, 1).GetFormattedString().Trim();
                            string value = worksheet.Cell(row, 2).GetFormattedString().Trim();
                            string id = worksheet.Cell(row, 3).GetFormattedString().Trim();
                            string description = NormalizeText(worksheet.Cell(row, 4).GetFormattedString().Trim());

                            if (string.IsNullOrWhiteSpace(date) || string.IsNullOrWhiteSpace(value)) continue;

                            Operation? existedOperation = await operationRepository.GetByOriginIdAsync(id);

                            List<string> splitDate = date.Split(['/', '-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                            if (splitDate.Count < 3) continue;

                            List<string> splitName = description.Split(" - ").Select(x => x.Trim()).ToList();
                            string type = splitName.Count > 0 ? splitName[0] : description;
                            string name = splitName.Count > 1 ? splitName[1] : "";

                            if (!worksheet.Cell(row, 2).TryGetValue<decimal>(out decimal rawCost))
                            {
                                CultureInfo culture = value.Contains(',') ? new CultureInfo("pt-BR") : CultureInfo.InvariantCulture;
                                rawCost = decimal.Parse(value, NumberStyles.Any, culture);
                            }
                            decimal cost = Math.Abs(rawCost);

                            DateTime dateTime = splitDate[0].Length == 4
                                ? new DateTime(int.Parse(splitDate[0]), int.Parse(splitDate[1]), int.Parse(splitDate[2]))
                                : new DateTime(int.Parse(splitDate[2]), int.Parse(splitDate[1]), int.Parse(splitDate[0]));
                            Operation? operation = null;

                            bool isSelfTransfer = !string.IsNullOrWhiteSpace(name) && myTranferName.Contains(name.ToUpper());
                            bool isIncome = type.ToUpper().Contains("TRANSFERÊNCIA RECEBIDA")
                                || type.ToUpper().Contains("TRANSFERENCIA RECEBIDA")
                                || type.ToUpper().Contains("RECEBIDA")
                                || rawCost > 0;

                            if (!isSelfTransfer && isIncome)
                            {
                                operation = new()
                                {
                                    Origin = "importation",
                                    OriginId = id,
                                    Value = cost,
                                    CreatedAt = dateTime,
                                    CategoryId = "",
                                    Repeat = false,
                                    Active = true,
                                    BankId = bank is null ? "" : bank.Id,
                                    Description = description.ToUpper(),
                                    Type = "income",
                                    CreatedBy = request.CreatedBy,
                                    UpdatedBy = request.CreatedBy,

                                };
                            }
                            else
                            {
                                if (!isSelfTransfer)
                                {
                                    operation = new()
                                    {
                                        Origin = "importation",
                                        OriginId = id,
                                        Value = cost,
                                        CreatedAt = dateTime,
                                        CategoryId = "",
                                        Repeat = false,
                                        Active = true,
                                        BankId = bank is null ? "" : bank.Id,
                                        Description = description.ToUpper(),
                                        Type = "expense",
                                        CreatedBy = request.CreatedBy,
                                        UpdatedBy = request.CreatedBy
                                    };
                                }
                                else
                                {
                                    operation = new()
                                    {
                                        Origin = "importation",
                                        OriginId = id,
                                        Value = cost,
                                        CreatedAt = dateTime,
                                        CategoryId = "",
                                        Repeat = false,
                                        Active = true,
                                        BankId = bank is null ? "" : bank.Id,
                                        Description = description.ToUpper(),
                                        Type = "transfer",
                                        CreatedBy = request.CreatedBy,
                                        UpdatedBy = request.CreatedBy
                                    };

                                    string bankPart = splitName.Count > 3 ? splitName[3].ToUpper() : description.ToUpper();
                                    Bank? existedBank = await bankRepository.GetByCodeAsync(GetCodeBank(bankPart));
                                    if (existedBank is not null)
                                    {
                                        operation.DestinationBankId = existedBank.Id;
                                    }
                                    else
                                    {
                                        Bank? firstBank = await bankRepository.GetFirstAsync();
                                        if (firstBank is not null)
                                        {
                                            operation.DestinationBankId = firstBank.Id;
                                        }
                                    }
                                }
                            }

                            if (operation is not null)
                            {
                                if (existedOperation is null)
                                {
                                    await operationRepository.CreateAsync(operation);
                                }
                                else if (existedOperation.Type != operation.Type || existedOperation.Description != operation.Description)
                                {
                                    existedOperation.Type = operation.Type;
                                    existedOperation.Description = operation.Description;
                                    existedOperation.DestinationBankId = operation.DestinationBankId;
                                    existedOperation.UpdatedAt = DateTime.UtcNow;
                                    await operationRepository.UpdateAsync(existedOperation);
                                }
                            }
                        }

                        break;

                    case "picpay":
                        await repository.CreateAsync(new()
                        {
                            Name = "Importação",
                            CreatedBy = request.CreatedBy,
                        });

                        for (int row = 2; row <= lastRow; row++)
                        {
                            string date = worksheet.Cell(row, 1).GetFormattedString().Trim();
                            string hour = worksheet.Cell(row, 2).GetFormattedString().Trim();
                            string type = NormalizeText(worksheet.Cell(row, 3).GetFormattedString().Trim());
                            string description = NormalizeText(worksheet.Cell(row, 4).GetFormattedString().Trim());
                            string value = Regex.Replace(worksheet.Cell(row, 5).GetFormattedString(), @"[^\d.,]", "");

                            if (string.IsNullOrWhiteSpace(date) || string.IsNullOrWhiteSpace(value)) continue;

                            string id = $"{row}-{date}-{hour}-{value}";

                            Operation? existedOperation = await operationRepository.GetByOriginIdAsync(id);

                            List<string> splitDate = date.Split(['/', '-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                            if (splitDate.Count < 3) continue;

                            CultureInfo picpayCulture = value.Contains(',') ? new CultureInfo("pt-BR") : CultureInfo.InvariantCulture;
                            decimal cost = decimal.Parse(value, NumberStyles.Any, picpayCulture);
                            cost = Math.Abs(cost);
                            DateTime dateTime = splitDate[0].Length == 4
                                ? new DateTime(int.Parse(splitDate[0]), int.Parse(splitDate[1]), int.Parse(splitDate[2]))
                                : new DateTime(int.Parse(splitDate[2]), int.Parse(splitDate[1]), int.Parse(splitDate[0]));
                            Operation? operation = null;

                            bool isPicpayIncome = type.ToUpper().Contains("TRANSFERÊNCIA RECEBIDA")
                                || type.ToUpper().Contains("TRANSFERENCIA RECEBIDA")
                                || type.ToUpper().Contains("RECEBIDA");

                            if (!myTranferName.Contains(description.ToUpper()) && isPicpayIncome)
                            {
                                operation = new()
                                {
                                    Origin = "importation",
                                    OriginId = id,
                                    Value = cost,
                                    CreatedAt = dateTime,
                                    CategoryId = "",
                                    Repeat = false,
                                    Active = true,
                                    BankId = bank is null ? "" : bank.Id,
                                    Description = description.ToUpper(),
                                    Type = "income",
                                    CreatedBy = request.CreatedBy,
                                    UpdatedBy = request.CreatedBy,

                                };
                            }
                            else
                            {
                                if (!myTranferName.Contains(description.ToUpper()))
                                {
                                    operation = new()
                                    {
                                        Origin = "importation",
                                        OriginId = id,
                                        Value = cost,
                                        CreatedAt = dateTime,
                                        CategoryId = "",
                                        Repeat = false,
                                        Active = true,
                                        BankId = bank is null ? "" : bank.Id,
                                        Description = description.ToUpper(),
                                        Type = "expense",
                                        CreatedBy = request.CreatedBy,
                                        UpdatedBy = request.CreatedBy
                                    };
                                }
                                else
                                {
                                    operation = new()
                                    {
                                        Origin = "importation",
                                        OriginId = id,
                                        Value = cost,
                                        CreatedAt = dateTime,
                                        CategoryId = "",
                                        Repeat = false,
                                        Active = true,
                                        BankId = bank is null ? "" : bank.Id,
                                        Description = description.ToUpper(),
                                        Type = "transfer",
                                        CreatedBy = request.CreatedBy,
                                        UpdatedBy = request.CreatedBy
                                    };

                                    Bank? firstBank = await bankRepository.GetFirstAsync();
                                    if (firstBank is not null)
                                    {
                                        operation.DestinationBankId = firstBank.Id;
                                    }
                                }
                            }

                            if (operation is not null)
                            {
                                if (existedOperation is null)
                                {
                                    await operationRepository.CreateAsync(operation);
                                }
                                else if (existedOperation.Type != operation.Type || existedOperation.Description != operation.Description)
                                {
                                    existedOperation.Type = operation.Type;
                                    existedOperation.Description = operation.Description;
                                    existedOperation.DestinationBankId = operation.DestinationBankId;
                                    existedOperation.UpdatedAt = DateTime.UtcNow;
                                    await operationRepository.UpdateAsync(existedOperation);
                                }
                            }
                        }

                        break;
                }

                return new(null, 201, "Importação feita com sucesso.");
            }
            catch(Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
            }
        }

        #endregion

        #region UPDATE
        public async Task<ResponseApi<Importation?>> UpdateAsync(UpdateImportationRequest request)
        {
            try
            {
                ResponseApi<Importation?> existed = await repository.GetByIdAsync(request.Id);
                if (existed.Data is null) return new(null, 404, "Falha ao atualizar");

                existed.Data.UpdatedAt = DateTime.UtcNow;
                existed.Data.UpdatedBy = request.UpdatedBy;

                ResponseApi<Importation?> response = await repository.UpdateAsync(existed.Data);
                if (!response.IsSuccess) return new(null, 400, "Falha ao atualizar");

                return new(response.Data, 200, "Atualizado com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion

        #region DELETE
        public async Task<ResponseApi<Importation>> DeleteAsync(DeleteRequest request)
        {
            try
            {
                ResponseApi<Importation> attachment = await repository.DeleteAsync(request);
                if (!attachment.IsSuccess) return new(null, 400, attachment.Message);
                return new(attachment.Data, 204, "Anexo excluído com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion  
        #region READ FILE
        private static string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            string normalized = text.Replace("â€¢", "•");

            if (normalized.Contains('Ã') || normalized.Contains('Â') || normalized.Contains("â"))
            {
                byte[] bytes = Encoding.Latin1.GetBytes(normalized);
                string decoded = Encoding.UTF8.GetString(bytes);
                if (!decoded.Contains('\uFFFD'))
                {
                    normalized = decoded;
                }
            }

            return normalized
                .Replace("Ãª", "ê")
                .Replace("ÃŠ", "Ê")
                .Replace("Ã©", "é")
                .Replace("Ã‰", "É")
                .Replace("Ã£", "ã")
                .Replace("Ãƒ", "Ã")
                .Replace("Ã§", "ç")
                .Replace("Ã‡", "Ç")
                .Replace("Ãµ", "õ")
                .Replace("Ã•", "Õ")
                .Replace("Ã³", "ó")
                .Replace("Ã“", "Ó")
                .Replace("Ã¡", "á")
                .Replace("Ã\u0081", "Á")
                .Replace("Ãº", "ú")
                .Replace("Ãš", "Ú")
                .Replace("Ã­", "í")
                .Replace("Ã\u008d", "Í")
                .Replace("Ã´", "ô")
                .Replace("Ã”", "Ô")
                .Replace("Ã¢", "â")
                .Replace("Ã‚", "Â");
        }

        private string GetCodeBank(string bankName)
        {
            if (string.IsNullOrWhiteSpace(bankName))
                return "";

            var name = bankName.ToUpperInvariant();

            foreach (var entry in BankCodeMap)
            {
                if (name.Contains(entry.Key))
                    return entry.Value;
            }

            return "";
        }
        private static readonly Dictionary<string, string> BankCodeMap = new()
        {
            { "NU PAGAMENTOS", "nubank" },
            { "NUBANK", "nubank" },
            { "PICPAY", "picpay" },
            { "MERCADO PAGO", "mercado-pago" },
            { "MERCADOPAGO", "mercado-pago" },
            { "ITAU", "itau" },
            { "ITAÚ", "itau" },
            { "BANCO ITAU", "itau" },
            { "BRADESCO", "bradesco" },
            { "BANCO BRADESCO", "bradesco" },
            { "BANCO DO BRASIL", "banco-do-brasil" },
            { "BB ", "banco-do-brasil" },
            { "CAIXA ECONOMICA", "caixa" },
            { "CAIXA ECONÔMICA", "caixa" },
            { "SANTANDER", "santander" },
            { "BANCO SANTANDER", "santander" },
            { "INTER", "inter" },
            { "BANCO INTER", "inter" },
            { "C6 BANK", "c6" },
            { "C6", "c6" },
            { "NEON", "neon" },
            { "NEON PAGAMENTOS", "neon" },
            { "NEXT", "next" },
            { "BANCO NEXT", "next" },
            { "WILL BANK", "will" },
            { "WILL", "will" },
            { "PAGSEGURO", "pagseguro" },
            { "PAGBANK", "pagseguro" },
            { "STONE", "stone" },
            { "SICOOB", "sicoob" },
            { "SICREDI", "sicredi" },
            { "BTG PACTUAL", "btg" },
            { "BTG", "btg" },
            { "ORIGINAL", "original" },
            { "BANCO ORIGINAL", "original" },
            { "SAFRA", "safra" },
            { "BANCO SAFRA", "safra" },
            { "VOTORANTIM", "banco-votorantim" },
            { "BV ", "bv" },
            { "BANCO BV", "bv" },
        };
        #endregion      
    }
}