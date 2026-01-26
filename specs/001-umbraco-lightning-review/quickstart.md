# Developer Quickstart: Umbraco Lightning Payments Improvements

**Feature Branch**: `001-umbraco-lightning-review`
**Created**: 2026-01-26

## Prerequisites

### Required Software

| Software | Version | Purpose |
|----------|---------|---------|
| .NET SDK | 9.0+ | Backend development |
| Node.js | 20 LTS | Backoffice UI development |
| npm | 10+ | Package management |
| Git | 2.40+ | Version control |

### Recommended Tools

- **IDE**: Visual Studio 2022 / VS Code / Rider
- **Database Tools**: SQLite Browser, Azure Data Studio
- **API Testing**: Postman, Bruno, or VS Code REST Client

---

## Quick Start

### 1. Clone and Branch Setup

```bash
git clone <repository-url>
cd TestingAgents
git checkout 001-umbraco-lightning-review
```

### 2. Restore Dependencies

```bash
# Backend
dotnet restore

# Frontend (Backoffice UI)
cd src/Umbraco.Community.Bitcoin.LightningPayments.Core/BackofficeUI
npm install
```

### 3. Build Everything

```bash
# From repository root
dotnet build

# Build backoffice UI (outputs to App_Plugins/)
cd src/Umbraco.Community.Bitcoin.LightningPayments.Core/BackofficeUI
npm run build
```

### 4. Run Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test --filter "FullyQualifiedName~Umbraco.Community.Bitcoin.LightningPayments.CoreTests"

# Watch mode for TDD
dotnet watch test --project tests/Breez.Sdk.Liquid.Extensions.Core.Tests
```

---

## Project Structure

```
TestingAgents/
├── src/
│   └── Umbraco.Community.Bitcoin.LightningPayments.Core/
│       ├── Configuration/           # Settings and options
│       ├── Data/
│       │   ├── Models/             # Entity classes
│       │   └── Migrations/         # EF Core migrations
│       ├── Services/               # Business logic
│       ├── Api/                    # Controllers
│       ├── Features/               # Feature modules
│       ├── BackofficeUI/           # Lit/TypeScript UI
│       │   ├── src/
│       │   └── package.json
│       └── App_Plugins/            # Built UI output
├── tests/
│   └── Umbraco.Community.Bitcoin.LightningPayments.CoreTests/
└── specs/
    └── 001-umbraco-lightning-review/
        ├── spec.md                 # Feature specification
        ├── plan.md                 # Implementation plan
        ├── research.md             # Technology research
        ├── data-model.md           # Entity definitions
        ├── quickstart.md           # This file
        └── contracts/              # OpenAPI specs
```

---

## Development Workflows

### Backend Development

#### Creating a New Service

1. **Create interface** in `Services/[Feature]/I[Service]Service.cs`:
```csharp
public interface IBolt12OfferService
{
    Task<Bolt12Offer> CreateOfferAsync(ulong? amountSat, string description, CancellationToken ct);
}
```

2. **Create implementation** in `Services/[Feature]/[Service]Service.cs`:
```csharp
public class Bolt12OfferService : IBolt12OfferService
{
    private readonly PaymentDbContext _context;
    private readonly IBreezSdkService _sdkService;

    public Bolt12OfferService(PaymentDbContext context, IBreezSdkService sdkService)
    {
        _context = context;
        _sdkService = sdkService;
    }

    public async Task<Bolt12Offer> CreateOfferAsync(ulong? amountSat, string description, CancellationToken ct)
    {
        // Implementation
    }
}
```

3. **Register in DI** in `Extensions/LightningPaymentsExtensions.cs`:
```csharp
services.AddScoped<IBolt12OfferService, Bolt12OfferService>();
```

4. **Write tests first** (TDD):
```csharp
[Fact]
public async Task CreateOfferAsync_ValidInput_ReturnsOffer()
{
    // Arrange
    var service = CreateService();

    // Act
    var result = await service.CreateOfferAsync(5000, "Test", CancellationToken.None);

    // Assert
    Assert.NotNull(result);
}
```

#### Creating a New API Endpoint

1. **Create controller** in `Api/Management/`:
```csharp
[ApiController]
[Authorize(Policy = "UmbracoManagementApi")]
[Route("umbraco/management/api/v1/lightning/[controller]")]
[MapToApi("lightning-management-api")]
public class Bolt12Controller : ManagementApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool activeOnly = true)
    {
        // Implementation
    }
}
```

#### Database Migrations

```bash
# Add migration
cd src/Umbraco.Community.Bitcoin.LightningPayments.Core
dotnet ef migrations add AddBolt12Offers --context PaymentDbContext

# Apply migration (local testing)
dotnet ef database update --context PaymentDbContext

# Generate SQL script
dotnet ef migrations script --context PaymentDbContext
```

### Frontend Development (Backoffice UI)

#### Project Setup

```bash
cd src/Umbraco.Community.Bitcoin.LightningPayments.Core/BackofficeUI
npm install
```

#### Development Commands

```bash
# Build once
npm run build

# Watch mode (auto-rebuild on changes)
npm run dev

# Type checking
npm run type-check

