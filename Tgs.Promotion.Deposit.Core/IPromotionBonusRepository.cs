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
        Guid bonusId,
        Guid defaultWalletId,
        decimal bonusAmount,
        Guid? userId,
        string? externalId,
        string? description,
        CancellationToken cancellationToken);
}