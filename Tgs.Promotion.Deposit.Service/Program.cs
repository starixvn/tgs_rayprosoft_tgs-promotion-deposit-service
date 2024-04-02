using FluentdForward.OpenTelemetry.MessagePack;
using Microsoft.EntityFrameworkCore;
using NATS.Client.Core;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using Tgs.Promotion.Deposit.Core;
using Tgs.Promotion.Deposit.Core.MessageReply;
using Tgs.Promotion.Deposit.Repository.DependencyInjection;
using Tgs.Promotion.Deposit.Service.NatsSerializers;

var builder = WebApplication.CreateBuilder(args);

builder.Logging
	.AddOpenTelemetry(options =>
	{
		options.IncludeFormattedMessage = true;
		_ = options.AddFluentdForwardExporter(fluentOptions =>
		{
			var nodeIp = builder.Configuration.GetValue<string>("NODE_IP")!;
			fluentOptions.Host = nodeIp;
			fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME")!;
			fluentOptions.UseMessagePack(LogRecordFormatterResolver.GetResolverInstanceWithExtendInfo(
				"MachineName",
				() => Environment.MachineName));
		});
	});

builder.Services
	.AddKeyedSingleton<IConnectionMultiplexer>(
		"Redis",
		(sp, _) => ConnectionMultiplexer.Connect(
			builder.Configuration.GetConnectionString("Redis")!))
	.AddKeyedSingleton(
		"Redis",
		(sp, _) => sp.GetRequiredKeyedService<IConnectionMultiplexer>("Redis").GetDatabase())
	.AddPromotionDepositCore()
	.AddDepositRewardRepositories(
		dbBuilder => dbBuilder
			.UseNpgsql("name=Tgs"),
		sp => sp.GetRequiredKeyedService<IDatabase>("Redis"))
	.AddPromotionClientServices((opts, _) => opts.Url =
		builder.Configuration.GetValue<Uri>("DepositReward:PromotionServiceBaseUrl")!)
	.AddPaymentServiceClient(httpBuilder => httpBuilder
		.ConfigureHttpClient(httpClient => httpClient.BaseAddress =
			builder.Configuration.GetValue<Uri>("DepositReward:PaymentServiceBaseUrl")!))
	.AddCalculateClient(httpBuilder => httpBuilder
		.ConfigureHttpClient(httpClient => httpClient.BaseAddress =
			builder.Configuration.GetValue<Uri>("DepositReward:CalculateServiceBaseUrl")!))
	.AddMemoryCache();

var instrumentationExtensionFilters = new[] { "html", "ico", "png", "jpg" };
_ = builder.Services
	.AddOpenTelemetry()
	.WithTracing(
		tracingBuilder => tracingBuilder
			.SetResourceBuilder(ResourceBuilder.CreateDefault()
				.AddEnvironmentVariableDetector())
			.AddSource("Tgs.*")
			.AddSource("TGS.*")
			.AddSource("Valhalla.*")
			.AddAspNetCoreInstrumentation(options => options.Filter =
				httpContext => !httpContext.Request.Path.HasValue
							   || !httpContext.Request.Path.StartsWithSegments("/swagger")
							   && !httpContext.Request.Path.StartsWithSegments("/healthz")
							   && !httpContext.Request.Path.StartsWithSegments("/metrics")
							   && !httpContext.Request.Path.StartsWithSegments("/profiler/")
							   && !instrumentationExtensionFilters.Any(
								   ext => httpContext.Request.Path.Value.EndsWith($".{ext}")))
			.AddProfileViewExporter()
			.AddOtlpExporter())
	.WithMetrics(meterBuilder => meterBuilder
		.SetResourceBuilder(ResourceBuilder.CreateDefault()
			.AddEnvironmentVariableDetector())
		.AddAspNetCoreInstrumentation()
		.AddRuntimeInstrumentation()
		.AddOtlpExporter())
	.Services
	.AddHealthChecks()
	.Services
	.AddProfileViewer();

builder.Services
	.AddKeyedSingleton(
		"Nats",
		(sp, _) => new NatsConnection(NatsOpts.Default with
		{
			Url = builder.Configuration.GetConnectionString("Nats")!,
			SerializerRegistry = AccountFirstDepositSerizerRegistration.Default
		}))
	.AddMessageQueue()
	.AddNatsMessageQueue(natsConfiguration => natsConfiguration
		.ConfigureResolveConnection(sp => sp.GetRequiredKeyedService<NatsConnection>("Nats"))
		.AddProcessor<DepositRuleReplyProcessor>("tgs.promotion.depositreward.setting",
			"tgs.promotion.depositreward.setting")
		.AddHandler(
			ProcessorTypes.DepositFormStatusChangedHandlerType,
			"tgs.payment.deposit.status_ex",
			"tgs.promotion.daily-first-deposit.deposit"))
	.AddNatsGlobPatternExchange("*");

var app = builder.Build();
{
	app.MapHealthChecks("/healthz");
	app.MapProfileViewer();
}

app.Run();