# Lint
npm run lint
```

#### Creating a New Component

1. **Create element file** in `src/[feature]/[component].element.ts`:
```typescript
import { LitElement, html, css, customElement, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';

@customElement('lightning-my-component')
export class MyComponentElement extends UmbElementMixin(LitElement) {
  @state()
  private _data: MyData | null = null;

  render() {
    return html`
      <uui-box headline="My Component">
        <!-- Content -->
      </uui-box>
    `;
  }

  static styles = css`
    :host { display: block; }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    'lightning-my-component': MyComponentElement;
  }
}
```

2. **Register in manifest** (`umbraco-package.json`):
```json
{
  "extensions": [
    {
      "type": "dashboard",
      "alias": "lightning.my-component",
      "name": "My Component",
      "element": "/App_Plugins/LightningPayments/dist/my-component.js"
    }
  ]
}
```

3. **Add to build config** (`vite.config.ts`):
```typescript
build: {
  lib: {
    entry: {
      'my-component': 'src/my-component/my-component.element.ts'
    }
  }
}
```

---

## Testing Guide

### Test Structure

```
tests/Umbraco.Community.Bitcoin.LightningPayments.CoreTests/
├── Services/
│   ├── Bolt12OfferServiceTests.cs
│   ├── RefundServiceTests.cs
│   └── NotificationServiceTests.cs
├── Api/
│   └── DashboardControllerTests.cs
└── Features/
    └── DashboardStatsServiceTests.cs
```

### Test Patterns

#### Unit Test Pattern
```csharp
public class Bolt12OfferServiceTests
{
    private readonly PaymentDbContext _context;
    private readonly Mock<IBreezSdkService> _mockSdkService;
    private readonly Bolt12OfferService _sut;

    public Bolt12OfferServiceTests()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new PaymentDbContext(options);
        _mockSdkService = new Mock<IBreezSdkService>();
        _sut = new Bolt12OfferService(_context, _mockSdkService.Object);
    }

    [Fact]
    public async Task CreateOfferAsync_ValidInput_SavesOffer()
    {
        // Arrange
        _mockSdkService
            .Setup(x => x.CreateBolt12OfferAsync(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("lno1abc...");

        // Act
        var result = await _sut.CreateOfferAsync(5000, "Test offer", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("lno1abc...", result.OfferString);
        Assert.True(result.IsActive);
        Assert.Single(await _context.Bolt12Offers.ToListAsync());
    }
}
```

#### Fast Test Policy Pattern (for resilience tests)
```csharp
// Avoid slow tests - use short delays for test policies
private static ResiliencePipeline CreateFastTestPolicy() =>
    new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(50),  // Fast for tests
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        })
        .Build();
```

### Running Tests

```bash
# All tests
dotnet test

# Specific class
dotnet test --filter "FullyQualifiedName~Bolt12OfferServiceTests"

# With coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate report
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report
```

---

## Configuration

### appsettings.json Structure

```json
{
  "LightningPayments": {
    "BreezApiKey": "your-api-key",
    "Network": "Testnet",
    "ConnectionString": "Data Source=App_Data/LightningPayments/payment.db",

    "Notifications": {
      "EmailEnabled": true,
      "EmailRecipients": ["admin@example.com"],
      "WebhookEnabled": true,
      "WebhookUrl": "https://your-app.com/webhooks/lightning",
      "WebhookSecret": "your-webhook-secret"
    },

    "ExchangeRates": {
      "Enabled": true,
      "DefaultCurrency": "USD",
      "CacheDurationMinutes": 5
    },

    "RateLimiting": {
      "Enabled": true,
      "PermitLimit": 5,
      "WindowSeconds": 60
    }
  }
}
```

### Environment Variables

```bash
# Production secrets (never commit)
BREEZ_API_KEY=your-api-key
BREEZ_MNEMONIC=your-mnemonic
LIGHTNING_WEBHOOK_SECRET=your-webhook-secret
COINGECKO_API_KEY=optional-pro-key
```

---

## Common Tasks

### Adding a New Entity

1. Create model in `Data/Models/`
2. Add DbSet to `PaymentDbContext`
3. Configure in `OnModelCreating()`
4. Create migration
5. Update data access service

### Adding a New API Endpoint

1. Create controller in `Api/Management/` or `Api/Public/`
2. Add OpenAPI documentation
3. Write integration test
4. Update `contracts/*.yaml`

### Adding a Backoffice UI Component

1. Create `.element.ts` file
2. Register in `umbraco-package.json`
3. Add to Vite build entry
4. Test in Umbraco backoffice

---

## Troubleshooting

### Build Errors

```bash
# Clear build artifacts
dotnet clean
rm -rf src/*/bin src/*/obj

# Restore and rebuild
dotnet restore
dotnet build
```

### Database Issues

```bash
# Reset database
rm -f App_Data/LightningPayments/payment.db
dotnet ef database update

# Check migrations
dotnet ef migrations list
```

### Frontend Issues

```bash
# Clear node_modules
rm -rf node_modules package-lock.json
npm install

# Clear build cache
rm -rf dist
npm run build
```

### Test Failures

```bash
# Run with verbose output
dotnet test -v detailed

# Run specific test with logging
dotnet test --filter "FullyQualifiedName~MyTest" --logger "console;verbosity=detailed"
```

---

## Key Files Reference

| File | Purpose |
|------|---------|
| `LightningPaymentsExtensions.cs` | DI registration |
| `PaymentDbContext.cs` | Database context |
| `BreezSdkService.cs` | SDK integration |
| `umbraco-package.json` | Backoffice manifest |
| `vite.config.ts` | Frontend build |

---

## Links

- **Specification**: [spec.md](./spec.md)
- **Implementation Plan**: [plan.md](./plan.md)
- **Research**: [research.md](./research.md)
- **Data Model**: [data-model.md](./data-model.md)
- **API Contracts**: [contracts/](./contracts/)

---

*Last updated: 2026-01-26*
