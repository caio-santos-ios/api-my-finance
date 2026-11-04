namespace api_finances.src.Requests
{
    public class UpdateCategoryIrTreatmentRequest
    {
        public string IrTreatment { get; set; } = "NONE";
        public string? IrSubtype { get; set; }
    }
}
