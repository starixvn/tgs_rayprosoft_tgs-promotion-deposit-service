using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

public static class PromotionRegisterCoreExtensions
{
	public static IServiceCollection AddPromotionDepositCore(this IServiceCollection services)
	{
		_ = services.RegisterAllServiceUtilityTypes(
				Assembly.GetExecutingAssembly(),
				ServiceLifetime.Transient);

		return services;
	}
}
