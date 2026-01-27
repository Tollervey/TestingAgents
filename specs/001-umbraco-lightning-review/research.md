# Research Document: Umbraco Lightning Payments Code Review & Improvements

**Feature Branch**: `001-umbraco-lightning-review`
**Created**: 2026-01-26
**Status**: Complete

## Executive Summary

This research document consolidates findings from investigation into the key technology areas required for implementing the Umbraco Lightning Payments improvements. All "NEEDS CLARIFICATION" items from the specification have been resolved through research.

---

## Research Areas

### 1. Umbraco v17 Bellissima Backoffice Development

**Decision**: Use Lit/TypeScript components following Umbraco Bellissima patterns

**Rationale**:
- Umbraco v17 standardizes on Web Components via Lit framework
- UUI (Umbraco UI Library) provides consistent design system
- Management API provides secure backend communication
- Context API enables state management across components

**Alternatives Considered**:
- React/Preact wrapper: Rejected (conflicts with native Umbraco architecture)
- Server-side Razor: Rejected (Bellissima is purely client-side)

**Key Implementation Patterns**:

#### Package Manifest (`umbraco-package.json`)
```json
{
  "$schema": "https://json.schemastore.org/umbraco-package.json",
  "name": "Lightning Payments",
  "version": "1.0.0",
  "extensions": [
    {
      "type": "dashboard",
      "alias": "lightning.dashboard.payments",
      "name": "Lightning Payments Dashboard",
      "element": "/App_Plugins/LightningPayments/dist/dashboard/lightning-dashboard.js",
      "weight": 10,
      "meta": {
        "label": "Lightning Payments",
        "pathname": "lightning-payments"
      },
      "conditions": [
        {
          "alias": "Umb.Condition.SectionAlias",
          "match": "Umb.Section.Settings"
        }
      ]
    },
    {
      "type": "propertyEditorUI",
      "alias": "Lightning.PropertyEditor.PaywallConfig",
      "name": "Paywall Configuration Editor",
      "element": "/App_Plugins/LightningPayments/dist/property-editor/paywall-editor.js",
      "meta": {
        "label": "Paywall Configuration",
        "icon": "icon-coin",
        "group": "payment",
        "propertyEditorSchemaAlias": "Lightning.PropertyEditor.PaywallConfigSchema"
      }
    }
  ]
}
```

#### Dashboard Component Pattern
```typescript
@customElement('lightning-dashboard')
export class LightningDashboardElement extends UmbElementMixin(LitElement) {
  @state() private _stats: PaymentStats | null = null;
  @state() private _loading = true;

  async connectedCallback() {
    super.connectedCallback();
    await this._loadStats();
  }

  private async _loadStats() {
    const response = await fetch('/umbraco/management/api/v1/lightning/dashboard/stats');
    this._stats = await response.json();
  }
}
```

#### Property Editor Pattern
```typescript
@customElement('paywall-editor')
export class PaywallEditorElement extends UmbLitElement implements UmbPropertyEditorUiElement {
  @property({ type: Object }) value?: PaywallConfig;
  @property({ type: Object }) config?: UmbPropertyEditorConfigCollection;

  private _updateValue(newValue: PaywallConfig) {
    this.value = newValue;
    this.dispatchEvent(new CustomEvent('property-value-change', {
      detail: { value: newValue }
    }));
  }
}
```

**UUI Components to Use**:
- `<uui-box>` - Container with headline
- `<uui-card-content-node>` - Stats cards
- `<uui-table>` - Payment history
- `<uui-badge>` - Status indicators
- `<uui-input>`, `<uui-toggle>`, `<uui-select>` - Form controls
- `<uui-loader>` - Loading states
- `<uui-button>` - Actions

---

### 2. CoinGecko API for Exchange Rates

**Decision**: Use CoinGecko free API with 5-minute caching

