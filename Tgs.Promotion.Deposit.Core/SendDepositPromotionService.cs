using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tgs.PlayerBucket.Client;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Deposit.Core;

internal class SendDepositPromotionService : IGetService<SendDepositPromotion, SendDepositPromotionResponse>
{
	private readonly ILogger<SendDepositPromotionService> _logger;
	private readonly IPlayerBucketForTypesClient _playerBucketForTypesClient;
	private readonly IPromotionBonusRepository _promotionBonusRepository;
	private readonly IPromotionRepository _promotionRepository;
	private static readonly string[] _AllowDistributePromotionTypes = ["DailySecondDeposit", "DailyFirstDeposit"];

	public SendDepositPromotionService(
		IPromotionRepository promotionRepository,
		IPromotionBonusRepository promotionBonusRepository,
		IPlayerBucketForTypesClient playerBucketForTypesClient,
		ILogger<SendDepositPromotionService> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_playerBucketForTypesClient = playerBucketForTypesClient;
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
		_promotionBonusRepository = promotionBonusRepository ??
			throw new ArgumentNullException(nameof(promotionBonusRepository));
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

		if (!_AllowDistributePromotionTypes.Contains(promotion.PromotionType))
		{
			return new(null, 0);
		}

		var (bonusAmount, bucketAmount) = CalculateAmount(promotion, command.DepositAmount);

		var (bonusId, promotionBonusPlayerId) = await _promotionBonusRepository.CreatePromotionBonusAsync(
			promotion,
			command.PlayerCode,
			bonusAmount,
			bucketAmount,
			cancellationToken);

		if (!promotion.AutoSentPlayerBonus)
			return new SendDepositPromotionResponse(promotionBonusPlayerId, bonusAmount);

		var rule = JsonSerializer.Deserialize<DepositRule>(promotion.CampaignDetail,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

		await _playerBucketForTypesClient.CreateBucketForBucketTypesAsync(
			command.PlayerCode,
			bucketAmount,
			Service.PlayerBuckets.BucketType.Promotion,
			promotionBonusPlayerId.ToString(),
			"",
			rule.BucketTypes,
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

		return new SendDepositPromotionResponse(promotionBonusPlayerId, bonusAmount);
	}

	private static (decimal bonusAmount, decimal bucketAmount) CalculateAmount(Promotion promotion,
		decimal depositAmount)
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