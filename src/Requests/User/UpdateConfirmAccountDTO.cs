namespace api_finances.src.Requests
{
    public class UpdateConfirmAccountDTO : Request
    {
        public string Code { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}