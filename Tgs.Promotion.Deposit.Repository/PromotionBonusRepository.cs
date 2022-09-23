using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tgs.CashService.Client;
using Tgs.Promotion.Clients;
using Tgs.Promotion.Deposit.Core;
using Tgs.Promotion.Register.Repository.Entities;

namespace Tgs.Promotion.Deposit.Repository;

internal class PromotionBonusRepository : IPromotionBonusRepository
{
	private readonly ICashServiceClient _cashServiceClient;
	private readonly TgsContext _tgsContext;

	public PromotionBonusRepository(TgsContext tgsContext,
		ICashServiceClient cashServiceClient)
	{
		_tgsContext = tgsContext;
		_cashServiceClient = cashServiceClient;
	}

	public async ValueTask<(Guid, int)> CreatePromotionBonusAsync(Core.Promotion promotion, string playerCode,
		decimal bonusAmount,
		decimal bucketAmount, CancellationToken cancellationToken)
	{
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
			BrandId = promotion.BrandId,
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
					PlayerCode = playerCode,
					PromotionBonusId = bonusId,
					BonusAmount = bonusAmount,
					SentOn = promotion.AutoSentPlayerBonus ? DateTimeOffset.UtcNow.UtcDateTime : null,
					BucketAmountByManual = bucketAmount
				}
			}.ToHashSet()
		};

		await _tgsContext.PromotionBonuses.AddAsync(bonus, cancellationToken).ConfigureAwait(false);

		await _tgsContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

		return (bonusId,bonus.PromotionPlayers.First().Id);
	}

	public async ValueTask SendBonusAsync(
		Core.Promotion promotion, 
		string playerCode, 
		Guid bonusId,
		Guid defaultWalletId, 
		decimal bonusAmount,
		decimal bucketAmount,
		Guid? userId,
		string? externalId,
		string? description,
		CancellationToken cancellationToken)
	{
		var response = await _cashServiceClient.PromotionAsync(
			defaultWalletId,
			bonusAmount,
			description,
			externalId,
			userId,
			cancellationToken).ConfigureAwait(false);

		var transactionPromotion = new TransactionPromotion
		{
			TransactionId = response.TransactionId,
			PromotionId = promotion.Id,
			PromotionBonusId = bonusId,
			PromotionType = promotion.PromotionType
		};

		var playerBucket = new PlayerBucket
		{
			Id = Guid.NewGuid(),
			PlayerCode = playerCode,
			BucketAmount = bucketAmount,
			BucketType = (short)BucketType.Promotion,
			IsFull = false,
			CreatedOn = DateTimeOffset.UtcNow.UtcDateTime
		};
		await _tgsContext.PlayerBuckets.AddAsync(playerBucket, cancellationToken).ConfigureAwait(false);
		await _tgsContext.TransactionPromotions.AddAsync(transactionPromotion, cancellationToken).ConfigureAwait(false);
		await _tgsContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}
}