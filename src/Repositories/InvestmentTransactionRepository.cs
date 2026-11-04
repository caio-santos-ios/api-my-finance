using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using MongoDB.Driver;

namespace api_finances.src.Repository
{
    public class InvestmentTransactionRepository(AppDbContext context) : IInvestmentTransactionRepository
    {
        public async Task<InvestmentTransaction?> GetByIdAsync(string id)
        {
            return await context.InvestmentTransactions.Find(x => x.Id == id && !x.Deleted).FirstOrDefaultAsync();
        }

        public async Task<List<InvestmentTransaction>> GetAllByUserAsync(string userId, string? assetId = null, string? brokerId = null, string? kind = null, DateTime? start = null, DateTime? end = null)
        {
            var builder = Builders<InvestmentTransaction>.Filter;
            var filter = builder.Eq(x => x.UserId, userId) & builder.Eq(x => x.Deleted, false);

            if (!string.IsNullOrEmpty(assetId))
                filter &= builder.Eq(x => x.InvestmentAssetId, assetId);

            if (!string.IsNullOrEmpty(brokerId))
                filter &= builder.Eq(x => x.BrokerId, brokerId);

            if (!string.IsNullOrEmpty(kind))
                filter &= builder.Eq(x => x.Kind, kind.ToUpperInvariant());

            if (start.HasValue)
                filter &= builder.Gte(x => x.Date, start.Value);

            if (end.HasValue)
                filter &= builder.Lte(x => x.Date, end.Value);

            return await context.InvestmentTransactions
                .Find(filter)
                .SortByDescending(x => x.Date)
                .ThenByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<InvestmentTransaction>> GetByAssetIdAsync(string assetId)
        {
            return await context.InvestmentTransactions
                .Find(x => x.InvestmentAssetId == assetId && !x.Deleted)
                .SortBy(x => x.Date)
                .ThenBy(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<InvestmentTransaction?> CreateAsync(InvestmentTransaction transaction)
        {
            transaction.CreatedAt = DateTime.UtcNow;
            transaction.UpdatedAt = DateTime.UtcNow;
            await context.InvestmentTransactions.InsertOneAsync(transaction);
            return transaction;
        }

        public async Task<InvestmentTransaction?> UpdateAsync(InvestmentTransaction transaction)
        {
            transaction.UpdatedAt = DateTime.UtcNow;
            await context.InvestmentTransactions.ReplaceOneAsync(x => x.Id == transaction.Id, transaction);
            return transaction;
        }

        public async Task<InvestmentTransaction> DeleteAsync(InvestmentTransaction transaction)
        {
            transaction.Deleted = true;
            transaction.DeletedAt = DateTime.UtcNow;
            await context.InvestmentTransactions.ReplaceOneAsync(x => x.Id == transaction.Id, transaction);
            return transaction;
        }
    }
}
