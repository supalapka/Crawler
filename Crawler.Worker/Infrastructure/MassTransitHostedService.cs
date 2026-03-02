using MassTransit;
using Microsoft.Extensions.Hosting;

namespace Crawler.Worker.Infrastructure
{
    internal sealed class MassTransitHostedService : IHostedService
    {
        private readonly IBusControl _busControl;

        public MassTransitHostedService(IBusControl busControl) => _busControl = busControl;

        public Task StartAsync(CancellationToken cancellationToken) => _busControl.StartAsync(cancellationToken);
        public Task StopAsync(CancellationToken cancellationToken) => _busControl.StopAsync(cancellationToken);
    }
}
