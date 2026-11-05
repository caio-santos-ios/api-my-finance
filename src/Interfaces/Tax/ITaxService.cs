using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;

namespace api_finances.src.Interfaces
{
    public interface ITaxService
    {
        Task<ResponseApi<TaxPreviewDto>> GetPreviewAsync(int baseYear, string userId);
        Task<ResponseApi<dynamic>> GetIncomeAsync(int baseYear, string userId);
        Task<ResponseApi<dynamic>> GetDeductionsAsync(int baseYear, string userId);
        Task<ResponseApi<dynamic>> GetAssetsAsync(int baseYear, string userId);
        Task<ResponseApi<dynamic>> GetDebtsAsync(int baseYear, string userId);
        Task<ResponseApi<List<TaxPendingItemDto>>> GetPendingAsync(int baseYear, string userId);
        Task<byte[]> ExportSpreadsheetAsync(int baseYear, string userId);
        Task<byte[]> ExportPdfAsync(int baseYear, string userId);
        Task<ResponseApi<List<TaxYearSnapshot>>> GetSnapshotsAsync(string userId, int? baseYear = null);
        Task<ResponseApi<bool>> UpdateCategoryTreatmentAsync(string categoryId, UpdateCategoryIrTreatmentRequest request, string userId);
    }
}
