using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests.Dashboard;
using api_finances.src.Shared.DTOs;

namespace api_finances.src.Interfaces
{
    public interface IDashboardService
    {
        Task<ResponseApi<dynamic>> GetDashAsync(GetAllRequest request);
        Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllRequest request);
        Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id);
        Task<ResponseApi<Dashboard?>> CreateAsync(CreateDashboardRequest request);
        Task<ResponseApi<Dashboard?>> UpdateAsync(UpdateDashboardRequest request);
    }
}