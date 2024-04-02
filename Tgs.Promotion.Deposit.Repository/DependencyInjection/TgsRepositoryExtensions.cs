using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Tgs.Promotion.Deposit.Core;
using Tgs.Promotion.Deposit.Repository.Entities;

namespace Tgs.Promotion.Deposit.Repository.DependencyInjection;

public static class TgsRepositoryExtensions
{
    public static IServiceCollection AddTgsRepository(this IServiceCollection services, string connectionString)
    {
        return services
            .AddDbContext<TgsContext>(options => options
                .UseNpgsql(connectionString))
            .AddScoped<ITgsRepository, TgsRepository>();
    }

    public static IServiceCollection AddDepositRewardRepositories(
	    this IServiceCollection services,
	    Action<DbContextOptionsBuilder> dbConfigure,
	    Func<IServiceProvider, IDatabase> resolveRedisDatabase)
	    => services
		    .AddDbContext<TgsContext>(dbConfigure)
		    .AddScoped<ITgsRepository, TgsRepository>();

}