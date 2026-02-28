using Crawler.Contracts;
using Crawler.Worker.Config;
using Crawler.Worker.Infrastructure;
using Crawler.Worker.Parsing;
using Crawler.Worker.Services;
using MassTransit;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;


internal class ForkLogCrawlService : ICrawlService
{
    private class ForkLogPostDto
    {
        public string link { get; set; } = null!;
    }
    private const string AjaxUrl = "https://forklog.com/wp-content/themes/forklogv2/ajax/getPosts.php";
    private static readonly int[] RetryDelaysMs = { 3000, 8000, 15000 };

    private readonly IPublishEndpoint _publishEndpoint;

    private readonly ForklogCrawlPolicy _policy;
    private readonly HtmlCache _htmlCache = new();

    private readonly ForkLogFilterParsing _filterParsing = new ForkLogFilterParsing();
    private readonly PageFetcher _fetcher = new PageFetcher();

    public ForkLogCrawlService(IPublishEndpoint publishEndpoint, IOptions<ForklogCrawlPolicy> options)
    {
        _publishEndpoint = publishEndpoint;
        _policy = options.Value;
    }

    public async Task StartAsync(string filter, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[CrawlService] Start ForkLog crawl for {filter}");
        int pagePostsOffset = 0;
        int articlesProcessed = 0;

        while (articlesProcessed < _policy.MaxPages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var links = await FetchPageLinksAsync(pagePostsOffset, cancellationToken);

            if (!links.Any())
                break;

            foreach (var link in links)
            {
                if (articlesProcessed >= _policy.MaxPages)
                    break;

                string html = await GetCachedOrFetchAsync(link, cancellationToken);

                if (await _filterParsing.ContentMatchFilter(html, filter))
                    await _publishEndpoint.Publish(new UrlMatched(filter, "", link), cancellationToken);

                articlesProcessed++;
                await Task.Delay(_policy.DelayBetweenRequestsMs, cancellationToken);
            }

            pagePostsOffset += _policy.PostsPerPage;
            await Task.Delay(_policy.DelayBetweenRequestsMs, cancellationToken);
        }

        Console.WriteLine($"[CrawlService] END. Articles processed: {articlesProcessed}");
    }

    private async Task<string> GetCachedOrFetchAsync(string url, CancellationToken cancellationToken)
    {
        if (_htmlCache.TryGet(url, out var cached))
            return cached;

        var html = await FetchWithRetryAsync(url, cancellationToken);
        _htmlCache.Store(url, html);
        return html;
    }

    private async Task<List<string>> FetchPageLinksAsync(int pagePostsOffset, CancellationToken cancellationToken)
    {
        var bodyPost = GenerateBodyPost(pagePostsOffset);
        string ajaxResponse = await _fetcher.FetchPostAsync(AjaxUrl, bodyPost, cancellationToken);

        if (string.IsNullOrWhiteSpace(ajaxResponse) || ajaxResponse.Length < 100)
            return new List<string>();

        var posts = JsonSerializer.Deserialize<List<ForkLogPostDto>>(ajaxResponse);

        if (posts == null || posts.Count == 0)
            return [];

        return posts
            .Select(p => p.link)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Distinct()
            .ToList();
    }

    private Dictionary<string, string> GenerateBodyPost(int pagePostsOffset)
    {
        return new Dictionary<string, string>
        {
            ["action"] = "getPostsByTag",
            ["tag"] = _policy.CryptoTagId.ToString(),
            ["offset"] = pagePostsOffset.ToString(),
            ["postperpage"] = _policy.PostsPerPage.ToString()
        };
    }

    private async Task<string> FetchWithRetryAsync(string url, CancellationToken cancellationToken)
    {
        foreach (var delay in RetryDelaysMs)
        {
            var start = DateTime.UtcNow;
            try
            {
                Console.WriteLine($"[Fetch start] {start:O}");
                var result = await _fetcher.FetchAsync(url, cancellationToken);
                Console.WriteLine($"[Fetch end] {DateTime.UtcNow:O}, elapsed: {DateTime.UtcNow - start}");
                return result;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                Console.WriteLine($"[CrawlService] 403 on {url}, waiting {delay}ms before retry");
                await Task.Delay(delay, cancellationToken);
            }
        }

        // final attempt, let it throw if still 403
        return await _fetcher.FetchAsync(url, cancellationToken);
    }
}
