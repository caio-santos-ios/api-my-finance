using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;

namespace api_finances.src.Interfaces
{
    public interface IAssetService
    {
        Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllRequest request);
        Task<ResponseApi<List<dynamic>>> GetSelectAsync(GetAllRequest request);
        Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id);
        Task<ResponseApi<Asset?>> CreateAsync(CreateAssetRequest request);
        Task<ResponseApi<Asset?>> UpdateAsync(UpdateAssetRequest request);
        Task<ResponseApi<Asset>> DeleteAsync(DeleteRequest request);
        Task<ResponseApi<List<AssetValuation>>> GetValuationsAsync(string assetId);
        Task<ResponseApi<AssetValuation?>> AddValuationAsync(string assetId, Requests.Asset.AddAssetValuationRequest request);
        Task<ResponseApi<AssetValuation?>> UpdateValuationAsync(string assetId, string valuationId, Requests.Asset.UpdateAssetValuationRequest request);
        Task<ResponseApi<bool>> DeleteValuationAsync(string assetId, string valuationId, string userId);
    }
}
