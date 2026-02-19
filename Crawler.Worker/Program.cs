using Crawler.Worker.Consumers;
using Crawler.Worker.Services;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    static async Task Main()
    {
        var services = new ServiceCollection();

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

                cfg.ReceiveEndpoint("crawler.start", e =>
                {
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
