using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Crawler.Contracts;
using Crawler.Worker.Config;
using Crawler.Worker.Infrastructure;
using Crawler.Worker.Parsing;
using Crawler.Worker.Services;
using MassTransit;
using Microsoft.Extensions.Options;

internal class ForkLogCrawlService : ICrawlService
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IBrowsingContext _context;

    private readonly CrawlPolicy _policy;
    private readonly VisitedSet _visited = new();

    private readonly string _baseUrl = "https://forklog.com/tag/crypto";

    private ForkLogFilterParsing _filterParsing = new ForkLogFilterParsing();
    private PageFetcher _fetcher = new PageFetcher();

    public ForkLogCrawlService(IPublishEndpoint publishEndpoint,
        IOptions<CrawlPolicy> options)
    {
        _publishEndpoint = publishEndpoint;
        _policy = options.Value;
        _context = BrowsingContext.New(Configuration.Default);
    }

    public async Task StartAsync(string filter, CancellationToken cancellationToken)
    {
        // var coinSymbol = filter.ToLower();
        var coinSymbol = "биткоин"; // tmp fast solution
        Console.WriteLine($"[CrawlService] Start crawling for {coinSymbol}");

        var html = await _fetcher.FetchAsync(_baseUrl, cancellationToken);
        var document = await GetDocumentFromHtmlAsync(html);
        var links = ExtractArticleLinks(document);

        foreach (var url in links)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_visited.TryAdd(url))
                continue;

            var pageHtml = await _fetcher.FetchAsync(url, cancellationToken);

            if (await _filterParsing.ContentMatchFilter(pageHtml, coinSymbol))
                await _publishEndpoint.Publish(new UrlMatched(coinSymbol, url), cancellationToken);

            await Task.Delay(_policy.DelayBetweenRequestsMs, cancellationToken);
        }

        Console.WriteLine($"[CrawlService] END");
    }

    private async Task<IDocument> GetDocumentFromHtmlAsync(string html)
    {
        return await _context.OpenAsync(req => req.Content(html));
    }

    private List<string> ExtractArticleLinks(IDocument document)
    {
        return document.QuerySelectorAll("a")
            .OfType<IHtmlAnchorElement>()
            .Select(a => a.Href)
            .Where(href => href.Contains("/news/"))
            .Distinct()
            .Take(_policy.MaxPages)
            .ToList();
    }
}
