using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests
{
    public class UpdateCategoryClassificationRequest : Request
    {
        [Required(ErrorMessage = "A classe de custo de vida é obrigatória.")]
        public string LifeCostClass { get; set; } = "DISCRETIONARY";

        public string LifeCostRecurrence { get; set; } = "VARIABLE";
    }
}
