using Crawler.Contracts;
using MassTransit;

namespace Crawler.ResultListener
{
    class UrlMatchedConsumer : IConsumer<UrlMatched>
    {
        public Task Consume(ConsumeContext<UrlMatched> context)
        {
            Console.WriteLine($"{context.Message.Title} \n {context.Message.Url} \n \n");
            return Task.CompletedTask;
        }
    }
}
