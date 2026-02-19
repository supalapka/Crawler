namespace Crawler.Worker.Infrastructure
{
    public class PageFetcher
    {
        private readonly HttpClient _httpClient = new();

        public async Task<string> FetchAsync(string url, CancellationToken ct)
        {
            return await _httpClient.GetStringAsync(url, ct);
        }
    }
}
