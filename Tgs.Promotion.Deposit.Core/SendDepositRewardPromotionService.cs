using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tgs.Payment.Clients;
using Tgs.PlayerBucket.Client;
using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Deposit.Core;

internal class
    SendDepositRewardPromotionService : IGetService<SendDepositRewardPromotion, SendDepositRewardPromotionResponse>
{
    private readonly ILogger<SendDepositRewardPromotionService> _logger;
    private readonly IPaymentServiceClient _paymentServiceClient;
    private readonly IPlayerBucketForTypesClient _playerBucketForTypesClient;
    private readonly IPromotionBonusRepository _promotionBonusRepository;
    private readonly IPromotionRepository _promotionRepository;
    private readonly IPromotionServiceClient _promotionServiceClient;

    public SendDepositRewardPromotionService(
        IPromotionRepository promotionRepository,
        IPromotionBonusRepository promotionBonusRepository,
        IPromotionServiceClient promotionServiceClient,
        IPaymentServiceClient paymentServiceClient,
        IPlayerBucketForTypesClient playerBucketForTypesClient,
        ILogger<SendDepositRewardPromotionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _playerBucketForTypesClient = playerBucketForTypesClient;
        _promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
        _promotionBonusRepository = promotionBonusRepository ??
                                    throw new ArgumentNullException(nameof(promotionBonusRepository));
        _promotionServiceClient =
            promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
        _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
    }

    public async ValueTask<SendDepositRewardPromotionResponse> GetAsync(
        SendDepositRewardPromotion command,
        CancellationToken cancellationToken = default)
    {
        var promotion = await _promotionRepository.GetPromotionWithCacheAsync(command.PromotionId)
            .ConfigureAwait(false);

        if (promotion == null)
        {
            _logger.LogError("promotion is not found. ({command.PromotionId})", command.PromotionId);

            return new SendDepositRewardPromotionResponse(null, 0);
        }

        var depositForm = await _paymentServiceClient.GetDepositFormAsync(command.FormId, cancellationToken)
            .ConfigureAwait(false);

        if (depositForm == null)
        {
            _logger.LogError("depositForm is not found. ({command.FormId})", command.FormId);

            return new SendDepositRewardPromotionResponse(null, 0);
        }

        var rule = JsonSerializer.Deserialize<DepositRewardRule>(promotion.CampaignDetail,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        var depositAmount = depositForm.ActualAmount ?? depositForm.Amount;

        if (rule.DepositMinimumLimit > depositAmount)
        {
            _logger.LogError(
                "DepositAmount {depositAmount} less then DepositMinimumLimit {rule.DepositMinimumLimit}. ({command.PromotionId})",
                depositAmount,
                rule.DepositMinimumLimit,
                command.PromotionId);

            return new SendDepositRewardPromotionResponse(null, 0);
        }

        var (bonusAmount, bucketAmount) = CalculateAmount(rule, command.DepositAmount);

        var totalAmount = await _promotionRepository.IncreaseToTotalAmountAsync(promotion.Id, bonusAmount);

        if (promotion.Unlimited == 1 ||
            totalAmount - bonusAmount <= promotion.TotalAmountLimit)
        {
            var (bonusId, promotionBonusPlayerId) = await _promotionBonusRepository.CreatePromotionBonusAsync(promotion,
                command.PlayerCode,
                bonusAmount,
                bucketAmount,
                cancellationToken);

            if (promotion.AutoSentPlayerBonus)
            {
                _logger.LogInformation(
                    "total amount record: {JsonSerializer.Serialize(totalAmount)}",
                    promotion.AmountUsed + bonusAmount);

                await _playerBucketForTypesClient.CreateBucketForBucketTypesAsync(
                        command.PlayerCode,
                        bucketAmount,
                        Service.PlayerBuckets.BucketType.Promotion,
                        promotionBonusPlayerId.ToString(),
                        "",
                        rule.BucketTypes,
                        cancellationToken)
                    .ConfigureAwait(false);

                await _promotionBonusRepository.SendBonusAsync(
                    promotion,
                    bonusId,
                    command.DefaultWalletId,
                    bonusAmount,
                    command.UserId,
                    promotionBonusPlayerId.ToString(),
                    command.Remark,
                    cancellationToken);
            }

            await _promotionServiceClient.UpdatePromotionUsedAmountAsync(
                    promotion.Id,
                    totalAmount,
                    null,
                    cancellationToken)
                .ConfigureAwait(false);

            return new SendDepositRewardPromotionResponse(promotionBonusPlayerId, bonusAmount);
        }

        return new SendDepositRewardPromotionResponse(null, bonusAmount);
    }

    private static (decimal bonusAmount, decimal bucketAmount) CalculateAmount(DepositRewardRule rule,
        decimal depositAmount)
    {
        var depositBonusAmount = depositAmount;

        var bonusAmount = rule.Percentage * depositAmount / 100M;

        bonusAmount = bonusAmount > rule.BonusAmountLimit
            ? rule.BonusAmountLimit
            : bonusAmount;

        depositBonusAmount += bonusAmount;

        var bucketAmount = depositBonusAmount * rule.TurnoverMultiple - depositAmount;

        return (bonusAmount, bucketAmount);
    }
}