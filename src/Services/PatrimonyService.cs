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

            Dictionary<string, StringValues> query = new()
            {
                ["createdBy"] = userId,
                ["pageSize"] = "100"
            };

            var banksResponse = await bankService.GetAllAsync(new(new QueryCollection(query)));
            decimal totalBankBalance = 0;
            List<dynamic>? banksList = banksResponse.Data?.Data;
            if (banksList != null)
            {
                foreach (var item in banksList)
                {
                    var dict = item as IDictionary<string, object>;
                    if (dict != null && dict.TryGetValue("balance", out var val) && val != null)
                    {
                        totalBankBalance += Convert.ToDecimal(val);
                    }
                }
            }

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
