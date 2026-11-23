using api_finances.src.Models.Base;
using api_finances.src.Requests.Ai;

namespace api_finances.src.Interfaces
{
    public interface IAiService
    {
        Task<ResponseApi<string>> ChatAsync(string userId, AiChatRequest request);
    }
}
