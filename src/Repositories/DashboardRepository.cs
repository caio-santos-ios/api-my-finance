using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using MongoDB.Driver;

namespace api_finances.src.Repository
{
    public class DashboardRepository(AppDbContext context) : IDashboardRepository
    {
        public async Task<ResponseApi<dynamic>> GetAllAsync(string userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                List<Operation> operations = await context.Operations.Find(x => !x.Deleted && x.CreatedBy == userId && x.CreatedAt.Date >= startDate.Date.AddDays(-1) && x.CreatedAt.Date < endDate.Date.AddDays(1)).ToListAsync();
                decimal totalAccountIncome = operations.Where(x => x.Type == "income").Sum(x => x.Value);
                decimal totalAccountExpense = operations.Where(x => x.Type == "expense").Sum(x => x.Value);

                dynamic data = new
                {
                    totalAccount = totalAccountIncome - totalAccountExpense,
                    totalAccountIncome,
                    totalAccountExpense,
                    operations
                };

                return new(data);
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
    }
}