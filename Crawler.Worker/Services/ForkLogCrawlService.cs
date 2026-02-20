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
    private readonly Queue<(string Url, int Depth)> _queue = new();

    private ForkLogFilterParsing _filterParsing = new ForkLogFilterParsing();
    private PageFetcher _fetcher = new PageFetcher();

    public ForkLogCrawlService(IPublishEndpoint publishEndpoint, IOptions<CrawlPolicy> options)
    {
        _publishEndpoint = publishEndpoint;
        _policy = options.Value;
        _context = BrowsingContext.New(Configuration.Default);
        _queue.Enqueue((_baseUrl, 0));
    }

    public async Task StartAsync(string filter, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[CrawlService] Start widthraw for {filter}");
        var coinSymbol = "биткоин"; // tmp fast solution

        while (_queue.Any() && _visited.Count < _policy.MaxPages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (url, depth) = _queue.Dequeue();

            if (!_visited.TryAdd(url))
                continue;

            var html = await _fetcher.FetchAsync(url, cancellationToken);

            if (await _filterParsing.ContentMatchFilter(html, coinSymbol))
                await _publishEndpoint.Publish(new UrlMatched(coinSymbol, "", url), cancellationToken);

            if (depth < _policy.MaxDepth)
            {
                var document = await GetDocumentFromHtmlAsync(html);
                var links = ExtractArticleLinks(document);

                foreach (var link in links)
                    _queue.Enqueue((link, depth + 1));
            }
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
            .ToList();
    }
}
