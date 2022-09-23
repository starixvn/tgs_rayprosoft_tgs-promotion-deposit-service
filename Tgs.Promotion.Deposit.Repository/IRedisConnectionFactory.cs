using StackExchange.Redis;

namespace Tgs.Promotion.Deposit.Repository;

public interface IRedisConnectionFactory
{
    public IDatabase GetDataBase();
}