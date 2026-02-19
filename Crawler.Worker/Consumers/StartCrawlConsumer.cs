using Crawler.Contracts;
using Crawler.Worker.Services;
using MassTransit;

namespace Crawler.Worker.Consumers
{
    class StartCrawlConsumer : IConsumer<StartCrawl>
    {
        private readonly ICrawlService _crawlService;

        public StartCrawlConsumer(ICrawlService crawlService)
        {
            _crawlService = crawlService;
        }

        public Task Consume(ConsumeContext<StartCrawl> context)
        {
            return _crawlService.StartAsync(
                context.Message.Filter,
                context.CancellationToken
            );
        }
    }
}
