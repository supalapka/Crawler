using Crawler.Contracts;
using Crawler.Worker.Config;
using Crawler.Worker.Infrastructure;
using Crawler.Worker.Parsing;
using Crawler.Worker.Services;
using MassTransit;
using Microsoft.Extensions.Options;
using System.Text.Json;


internal class ForkLogCrawlService : ICrawlService
{
    private class ForkLogPostDto
    {
        public string link { get; set; } = null!;
    }

    private readonly IPublishEndpoint _publishEndpoint;

    private readonly ForklogCrawlPolicy _policy;
    private readonly HtmlCache _htmlCache = new();

    private readonly ForkLogFilterParsing _filterParsing = new ForkLogFilterParsing();
    private readonly PageFetcher _fetcher = new PageFetcher();

    private readonly string _ajaxUrl = "https://forklog.com/wp-content/themes/forklogv2/ajax/getPosts.php";

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

            var bodyPost = GenerateBodyPost(pagePostsOffset);
            var links = await GetNewsLinksFromPage(bodyPost, cancellationToken);

            if (!links.Any())
                break;

            foreach (var link in links)
            {
                if (articlesProcessed >= _policy.MaxPages)
                    break;

                if (_htmlCache.TryGet(link, out var cachedHtml))
                {
                    if (await _filterParsing.ContentMatchFilter(cachedHtml, filter))
                        await _publishEndpoint.Publish(new UrlMatched(filter, "", link), cancellationToken);

                    articlesProcessed++;
                    continue;
                }

                string articleHtml = await FetchWithRetryAsync(link, cancellationToken);
                _htmlCache.Store(link, articleHtml);

                if (await _filterParsing.ContentMatchFilter(articleHtml, filter))
                    await _publishEndpoint.Publish(new UrlMatched(filter, "", link), cancellationToken);

                articlesProcessed++;
                await Task.Delay(_policy.DelayBetweenRequestsMs, cancellationToken);
            }

            pagePostsOffset += _policy.PostsPerPage;
            await Task.Delay(_policy.DelayBetweenRequestsMs, cancellationToken);
        }

        Console.WriteLine($"[CrawlService] END. Articles processed: {articlesProcessed}");
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

    private async Task<List<string>> GetNewsLinksFromPage(Dictionary<string, string> bodyPost, CancellationToken cancellationToken)
    {
        string ajaxResponse = await _fetcher.FetchPostAsync(_ajaxUrl, bodyPost, cancellationToken);

        if (string.IsNullOrWhiteSpace(ajaxResponse) || ajaxResponse.Length < 100)
            return new List<string>();

        var posts = JsonSerializer.Deserialize<List<ForkLogPostDto>>(ajaxResponse);

        if (posts == null || posts.Count == 0)
            return new List<string>();

        var links = posts
            .Select(p => p.link)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Distinct()
            .ToList();

        return links;
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
}
