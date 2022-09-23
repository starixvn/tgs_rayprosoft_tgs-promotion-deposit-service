using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Tgs.Promotion.Deposit.Repository;

internal class RedisConnectionFactory : IRedisConnectionFactory, IDisposable
{
    private readonly ConnectionMultiplexer _connectionMultiplexer;
    private bool _disposedValue;

    public RedisConnectionFactory(
        IOptions<RedisOptions> optionsAccessor)
    {
        if (optionsAccessor is null)
        {
            throw new ArgumentNullException(nameof(optionsAccessor));
        }

        var options = optionsAccessor.Value;

        var connectionOptions = ConfigurationOptions.Parse(options.ConnectionString);

        _connectionMultiplexer = ConnectionMultiplexer.Connect(connectionOptions);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public IDatabase GetDataBase()
        => _connectionMultiplexer.GetDatabase();

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _connectionMultiplexer?.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
    }
}