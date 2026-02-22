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
    private readonly HtmlCache _htmlCache = new();

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
        var coinSymbol = filter;
        int pagesCrawledThisRun = 0;

        if (_htmlCache.Any())
        {
            Console.WriteLine($"[CrawlService] Scanning {_htmlCache.Count} cached pages for {filter}");
            foreach (var (url, html) in _htmlCache.GetAll())
            {
                if (await _filterParsing.ContentMatchFilter(html, filter))
                    await _publishEndpoint.Publish(new UrlMatched(filter, "", url), cancellationToken);
            }
        }

        if (!_queue.Any())
            _queue.Enqueue((_baseUrl, 0));

        while (_queue.Any() && pagesCrawledThisRun < _policy.MaxPages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (url, depth) = _queue.Dequeue();

            if (!_visited.TryAdd(url))
                continue;

            string html = await FetchWithRetryAsync(url, cancellationToken);
            _htmlCache.Store(url, html);

            if (await _filterParsing.ContentMatchFilter(html, coinSymbol))
                await _publishEndpoint.Publish(new UrlMatched(coinSymbol, "", url), cancellationToken);

            if (depth < _policy.MaxDepth)
            {
                var document = await GetDocumentFromHtmlAsync(html);
                var links = ExtractArticleLinks(document);

                foreach (var link in links)
                    _queue.Enqueue((link, depth + 1));
            }
            pagesCrawledThisRun++;
            await Task.Delay(_policy.DelayBetweenRequestsMs, cancellationToken);
        }

        Console.WriteLine($"[CrawlService] END");
    }

    private async Task<string> FetchWithRetryAsync(string url, CancellationToken cancellationToken)
    {
        int[] delays = { 3000, 8000, 15000 };

        for (int i = 0; i < delays.Length; i++)
        {
            try
            {
                return await _fetcher.FetchAsync(url, cancellationToken);
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("403"))
            {
                Console.WriteLine($"[CrawlService] 403 on {url}, waiting {delays[i]}ms before retry {i + 1}");
                await Task.Delay(delays[i], cancellationToken);
            }
        }

        // final attempt, let it throw if still 403
        return await _fetcher.FetchAsync(url, cancellationToken);
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
            .Where(href => href.Contains("forklog.com") && href.Contains("/news/"))
            .Distinct()
            .ToList();
    }
}
