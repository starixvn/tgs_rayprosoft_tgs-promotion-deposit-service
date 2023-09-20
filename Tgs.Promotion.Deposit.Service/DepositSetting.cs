using Tgs.Promotion.Deposit.Core;

namespace Tgs.Promotion.Deposit.Service;

public class DepositSetting
{
	public decimal Percentage { get; set; }
    public decimal BonusAmountLimit { get; set; }
    public decimal TurnoverMultiple { get; set; }
    public GameType? GameType { get; set; }
	public string BucketType { get; set; } = string.Empty;
}