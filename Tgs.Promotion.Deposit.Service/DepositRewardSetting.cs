namespace Tgs.Promotion.Deposit.Service;

public class DepositRewardSetting
{
    public decimal DepositMinimumLimit { get; set; }
    public decimal Percentage { get; set; }
    public decimal BonusAmountLimit { get; set; }
    public decimal TurnoverMultiple { get; set; }
	public string BucketType { get; set; } = string.Empty;
}