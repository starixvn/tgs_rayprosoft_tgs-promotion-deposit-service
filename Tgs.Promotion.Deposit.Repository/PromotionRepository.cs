using Microsoft.Extensions.Caching.Memory;
using Tgs.Promotion.Deposit.Core;
using Tgs.Promotion.Register.Repository.Entities;

namespace Tgs.Promotion.Deposit.Repository;

internal class PromotionRepository : IPromotionRepository
{
    private readonly TgsContext _tgsContext;
    private readonly IMemoryCache _memoryCache;
    private readonly IRedisConnectionFactory _redisConnectionFactory;
    private const string TotalUsedAmountKey = "promotion:deposit:totalusedamount:{0}";

	public PromotionRepository(TgsContext tgsContext, IMemoryCache memoryCache, IRedisConnectionFactory redisConnectionFactory)
    {
        _tgsContext = tgsContext;
        _memoryCache = memoryCache;
		_redisConnectionFactory = redisConnectionFactory;
    }

    public async ValueTask<Core.Promotion?> GetPromotionWithCacheAsync(Guid promotionId)
    {
		return await _memoryCache.GetOrCreateAsync($"promotion:{promotionId}", async cacheEntry =>
		{
			cacheEntry.SetSize(1);
			cacheEntry.SetSlidingExpiration(TimeSpan.FromHours(1));
		    cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);

		    var promotion = await _tgsContext.Promotions.FindAsync(promotionId);

		    if (promotion is null)
		    {
			    return null;
		    }

		    return new Core.Promotion
		    {
			    Id = promotion.Id,
			    Name = promotion.Name,
			    From = promotion.From,
			    To = promotion.To,
			    Status = promotion.Status,
			    AutoSentPlayerBonus = promotion.AutoSentPlayerBonus,
			    BrandId = promotion.BrandId,
			    PromotionType = promotion.PromotionType,
			    SettlementIntervalType = promotion.SettlementIntervalType,
			    TotalAmountLimit = promotion.TotalAmountLimit,
			    AmountUsed = promotion.AmountUsed,
			    Unlimited = promotion.Unlimited,
			    GameType = promotion.GameType,
			    Percentage = promotion.Percentage,
			    BonusAmountLimit = promotion.BonusAmountLimit,
			    TurnoverMultiple = promotion.TurnoverMultiple,
			    VipLevelId = promotion.VipLevelId,
			    CampaignDetail = promotion.CampaignDetail
		    };
	    });
    }

    public async ValueTask<long> IncreaseToTotalAmountAsync(Guid promotionId,long amount)
    {
        var key = string.Format(TotalUsedAmountKey, promotionId);
        var db = _redisConnectionFactory.GetDataBase();
        var totalAmount =  await db.StringIncrementAsync(key, amount);

        return totalAmount;
    }
}