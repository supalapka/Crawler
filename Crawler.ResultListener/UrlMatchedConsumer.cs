using Crawler.Contracts;
using MassTransit;

namespace Crawler.ResultListener
{
    class UrlMatchedConsumer : IConsumer<UrlMatched>
    {
        public Task Consume(ConsumeContext<UrlMatched> context)
        {
                Console.WriteLine($"[Listener + output] Coin matched:{context.Message.Url}: {context.Message.Url}");
            return Task.CompletedTask;
        }
    }
}
