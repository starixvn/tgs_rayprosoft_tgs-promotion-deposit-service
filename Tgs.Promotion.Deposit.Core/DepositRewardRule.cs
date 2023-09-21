namespace Tgs.Promotion.Deposit.Core;

public record DepositRewardRule(
    decimal BonusAmountLimit,
    decimal Percentage,
    decimal TurnoverMultiple,
    decimal DepositMinimumLimit,
    IEnumerable<string>? BucketTypes);