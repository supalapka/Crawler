namespace Crawler.Worker.Config
{
    public class ForklogCrawlPolicy
    {
        public int MaxPages { get; init; }
        public int DelayBetweenRequestsMs { get; init; }
        public int CryptoTagId { get; init; }
        public int PostsPerPage { get; init; }
    }
}
