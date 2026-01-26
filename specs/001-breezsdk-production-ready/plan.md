# Implementation Plan: BreezSDK Production-Ready NuGet Package

**Branch**: `001-breezsdk-production-ready` | **Date**: 2026-01-24 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-breezsdk-production-ready/spec.md`

## Summary

Transform the existing Umbraco-coupled BreezSDK wrapper into a production-ready, platform-agnostic NuGet package suite (`Breez.Sdk.Liquid.Extensions.*`) that any .NET 8+ application can use for Lightning payment integration. The approach extracts core functionality into a reusable package while providing platform-specific integration packages for ASP.NET Core and Umbraco.

## Technical Context

**Language/Version**: C# / .NET 8.0 (minimum), .NET 9.0 (current development)
**Primary Dependencies**:
- `Breez.Sdk.Liquid` 0.11.9 - Core SDK bindings
- `Polly` 8.x - Resilience and retry policies
- `OpenTelemetry.Api` 1.x - Observability instrumentation
- `Microsoft.Extensions.*` 8.x/9.x - DI, Configuration, Logging, Options
- `Microsoft.EntityFrameworkCore` 8.x/9.x - Persistence abstractions

**Storage**:
- Default: In-memory (development/testing)
- Production: SQL Server, PostgreSQL, SQLite (via extension packages)
- Existing: SQLite with EF Core 9.0

**Testing**:
- xUnit 2.9.x - Test framework
- Moq 4.20.x - Mocking
- Coverlet - Code coverage
- In-memory EF Core - Database testing

**Target Platform**:
- Primary: .NET 8.0+ server applications
- Supported: Console, ASP.NET Core, Blazor, MAUI (core package)
- Extended: Umbraco 16+ (integration package)

**Project Type**: Multi-package solution (library packages)

**Performance Goals**:
- SDK operations complete within 5 seconds (95th percentile) under normal conditions
- Health check responds within 10 seconds of SDK state change
- Invoice creation under 2 seconds typical

**Constraints**:
- Single SDK instance per application (no multi-tenancy)
- Lightning-only operations (no on-chain in v1)
- Mnemonic management by consuming application
- Network connectivity required for live mode

**Scale/Scope**:
- Target: 10k+ NuGet downloads
- Support: Multiple .NET application types
- Package count: 5-7 packages in suite

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Research Gate Evaluation

| Article | Principle | Status | Notes |
|---------|-----------|--------|-------|
| I.1 | Clean Architecture Mandate | PASS | Existing code follows layered architecture; new packages will enforce Domain → Application → Infrastructure |
| I.2 | Domain-Driven Design | PASS | Domain entities (PaymentState, Invoice, PaymentEvent) express business concepts |
| I.3 | Modular Decomposition | PASS | Package suite designed for independent deployment and loose coupling |
| II.1 | Single Responsibility | PASS | Each package has single purpose; interfaces focused |
| II.2 | Interface Segregation | PASS | Existing interfaces are cohesive (IBreezSdkService, IPaymentRepository) |
| II.3 | Explicit Over Implicit | PASS | Constructor injection, IOptions<T> pattern, explicit configuration |
| II.4 | Self-Documenting Code | PASS | XML documentation, clear naming conventions |
| III.1 | Test-First Imperative | **REQUIRED** | Must write tests before implementation |
| III.2 | Testing Pyramid | PASS | Existing test structure follows pyramid |
| III.3 | Meaningful Coverage | **REQUIRED** | Target 80%+ coverage |
| III.4 | Automated Validation | PASS | `dotnet test` runs full suite |
| IV.1 | Repository Pattern | PASS | IPaymentRepository abstraction exists |
| IV.2 | Migration-First Schema | PASS | EF Core migrations in use |
| IV.3 | Query Optimization | PASS | AsNoTracking for reads, explicit loading |
| V.1 | Contract-First Development | PASS | API contracts will be defined in `/contracts/` |
| V.2 | RESTful Resource Design | PASS | Webhook endpoints follow REST principles |
| V.3 | Versioning Strategy | PASS | Package versioning via SemVer; API versioning in ASP.NET Core package |
| VI.1 | Defence in Depth | PASS | HMAC webhook validation, secrets redaction |
| VI.2 | Auth Separation | N/A | Package consumers handle auth |
| VI.3 | Secrets Management | PASS | IOptions<T> binding, never logged |
| VI.4 | Input Validation | PASS | Data annotations, FluentValidation-compatible |
| VII.1 | Structured Exceptions | PASS | Custom exception hierarchy exists |
| VII.2 | Comprehensive Logging | PASS | ILogger<T>, correlation IDs, sanitization |
| VII.3 | Health & Diagnostics | PASS | BreezSdkHealthCheck, OpenTelemetry integration |
| IX.1 | YAGNI | PASS | Only extracting existing functionality |
| IX.2 | Maximum Project Limit | **WARNING** | 5-7 packages justified by platform separation (see Complexity Tracking) |
| IX.3 | Dependency Scrutiny | PASS | Dependencies justified, framework-native where possible |
| X.1 | Living Documentation | PASS | XML docs on public APIs |
| X.2 | ADRs | **REQUIRED** | Document package structure decision |
| X.3 | Runbook Requirements | PASS | Migration guide serves as operational runbook |
| XI.1 | Baseline Metrics | PASS | Performance goals defined in spec |
| XI.2 | Async-First | PASS | All SDK operations async |
| XI.3 | Caching Strategy | PASS | Explicit TTLs for payment state |
| XII.1 | Task Independence | PASS | Package extraction enables parallel development |
| XII.2 | Incremental Validation | PASS | Tests per package |
| XII.3 | Context Efficiency | PASS | Sub-agents for specialized packages |
| XII.4 | Deterministic Automation | PASS | `dotnet build`, `dotnet test`, `dotnet format` |
| XIII.1 | Constitution Supremacy | PASS | This plan follows constitution |

**Overall Status**: PASS with documented justifications

## Project Structure

### Documentation (this feature)

```text
specs/001-breezsdk-production-ready/
├── plan.md              # This file
├── research.md          # Phase 0 output - technology decisions
├── data-model.md        # Phase 1 output - entity definitions
├── quickstart.md        # Phase 1 output - integration guide
├── contracts/           # Phase 1 output - API contracts
│   ├── webhook-api.yaml # Webhook endpoint OpenAPI spec
│   └── events.md        # Event schema documentation
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
# NuGet Package Suite Structure
src/
├── Breez.Sdk.Liquid.Extensions.Core/           # Core package - platform agnostic
│   ├── Abstractions/                           # Public interfaces
│   │   ├── IBreezSdkService.cs
│   │   ├── IBreezSdkWrapper.cs
│   │   ├── IPaymentRepository.cs
│   │   ├── IPaymentEventHandler.cs
│   │   └── IBreezHealthCheck.cs
│   ├── Configuration/                          # Settings and validation
│   │   ├── BreezSdkOptions.cs
│   │   ├── BreezSdkOptionsValidator.cs
│   │   └── ConfigurationConstants.cs
│   ├── Domain/                                 # Domain entities
│   │   ├── PaymentState.cs
│   │   ├── PaymentEvent.cs
│   │   ├── Invoice.cs
│   │   └── OperationResult.cs
│   ├── Exceptions/                             # Exception hierarchy
│   │   ├── BreezSdkException.cs
│   │   ├── PaymentException.cs
│   │   ├── ConfigurationException.cs
│   │   └── TransientException.cs
│   ├── Extensions/                             # DI registration
│   │   └── ServiceCollectionExtensions.cs
│   ├── Infrastructure/                         # Internal implementations
│   │   ├── BreezSdkService.cs
│   │   ├── BreezSdkWrapper.cs
│   │   ├── OfflineBreezSdkService.cs
│   │   └── ResiliencePolicies.cs
│   ├── Observability/                          # Telemetry
│   │   ├── BreezSdkMetrics.cs
│   │   └── ActivitySources.cs
│   └── Persistence/                            # Repository implementations
│       └── InMemoryPaymentRepository.cs
│
├── Breez.Sdk.Liquid.Extensions.AspNetCore/     # ASP.NET Core integration
│   ├── Extensions/
│   │   └── WebApplicationBuilderExtensions.cs
│   ├── HealthChecks/
│   │   └── BreezSdkHealthCheck.cs
│   ├── Middleware/
│   │   └── WebhookValidationMiddleware.cs
│   └── Endpoints/
│       └── WebhookEndpoints.cs
│
├── Breez.Sdk.Liquid.Extensions.SqlServer/      # SQL Server persistence
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs
│   ├── Data/
│   │   ├── SqlServerPaymentDbContext.cs
│   │   └── SqlServerPaymentRepository.cs
│   └── Migrations/
│
├── Breez.Sdk.Liquid.Extensions.PostgreSql/     # PostgreSQL persistence
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs
│   ├── Data/
│   │   ├── PostgreSqlPaymentDbContext.cs
│   │   └── PostgreSqlPaymentRepository.cs
│   └── Migrations/
│
├── Breez.Sdk.Liquid.Extensions.Sqlite/         # SQLite persistence
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs
│   ├── Data/
│   │   ├── SqlitePaymentDbContext.cs
│   │   └── SqlitePaymentRepository.cs
│   └── Migrations/
│
└── Breez.Sdk.Liquid.Extensions.Umbraco/        # Umbraco CMS integration
    ├── Composers/
    │   └── BreezSdkComposer.cs
    ├── Components/
    │   └── BreezSdkComponent.cs
    ├── PropertyEditors/                        # (future - backoffice UI)
    └── Extensions/
        └── UmbracoBuilderExtensions.cs

