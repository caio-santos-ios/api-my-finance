namespace api_finances.src.Shared.DTOs
{
    public class GetAllDTO
    {
        public GetAllDTO(IQueryCollection queries)
        {
            foreach (var query in queries)
            { 
                QueryParams.Add(query.Key, query.Value!);
            }
        }
        public Dictionary<string, string> QueryParams { get; set; } = [];

    }
}