using System.Buffers;
using Google.Protobuf;
using NATS.Client.Core;

namespace Tgs.Promotion.Deposit.Service.NatsSerializers;

public class ProtobufSerializer<T>(INatsSerializer<T>? next)
	: INatsSerializer<T>
{
	public static readonly ProtobufSerializer<T> Default = new(NatsRawSerializer<T>.Default);

	public void Serialize(IBufferWriter<byte> bufferWriter, T value)
	{
		if (value is IMessage message)
		{
			message.WriteTo(bufferWriter);
			return;
		}

		if (next == null)
			throw new NatsException($"Can't serialize {typeof(T)}");

		next.Serialize(bufferWriter, value);
	}

	public T? Deserialize(in ReadOnlySequence<byte> buffer)
	{
		if (typeof(IMessage).IsAssignableFrom(typeof(T)))
		{
			var instance = Activator.CreateInstance(typeof(T))!;
			((IMessage)instance).MergeFrom(buffer);

			return (T)instance;
		}

		return next == null
			? throw new NatsException($"Can't deserialize {typeof(T)}")
			: next.Deserialize(buffer);
	}
}
