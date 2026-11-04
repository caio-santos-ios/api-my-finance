using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using MongoDB.Driver;

namespace api_finances.src.Repository
{
    public class LifeCostRepository(AppDbContext context) : ILifeCostRepository
    {
        public async Task<LifeCostSettings?> GetByUserIdAsync(string userId)
        {
            return await context.LifeCostSettings
                .Find(x => (x.UserId == userId || x.CreatedBy == userId) && !x.Deleted)
                .FirstOrDefaultAsync();
        }

        public async Task<LifeCostSettings> UpsertSettingsAsync(LifeCostSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Id))
            {
                settings.CreatedAt = DateTime.UtcNow;
                settings.UpdatedAt = DateTime.UtcNow;
                await context.LifeCostSettings.InsertOneAsync(settings);
                return settings;
            }

            settings.UpdatedAt = DateTime.UtcNow;
            await context.LifeCostSettings.ReplaceOneAsync(x => x.Id == settings.Id, settings, new ReplaceOptions { IsUpsert = true });
            return settings;
        }

        public async Task<bool> AddAdjustmentAsync(string userId, MonthlyAdjustment adjustment)
        {
            LifeCostSettings? settings = await GetByUserIdAsync(userId);
            if (settings is null)
            {
                settings = new LifeCostSettings
                {
                    UserId = userId,
                    CreatedBy = userId,
                    MonthlyAdjustments = [adjustment]
                };
                await UpsertSettingsAsync(settings);
                return true;
            }

            FilterDefinition<LifeCostSettings> filter = Builders<LifeCostSettings>.Filter.Eq(x => x.Id, settings.Id);
            UpdateDefinition<LifeCostSettings> update = Builders<LifeCostSettings>.Update
                .Push(x => x.MonthlyAdjustments, adjustment)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            UpdateResult result = await context.LifeCostSettings.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteAdjustmentAsync(string userId, string adjustmentId)
        {
            LifeCostSettings? settings = await GetByUserIdAsync(userId);
            if (settings is null) return false;

            FilterDefinition<LifeCostSettings> filter = Builders<LifeCostSettings>.Filter.Eq(x => x.Id, settings.Id);
            UpdateDefinition<LifeCostSettings> update = Builders<LifeCostSettings>.Update
                .PullFilter(x => x.MonthlyAdjustments, a => a.Id == adjustmentId)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            UpdateResult result = await context.LifeCostSettings.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }
    }
}
