using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Promotion.Deposit.Core.ApplicationServices;
using Tgs.ServiceUtility;
using Valhalla.MessageQueue;

namespace Tgs.Promotion.Deposit.Core.MessageReply;

internal class DepositFormStatusChangedHandler(
    IPaymentServiceClient paymentServiceClient,
    ICommandService<DistributeDepositRewardBonusCommand> distributeService)
    : IMessageHandler<FormEventEx>
{
    public async ValueTask HandleAsync(string subject, FormEventEx data, IEnumerable<MessageHeaderValue>? headerValues, CancellationToken cancellationToken = default)
    {
        var previousStatus = Enum.Parse<PaymentFlowState>(data.Status);

        var depositForm = await paymentServiceClient.GetDepositFormAsync(
            data.FormId,
            cancellationToken).ConfigureAwait(false);

        if (depositForm?.PromotionId is null)
            return;

        if (previousStatus != depositForm.Status
            && depositForm.Status == PaymentFlowState.Approved)
        {
            var command = new DistributeDepositRewardBonusCommand(depositForm);

            await distributeService.ExecuteAsync(
                command,
                cancellationToken).ConfigureAwait(false);
        }
    }
}
