using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests
{
    public class UpdateAttachmentRequest : Request
    {
        [Required(ErrorMessage = "O Id é obrigatório.")]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "O ParentId é obrigatório.")]
        public string ParentId { get; set; } = string.Empty;

        [Required(ErrorMessage = "O Parent é obrigatório.")]
        public string Parent { get; set; } = string.Empty;
        public IFormFile? File { get; set; }
    }
}