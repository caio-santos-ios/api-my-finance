namespace api_finances.src.Shared.DTOs
{
    public class GetAllRequest
    {
        public GetAllRequest(IQueryCollection queries)
        {
            foreach (var query in queries)
            { 
                QueryParams.Add(query.Key, query.Value!);
            }
        }
        public Dictionary<string, string> QueryParams { get; set; } = [];

    }
}