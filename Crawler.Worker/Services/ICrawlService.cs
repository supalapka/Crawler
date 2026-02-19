namespace Crawler.Worker.Services
{
    internal interface ICrawlService
    {
        Task StartAsync(string filter, CancellationToken cancellationToken);
    }
}
