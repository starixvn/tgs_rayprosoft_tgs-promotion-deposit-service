using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tgs.CashService.Client;
using Tgs.Promotion.Register.Repository.Entities;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Deposit.Core;

internal class SendDepositRewardPromotionService : IGetService<SendDepositRewardPromotion, SendDepositRewardPromotionResponse>
{
	private readonly ILogger<SendDepositRewardPromotionService> _logger;
    private readonly TgsContext _tgsContext;
    private readonly ICashServiceClient _cashServiceClient;

	public SendDepositRewardPromotionService(
        TgsContext tgsContext,
        ICashServiceClient cashServiceClient,
		ILogger<SendDepositRewardPromotionService> logger)
	{
		_logger = logger;
        _tgsContext = tgsContext;
        _cashServiceClient = cashServiceClient;
    }

    public async ValueTask<SendDepositRewardPromotionResponse> GetAsync(
        SendDepositRewardPromotion command,
        CancellationToken cancellationToken = default)
    {
        var depositBonusAmount = command.DepositAmount;

        var promotion = await _tgsContext.Promotions.FirstOrDefaultAsync(x => x.Id == command.PromotionId, cancellationToken);

        if (promotion == null)
        {
            _logger.LogError($"promotion is not found. ({command.PromotionId})");

            return new SendDepositRewardPromotionResponse(null,0);
        }

        var rule = JsonSerializer.Deserialize<DepositRewardRule>(promotion.CampaignDetail)!;

        if (rule.DepositMinimumAmount > command.DepositAmount)
        {
            _logger.LogError($"DepositAmount {command.DepositAmount} less then DepositMinimumAmount {rule.DepositMinimumAmount}. ({command.PromotionId})");

            return new SendDepositRewardPromotionResponse(null,0);
        }

        var bonusAmount = rule.Percentage * command.DepositAmount / 100M;

        bonusAmount = bonusAmount > rule.BonusAmountLimit
            ? rule.BonusAmountLimit
            : bonusAmount;

        depositBonusAmount += bonusAmount;

        var now = DateTimeOffset.Now.ToOffset(new(8, 0, 0));

        var reportDate = new DateTimeOffset(now.Year,
            now.Month,
            now.Day,
            0,
            0,
            0,
            now.Offset);

        var bonusId = Guid.NewGuid();

        var bonus = new PromotionBonus
        {
            Id = bonusId,
            BrandId = command.BrandId,
            PromotionId = promotion.Id,
            From = reportDate.UtcDateTime,
            To = reportDate.UtcDateTime,
            PromotionType = promotion.PromotionType,
            TotalAmountLimit = promotion.TotalAmountLimit,
            GameType = promotion.GameType,
            Percentage = promotion.Percentage,
            BonusAmountLimit = promotion.BonusAmountLimit,
            TurnoverMultiple = promotion.TurnoverMultiple,
            CreatedOn = DateTimeOffset.UtcNow.UtcDateTime,
            PromotionPlayers = new[]
            {
                new PromotionPlayer
                {
                    PlayerCode = command.PlayerCode,
                    PromotionBonusId = bonusId,
                    BonusAmount = bonusAmount,
                    SentOn = promotion.AutoSentPlayerBonus ? DateTimeOffset.UtcNow.UtcDateTime : null,
					BucketAmountByManual = depositBonusAmount * rule.TurnoverMultiple - command.DepositAmount
                }
            }.ToHashSet()
        };

        await _tgsContext.PromotionBonuses.AddAsync(bonus,cancellationToken).ConfigureAwait(false);

        if (promotion.AutoSentPlayerBonus)
        {
	        var playerBucket = new PlayerBucket
	        {
		        Id = Guid.NewGuid(),
		        PlayerCode = command.PlayerCode,
		        BucketAmount = depositBonusAmount * rule.TurnoverMultiple - command.DepositAmount,
		        BucketType = (short)BucketType.Promotion,
		        IsFull = false,
		        CreatedOn = DateTimeOffset.UtcNow.UtcDateTime
	        };
	        await _tgsContext.PlayerBuckets.AddAsync(playerBucket,cancellationToken).ConfigureAwait(false);
        }

        await _tgsContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var response = await _cashServiceClient.PromotionAsync(
            command.DefaultWalletId,
            bonusAmount,
            null,
            null,
            null,
            cancellationToken).ConfigureAwait(false);

        var transactionPromotion = new TransactionPromotion
        {
            TransactionId = response.TransactionId,
            PromotionId = promotion.Id,
            PromotionBonusId = bonusId,
            PromotionType = promotion.PromotionType
        };

        await _tgsContext.TransactionPromotions.AddAsync(transactionPromotion,cancellationToken).ConfigureAwait(false);
        await _tgsContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);


        return new SendDepositRewardPromotionResponse(bonus.PromotionPlayers.First().Id, bonusAmount);
    }
}