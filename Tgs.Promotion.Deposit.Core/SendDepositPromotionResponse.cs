namespace Tgs.Promotion.Deposit.Core;

public record SendDepositPromotionResponse(
    int? PromotionBonusPlayerId,
    decimal DepositBonusAmount);