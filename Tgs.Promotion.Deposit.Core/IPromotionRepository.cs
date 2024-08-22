namespace Tgs.Promotion.Deposit.Core;

public interface IPromotionRepository
{
    ValueTask<Promotion?> GetPromotionWithCacheAsync(Guid promotionId);
    ValueTask<decimal> IncreaseToTotalAmountAsync(Guid promotionId, decimal amount);
}