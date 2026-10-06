using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests.Dashboard;

namespace api_finances.src.Interfaces
{
    public interface IDashboardService
    {
        Task<ResponseApi<dynamic>> GetAllAsync(string userId, DateTime startDate, DateTime endDate);
        Task<ResponseApi<Dashboard?>> CreateAsync(CreateDashboardRequest request);
    }
}