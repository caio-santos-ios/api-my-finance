using api_finances.src.Models.Base;

namespace api_finances.src.Interfaces
{
    public interface IDashboardRepository
    {
        Task<ResponseApi<dynamic>> GetAllAsync(string userId, DateTime startDate, DateTime endDate);
    }
}