**Rationale**:
- No API key required for free tier
- Simple REST API with straightforward JSON responses
- 10-50 requests/minute rate limit sufficient for cached usage
- Supports all major fiat currencies

**Alternatives Considered**:
- Kraken API: Rejected (requires API key, overkill for display purposes)
- CoinMarketCap: Rejected (requires paid plan for reliable access)
- Custom aggregator: Rejected (unnecessary complexity)

**API Endpoint**:
```
GET https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd,eur,gbp
```

**Response Format**:
```json
{
  "bitcoin": {
    "usd": 45000.00,
    "eur": 41500.00,
    "gbp": 35000.00
  }
}
```

**Caching Strategy**:
- Cache TTL: 5 minutes (FR-020)
- Use `IDistributedCache` for multi-instance deployments
- Fall back to in-memory cache for single instances
- Graceful degradation: show satoshi-only when rates unavailable

**Service Implementation Pattern**:
```csharp
public class CoinGeckoExchangeRateService : IExchangeRateService
{
    private readonly HttpClient _httpClient;
    private readonly IDistributedCache _cache;
    private const string CacheKey = "exchange_rates_btc";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<ExchangeRates?> GetRatesAsync(CancellationToken ct)
    {
        var cached = await _cache.GetStringAsync(CacheKey, ct);
        if (cached != null)
            return JsonSerializer.Deserialize<ExchangeRates>(cached);

        var response = await _httpClient.GetAsync(
            "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd,eur,gbp", ct);

        if (!response.IsSuccessStatusCode)
            return null; // Graceful degradation

        var rates = await ParseRatesAsync(response.Content, ct);
        await _cache.SetStringAsync(CacheKey, JsonSerializer.Serialize(rates),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration }, ct);

        return rates;
    }
}
```

**Error Handling**:
- HTTP 429 (Rate Limited): Return cached/stale data, log warning
- HTTP 5xx (Server Error): Return cached/stale data, log error
- Network timeout: Return cached/stale data, log error
- All failures: Continue showing satoshi amounts (FR-022)

---

### 3. BreezSDK Bolt12 and Send Payment Capabilities

**Decision**: Extend existing BreezSDK integration to expose send payment and Bolt12 lifecycle management

**Rationale**:
- SDK wrapper methods already exist but are not exposed in IBreezSdkService
- Two-step send pattern (prepare → execute) enables fee display before confirmation
- Bolt12 offers are natively supported with optional amounts

**Bolt12 Offer Implementation**:

Current state: `CreateBolt12OfferAsync` exists in `BreezSdkService` but:
- No database entity to track offers
- No lifecycle management (activate/deactivate)
- No payment-to-offer association

**Required Extensions**:
```csharp
// New entity: Bolt12Offer
public class Bolt12Offer
{
    public Guid OfferId { get; set; }
    public string OfferString { get; set; }
    public string Description { get; set; }
    public ulong? AmountSat { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DeactivatedAt { get; set; }
}

// New service interface
public interface IBolt12OfferService
{
    Task<Bolt12Offer> CreateOfferAsync(ulong? amountSat, string description, CancellationToken ct);
    Task<Bolt12Offer?> GetOfferAsync(Guid offerId, CancellationToken ct);
    Task<IReadOnlyList<Bolt12Offer>> ListOffersAsync(bool activeOnly, CancellationToken ct);
    Task DeactivateOfferAsync(Guid offerId, CancellationToken ct);
    Task<IReadOnlyList<PaymentState>> GetOfferPaymentsAsync(Guid offerId, CancellationToken ct);
}
```

**Send Payment/Refund Implementation**:

Existing wrapper methods in `IBreezSdkWrapper`:
- `PrepareSendPaymentAsync()` - exists
- `SendPaymentAsync()` - exists

