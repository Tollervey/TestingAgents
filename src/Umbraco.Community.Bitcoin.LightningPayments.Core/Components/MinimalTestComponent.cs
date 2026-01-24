using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Components
{
    public class MinimalTestComponent : IComponent
    {
        private readonly ILogger<MinimalTestComponent> _logger;

        public MinimalTestComponent(ILogger<MinimalTestComponent> logger)
        {
            _logger = logger;
        }

        public void Initialize()
        {
            // This log proves that the assembly is loaded and Umbraco is running our code.
            _logger.LogInformation("--- MINIMAL TEST: LightningPayments Assembly Loaded Successfully ---");
        }

        public void Terminate()
        {
        }
    }
}

