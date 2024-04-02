using System.Text.Json.Serialization;

namespace Tgs.Promotion.Deposit.Core;
public record PromotionRule(
	[property: JsonPropertyName("compaignDetail")] DepositRewardSetting CampaignDetail);