Required IBreezSdkService extensions:
```csharp
public interface IBreezSdkService
{
    // Existing methods...

    // NEW: Send payment
    Task<PrepareSendResponse> PrepareSendPaymentAsync(string invoice, ulong? amountSat = null, CancellationToken ct = default);
    Task<SendPaymentResponse> SendPaymentAsync(string invoice, CancellationToken ct = default);

    // NEW: Balance query
    Task<ulong> GetWalletBalanceAsync(CancellationToken ct = default);

    // NEW: Invoice parsing
    Task<ParsedInvoice> ParseInvoiceAsync(string invoice, CancellationToken ct = default);
}
```

**Refund Service Pattern**:
```csharp
public class RefundService : IRefundService
{
    public async Task<RefundTransaction> InitiateRefundAsync(
        string originalPaymentHash,
        string destinationInvoice,
        CancellationToken ct)
    {
        // 1. Validate original payment exists
        var originalPayment = await _paymentService.GetByPaymentHashAsync(originalPaymentHash, ct);
        if (originalPayment == null)
            throw new NotFoundException("Original payment not found");

        // 2. Parse and validate refund amount
        var parsed = await _sdkService.ParseInvoiceAsync(destinationInvoice, ct);
        if (parsed.AmountSat > originalPayment.AmountSat)
            throw new ValidationException("Refund amount exceeds original payment");

        // 3. Check wallet balance
        var balance = await _sdkService.GetWalletBalanceAsync(ct);
        if (balance < parsed.AmountSat)
            throw new InsufficientBalanceException("Insufficient balance for refund");

        // 4. Create record and execute
        var refund = new RefundTransaction { /* ... */ };
        await _sdkService.SendPaymentAsync(destinationInvoice, ct);

        return refund;
    }
}
```

---

### 4. Payment Notification System

**Decision**: Implement email and webhook notifications with Polly retry policies

**Rationale**:
- Email via MailKit (async SMTP with modern TLS support)
- Webhooks with HMAC-SHA256 signing for security
- Polly exponential backoff achieves ~30 minute retry window
- Outbox pattern ensures delivery reliability

**Retry Policy Configuration** (5 retries over ~30 minutes):
```csharp
// Delays: 2s, 4s, 8s, 16s, 32s = 62s base
// With 25% jitter: ~47-78 seconds per attempt
// Total window: ~4-6.5 minutes for retries + processing
// For ~30 minutes: use multiplier or increase base delays

var retryPolicy = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 5,
        Delay = TimeSpan.FromMinutes(2),  // 2m, 4m, 8m, 16m, 32m = ~62 minutes max
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        ShouldHandle = new PredicateBuilder()
            .Handle<HttpRequestException>()
            .Handle<TimeoutException>()
    })
    .Build();

// Alternative: Fixed delays for precise ~30 minute window
var delays = new[] { 1, 2, 5, 10, 15 }; // minutes = 33 minutes total
```

**HMAC-SHA256 Webhook Signing**:
```csharp
public static string SignPayload(string payload, string secret)
{
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
    return Convert.ToHexString(hash).ToLowerInvariant();
}

// Verification (constant-time comparison)
public static bool VerifySignature(string payload, string signature, string secret)
{
    var expected = SignPayload(payload, secret);
    return CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(signature),
        Encoding.UTF8.GetBytes(expected));
}
```

**Notification Entity**:
```csharp
public class PaymentNotification
{
    public Guid NotificationId { get; set; }
    public string PaymentHash { get; set; }
    public NotificationType Type { get; set; } // Email, Webhook
    public string Destination { get; set; } // Email address or URL
    public NotificationStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? FailedAt { get; set; }
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Failed,
    Retrying
}
```

**Webhook Payload Structure**:
```json
{
  "event": "payment.confirmed",
  "timestamp": "2026-01-26T10:30:00Z",
  "data": {
    "paymentHash": "abc123...",
    "amountSat": 5000,
    "contentId": 1234,
    "contentName": "Premium Article",
    "confirmedAt": "2026-01-26T10:29:55Z"
  }
}
```

