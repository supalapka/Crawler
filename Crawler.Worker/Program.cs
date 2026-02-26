using Crawler.Worker.Config;
using Crawler.Worker.Consumers;
using Crawler.Worker.Services;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

class Program
{
    static async Task Main()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
           .AddJsonFile("appsettings.json", optional: false)
           .AddEnvironmentVariables()
           .Build();

        services.AddOptions<ForklogCrawlPolicy>()
            .Bind(configuration.GetSection("ForklogCrawlPolicy"))
            .Validate(p => p.MaxPages > 0);

        services.AddOptions<RetryPolicy>()
            .Bind(configuration.GetSection("RetryPolicy"))
            .Validate(p => p.RetryCount > 0 && p.IntervalSeconds > 0);

        services.AddSingleton<ICrawlService, ForkLogCrawlService>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<StartCrawlConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host("localhost", "/", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });

                var retryPolicy = context.GetRequiredService<IOptions<RetryPolicy>>().Value;

                cfg.ReceiveEndpoint("crawler.start", e =>
                {
                    e.UseMessageRetry(r => r.Interval(retryPolicy.RetryCount, 
                        TimeSpan.FromSeconds(retryPolicy.IntervalSeconds)));
                    e.ConfigureConsumer<StartCrawlConsumer>(context);
                });
            });
        });

        var provider = services.BuildServiceProvider();

        var bus = provider.GetRequiredService<IBusControl>();
        await bus.StartAsync();

        Console.WriteLine("Worker started");
        Console.ReadLine();
    }
}
