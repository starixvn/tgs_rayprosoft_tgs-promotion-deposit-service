namespace Tgs.Promotion.Deposit.Core;

public record SendDepositRewardPromotionResponse(
    int? PromotionBonusPlayerId,
    decimal DepositBonusAmount);