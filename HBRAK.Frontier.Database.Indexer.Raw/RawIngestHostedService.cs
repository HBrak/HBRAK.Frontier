using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HBRAK.Frontier.Database.Indexer.Raw;

public sealed class RawIngestHostedService(IRawIndexer indexer, ILogger<RawIngestHostedService> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var didWork = await indexer.RunOnceAsync(stoppingToken);
            if (!didWork)
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
