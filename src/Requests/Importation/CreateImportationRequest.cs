namespace api_finances.src.Requests
{
    public class CreateImportationRequest : Request
    {
        public IFormFile? File { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Bank { get; set; } = string.Empty;
    }
}