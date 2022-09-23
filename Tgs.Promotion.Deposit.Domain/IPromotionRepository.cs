namespace Tgs.Promotion.Deposit.Domain;

public interface IPromotionRepository
{
	ValueTask<Promotion?> GetPromotionWithCacheAsync(Guid promotionId);
	ValueTask<long> IncreaseToTotalAmountAsync(Guid promotionId, long amount);
}