namespace Tgs.Promotion.Deposit.Core;

public class Promotion
{
	public Guid Id { get; set; }
	public string Name { get; set; } = null!;
	public DateTime From { get; set; }
	public DateTime To { get; set; }
	public short Status { get; set; }
	public bool AutoSentPlayerBonus { get; set; }
	public Guid BrandId { get; set; }
	public string PromotionType { get; set; } = null!;
	public string? SettlementIntervalType { get; set; }
	public decimal TotalAmountLimit { get; set; }
	public decimal AmountUsed { get; set; }
	public short Unlimited { get; set; }
	public short? GameType { get; set; }
	public decimal Percentage { get; set; }
	public decimal BonusAmountLimit { get; set; }
	public decimal TurnoverMultiple { get; set; }
	public Guid? VipLevelId { get; set; }
	public string CampaignDetail { get; set; } = null!;
}