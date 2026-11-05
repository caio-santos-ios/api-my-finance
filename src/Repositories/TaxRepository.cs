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
            var filter = Builders<Category>.Filter.Eq(c => c.Deleted, false) &
                         (Builders<Category>.Filter.Eq(c => c.CreatedBy, userId) |
                          Builders<Category>.Filter.Eq(c => c.CreatedBy, "") |
                          Builders<Category>.Filter.Eq(c => c.CreatedBy, null!));

            return await context.Categories
                .Find(filter)
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

            var res = await context.Categories.UpdateOneAsync(c => c.Id == categoryId && (c.CreatedBy == userId || c.CreatedBy == "" || c.CreatedBy == null) && !c.Deleted, update);
            return res.ModifiedCount > 0;
        }

        public async Task<List<Operation>> GetOperationsByPeriodAsync(string userId, DateTime start, DateTime end)
        {
            var filter = Builders<Operation>.Filter.Eq(o => o.Deleted, false) &
                         (Builders<Operation>.Filter.Eq(o => o.CreatedBy, userId) |
                          Builders<Operation>.Filter.Eq(o => o.CreatedBy, "") |
                          Builders<Operation>.Filter.Eq(o => o.CreatedBy, null!)) &
                         Builders<Operation>.Filter.Gte(o => o.CreatedAt, start.AddDays(-2)) &
                         Builders<Operation>.Filter.Lte(o => o.CreatedAt, end.AddDays(2));

            var list = await context.Operations
                .Find(filter)
                .ToListAsync();

            return list
                .Where(o => o.CreatedAt.Year == start.Year || o.CreatedAt.ToLocalTime().Year == start.Year || (o.CreatedAt >= start && o.CreatedAt <= end))
                .ToList();
        }

        public async Task<List<Asset>> GetAssetsAsync(string userId)
        {
            var filter = Builders<Asset>.Filter.Eq(a => a.Deleted, false) &
                         (Builders<Asset>.Filter.Eq(a => a.CreatedBy, userId) |
                          Builders<Asset>.Filter.Eq(a => a.CreatedBy, "") |
                          Builders<Asset>.Filter.Eq(a => a.CreatedBy, null!)) &
                         Builders<Asset>.Filter.Ne(a => a.IncludeInIr, false);

            return await context.Assets
                .Find(filter)
                .ToListAsync();
        }

        public async Task<List<InvestmentAsset>> GetInvestmentAssetsAsync(string userId)
        {
            var filter = Builders<InvestmentAsset>.Filter.Eq(a => a.Deleted, false) &
                         (Builders<InvestmentAsset>.Filter.Eq(a => a.UserId, userId) |
                          Builders<InvestmentAsset>.Filter.Eq(a => a.CreatedBy, userId) |
                          Builders<InvestmentAsset>.Filter.Eq(a => a.CreatedBy, "") |
                          Builders<InvestmentAsset>.Filter.Eq(a => a.CreatedBy, null!));

            return await context.InvestmentAssets
                .Find(filter)
                .ToListAsync();
        }

        public async Task<List<InvestmentTransaction>> GetInvestmentTransactionsAsync(string userId, DateTime upToDate)
        {
            var filter = Builders<InvestmentTransaction>.Filter.Eq(t => t.Deleted, false) &
                         (Builders<InvestmentTransaction>.Filter.Eq(t => t.UserId, userId) |
                          Builders<InvestmentTransaction>.Filter.Eq(t => t.CreatedBy, userId) |
                          Builders<InvestmentTransaction>.Filter.Eq(t => t.CreatedBy, "") |
                          Builders<InvestmentTransaction>.Filter.Eq(t => t.CreatedBy, null!)) &
                         Builders<InvestmentTransaction>.Filter.Lte(t => t.Date, upToDate.AddDays(2));

            return await context.InvestmentTransactions
                .Find(filter)
                .ToListAsync();
        }

        public async Task<List<Debt>> GetDebtsAsync(string userId)
        {
            var filter = Builders<Debt>.Filter.Eq(d => d.Deleted, false) &
                         (Builders<Debt>.Filter.Eq(d => d.CreatedBy, userId) |
                          Builders<Debt>.Filter.Eq(d => d.CreatedBy, "") |
                          Builders<Debt>.Filter.Eq(d => d.CreatedBy, null!));

            return await context.Debts
                .Find(filter)
                .ToListAsync();
        }

        public async Task<List<DebtPayment>> GetDebtPaymentsAsync(string userId, DateTime upToDate)
        {
            var filter = Builders<DebtPayment>.Filter.Eq(p => p.Deleted, false) &
                         (Builders<DebtPayment>.Filter.Eq(p => p.CreatedBy, userId) |
                          Builders<DebtPayment>.Filter.Eq(p => p.CreatedBy, "") |
                          Builders<DebtPayment>.Filter.Eq(p => p.CreatedBy, null!)) &
                         Builders<DebtPayment>.Filter.Lte(p => p.Date, upToDate.AddDays(2));

            return await context.DebtPayments
                .Find(filter)
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

        public async Task<List<TaxYearSnapshot>> GetSnapshotsAsync(string userId, int? baseYear = null)
        {
            var filter = Builders<TaxYearSnapshot>.Filter.Eq(s => s.UserId, userId) &
                         Builders<TaxYearSnapshot>.Filter.Eq(s => s.Deleted, false);

            if (baseYear.HasValue)
            {
                filter &= Builders<TaxYearSnapshot>.Filter.Eq(s => s.BaseYear, baseYear.Value);
            }

            return await context.TaxYearSnapshots
                .Find(filter)
                .SortByDescending(s => s.GeneratedAt)
                .ToListAsync();
        }
    }
}
