namespace api_finances.src.Requests
{
    public class UpdateFCMUserDTO : Request
    {
        public string Id { get; set; } = string.Empty;
        public string FCM { get; set; } = string.Empty;    
    }
}