using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace api_finances.src.Services;

public class PatrimonyService(
    IAssetRepository assetRepository,
    IDebtRepository debtRepository,
    IBankService bankService
) : IPatrimonyService
{
    public async Task<ResponseApi<dynamic>> GetSummaryAsync(string userId)
    {
        try
        {
            List<Asset> assets = await assetRepository.GetActiveByUserAsync(userId);
            List<Debt> debts = await debtRepository.GetActiveByUserAsync(userId);

            decimal totalBankBalance = 0;
            try
            {
                Dictionary<string, StringValues> query = new()
                {
                    ["pageSize"] = "100"
                };
                if (!string.IsNullOrEmpty(userId))
                {
                    query["createdBy"] = userId;
                }

                var banksResponse = await bankService.GetAllAsync(new(new QueryCollection(query)));
                List<dynamic>? banksList = banksResponse.Data?.Data;
                if (banksList != null)
                {
                    foreach (var item in banksList)
                    {
                        if (item is MongoDB.Bson.BsonDocument doc)
                        {
                            if (doc.Contains("balance") && !doc["balance"].IsBsonNull)
                            {
                                totalBankBalance += Convert.ToDecimal(doc["balance"].ToDouble());
                            }
                        }
                        else if (item is IDictionary<string, object> dict && dict.TryGetValue("balance", out var val) && val != null)
                        {
                            totalBankBalance += Convert.ToDecimal(val);
                        }
                        else
                        {
                            try
                            {
                                string json = System.Text.Json.JsonSerializer.Serialize(item);
                                using var jsonDoc = System.Text.Json.JsonDocument.Parse(json);
                                if (jsonDoc.RootElement.TryGetProperty("balance", out var prop))
                                {
                                    totalBankBalance += prop.GetDecimal();
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }

            decimal totalAssets = assets.Sum(x => x.CurrentValue);
            decimal totalDebts = debts.Sum(x => x.RemainingAmount);
            decimal netWorth = totalBankBalance + totalAssets - totalDebts;

            var result = new
            {
                totalBankBalance,
                totalAssets,
                totalDebts,
                netWorth,
                assetCount = assets.Count,
                debtCount = debts.Count
            };

            return new(result, 200);
        }
        catch (Exception ex)
        {
            return new(null, 500, $"Ocorreu um erro inesperado - {ex.Message}");
        }
    }
}
