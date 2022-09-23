namespace Tgs.Promotion.Deposit.Core;

public interface IPromotionBonusRepository
{
	ValueTask<(Guid bonusId, int promotionBonusPlayerId)> CreatePromotionBonusAsync(
		Promotion promotion, 
		string playerCode, 
		decimal bonusAmount,
		decimal bucketAmount, 
		CancellationToken cancellationToken);

	ValueTask SendBonusAsync(
		Promotion promotion, 
		string playerCode,
		Guid bonusId, 
		Guid defaultWalletId, 
		decimal bonusAmount,
		decimal bucketAmount,
		Guid? userId,
		string? externalId,
		string? description, 
		CancellationToken cancellationToken);
}