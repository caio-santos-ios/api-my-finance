using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using MongoDB.Driver;

namespace api_finances.src.Repository;

public class DebtPaymentRepository(AppDbContext context) : IDebtPaymentRepository
{
    public async Task<List<DebtPayment>> GetByDebtIdAsync(string debtId)
    {
        return await context.DebtPayments
            .Find(x => x.DebtId == debtId && !x.Deleted)
            .SortByDescending(x => x.Date)
            .ToListAsync();
    }

    public async Task<DebtPayment?> CreateAsync(DebtPayment entity)
    {
        await context.DebtPayments.InsertOneAsync(entity);
        return entity;
    }

    public async Task<DebtPayment?> UpdateAsync(DebtPayment entity)
    {
        await context.DebtPayments.ReplaceOneAsync(x => x.Id == entity.Id, entity);
        return entity;
    }

    public async Task<DebtPayment> DeleteAsync(DebtPayment entity)
    {
        await context.DebtPayments.ReplaceOneAsync(x => x.Id == entity.Id, entity);
        return entity;
    }

    public async Task<DebtPayment?> GetByIdAsync(string id)
    {
        return await context.DebtPayments.Find(x => x.Id == id && !x.Deleted).FirstOrDefaultAsync();
    }
}
