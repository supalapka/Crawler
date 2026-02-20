namespace Crawler.Worker.Config
{
    public class CrawlPolicy
    {
        public int MaxPages { get; init; }
        public int DelayBetweenRequestsMs { get; init; }
        public int MaxDepth { get; init; }
    }
}
