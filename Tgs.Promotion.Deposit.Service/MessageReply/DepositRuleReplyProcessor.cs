using System.Text.Json;
using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients.Protos;

namespace Tgs.Promotion.Deposit.Service.MessageReply;

public class DepositRuleReplyProcessor : IMessageProcessor
{
	private readonly ILogger<DepositRuleReplyProcessor> _logger;

	public DepositRuleReplyProcessor(ILogger<DepositRuleReplyProcessor> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken)
	{
		var request = CampaignDetailRequest.Parser.ParseFrom(data.Span);

		var settings = JsonSerializer.Deserialize<DepositRewardSetting>(
			request.Settings,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
			?? throw new NullReferenceException("Deserialize DepositRewardSetting is null");

		_logger.LogInformation("Account deposit setting : {settings}", settings);

		var response = new CampaignDetailResponse
		{
			CampaignDetail = JsonSerializer.Serialize(
				settings,
				new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
					WriteIndented = true
				}),
			BonusAmountLimit = settings.BonusAmountLimit,
			Percentage = settings.Percentage,
			TurnoverMultiple = settings.TurnoverMultiple
		};

		return ValueTask.FromResult((ReadOnlyMemory<byte>)response.ToByteArray());
	}
}
