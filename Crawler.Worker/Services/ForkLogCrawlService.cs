using Crawler.Contracts;
using Crawler.Worker.Services;
using MassTransit;

internal class ForkLogCrawlService : ICrawlService
{
    private readonly IPublishEndpoint _publishEndpoint;

    public ForkLogCrawlService(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task StartAsync(string filter, CancellationToken cancellationToken)
    {
        string coinSymbol = filter.ToLower();
        Console.WriteLine($"[CrawlService] Start crawling for {coinSymbol}");

        if (coinSymbol == "btc")
        {
            await MessageMatchFound(coinSymbol, cancellationToken);
        }

    }

    private async Task MessageMatchFound(string coinSymbol, CancellationToken cancellationToken)
    {
        var fdemoUrl = "https://forklog.com/tag/bitcoin";

        await _publishEndpoint.Publish(
            new UrlMatched(coinSymbol, fdemoUrl),
            cancellationToken
        );
    }
}