tests/
├── Breez.Sdk.Liquid.Extensions.Core.Tests/
│   ├── Services/
│   ├── Domain/
│   ├── Configuration/
│   └── Persistence/
│
├── Breez.Sdk.Liquid.Extensions.AspNetCore.Tests/
│   ├── Middleware/
│   ├── HealthChecks/
│   └── Endpoints/
│
├── Breez.Sdk.Liquid.Extensions.Integration.Tests/
│   ├── SqlServer/
│   ├── PostgreSql/
│   └── EndToEnd/
│
└── Breez.Sdk.Liquid.Extensions.TestUtilities/  # Shared test helpers
    ├── Builders/
    │   └── MockBreezSdkBuilder.cs
    ├── Fakes/
    │   └── FakeBreezSdkWrapper.cs
    └── Extensions/
        └── TestServiceCollectionExtensions.cs
```

**Structure Decision**: Multi-package solution chosen to enable:
1. Platform-agnostic core for maximum reusability
2. Optional persistence packages (pay for what you use)
3. Platform-specific integrations without polluting core
4. Independent versioning and deployment cycles
5. Minimal dependency footprint per consuming application

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| IX.2: 7 packages vs minimum | Platform separation, optional features, independent persistence | Single package would force all dependencies; 3-package minimum (Core, AspNetCore, Persistence) still requires database provider separation for enterprise requirements |
| X.2: ADR required | Package structure is significant architectural decision | Inline comments insufficient for cross-project decision |

## Wave Execution Strategy

### Wave 1: Foundation (Sequential)
- Create solution structure and project files
- Define public abstractions (interfaces)
- Implement domain entities
- Set up test infrastructure

### Wave 2: Core Implementation (Parallel)
- [P] Extract BreezSdkService to Core package
- [P] Implement InMemoryPaymentRepository
- [P] Implement configuration validation
- [P] Create exception hierarchy

### Wave 3: Platform Integrations (Parallel)
- [P] ASP.NET Core health checks and middleware
- [P] SQLite persistence package
- [P] SQL Server persistence package
- [P] PostgreSQL persistence package

### Wave 4: Umbraco Migration (Sequential)
- Extract Umbraco-specific code to integration package
- Create migration guide
- Update existing project to consume new packages

### Wave 5: Documentation & Polish (Parallel)
- [P] Quickstart documentation
- [P] API reference generation
- [P] Sample applications
- [P] NuGet packaging and publishing setup

## Agent Assignments

| Wave | Task Type | Agent | Rationale |
|------|-----------|-------|-----------|
| 1 | Architecture setup | solution-architect | Complex structure decisions |
| 1 | Project scaffolding | backend-developer | .csproj creation, solution structure |
| 2 | Core service extraction | breezsdk-developer | SDK-specific patterns |
| 2 | Repository implementation | backend-developer | EF Core patterns |
| 2 | Configuration system | backend-developer | Options pattern |
| 2 | Unit tests | breezsdk-test-engineer | SDK mocking expertise |
| 3 | ASP.NET Core | backend-developer | Middleware patterns |
| 3 | Persistence packages | database-architect | EF Core migrations |
| 3 | Integration tests | test-engineer | Cross-package testing |
| 4 | Umbraco extraction | backend-developer | CMS patterns |
| 4 | Migration guide | breezsdk-architect | Architecture documentation |
| 5 | Documentation | solution-architect | API design review |
| All | Quality review | code-reviewer, breezsdk-reviewer | Constitution compliance |
| All | Security review | security-auditor | OWASP checks |

## Dependencies

External packages (justified per IX.3):

| Package | Version | Justification |
|---------|---------|---------------|
| `Breez.Sdk.Liquid` | 0.11.9 | Core SDK bindings - required |
| `Polly` | 8.x | Industry-standard resilience - no .NET native equivalent |
| `OpenTelemetry.Api` | 1.x | Observability standard - better than custom telemetry |
| `Microsoft.Extensions.*` | 8.x+ | Framework-native DI, config, logging |
| `Microsoft.EntityFrameworkCore` | 8.x+ | Framework-native ORM |

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Breaking changes in Breez.Sdk.Liquid | Medium | High | Pin to specific version; abstraction layer |
| Multi-tenancy requests | Medium | Medium | Document limitation; design for future extension |
| Performance regression | Low | High | Baseline metrics; performance tests |
| Complex migration for existing users | High | Medium | Detailed migration guide; compatibility shims |

## Success Metrics

Per specification SC-001 through SC-010:
- [ ] 15-minute integration (SC-001)
- [ ] 3+ application types supported (SC-002)
- [ ] 95% operations under 5s (SC-003)
- [ ] Payment state consistency (SC-004)
- [ ] No secrets in logs (SC-005)
- [ ] Health check accuracy (SC-006)
- [ ] 80%+ test coverage (SC-007)
- [ ] Testnet E2E success (SC-008)
- [ ] Working examples per platform (SC-009)
- [ ] No security vulnerabilities (SC-010)
