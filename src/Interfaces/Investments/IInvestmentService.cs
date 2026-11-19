using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;

namespace api_finances.src.Interfaces
{
    public interface IInvestmentService
    {
        Task<ResponseApi<List<InvestmentAsset>>> GetAssetsAsync(string userId);
        Task<ResponseApi<InvestmentAsset?>> GetAssetByIdAsync(string id);
        Task<ResponseApi<InvestmentAsset?>> CreateAssetAsync(string userId, CreateInvestmentAssetRequest request);
        Task<ResponseApi<InvestmentAsset?>> UpdateAssetAsync(string userId, UpdateInvestmentAssetRequest request);
        Task<ResponseApi<bool>> DeleteAssetAsync(string userId, string id);

        Task<ResponseApi<List<MarketAssetSearchResultDto>>> SearchMarketAssetsAsync(string userId, string query);
        Task<ResponseApi<MarketAssetCreateResultDto>> CreateFromMarketAsync(string userId, CreateMarketAssetRequest request);

        Task<ResponseApi<List<InvestmentTransactionResponse>>> GetTransactionsAsync(string userId, string? assetId = null, string? brokerId = null, string? kind = null, DateTime? start = null, DateTime? end = null);
        Task<ResponseApi<InvestmentTransaction?>> CreateTransactionAsync(string userId, CreateInvestmentTransactionRequest request);
        Task<ResponseApi<InvestmentTransaction?>> UpdateTransactionAsync(string userId, UpdateInvestmentTransactionRequest request);
        Task<ResponseApi<bool>> DeleteTransactionAsync(string userId, string id);

        Task<ResponseApi<B3PreviewResponseDto>> PreviewB3ImportAsync(string userId, IFormFile file);
        Task<ResponseApi<B3ConfirmImportResponseDto>> ConfirmB3ImportAsync(string userId, B3ConfirmImportRequest request);

        Task<ResponseApi<PortfolioResponse>> GetPortfolioAsync(string userId);
    }
}
