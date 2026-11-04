using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using MongoDB.Driver;

namespace api_finances.src.Repository
{
    public class TaxRepository(AppDbContext context) : ITaxRepository
    {
        public async Task<List<Category>> GetCategoriesAsync(string userId)
        {
            return await context.Categories
                .Find(c => c.CreatedBy == userId && !c.Deleted)
                .ToListAsync();
        }

        public async Task<Category?> GetCategoryByIdAsync(string categoryId)
        {
            return await context.Categories
                .Find(c => c.Id == categoryId && !c.Deleted)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateCategoryTreatmentAsync(string categoryId, string treatment, string? subtype, string userId)
        {
            var update = Builders<Category>.Update
                .Set(c => c.IrTreatment, treatment)
                .Set(c => c.IrSubtype, subtype)
                .Set(c => c.UpdatedBy, userId)
                .Set(c => c.UpdatedAt, DateTime.UtcNow);

            var res = await context.Categories.UpdateOneAsync(c => c.Id == categoryId, update);
            return res.ModifiedCount > 0;
        }

        public async Task<List<Operation>> GetOperationsByPeriodAsync(string userId, DateTime start, DateTime end)
        {
            return await context.Operations
                .Find(o => o.CreatedBy == userId && !o.Deleted && o.CreatedAt >= start && o.CreatedAt <= end)
                .ToListAsync();
        }

        public async Task<List<Asset>> GetAssetsAsync(string userId)
        {
            return await context.Assets
                .Find(a => a.CreatedBy == userId && !a.Deleted && a.IncludeInIr)
                .ToListAsync();
        }

        public async Task<List<InvestmentAsset>> GetInvestmentAssetsAsync(string userId)
        {
            return await context.InvestmentAssets
                .Find(a => a.CreatedBy == userId && !a.Deleted)
                .ToListAsync();
        }

        public async Task<List<InvestmentTransaction>> GetInvestmentTransactionsAsync(string userId, DateTime upToDate)
        {
            return await context.InvestmentTransactions
                .Find(t => t.CreatedBy == userId && !t.Deleted && t.Date <= upToDate)
                .ToListAsync();
        }

        public async Task<List<Debt>> GetDebtsAsync(string userId)
        {
            return await context.Debts
                .Find(d => d.CreatedBy == userId && !d.Deleted)
                .ToListAsync();
        }

        public async Task<List<DebtPayment>> GetDebtPaymentsAsync(string userId, DateTime upToDate)
        {
            return await context.DebtPayments
                .Find(p => p.CreatedBy == userId && !p.Deleted && p.Date <= upToDate)
                .ToListAsync();
        }

        public async Task<List<Attachment>> GetAttachmentsByOriginIdsAsync(List<string> originIds)
        {
            if (originIds.Count == 0) return [];
            return await context.Attachments
                .Find(a => originIds.Contains(a.ParentId) && !a.Deleted)
                .ToListAsync();
        }

        public async Task<TaxYearSnapshot> SaveSnapshotAsync(TaxYearSnapshot snapshot)
        {
            await context.TaxYearSnapshots.InsertOneAsync(snapshot);
            return snapshot;
        }

        public async Task<List<TaxYearSnapshot>> GetSnapshotsAsync(string userId)
        {
            return await context.TaxYearSnapshots
                .Find(s => s.UserId == userId && !s.Deleted)
                .SortByDescending(s => s.GeneratedAt)
                .ToListAsync();
        }
    }
}
