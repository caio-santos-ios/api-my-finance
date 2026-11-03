using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;
using api_finances.src.Utils;
using MongoDB.Bson;

namespace api_finances.src.Services
{
    public class AssetService(
        IAssetRepository repository,
        IAssetValuationRepository valuationRepository
    ) : IAssetService
    {
        public async Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllRequest request)
        {
            try
            {
                PaginationUtil<Asset> pagination = new(request.QueryParams);
                BsonDocument[] pipeline = [
                    new("$match", pagination.PipelineFilter),
                    new("$sort", pagination.PipelineSort),
                    new("$skip", pagination.Skip),
                    new("$limit", pagination.Limit),
                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"id", new BsonDocument("$toString", "$_id")},
                        {"name", 1},
                        {"type", 1},
                        {"purchaseValue", 1},
                        {"currentValue", 1},
                        {"acquisitionDate", 1},
                        {"description", 1},
                        {"active", 1},
                        {"createdAt", 1}
                    })
                ];

                List<dynamic> response = await repository.GetAllAsync(pipeline);
                int totalCount = await repository.GetCountDocumentsAsync(pipeline);
                PaginationApi<List<dynamic>> result = new(response, totalCount, pagination.PageNumber, pagination.PageSize);

                return new(result, 200, "Bens listados com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
            }
        }

        public async Task<ResponseApi<List<dynamic>>> GetSelectAsync(GetAllRequest request)
        {
            try
            {
                PaginationUtil<Asset> pagination = new(request.QueryParams);
                BsonDocument[] pipeline = [
                    new("$match", pagination.PipelineFilter),
                    new("$sort", new BsonDocument("name", 1)),
                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"id", new BsonDocument("$toString", "$_id")},
                        {"name", 1},
                        {"currentValue", 1},
                        {"type", 1}
                    })
                ];

                List<dynamic> response = await repository.GetSelectAsync(pipeline);
                return new(response, 200, "Bens listados com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
            }
        }

        public async Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id)
        {
            try
            {
                BsonDocument[] pipeline = [
                    new("$match", new BsonDocument{
                        {"_id", new ObjectId(id)},
                        {"deleted", false}
                    }),
                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"id", new BsonDocument("$toString", "$_id")},
                        {"name", 1},
                        {"type", 1},
                        {"purchaseValue", 1},
                        {"currentValue", 1},
                        {"acquisitionDate", 1},
                        {"description", 1},
                        {"active", 1},
                        {"createdAt", 1}
                    })
                ];

                dynamic? entity = await repository.GetByIdAggregateAsync(pipeline);
                if (entity is null) return new(null, 404, "Bem não encontrado.");
                return new(entity, 200, "Bem encontrado.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }

        public async Task<ResponseApi<Asset?>> CreateAsync(CreateAssetRequest request)
        {
            try
            {
                Asset asset = ObjectMapper.Map<CreateAssetRequest, Asset>(request);
                Asset? response = await repository.CreateAsync(asset);
                if (response is null) return new(null, 400, "Falha ao cadastrar bem.");

                return new(response, 201, "Bem cadastrado com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
            }
        }

        public async Task<ResponseApi<Asset?>> UpdateAsync(UpdateAssetRequest request)
        {
            try
            {
                Asset? existed = await repository.GetByIdAsync(request.Id);
                if (existed is null) return new(null, 404, "Falha ao atualizar. Bem não encontrado.");

                existed.UpdatedAt = DateTime.UtcNow;
                existed.UpdatedBy = request.UpdatedBy;
                existed.Name = request.Name;
                existed.Type = request.Type;
                existed.PurchaseValue = request.PurchaseValue;
                existed.CurrentValue = request.CurrentValue;
                existed.AcquisitionDate = request.AcquisitionDate;
                existed.Description = request.Description;

                Asset? response = await repository.UpdateAsync(existed);
                if (response is null) return new(null, 400, "Falha ao atualizar bem.");

                return new(response, 200, "Bem atualizado com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
            }
        }

        public async Task<ResponseApi<Asset>> DeleteAsync(DeleteRequest request)
        {
            try
            {
                Asset? existed = await repository.GetByIdAsync(request.Id);
                if (existed is null) return new(null, 404, "Falha ao excluir. Bem não encontrado.");

                existed.Deleted = true;
                existed.DeletedAt = DateTime.UtcNow;
                existed.DeletedBy = request.DeletedBy;

                Asset response = await repository.DeleteAsync(existed);
                return new(response, 200, "Bem excluído com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
            }
        }

        public async Task<ResponseApi<List<AssetValuation>>> GetValuationsAsync(string assetId)
        {
            try
            {
                List<AssetValuation> valuations = await valuationRepository.GetByAssetIdAsync(assetId);
                return new(valuations, 200);
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado - {ex.Message}");
            }
        }

        public async Task<ResponseApi<AssetValuation?>> AddValuationAsync(string assetId, Requests.Asset.AddAssetValuationRequest request)
        {
            try
            {
                Asset? asset = await repository.GetByIdAsync(assetId);
                if (asset is null) return new(null, 404, "Bem não encontrado.");

                decimal previousValue = asset.CurrentValue;
                decimal newValue;
                decimal amount;

                if (request.NewValue.HasValue)
                {
                    newValue = request.NewValue.Value;
                    amount = Math.Abs(newValue - previousValue);
                    request.Type = newValue >= previousValue ? "appreciation" : "depreciation";
                }
                else if (request.Type == "appreciation")
                {
                    amount = request.Amount;
                    newValue = previousValue + amount;
                }
                else
                {
                    amount = request.Amount;
                    newValue = Math.Max(0, previousValue - amount);
                }

                AssetValuation valuation = new()
                {
                    AssetId = assetId,
                    Date = request.Date ?? DateTime.UtcNow,
                    Type = request.Type,
                    Amount = amount,
                    PreviousValue = previousValue,
                    CurrentValue = newValue,
                    Note = request.Note,
                    CreatedBy = request.CreatedBy
                };

                await valuationRepository.CreateAsync(valuation);

                asset.CurrentValue = newValue;
                asset.UpdatedAt = DateTime.UtcNow;
                asset.UpdatedBy = request.CreatedBy;
                await repository.UpdateAsync(asset);

                return new(valuation, 201, "Valorização/desvalorização registrada com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado - {ex.Message}");
            }
        }

        public async Task<ResponseApi<AssetValuation?>> UpdateValuationAsync(string assetId, string valuationId, Requests.Asset.UpdateAssetValuationRequest request)
        {
            try
            {
                Asset? asset = await repository.GetByIdAsync(assetId);
                if (asset is null) return new(null, 404, "Bem não encontrado.");

                AssetValuation? valuation = await valuationRepository.GetByIdAsync(valuationId);
                if (valuation is null) return new(null, 404, "Item de valorização não encontrado.");

                decimal amount = request.Amount;
                if (request.NewValue.HasValue)
                {
                    amount = Math.Abs(request.NewValue.Value - valuation.PreviousValue);
                    request.Type = request.NewValue.Value >= valuation.PreviousValue ? "appreciation" : "depreciation";
                }

                valuation.Type = request.Type;
                valuation.Amount = amount;
                valuation.Date = request.Date ?? valuation.Date;
                valuation.Note = request.Note;
                valuation.UpdatedAt = DateTime.UtcNow;
                valuation.UpdatedBy = request.UpdatedBy;

                await valuationRepository.UpdateAsync(valuation);
                await RecalculateAssetCurrentValueAsync(asset);

                return new(valuation, 200, "Item atualizado com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado - {ex.Message}");
            }
        }

        public async Task<ResponseApi<bool>> DeleteValuationAsync(string assetId, string valuationId, string userId)
        {
            try
            {
                Asset? asset = await repository.GetByIdAsync(assetId);
                if (asset is null) return new(false, 404, "Bem não encontrado.");

                AssetValuation? valuation = await valuationRepository.GetByIdAsync(valuationId);
                if (valuation is null) return new(false, 404, "Item de valorização não encontrado.");

                valuation.Deleted = true;
                valuation.DeletedAt = DateTime.UtcNow;
                valuation.DeletedBy = userId;

                await valuationRepository.DeleteAsync(valuation);
                await RecalculateAssetCurrentValueAsync(asset);

                return new(true, 200, "Item excluído com sucesso.");
            }
            catch (Exception ex)
            {
                return new(false, 500, $"Ocorreu um erro inesperado - {ex.Message}");
            }
        }

        private async Task RecalculateAssetCurrentValueAsync(Asset asset)
        {
            List<AssetValuation> valuations = await valuationRepository.GetByAssetIdAsync(asset.Id);
            valuations.Reverse();
            decimal current = asset.PurchaseValue > 0 ? asset.PurchaseValue : (valuations.FirstOrDefault()?.PreviousValue ?? 0);
            foreach (var v in valuations)
            {
                v.PreviousValue = current;
                current = v.Type == "appreciation" ? current + v.Amount : Math.Max(0, current - v.Amount);
                v.CurrentValue = current;
                await valuationRepository.UpdateAsync(v);
            }
            asset.CurrentValue = current;
            asset.UpdatedAt = DateTime.UtcNow;
            await repository.UpdateAsync(asset);
        }
    }
}
