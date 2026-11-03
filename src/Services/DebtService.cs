using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;
using api_finances.src.Utils;
using MongoDB.Bson;

namespace api_finances.src.Services
{
    public class DebtService(
        IDebtRepository repository,
        IOperationRepository operationRepository,
        IDebtPaymentRepository paymentRepository
    ) : IDebtService
    {
        public async Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllRequest request)
        {
            try
            {
                PaginationUtil<Debt> pagination = new(request.QueryParams);
                BsonDocument[] pipeline = [
                    new("$match", pagination.PipelineFilter),
                    new("$sort", pagination.PipelineSort),
                    new("$skip", pagination.Skip),
                    new("$limit", pagination.Limit),

                    new("$addFields", new BsonDocument
                    {
                        { "bankObjId", new BsonDocument("$cond", new BsonDocument
                            {
                                { "if", new BsonDocument("$in", new BsonArray { "$bankId", new BsonArray { BsonNull.Value, "" } }) },
                                { "then", BsonNull.Value },
                                { "else", new BsonDocument("$toObjectId", "$bankId") }
                            })
                        }
                    }),

                    MongoUtil.Lookup("banks", ["$bankObjId"], ["$_id"], "_banks", [["deleted", false]], 1),

                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"id", new BsonDocument("$toString", "$_id")},
                        {"name", 1},
                        {"type", 1},
                        {"totalAmount", 1},
                        {"remainingAmount", 1},
                        {"installmentAmount", 1},
                        {"totalInstallments", 1},
                        {"paidInstallments", 1},
                        {"dueDay", 1},
                        {"bankId", 1},
                        {"bankName", MongoUtil.First("_banks.name")},
                        {"description", 1},
                        {"active", 1},
                        {"createdAt", 1}
                    })
                ];

                List<dynamic> response = await repository.GetAllAsync(pipeline);
                int totalCount = await repository.GetCountDocumentsAsync(pipeline);
                PaginationApi<List<dynamic>> result = new(response, totalCount, pagination.PageNumber, pagination.PageSize);

                return new(result, 200, "Dívidas listadas com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
            }
        }

        public async Task<ResponseApi<List<dynamic>>> GetSelectAsync(GetAllRequest request)
        {
            try
            {
                PaginationUtil<Debt> pagination = new(request.QueryParams);
                BsonDocument[] pipeline = [
                    new("$match", pagination.PipelineFilter),
                    new("$sort", new BsonDocument("name", 1)),
                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"id", new BsonDocument("$toString", "$_id")},
                        {"name", 1},
                        {"remainingAmount", 1},
                        {"installmentAmount", 1}
                    })
                ];

                List<dynamic> response = await repository.GetSelectAsync(pipeline);
                return new(response, 200, "Dívidas listadas com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
            }
        }

        public async Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id)
        {
            try
            {
                BsonDocument[] pipeline = [
                    new("$match", new BsonDocument{
                        {"_id", new ObjectId(id)},
                        {"deleted", false}
                    }),

                    new("$addFields", new BsonDocument
                    {
                        { "bankObjId", new BsonDocument("$cond", new BsonDocument
                            {
                                { "if", new BsonDocument("$in", new BsonArray { "$bankId", new BsonArray { BsonNull.Value, "" } }) },
                                { "then", BsonNull.Value },
                                { "else", new BsonDocument("$toObjectId", "$bankId") }
                            })
                        }
                    }),

                    MongoUtil.Lookup("banks", ["$bankObjId"], ["$_id"], "_banks", [["deleted", false]], 1),

                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"id", new BsonDocument("$toString", "$_id")},
                        {"name", 1},
                        {"type", 1},
                        {"totalAmount", 1},
                        {"remainingAmount", 1},
                        {"installmentAmount", 1},
                        {"totalInstallments", 1},
                        {"paidInstallments", 1},
                        {"dueDay", 1},
                        {"bankId", 1},
                        {"bankName", MongoUtil.First("_banks.name")},
                        {"description", 1},
                        {"active", 1},
                        {"createdAt", 1}
                    })
                ];

                dynamic? entity = await repository.GetByIdAggregateAsync(pipeline);
                if (entity is null) return new(null, 404, "Dívida não encontrada.");
                return new(entity, 200, "Dívida encontrada.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }

        public async Task<ResponseApi<Debt?>> CreateAsync(CreateDebtRequest request)
        {
            try
            {
                Debt debt = ObjectMapper.Map<CreateDebtRequest, Debt>(request);
                if (debt.RemainingAmount <= 0)
                {
                    debt.RemainingAmount = debt.TotalAmount;
                }
                Debt? response = await repository.CreateAsync(debt);
                if (response is null) return new(null, 400, "Falha ao cadastrar dívida.");

                return new(response, 201, "Dívida cadastrada com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
            }
        }

        public async Task<ResponseApi<Debt?>> UpdateAsync(UpdateDebtRequest request)
        {
            try
            {
                Debt? existed = await repository.GetByIdAsync(request.Id);
                if (existed is null) return new(null, 404, "Falha ao atualizar. Dívida não encontrada.");

                existed.UpdatedAt = DateTime.UtcNow;
                existed.UpdatedBy = request.UpdatedBy;
                existed.Name = request.Name;
                existed.Type = request.Type;
                existed.TotalAmount = request.TotalAmount;
                existed.RemainingAmount = request.RemainingAmount;
                existed.InstallmentAmount = request.InstallmentAmount;
                existed.TotalInstallments = request.TotalInstallments;
                existed.PaidInstallments = request.PaidInstallments;
                existed.DueDay = request.DueDay;
                existed.BankId = request.BankId;
                existed.Description = request.Description;

                Debt? response = await repository.UpdateAsync(existed);
                if (response is null) return new(null, 400, "Falha ao atualizar dívida.");

                return new(response, 200, "Dívida atualizada com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
            }
        }

        public async Task<ResponseApi<Debt>> DeleteAsync(DeleteRequest request)
        {
            try
            {
                Debt? existed = await repository.GetByIdAsync(request.Id);
                if (existed is null) return new(null, 404, "Falha ao excluir. Dívida não encontrada.");

                existed.Deleted = true;
                existed.DeletedAt = DateTime.UtcNow;
                existed.DeletedBy = request.DeletedBy;

                Debt response = await repository.DeleteAsync(existed);
                return new(response, 200, "Dívida excluída com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
            }
        }

        public async Task<ResponseApi<Debt?>> PayInstallmentAsync(string id, PayDebtInstallmentRequest request)
        {
            try
            {
                Debt? debt = await repository.GetByIdAsync(id);
                if (debt is null) return new(null, 404, "Dívida não encontrada.");

                decimal paymentValue = request.Amount > 0 ? request.Amount : debt.InstallmentAmount;
                decimal prevAmount = debt.RemainingAmount;
                debt.RemainingAmount = Math.Max(0, debt.RemainingAmount - paymentValue);
                debt.PaidInstallments += 1;
                debt.UpdatedAt = DateTime.UtcNow;
                debt.UpdatedBy = request.CreatedBy;

                await repository.UpdateAsync(debt);

                DebtPayment payment = new()
                {
                    DebtId = debt.Id,
                    Date = request.PaymentDate ?? DateTime.UtcNow,
                    Type = "installment",
                    Amount = paymentValue,
                    BankId = request.BankId,
                    PreviousRemainingAmount = prevAmount,
                    NewRemainingAmount = debt.RemainingAmount,
                    InstallmentsCount = 1,
                    Note = $"Pagamento parcela {debt.PaidInstallments}/{debt.TotalInstallments}",
                    CreatedBy = request.CreatedBy
                };
                await paymentRepository.CreateAsync(payment);

                if (request.CreateExpenseOperation)
                {
                    string targetBankId = !string.IsNullOrEmpty(request.BankId) ? request.BankId : debt.BankId;
                    Operation op = new()
                    {
                        Description = $"Pagamento parcela {debt.PaidInstallments}/{debt.TotalInstallments} - {debt.Name}",
                        Value = paymentValue,
                        Type = "expense",
                        BankId = targetBankId,
                        Origin = "debt",
                        OriginId = debt.Id,
                        CreatedAt = request.PaymentDate ?? DateTime.UtcNow,
                        CreatedBy = request.CreatedBy,
                        Active = true
                    };
                    await operationRepository.CreateAsync(op);
                }

                return new(debt, 200, "Pagamento registrado com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
            }
        }

        public async Task<ResponseApi<List<DebtPayment>>> GetPaymentsAsync(string debtId)
        {
            try
            {
                List<DebtPayment> payments = await paymentRepository.GetByDebtIdAsync(debtId);
                return new(payments, 200);
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado - {ex.Message}");
            }
        }

        public async Task<ResponseApi<DebtPayment?>> AddPaymentAsync(string debtId, Requests.Debt.AddDebtPaymentRequest request)
        {
            try
            {
                Debt? debt = await repository.GetByIdAsync(debtId);
                if (debt is null) return new(null, 404, "Dívida não encontrada.");

                decimal prevAmount = debt.RemainingAmount;
                decimal newRemaining = Math.Max(0, debt.RemainingAmount - request.Amount);
                debt.RemainingAmount = newRemaining;

                if (request.Type == "installment" || request.InstallmentsCount > 0)
                {
                    debt.PaidInstallments += request.InstallmentsCount;
                }

                debt.UpdatedAt = DateTime.UtcNow;
                debt.UpdatedBy = request.CreatedBy;
                await repository.UpdateAsync(debt);

                DebtPayment payment = new()
                {
                    DebtId = debtId,
                    Date = request.PaymentDate ?? DateTime.UtcNow,
                    Type = request.Type,
                    Amount = request.Amount,
                    BankId = request.BankId,
                    PreviousRemainingAmount = prevAmount,
                    NewRemainingAmount = newRemaining,
                    InstallmentsCount = request.InstallmentsCount,
                    Note = request.Note,
                    CreatedBy = request.CreatedBy
                };
                await paymentRepository.CreateAsync(payment);

                if (request.CreateExpenseOperation)
                {
                    string targetBankId = !string.IsNullOrEmpty(request.BankId) ? request.BankId : debt.BankId;
                    Operation op = new()
                    {
                        Description = request.Type == "amortization"
                            ? $"Amortização/Abatimento - {debt.Name}"
                            : $"Pagamento parcela {debt.PaidInstallments}/{debt.TotalInstallments} - {debt.Name}",
                        Value = request.Amount,
                        Type = "expense",
                        BankId = targetBankId,
                        Origin = "debt",
                        OriginId = debt.Id,
                        CreatedAt = request.PaymentDate ?? DateTime.UtcNow,
                        CreatedBy = request.CreatedBy,
                        Active = true
                    };
                    await operationRepository.CreateAsync(op);
                }

                return new(payment, 201, "Lançamento registrado com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado - {ex.Message}");
            }
        }

        public async Task<ResponseApi<DebtPayment?>> UpdatePaymentAsync(string debtId, string paymentId, Requests.Debt.UpdateDebtPaymentRequest request)
        {
            try
            {
                Debt? debt = await repository.GetByIdAsync(debtId);
                if (debt is null) return new(null, 404, "Dívida não encontrada.");

                DebtPayment? payment = await paymentRepository.GetByIdAsync(paymentId);
                if (payment is null) return new(null, 404, "Pagamento não encontrado.");

                payment.Type = request.Type;
                payment.Amount = request.Amount;
                payment.BankId = request.BankId;
                payment.Date = request.PaymentDate ?? payment.Date;
                payment.InstallmentsCount = request.InstallmentsCount;
                payment.Note = request.Note;
                payment.UpdatedAt = DateTime.UtcNow;
                payment.UpdatedBy = request.UpdatedBy;

                await paymentRepository.UpdateAsync(payment);
                await RecalculateDebtRemainingAsync(debt);

                return new(payment, 200, "Pagamento atualizado com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado - {ex.Message}");
            }
        }

        public async Task<ResponseApi<bool>> DeletePaymentAsync(string debtId, string paymentId, string userId)
        {
            try
            {
                Debt? debt = await repository.GetByIdAsync(debtId);
                if (debt is null) return new(false, 404, "Dívida não encontrada.");

                DebtPayment? payment = await paymentRepository.GetByIdAsync(paymentId);
                if (payment is null) return new(false, 404, "Pagamento não encontrado.");

                payment.Deleted = true;
                payment.DeletedAt = DateTime.UtcNow;
                payment.DeletedBy = userId;

                await paymentRepository.DeleteAsync(payment);
                await RecalculateDebtRemainingAsync(debt);

                return new(true, 200, "Pagamento excluído com sucesso.");
            }
            catch (Exception ex)
            {
                return new(false, 500, $"Ocorreu um erro inesperado - {ex.Message}");
            }
        }

        private async Task RecalculateDebtRemainingAsync(Debt debt)
        {
            List<DebtPayment> payments = await paymentRepository.GetByDebtIdAsync(debt.Id);
            payments.Reverse();
            decimal remaining = debt.TotalAmount;
            int paidInstallments = 0;

            foreach (var p in payments)
            {
                p.PreviousRemainingAmount = remaining;
                remaining = Math.Max(0, remaining - p.Amount);
                p.NewRemainingAmount = remaining;
                paidInstallments += p.InstallmentsCount;
                await paymentRepository.UpdateAsync(p);
            }

            debt.RemainingAmount = remaining;
            debt.PaidInstallments = paidInstallments;
            debt.UpdatedAt = DateTime.UtcNow;
            await repository.UpdateAsync(debt);
        }
    }
}
