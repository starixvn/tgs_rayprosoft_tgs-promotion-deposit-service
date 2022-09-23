using Microsoft.Extensions.Logging;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Deposit.Core;

internal class SendDepositPromotionService : IGetService<SendDepositPromotion, SendDepositPromotionResponse>
{
	private readonly ILogger<SendDepositPromotionService> _logger;
	private readonly IPromotionBonusRepository _promotionBonusRepository;
	private readonly IPromotionRepository _promotionRepository;

	public SendDepositPromotionService(
		IPromotionRepository promotionRepository,
		IPromotionBonusRepository promotionBonusRepository,
		ILogger<SendDepositPromotionService> logger)
	{
		_logger = logger;
		_promotionRepository = promotionRepository;
		_promotionBonusRepository = promotionBonusRepository;
	}

	public async ValueTask<SendDepositPromotionResponse> GetAsync(
		SendDepositPromotion command,
		CancellationToken cancellationToken = default)
	{
		var depositBonusAmount = command.DepositAmount;

		var promotion = await _promotionRepository.GetPromotionWithCacheAsync(command.PromotionId)
			.ConfigureAwait(false);

		if (promotion == null)
		{
			_logger.LogError($"promotion is not found. ({command.PromotionId})");

			return new(null, 0);
		}

		var bonusAmount = promotion.Percentage * command.DepositAmount / 100M;

		bonusAmount = bonusAmount > promotion.BonusAmountLimit
			? promotion.BonusAmountLimit
			: bonusAmount;

		depositBonusAmount += bonusAmount;

		var bucketAmount = depositBonusAmount * promotion.TurnoverMultiple - command.DepositAmount;

		var (bonusId, promotionBonusPlayerId) = await _promotionBonusRepository.CreatePromotionBonusAsync(promotion,
			command.PlayerCode, bonusAmount,
			bucketAmount, cancellationToken);

		await _promotionBonusRepository.SendBonusAsync(
			promotion, 
			command.PlayerCode, 
			bonusId,
			command.DefaultWalletId, 
			bonusAmount,
			bucketAmount, 
			command.UserId,
			promotionBonusPlayerId.ToString(),
			command.Remark,
			cancellationToken);

		return new(promotionBonusPlayerId, bonusAmount);
	}
}