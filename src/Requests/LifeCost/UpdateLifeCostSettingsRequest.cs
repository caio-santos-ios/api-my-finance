using System.ComponentModel.DataAnnotations;

namespace api_finances.src.Requests
{
    public class UpdateLifeCostSettingsRequest : Request
    {
        [Range(1, 24, ErrorMessage = "A janela de média deve ser entre 1 e 24 meses.")]
        public int AverageWindowMonths { get; set; } = 6;

        [Range(0.001, 1.0, ErrorMessage = "A taxa de retirada segura deve ser válida (ex: 0.04 para 4%).")]
        public decimal SafeWithdrawalRate { get; set; } = 0.04m;

        [Range(1, 60, ErrorMessage = "A meta de meses da reserva de emergência deve ser entre 1 e 60.")]
        public int EmergencyReserveTargetMonths { get; set; } = 6;

        public bool IncludeDebtPayments { get; set; } = false;
    }
}
