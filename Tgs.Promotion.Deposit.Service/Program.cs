using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.Promotion.Deposit.Service.MessageReply;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("/config/appsettings.json", true)
    .AddEnvironmentVariables();

builder.Services
    .AddMessageQueue()
    .AddNatsMessageQueue(natsConfig =>
    {
        natsConfig
            .ConfigQueueOptions((options, sp) =>
            {
                var config = sp.GetRequiredService<IConfiguration>();

                options.Url = config.GetConnectionString("Nats");
            })
            .AddProcessor<DepositRuleReplyProcessor>("tgs.promotion.depositreward.setting", "tgs.promotion.depositreward.setting")
            .AddProcessor<DepositRewardMessageProcessor>("tgs.promotion.depositreward.distribute", "tgs.promotion.depositreward.distribute")
            .AddProcessor<DepositMessageProcessor>("tgs.promotion.firstdeposit.distribute", "tgs.promotion.firstdeposit.distribute")
            .AddProcessor<DepositMessageProcessor>("tgs.promotion.dailyfirstdeposit.distribute", "tgs.promotion.dailyfirstdeposit.distribute")
            .AddProcessor<DepositMessageProcessor>("tgs.promotion.dailyseconddeposit.distribute", "tgs.promotion.dailyseconddeposit.distribute");
    })
    .AddNatsGlobPatternExchange("*");

// Add services to the container.
builder.Services.AddTgsRepository(builder.Configuration.GetConnectionString("Tgs"))
	.AddRedis((redisOptions, _)
		=> redisOptions.ConnectionString = builder.Configuration.GetConnectionString("Redis"))
    .AddPromotionDepositCore()
	.AddPaymentServiceClient((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();

		options.PaymentServiceBaseUrl = config.GetValue<Uri>("Promotion:PaymentServiceUrl");
	})
    .AddCashServiceClient((options, sp) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();

        options.BaseUrl = config.GetValue<Uri>("Promotion:CashBaseUrl");
    })
    .AddPromotionClientServices((options, sp) =>
    {
	    var config = sp.GetRequiredService<IConfiguration>();

	    options.Url = config.GetValue<Uri>("Promotion:Url");
    })
	.AddMemoryCache();

builder.Logging
    .AddOpenTelemetry(options =>
    {
        options.IncludeFormattedMessage = true;
        options.ParseStateValues = true;
        _ = options.AddFluentdForwardExporter(fluentOptions =>
        {
            var nodeIp = builder.Configuration.GetValue<string>("NODE_IP");
            fluentOptions.Host = nodeIp;
            fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME");
            fluentOptions.UseMessagePack();
        });
    });

builder.Services
    .AddOptions<OtlpExporterOptions>()
    .Configure<IConfiguration>((options, conifg) =>
    {
        var nodeIp = conifg.GetValue<string>("NODE_IP");

        if (!string.IsNullOrWhiteSpace(nodeIp))
            options.Endpoint = new Uri($"http://{nodeIp}:4317");

        conifg.GetSection("Opentelemetry:Tracing").Bind(options);
    });

builder.Services
    .AddOpenTelemetryTracing(builder => builder
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddEnvironmentVariableDetector())
        .AddSource("Tgs.*")
        .AddSource("TGS.*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation(options =>
        {
            options.SetDbStatementForText = true;
        })
        .AddProfileViewExporter()
        .AddOtlpExporter())
    .Configure<AspNetCoreInstrumentationOptions>(options =>
    {
        options.Filter = httpContext =>
        {
            return !httpContext.Request.Path.HasValue
                || !httpContext.Request.Path.StartsWithSegments("/swagger")
                && !httpContext.Request.Path.StartsWithSegments("/health")
                && !httpContext.Request.Path.StartsWithSegments("/profiler/")
                && !new[]
                {
                    "html",
                    "ico",
                    "png",
                    "jpg"
                }.Any(
                    ext => httpContext.Request.Path.Value.EndsWith($".{ext}"));
        };
    });

builder.Services.AddProfileViewer();

builder.Services.AddHealthChecks();

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwaggerUI();
app.MapControllers();
app.MapSwagger();
app.MapHealthChecks("/healthz");

app.Run();
