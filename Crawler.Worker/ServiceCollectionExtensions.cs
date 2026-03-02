using Crawler.Worker.Config;
using Crawler.Worker.Consumers;
using Crawler.Worker.Infrastructure;
using Crawler.Worker.Services;
using GreenPipes;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCrawlerServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ForklogCrawlPolicy>()
            .Bind(configuration.GetSection("ForklogCrawlPolicy"))
            .Validate(p => p.MaxPages > 0);

        services.AddOptions<RetryPolicy>()
            .Bind(configuration.GetSection("RetryPolicy"))
            .Validate(p => p.RetryCount > 0 && p.IntervalSeconds > 0);

        services.AddHttpClient<PageFetcher>();
        services.AddSingleton<ICrawlService, ForkLogCrawlService>();

        return services;
    }

    public static IServiceCollection AddCrawlerMassTransit(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<StartCrawlConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration["RabbitMq:Host"] ?? "localhost", "/", h =>
                {
                    h.Username(configuration["RabbitMq:Username"] ?? "guest");
                    h.Password(configuration["RabbitMq:Password"] ?? "guest");
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

        return services;
    }
}
