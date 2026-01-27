using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Components;

public class MinimalTestComponent : IAsyncComponent
{
    private readonly ILogger<MinimalTestComponent> _logger;

    public MinimalTestComponent(ILogger<MinimalTestComponent> logger)
    {
        _logger = logger;
    }

    public Task InitializeAsync(bool isRestarting, CancellationToken cancellationToken)
    {
        // This log proves that the assembly is loaded and Umbraco is running our code.
        _logger.LogInformation("--- MINIMAL TEST: LightningPayments Assembly Loaded Successfully ---");
        return Task.CompletedTask;
    }

    public Task TerminateAsync(bool isRestarting, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
