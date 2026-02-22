namespace Crawler.Worker.Infrastructure
{
    public class PageFetcher
    {
        private readonly HttpClient _httpClient = new();

        public async Task<string> FetchAsync(string url, CancellationToken ct)
        {
            return await _httpClient.GetStringAsync(url, ct);
        }

        public async Task<string> FetchPostAsync(string url, Dictionary<string, string> form, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new FormUrlEncodedContent(form)
            };

            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.Add("Referer", "https://forklog.com/tag/crypto/");
            request.Headers.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122 Safari/537.36");

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct);
        }
    }
}
