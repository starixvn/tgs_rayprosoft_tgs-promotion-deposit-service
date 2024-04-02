namespace Tgs.Promotion.Deposit.Core;
public record DepositRewardSetting(
	decimal Percentage,
	decimal BonusAmountLimit,
	decimal TurnoverMultiple,
	decimal DepositMinimumLimit,
	int[] VipLevels,
	string[]? BucketTypes);
