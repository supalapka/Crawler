using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddMemoryCache();
        services.AddCrawlerServices(ctx.Configuration);
        services.AddCrawlerMassTransit(ctx.Configuration);
    });

await builder.Build().RunAsync();