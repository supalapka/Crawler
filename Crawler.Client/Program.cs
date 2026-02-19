using Crawler.Contracts;
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
        });

        await bus.StartAsync();

        string filter = "";

        while (filter.ToLower() != "exit")
        {

            Console.Write("Enter filter: ");
            filter = Console.ReadLine();

            if (filter.ToLower() == "exit")
                continue;

            var endpoint = await bus.GetSendEndpoint(new Uri("queue:crawler.start"));
            await endpoint.Send(new StartCrawl(filter));

            Console.WriteLine("Message sent");
        }

        await bus.StopAsync();
    }
}
