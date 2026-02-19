using Crawler.ResultListener;
using MassTransit;

class Program
{
    static async Task Main()
    {
        var bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
        {
            cfg.Host("localhost", "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });

            cfg.ReceiveEndpoint("url.matched", e =>
            {
                e.Consumer<UrlMatchedConsumer>();
            });
        });

        await bus.StartAsync();

        Console.WriteLine("Listener + Output started");
        Console.ReadLine();
    }
}