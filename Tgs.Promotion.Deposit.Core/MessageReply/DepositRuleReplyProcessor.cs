using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tgs.Promotion.Clients.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Promotion.Deposit.Core.MessageReply;

public class DepositRuleReplyProcessor(ILogger<DepositRuleReplyProcessor> logger)
    : IMessageProcessor<CampaignDetailRequest, CampaignDetailResponse>
{
    public ValueTask<CampaignDetailResponse> HandleAsync(
        string subject,
        CampaignDetailRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<DepositRewardSetting>(
                           data.Settings,
                           PromotionUtility.SerializerOptions) ??
                       throw new NullReferenceException("Deserialize DepositRewardSetting is null");

        logger.LogDebug("Deposit reward deposit setting : {settings}", settings);

        settings = settings with
        {
            BucketTypes = settings.BucketTypes?.Where(v => !string.IsNullOrWhiteSpace(v)).ToArray()
        };

        var response = new CampaignDetailResponse
        {
            CampaignDetail = JsonSerializer.Serialize(
                settings,
                PromotionUtility.SerializerOptions),
            BonusAmountLimit = settings.BonusAmountLimit,
            Percentage = settings.Percentage,
            TurnoverMultiple = settings.TurnoverMultiple
        };

        return new ValueTask<CampaignDetailResponse>(response);
    }
}