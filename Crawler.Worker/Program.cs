using Crawler.Worker.Config;
using Crawler.Worker.Consumers;
using Crawler.Worker.Infrastructure;
using Crawler.Worker.Services;
using GreenPipes;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddOptions<ForklogCrawlPolicy>()
            .Bind(ctx.Configuration.GetSection("ForklogCrawlPolicy"))
            .Validate(p => p.MaxPages > 0);

        services.AddOptions<RetryPolicy>()
            .Bind(ctx.Configuration.GetSection("RetryPolicy"))
            .Validate(p => p.RetryCount > 0 && p.IntervalSeconds > 0);

        services.AddMemoryCache();
        services.AddHttpClient<PageFetcher>();
        services.AddSingleton<ICrawlService, ForkLogCrawlService>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<StartCrawlConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitHost = ctx.Configuration["RABBITMQ_HOST"] ?? "localhost";

                cfg.Host(rabbitHost, "/", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });

                var retryPolicy = context.GetRequiredService<IOptions<RetryPolicy>>().Value;

                cfg.ReceiveEndpoint("crawler.start", e =>
                {
                    e.ConcurrentMessageLimit = 1;
                    e.PrefetchCount = 1;
                    e.UseMessageRetry(r =>
                        r.Intervals(
                            Enumerable.Repeat(
                                TimeSpan.FromSeconds(retryPolicy.IntervalSeconds),
                                retryPolicy.RetryCount
                            ).ToArray()
                        )
                    );

                    e.ConfigureConsumer<StartCrawlConsumer>(context);
                });
            });
        });

        services.AddHostedService<MassTransitHostedService>();
    });

await builder.Build().RunAsync();
