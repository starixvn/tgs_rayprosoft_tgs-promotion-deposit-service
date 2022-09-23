using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Deposit.Core;

internal class
	SendDepositRewardPromotionService : IGetService<SendDepositRewardPromotion, SendDepositRewardPromotionResponse>
{
	private readonly ILogger<SendDepositRewardPromotionService> _logger;
	private readonly IPromotionBonusRepository _promotionBonusRepository;
	private readonly IPromotionRepository _promotionRepository;
	private readonly IPromotionServiceClient _promotionServiceClient;

	public SendDepositRewardPromotionService(
		IPromotionRepository promotionRepository,
		IPromotionBonusRepository promotionBonusRepository,
		IPromotionServiceClient promotionServiceClient,
		ILogger<SendDepositRewardPromotionService> logger)
	{
		_logger = logger;
		_promotionRepository = promotionRepository;
		_promotionBonusRepository = promotionBonusRepository;
		_promotionServiceClient = promotionServiceClient;
	}

	public async ValueTask<SendDepositRewardPromotionResponse> GetAsync(
		SendDepositRewardPromotion command,
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

		var rule = JsonSerializer.Deserialize<DepositRewardRule>(promotion.CampaignDetail,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

		if (rule.DepositMinimumLimit > command.DepositAmount)
		{
			_logger.LogError(
				$"DepositAmount {command.DepositAmount} less then DepositMinimumLimit {rule.DepositMinimumLimit}. ({command.PromotionId})");

			return new(null, 0);
		}

		var bonusAmount = rule.Percentage * command.DepositAmount / 100M;

		bonusAmount = bonusAmount > rule.BonusAmountLimit
			? rule.BonusAmountLimit
			: bonusAmount;

		depositBonusAmount += bonusAmount;

		var totalAmount = await _promotionRepository.IncreaseToTotalAmountAsync(promotion.Id, (long)bonusAmount);

		if (promotion.Unlimited == 1 ||
		    totalAmount - bonusAmount <= promotion.TotalAmountLimit)
		{
			var bucketAmount = depositBonusAmount * rule.TurnoverMultiple;

			var (bonusId, promotionBonusPlayerId) = await _promotionBonusRepository.CreatePromotionBonusAsync(promotion,
				command.PlayerCode, bonusAmount,
				bucketAmount, cancellationToken);

			if (promotion.AutoSentPlayerBonus)
			{
				_logger.LogInformation("total amount record: {JsonSerializer.Serialize(totalAmount)}",
					JsonSerializer.Serialize(promotion.AmountUsed + bonusAmount));

				await _promotionServiceClient.UpdatePromotionUsedAmountAsync(
					promotion.Id,
					totalAmount,
					null, cancellationToken).ConfigureAwait(false);

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
			}

			return new(promotionBonusPlayerId, bonusAmount);
		}

		return new(-1, bonusAmount);
	}
}