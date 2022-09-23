using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tgs.Payment.Clients;
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
	private readonly IPaymentServiceClient _paymentServiceClient;
	public SendDepositRewardPromotionService(
		IPromotionRepository promotionRepository,
		IPromotionBonusRepository promotionBonusRepository,
		IPromotionServiceClient promotionServiceClient,
		IPaymentServiceClient paymentServiceClient,
		ILogger<SendDepositRewardPromotionService> logger)
	{
		_logger = logger;
		_promotionRepository = promotionRepository;
		_promotionBonusRepository = promotionBonusRepository;
		_promotionServiceClient = promotionServiceClient;
		_paymentServiceClient = paymentServiceClient;
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

		var depositForm = await _paymentServiceClient.GetDepositFormAsync(command.FormId, cancellationToken).ConfigureAwait(false);

		if (depositForm == null)
		{
			_logger.LogError($"depositForm is not found. ({command.FormId})");

			return new(null, 0);
		}

		var rule = JsonSerializer.Deserialize<DepositRewardRule>(promotion.CampaignDetail,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

		var depositAmount = depositForm.ActualAmount ?? depositForm.Amount;

		if (rule.DepositMinimumLimit > depositAmount)
		{
			_logger.LogError(
				$"DepositAmount {depositAmount} less then DepositMinimumLimit {rule.DepositMinimumLimit}. ({command.PromotionId})");

			return new(null, 0);
		}

		var bonusAmount = rule.Percentage * command.DepositAmount / 100M;

		bonusAmount = bonusAmount > rule.BonusAmountLimit
			? rule.BonusAmountLimit
			: bonusAmount;

		depositBonusAmount += bonusAmount;

		var totalAmount = await _promotionRepository.IncreaseToTotalAmountAsync(promotion.Id, bonusAmount);

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

		return new(null, bonusAmount);
	}
}