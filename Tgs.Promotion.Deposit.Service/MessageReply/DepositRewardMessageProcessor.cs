using System.Text;
using System.Text.Json;
using Tgs.MessageQueue;
using Tgs.Promotion.Deposit.Core;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Deposit.Service.MessageReply;

public class DepositRewardMessageProcessor : IMessageProcessor
{
	private readonly IGetService<SendDepositRewardPromotion, SendDepositRewardPromotionResponse> _getService;
	private readonly ILogger<DepositMessageProcessor> _logger;

	public DepositRewardMessageProcessor(ILogger<DepositMessageProcessor> logger,
		IGetService<SendDepositRewardPromotion, SendDepositRewardPromotionResponse> getService)
	{
		_getService = getService ?? throw new ArgumentNullException(nameof(getService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken)
	{
		var json = Encoding.UTF8.GetString(data.Span);
		var request = JsonSerializer.Deserialize<DepositMessageData>(json)
			?? throw new ArgumentNullException(nameof(data));

		_logger.LogInformation("deposit gift distribution request: {request}", request);

		var command =
			new SendDepositRewardPromotion(request.BrandId, request.PromotionId, request.PlayerCode, request.DefaultWalletId, request.DepositAmount, request.Remark, request.FormId, request.UserId);

		var response = await _getService.GetAsync(command, cancellationToken);

		var responseData = new DepositMessageDataResponse(response.PromotionBonusPlayerId, response.DepositBonusAmount);

		_logger.LogInformation("deposit reward distribution response: {responseData}", responseData);

		return (ReadOnlyMemory<byte>)Encoding.UTF8.GetBytes(JsonSerializer.Serialize(responseData));
	}
}
