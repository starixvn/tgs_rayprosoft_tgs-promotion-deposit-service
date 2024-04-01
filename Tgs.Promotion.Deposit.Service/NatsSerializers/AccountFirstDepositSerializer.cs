using NATS.Client.Core;

namespace Tgs.Promotion.Deposit.Service.NatsSerializers;

public static class AccountFirstDepositSerializer<T>
{
	public static readonly INatsSerializer<T> Default = ProtobufSerializer<T>.Default;
}
