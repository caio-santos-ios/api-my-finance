using api_finances.src.Requests;

namespace api_finances.src.Shared.DTOs
{
    public class DeleteDTO : Request
    {
        public string Id {get;set;} = string.Empty;
    }
}