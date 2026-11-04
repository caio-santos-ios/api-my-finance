using api_finances.src.Models;

namespace api_finances.src.Interfaces
{
    public interface ILifeCostRepository
    {
        Task<LifeCostSettings?> GetByUserIdAsync(string userId);
        Task<LifeCostSettings> UpsertSettingsAsync(LifeCostSettings settings);
        Task<bool> AddAdjustmentAsync(string userId, MonthlyAdjustment adjustment);
        Task<bool> DeleteAdjustmentAsync(string userId, string adjustmentId);
    }
}
