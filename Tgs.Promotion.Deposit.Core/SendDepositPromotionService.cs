using Microsoft.Extensions.Logging;
using Tgs.Service.PlayerBuckets;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Deposit.Core;

internal class SendDepositPromotionService : IGetService<SendDepositPromotion, SendDepositPromotionResponse>
{
	private readonly ILogger<SendDepositPromotionService> _logger;
	private readonly IPlayerBucketClient _playerBucketClient;
	private readonly IPromotionBonusRepository _promotionBonusRepository;
	private readonly IPromotionRepository _promotionRepository;

	public SendDepositPromotionService(
		IPromotionRepository promotionRepository,
		IPromotionBonusRepository promotionBonusRepository,
		IPlayerBucketClient playerBucketClient,
		ILogger<SendDepositPromotionService> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
		_promotionBonusRepository = promotionBonusRepository ?? throw new ArgumentNullException(nameof(promotionBonusRepository));
		_playerBucketClient = playerBucketClient ?? throw new ArgumentNullException(nameof(playerBucketClient));
	}

	public async ValueTask<SendDepositPromotionResponse> GetAsync(
		SendDepositPromotion command,
		CancellationToken cancellationToken = default)
	{
		var promotion = await _promotionRepository
			.GetPromotionWithCacheAsync(command.PromotionId)
			.ConfigureAwait(false);

		if (promotion == null)
		{
			_logger.LogError("promotion is not found. ({command.PromotionId})", command.PromotionId);

			return new(null, 0);
		}

		var (bonusAmount, bucketAmount) = CalculateAmount(promotion, command.DepositAmount);

		var (bonusId, promotionBonusPlayerId) = await _promotionBonusRepository.CreatePromotionBonusAsync(
			promotion,
			command.PlayerCode,
			bonusAmount,
			bucketAmount,
			cancellationToken);

		_ = await _playerBucketClient.CreateBucketAsync(
			command.PlayerCode,
			bucketAmount,
			Service.PlayerBuckets.BucketType.Promotion,
			promotionBonusPlayerId.ToString(),
			cancellationToken).ConfigureAwait(false);

		await _promotionBonusRepository.SendBonusAsync(
			promotion,
			bonusId,
			command.DefaultWalletId,
			bonusAmount,
			command.UserId,
			promotionBonusPlayerId.ToString(),
			command.Remark,
			cancellationToken);

		return new(promotionBonusPlayerId, bonusAmount);
	}

	private static (decimal bonusAmount, decimal bucketAmount) CalculateAmount(Promotion promotion, decimal depositAmount)
	{
		var depositBonusAmount = depositAmount;

		var bonusAmount = promotion.Percentage * depositAmount / 100M;

		bonusAmount = bonusAmount > promotion.BonusAmountLimit
			? promotion.BonusAmountLimit
			: bonusAmount;

		depositBonusAmount += bonusAmount;

		var bucketAmount = depositBonusAmount * promotion.TurnoverMultiple - depositAmount;

		return (bonusAmount, bucketAmount);
	}
}
