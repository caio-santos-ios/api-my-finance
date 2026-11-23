namespace api_finances.src.Requests.Ai
{
    public class ChatMessageDto
    {
        public string Role { get; set; } = "user"; // "user" | "model" | "assistant"
        public string Content { get; set; } = string.Empty;
    }

    public class AiChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<ChatMessageDto> History { get; set; } = [];
    }
}
