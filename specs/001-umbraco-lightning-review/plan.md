# Implementation Plan: Umbraco Lightning Payments Code Review & Improvements

**Branch**: `001-umbraco-lightning-review` | **Date**: 2026-01-26 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/001-umbraco-lightning-review/spec.md`

**Note**: This plan enhances the existing Umbraco.Community.Bitcoin.LightningPayments.Core project based on comprehensive code review findings.

## Summary

This implementation plan addresses improvements identified during code review of the Umbraco Lightning Payments integration. Primary enhancements include:

1. **Umbraco Backoffice Dashboard** - Bellissima (Lit/TypeScript) components for payment monitoring
2. **Enhanced Paywall Property Editor** - Visual UI replacing JSON configuration
3. **Bolt12 Offer Lifecycle Management** - Full API and UI support for recurring payments
4. **Notification System** - Email and webhook notifications with retry logic
5. **Refund Capability** - Admin-only refund workflow via BreezSDK
6. **Multi-Currency Display** - Fiat conversion via CoinGecko API

## Technical Context

**Language/Version**: C# / .NET 9.0 (existing codebase target)
**Primary Dependencies**:
- Umbraco.Cms 17.* (Bellissima backoffice, per spec.md target platform)
- Breez.Sdk.Liquid 0.11.9
- Entity Framework Core 9.0
- Polly 8.6.5 (resilience)
- OpenTelemetry.Api 1.11.2 (observability)
- Lit 3.x / TypeScript 5.x (Umbraco backoffice UI)

**Storage**: SQLite (default), PostgreSQL, SQL Server (via provider packages)
**Testing**: xUnit 2.9.2, Moq/NSubstitute, InMemoryDatabase, Testcontainers
**Target Platform**: Umbraco v17 on .NET 9.0, Windows/Linux server
**Project Type**: Umbraco Package (backend C# + frontend Lit/TypeScript)
**Performance Goals**:
- 100 concurrent payment sessions (SC-003)
- <30s notification delivery for 99% of payments (SC-004)
- <60s refund processing (SC-005)
- <5min exchange rate freshness (SC-006)

**Constraints**:
- Single-tenant deployment (one wallet per instance)
- Lightning payments only (no on-chain)
- Admin-only refunds (security requirement)

**Scale/Scope**:
- Single Umbraco instance with paywall-protected content
- ~6 new backoffice UI components
- ~15 new/modified API endpoints
- ~10 new database entities/migrations

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Article I: Architectural Foundation

| Gate | Status | Evidence |
|------|--------|----------|
| I.1 Clean Architecture | ✅ PASS | Existing layers: Configuration → Models → Data → Services → API |
| I.2 DDD Alignment | ✅ PASS | Domain models (PaymentState, Bolt12Offer) dependency-free |
| I.3 Modular Decomposition | ✅ PASS | Feature folders (Paywall, TipJar, Realtime), interface-based services |

### Article II: Code Quality Standards

| Gate | Status | Evidence |
|------|--------|----------|
| II.1 Single Responsibility | ✅ PASS | Services focused (IPaymentStateService, IBreezSdkService, etc.) |
| II.2 Interface Segregation | ✅ PASS | Small interfaces (IRateLimiter, ISseHub) |
| II.3 Explicit Over Implicit | ✅ PASS | Constructor injection, IOptions<T> pattern |
| II.4 Self-Documenting Code | ✅ PASS | Clear naming, XML comments on public APIs |

### Article III: Testing Philosophy

| Gate | Status | Evidence |
|------|--------|----------|
| III.1 Test-First Imperative | ⚠️ WARNING | Existing tests present, new features MUST follow TDD |
| III.2 Testing Pyramid | ✅ PASS | Unit tests > Integration tests structure exists |
| III.3 Meaningful Coverage | ⚠️ WARNING | Target 80% for new code (SC-007) |
| III.4 Automated Validation | ✅ PASS | `dotnet test` runs full suite |

### Article IV: Data Layer Governance

| Gate | Status | Evidence |
|------|--------|----------|
| IV.1 Repository Pattern | ✅ PASS | IPaymentStateService abstracts DbContext |
| IV.2 Migration-First | ✅ PASS | EF Core migrations in Data/Migrations/ |
| IV.3 Query Optimization | ✅ PASS | .AsNoTracking() used, explicit includes |

### Article V: API Design Principles

| Gate | Status | Evidence |
|------|--------|----------|
| V.1 Contract-First | ⚠️ WARNING | Contracts to be defined in Phase 1 |
| V.2 RESTful Design | ✅ PASS | Existing endpoints follow REST conventions |
| V.3 Versioning | ✅ PASS | `/umbraco/management/api/`, `/api/public/` patterns |

### Article VI: Security Framework

| Gate | Status | Evidence |
|------|--------|----------|
| VI.1 Defence in Depth | ✅ PASS | HMAC webhooks, rate limiting, auth middleware |
| VI.2 Auth Separation | ✅ PASS | Umbraco auth for admin, anonymous for public |
| VI.3 Secrets Management | ✅ PASS | IOptions<T>, no hardcoded secrets |
| VI.4 Input Validation | ✅ PASS | FluentValidation on settings |

### Article VII: Error Handling & Observability

| Gate | Status | Evidence |
|------|--------|----------|
| VII.1 Structured Exceptions | ⚠️ WARNING | Mixed hierarchy noted in spec - needs consolidation |
| VII.2 Comprehensive Logging | ✅ PASS | Correlation IDs, structured logging |
| VII.3 Health & Diagnostics | ✅ PASS | BreezSdkHealthCheck, /health endpoints |

### Article VIII: Frontend Architecture (Bellissima)

| Gate | Status | Evidence |
|------|--------|----------|
| VIII.1 Component-Based | ⚠️ WARNING | New components to follow Lit patterns |
| VIII.2 State Management | ⚠️ WARNING | Management API for state, local store for UI |
| VIII.3 Accessibility | ⚠️ WARNING | WCAG 2.1 AA required for new UI |

### Article IX: Simplicity & Anti-Complexity

| Gate | Status | Evidence |
|------|--------|----------|
| IX.1 YAGNI | ✅ PASS | Features from spec only, no speculation |
| IX.2 Max Project Limit | ✅ PASS | Adding to existing project structure |
| IX.3 Dependency Scrutiny | ✅ PASS | CoinGecko only new external dependency |

### Overall Pre-Research Gate: ✅ PASS (proceed to Phase 0)

**Warnings to address during implementation:**
- III.1/III.3: Enforce TDD for all new features
- V.1: Define OpenAPI contracts before implementation
- VII.1: Consolidate exception hierarchy
- VIII.*: Follow Umbraco Bellissima patterns for UI

## Project Structure

### Documentation (this feature)

```text
specs/001-umbraco-lightning-review/
├── plan.md              # This file
├── research.md          # Phase 0: Technology research
├── data-model.md        # Phase 1: Entity definitions
├── quickstart.md        # Phase 1: Developer setup guide
├── contracts/           # Phase 1: OpenAPI specifications
│   ├── management-api.yaml   # Admin endpoints
│   ├── public-api.yaml       # Public endpoints
│   └── webhook-api.yaml      # Notification webhooks
└── tasks.md             # Phase 2: Implementation tasks
```

### Source Code (repository root)

```text
src/Umbraco.Community.Bitcoin.LightningPayments.Core/
├── Configuration/
│   ├── LightningPaymentsSettings.cs          # Existing + new settings
│   ├── NotificationOptions.cs                # NEW: Email/webhook config
│   └── ExchangeRateOptions.cs                # NEW: CoinGecko config
├── Data/
│   ├── Models/
│   │   ├── PaymentState.cs                   # Existing
│   │   ├── Bolt12Offer.cs                    # NEW: Bolt12 offer entity
│   │   ├── PaymentNotification.cs            # NEW: Notification record
│   │   ├── RefundTransaction.cs              # NEW: Refund entity
│   │   └── ExchangeRate.cs                   # NEW: Cached rate entity
│   ├── Migrations/
│   │   └── [timestamp]_AddBolt12AndNotifications.cs  # NEW
│   └── PaymentDbContext.cs                   # Extended with new entities
├── Services/
│   ├── Bolt12/
│   │   ├── IBolt12OfferService.cs            # NEW: Offer management
│   │   └── Bolt12OfferService.cs             # NEW
│   ├── Notification/
│   │   ├── INotificationService.cs           # NEW: Email/webhook dispatch
│   │   ├── NotificationService.cs            # NEW
│   │   ├── EmailNotificationHandler.cs       # NEW
│   │   └── WebhookNotificationHandler.cs     # NEW
│   ├── Refund/
│   │   ├── IRefundService.cs                 # NEW: Refund workflow
│   │   └── RefundService.cs                  # NEW
│   ├── ExchangeRate/
│   │   ├── IExchangeRateService.cs           # NEW: Fiat conversion
│   │   ├── ExchangeRateService.cs            # NEW: CoinGecko client
│   │   └── CoinGeckoClient.cs                # NEW
│   └── Exception/
│       └── LightningPaymentsException.cs     # NEW: Unified hierarchy
├── Api/
│   ├── Management/
│   │   ├── DashboardController.cs            # NEW: Dashboard data
│   │   ├── Bolt12Controller.cs               # NEW: Offer management
│   │   ├── NotificationController.cs         # NEW: Notification config
│   │   └── RefundController.cs               # NEW: Refund endpoints
│   └── Public/
│       └── ExchangeRateController.cs         # NEW: Fiat rates
├── Features/
│   ├── Dashboard/                            # NEW: Dashboard feature
│   │   ├── DashboardStats.cs
│   │   └── DashboardStatsService.cs
│   └── PropertyEditor/                       # NEW: Enhanced editor
│       └── PaywallPropertyEditorValueConverter.cs
├── BackofficeUI/                             # NEW: Bellissima components
│   ├── umbraco-package.json                  # Package manifest
│   ├── src/
│   │   ├── dashboard/
│   │   │   ├── lightning-dashboard.element.ts
│   │   │   ├── payment-history-table.element.ts
│   │   │   └── connection-status.element.ts
│   │   ├── property-editor/
│   │   │   └── paywall-editor.element.ts
│   │   └── shared/
│   │       ├── lightning-api-client.ts
│   │       └── types.ts
│   └── package.json
└── Composers/
    └── LightningPaymentsComposer.cs          # Extended registration