---

### 5. Exception Hierarchy Consolidation

**Decision**: Create unified `LightningPaymentsException` base class

**Rationale**:
- Current codebase has mixed exception types (BreezSdkException, PaymentException, WebhookException)
- Unified hierarchy enables consistent error handling and API responses
- Error codes enable machine-readable error classification

**Exception Hierarchy**:
```csharp
public class LightningPaymentsException : Exception
{
    public string ErrorCode { get; }
    public int HttpStatusCode { get; }

    protected LightningPaymentsException(string message, string errorCode, int httpStatusCode = 500)
        : base(message)
    {
        ErrorCode = errorCode;
        HttpStatusCode = httpStatusCode;
    }
}

// Payment errors
public class PaymentNotFoundException : LightningPaymentsException
{
    public PaymentNotFoundException(string paymentHash)
        : base($"Payment not found: {paymentHash}", "PAYMENT_NOT_FOUND", 404) { }
}

public class InsufficientBalanceException : LightningPaymentsException
{
    public ulong Required { get; }
    public ulong Available { get; }

    public InsufficientBalanceException(ulong required, ulong available)
        : base($"Insufficient balance. Required: {required}, Available: {available}",
            "INSUFFICIENT_BALANCE", 400)
    {
        Required = required;
        Available = available;
    }
}

// SDK errors
public class BreezSdkConnectionException : LightningPaymentsException
{
    public BreezSdkConnectionException(string message)
        : base(message, "BREEZ_SDK_CONNECTION_ERROR", 503) { }
}

// Validation errors
public class InvalidInvoiceException : LightningPaymentsException
{
    public InvalidInvoiceException(string reason)
        : base($"Invalid invoice: {reason}", "INVALID_INVOICE", 400) { }
}

// Refund errors
public class RefundExceedsOriginalException : LightningPaymentsException
{
    public RefundExceedsOriginalException(ulong refundAmount, ulong originalAmount)
        : base($"Refund amount ({refundAmount}) exceeds original payment ({originalAmount})",
            "REFUND_EXCEEDS_ORIGINAL", 400) { }
}
```

---

## Technology Decisions Summary

| Area | Decision | Key Dependency | Version |
|------|----------|----------------|---------|
| Backoffice UI | Lit/TypeScript + UUI | @umbraco-cms/backoffice | 17.* |
| Exchange Rates | CoinGecko API (free) | HttpClient | N/A |
| Email Notifications | MailKit | MailKit | 4.8.0 |
| Webhook Signing | HMAC-SHA256 | System.Security.Cryptography | N/A |
| Retry Policies | Polly | Polly | 8.6.5 |
| Distributed Cache | IDistributedCache | Microsoft.Extensions.Caching | 9.0 |

---

## Resolved Clarifications

| Item | Resolution |
|------|------------|
| Backoffice UI Framework | Lit/TypeScript with Umbraco UUI components |
| Exchange Rate Source | CoinGecko free API with 5-minute cache |
| Bolt12 Offer Storage | New Bolt12Offer entity with EF Core |
| Send Payment Pattern | Two-step prepare/execute via existing wrapper |
| Notification Retry | Polly exponential backoff, 5 retries, ~30 min window |
| HMAC Signing | SHA256 hex-encoded with constant-time verification |
| Exception Hierarchy | Unified LightningPaymentsException base class |

---

## Next Steps

1. **Phase 1: Data Model Design** (`data-model.md`)
   - Define new entities (Bolt12Offer, RefundTransaction, PaymentNotification, ExchangeRate)
   - Update PaymentState with offer association

2. **Phase 1: API Contracts** (`contracts/`)
   - OpenAPI specs for Management and Public APIs
   - Webhook payload schemas

3. **Phase 1: Developer Guide** (`quickstart.md`)
   - Development environment setup
   - Build and test commands

---

*Research completed 2026-01-26*
