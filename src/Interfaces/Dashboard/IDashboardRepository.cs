using api_finances.src.Models;

namespace api_finances.src.Interfaces
{
    public interface IDashboardRepository
    {
        Task<List<Dashboard>> GetAllAsync(string userId);
        Task<Dashboard?> GetByIdAsync(string id);
        Task<Dashboard?> CreateAsync(Dashboard entity);
        Task<Dashboard?> UpdateAsync(Dashboard entity);
    }
}