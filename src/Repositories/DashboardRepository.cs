using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using MongoDB.Driver;

namespace api_finances.src.Repository
{
    public class DashboardRepository(AppDbContext context) : IDashboardRepository
    {
        public async Task<List<Dashboard>> GetAllAsync(string userId)
        {
            List<Dashboard> entities = await context.Dashboards.Find(x => !x.Deleted && x.Active && x.CreatedBy == userId).ToListAsync();
            return entities;
        }
        public async Task<Dashboard?> GetByIdAsync(string id)
        {
            Dashboard? entity = await context.Dashboards.Find(x => !x.Deleted && x.Id == id).FirstOrDefaultAsync();
            return entity;
        }
        public async Task<Dashboard?> CreateAsync(Dashboard entity)
        {
            await context.Dashboards.InsertOneAsync(entity);
            return entity;
        }
        public async Task<Dashboard?> UpdateAsync(Dashboard entity)
        {
            await context.Dashboards.ReplaceOneAsync(x => x.Id == entity.Id, entity);
            return entity;
        }
    }
}