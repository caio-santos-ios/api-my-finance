using api_finances.src.Models.Base;
using api_finances.src.Shared.DTOs;

namespace api_finances.src.Interfaces;

public interface IPatrimonyService
{
    Task<ResponseApi<dynamic>> GetSummaryAsync(string userId);
}
