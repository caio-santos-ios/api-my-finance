using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;

namespace api_finances.src.Interfaces
{
    public interface ILifeCostService
    {
        Task<ResponseApi<LifeCostSummaryResponse>> GetSummaryAsync(string userId);
        Task<ResponseApi<List<LifeCostMonthlyPoint>>> GetMonthlyAsync(string userId, int months = 12);
        Task<ResponseApi<List<LifeCostBreakdownItem>>> GetBreakdownAsync(string userId, DateTime? start, DateTime? end);
        Task<ResponseApi<dynamic>> GetCompareAsync(string userId, int year1, int month1, int year2, int month2);
        Task<ResponseApi<LifeCostSettings>> GetSettingsAsync(string userId);
        Task<ResponseApi<LifeCostSettings>> UpdateSettingsAsync(string userId, UpdateLifeCostSettingsRequest request);
        Task<ResponseApi<bool>> AddAdjustmentAsync(string userId, AddMonthlyAdjustmentRequest request);
        Task<ResponseApi<bool>> DeleteAdjustmentAsync(string userId, string adjustmentId);
        Task<ResponseApi<Category>> UpdateCategoryClassificationAsync(string categoryId, UpdateCategoryClassificationRequest request);
        Task<ResponseApi<List<LifeCostCategoryClassificationItem>>> GetCategoriesClassificationAsync(string userId);
    }
}
