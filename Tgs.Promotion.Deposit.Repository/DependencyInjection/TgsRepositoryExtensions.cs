using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Register.Repository.Entities;

namespace Microsoft.Extensions.DependencyInjection;

public static class TgsRepositoryExtensions
{
    public static IServiceCollection AddTgsRepository(this IServiceCollection services, string connectionString)
        => services
            .AddDbContext<TgsContext>(options => options
                .UseNpgsql(connectionString));
   //         .AddTransient<IPromotionRepository, PromotionRepository>()
   //         .AddTransient<IDistributeRegisterEventRepository,DistributeRegisterEventRepository>()
   //         .AddTransient<IPlayerBucketRepository, PlayerBucketRepository>()
   //         .AddTransient<IPromotionJoinLockRepository, PromotionJoinLockRepository>()
   //         .AddTransient<IPromotionParticipantRepository, PromotionParticipantRepository>()
   //         .AddTransient<IPromotionDistributionRepository, PromotionDistributionRepository>()
			//.AddSerialNumberGenerator();

    //public static IServiceCollection AddRedis(
    //    this IServiceCollection service,
    //    Action<RedisOptions, IServiceProvider> configure)
    //    => service
    //        .AddOptions<RedisOptions>()
    //        .Configure(configure)
    //        .Services
    //        .AddSingleton<IRedisConnectionFactory, RedisConnectionFactory>();
}