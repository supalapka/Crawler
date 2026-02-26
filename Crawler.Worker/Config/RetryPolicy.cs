namespace Crawler.Worker.Config
{
    public class RetryPolicy
    {
        public int RetryCount { get; init; }
        public int IntervalSeconds { get; init; }
    }
}