tests/Umbraco.Community.Bitcoin.LightningPayments.CoreTests/
├── Services/
│   ├── Bolt12OfferServiceTests.cs            # NEW
│   ├── NotificationServiceTests.cs           # NEW
│   ├── RefundServiceTests.cs                 # NEW
│   └── ExchangeRateServiceTests.cs           # NEW
├── Api/
│   ├── DashboardControllerTests.cs           # NEW
│   └── RefundControllerTests.cs              # NEW
└── Features/
    └── DashboardStatsServiceTests.cs         # NEW
```

**Structure Decision**: Extending existing Clean Architecture structure. New features added as:
- New service interfaces/implementations in `/Services/[Feature]/`
- New API controllers in `/Api/Management/` and `/Api/Public/`
- New Bellissima UI components in `/BackofficeUI/src/`
- New entities in `/Data/Models/` with EF Core migrations

## Complexity Tracking

> **No critical violations identified. Warnings documented above.**

| Warning | Mitigation | Timeline |
|---------|------------|----------|
| Exception hierarchy inconsistency | Introduce LightningPaymentsException base class | Wave 1 |
| Test coverage gaps | TDD mandatory for all new code | Throughout |
| Missing API contracts | OpenAPI specs created in Phase 1 | Before implementation |

---

## Post-Design Constitution Check

*GATE: Re-evaluation after Phase 1 design completion.*

### Resolved Warnings

| Original Warning | Resolution | Status |
|-----------------|------------|--------|
| V.1 Contract-First | OpenAPI specs created in `contracts/` (management-api.yaml, public-api.yaml, webhook-api.yaml) | ✅ RESOLVED |
| VII.1 Exception Hierarchy | `LightningPaymentsException` hierarchy defined in research.md | ✅ RESOLVED |
| VIII.1 Component-Based | Lit patterns documented in research.md with UUI components | ✅ RESOLVED |
| VIII.2 State Management | Context API + Management API pattern documented | ✅ RESOLVED |

### Remaining Implementation Requirements

| Gate | Requirement | Enforcement |
|------|-------------|-------------|
| III.1 Test-First | Write tests BEFORE implementation for ALL new code | Task dependencies in tasks.md |
| III.3 Coverage | Achieve 80%+ coverage for new code | CI gate in PR checks |
| VIII.3 Accessibility | WCAG 2.1 AA compliance for new UI | Manual audit before release |

### Post-Design Gate Status: ✅ PASS

All design artifacts comply with Constitution requirements. Implementation may proceed with documented constraints:

1. **TDD Mandate**: Every service, controller, and component must have failing tests before implementation code is written
2. **Contract Compliance**: API implementations must match OpenAPI specs exactly
3. **Accessibility Audit**: UI components require accessibility review before merge

---

## Generated Artifacts Summary

| Artifact | Path | Status |
|----------|------|--------|
| Implementation Plan | `plan.md` | ✅ Complete |
| Research Document | `research.md` | ✅ Complete |
| Data Model | `data-model.md` | ✅ Complete |
| Developer Quickstart | `quickstart.md` | ✅ Complete |
| Management API Contract | `contracts/management-api.yaml` | ✅ Complete |
| Public API Contract | `contracts/public-api.yaml` | ✅ Complete |
| Webhook API Contract | `contracts/webhook-api.yaml` | ✅ Complete |

---

## Next Steps

1. **Run `/speckit.tasks`** to generate implementation task breakdown
2. **Run `/speckit.checklist`** (optional) to generate quality gate checklists
3. **Run `/speckit.analyze`** to validate cross-artifact consistency
4. **Run `/speckit.implement`** to execute wave-based implementation

---

*Plan completed: 2026-01-26*
