# Feature Specification: BreezSDK Production-Ready NuGet Package

**Feature Branch**: `001-breezsdk-production-ready`
**Created**: 2026-01-24
**Status**: Draft
**Input**: User description: "Make BreezSDK wrapper production-ready as a reusable NuGet package that can be used by multiple .NET application types, with professional, robust production-ready code."

## Clarifications

### Session 2026-01-24

- Q: How long should payment records be retained? → A: Retain indefinitely (consuming app manages deletion)
- Q: How should incoming webhook requests be validated? → A: HMAC signature validation with shared secret
- Q: How should migration from existing Umbraco-coupled code be handled? → A: Clean break with migration guide (new package names, new APIs)
- Q: What should the circuit breaker default thresholds be? → A: Moderate (5 failures in 60s → 30 second break)
- Q: What NuGet package naming convention should be used? → A: Breez.Sdk.Liquid.Extensions.* (e.g., .Core, .AspNetCore, .Umbraco)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Library Consumer Integration (Priority: P1)

A developer building a .NET application (console, web API, Blazor, MAUI, or other) wants to integrate Bitcoin Lightning payment capabilities using BreezSDK without being tied to any specific CMS or framework.

**Why this priority**: This is the core value proposition - enabling any .NET developer to easily add Lightning payment functionality to their application through a simple NuGet package installation.

**Independent Test**: Can be fully tested by creating a minimal .NET console application that references the NuGet package, configures the SDK, and successfully creates a Lightning invoice.

**Acceptance Scenarios**:

1. **Given** a new .NET 8+ project, **When** the developer installs the Breez.Sdk.Liquid.Extensions.Core NuGet package and adds minimal configuration, **Then** they can create a working Lightning payment service with a single method call.

2. **Given** the NuGet package is installed, **When** the developer calls `services.AddBreezSdk(configuration)`, **Then** all required services are registered with proper lifetimes and the SDK is ready to use.

3. **Given** a configured BreezSDK service, **When** the developer creates an invoice for a specified amount, **Then** they receive a valid BOLT11 invoice string that can be paid by any Lightning wallet.

4. **Given** multiple .NET application types (ASP.NET Core, Console, MAUI), **When** the same package is referenced, **Then** it integrates seamlessly without framework-specific modifications.

---

### User Story 2 - Payment Lifecycle Management (Priority: P1)

A developer needs to manage the complete payment lifecycle including creating invoices, tracking payment status in real-time, handling payment confirmations, and querying payment history.

**Why this priority**: Payment tracking is essential for any production payment system - without it, merchants cannot verify payments or reconcile transactions.

**Independent Test**: Can be tested by creating an invoice, simulating/making a payment, and verifying the payment status transitions correctly from pending to confirmed.

**Acceptance Scenarios**:

1. **Given** a created invoice, **When** a payment is received, **Then** the system raises an event with payment details including amount, payment hash, and timestamp.

2. **Given** an in-progress payment, **When** the consuming application subscribes to payment events, **Then** they receive real-time updates as the payment progresses through states.

3. **Given** historical payments, **When** the developer queries the payment service, **Then** they receive a paginated list of payment records with filtering capabilities.

4. **Given** a payment confirmation event, **When** the application needs to verify the payment, **Then** they can query the SDK for confirmation details including preimage.

---

### User Story 3 - Secure Configuration Management (Priority: P1)

A developer needs to configure the SDK securely, with secrets (mnemonic, API keys) protected appropriately for development, staging, and production environments.

**Why this priority**: Security is non-negotiable for payment systems - misconfigured secrets can lead to fund loss.

**Independent Test**: Can be tested by configuring secrets through environment variables and Azure Key Vault references, verifying they are never logged or exposed.

**Acceptance Scenarios**:

1. **Given** a production deployment, **When** the mnemonic is configured via environment variable, **Then** the SDK retrieves it securely without logging the value.

2. **Given** configuration validation is enabled, **When** required secrets are missing or invalid, **Then** the application fails fast with a clear error message before accepting traffic.

3. **Given** secrets are configured through any supported method, **When** the application logs or serializes configuration, **Then** sensitive values are redacted.

4. **Given** a development environment, **When** using offline/mock mode, **Then** no real mnemonic is required and mock payments work without network access.

---

### User Story 4 - Extensible Persistence Layer (Priority: P2)

A developer wants to persist payment state using their preferred database technology (SQL Server, PostgreSQL, SQLite, or in-memory for testing) rather than being locked to a single provider.

**Why this priority**: Production applications have diverse database requirements - enterprise apps may require SQL Server, cloud-native apps may prefer PostgreSQL, and integration tests need in-memory storage.

**Independent Test**: Can be tested by configuring the package with different persistence providers and verifying payment state is correctly stored and retrieved from each.

**Acceptance Scenarios**:

1. **Given** the package installed with default configuration, **When** no persistence provider is explicitly configured, **Then** an in-memory provider is used (suitable for development/testing).

2. **Given** SQL Server is the application's database, **When** the developer configures `AddBreezSdkSqlServer(connectionString)`, **Then** payment state persists to SQL Server tables.

3. **Given** multiple persistence options available, **When** the developer reviews the package documentation, **Then** they find clear examples for each supported provider.

4. **Given** a custom database technology, **When** the developer implements `IPaymentRepository`, **Then** the SDK correctly uses their implementation.

---

### User Story 5 - Robust Error Handling and Resilience (Priority: P2)

A developer needs confidence that the SDK wrapper handles network failures, SDK errors, and edge cases gracefully without crashing their application or losing payment data.

**Why this priority**: Production payment systems must be reliable - network issues, SDK version incompatibilities, and transient failures should be handled gracefully.

**Independent Test**: Can be tested by simulating network failures, SDK disconnections, and verifying the system degrades gracefully and recovers automatically.

**Acceptance Scenarios**:

1. **Given** a network disconnection during payment processing, **When** connectivity is restored, **Then** the SDK automatically reconnects and resumes processing without data loss.

2. **Given** the SDK encounters a retryable error, **When** invoking a payment operation, **Then** the system automatically retries with exponential backoff before failing.

3. **Given** any SDK operation fails, **When** the error is caught by consuming code, **Then** a well-typed exception with clear error category (retryable, permanent, configuration) is thrown.

4. **Given** the SDK is temporarily unavailable, **When** health checks query the system, **Then** accurate health status is reported to orchestrators (Kubernetes, load balancers).

---

### User Story 6 - Observability and Diagnostics (Priority: P2)

A developer needs to monitor the SDK's behavior in production including payment metrics, operation latencies, error rates, and distributed tracing integration.

**Why this priority**: Production systems require observability for debugging issues, monitoring performance, and meeting SLA requirements.

**Independent Test**: Can be tested by running the SDK with OpenTelemetry exporters and verifying metrics, traces, and logs are correctly emitted.

**Acceptance Scenarios**:

1. **Given** OpenTelemetry is configured in the application, **When** payment operations execute, **Then** spans are created with relevant tags (operation type, payment hash, amount).

2. **Given** the SDK is processing payments, **When** the metrics endpoint is queried, **Then** counters for payments created/confirmed/failed and histograms for operation durations are available.

3. **Given** structured logging is enabled, **When** SDK operations occur, **Then** log entries include correlation IDs, operation context, and sanitized payment details (no secrets).

4. **Given** a distributed system with multiple services, **When** payment flows span services, **Then** trace context propagates correctly for end-to-end visibility.

---

### User Story 7 - Platform-Specific Integrations (Priority: P3)

Developers using specific platforms (ASP.NET Core, Umbraco, or other CMS) want optional packages that provide deeper framework integration while keeping the core SDK platform-agnostic.

**Why this priority**: While the core must be platform-agnostic, providing platform-specific conveniences increases adoption and developer experience.

**Independent Test**: Can be tested by installing the Umbraco-specific package and verifying backoffice integration, property editors, and CMS-specific features work correctly.

**Acceptance Scenarios**:

1. **Given** an ASP.NET Core application, **When** installing the ASP.NET Core integration package, **Then** webhook endpoints, health checks, and middleware are automatically configured.

2. **Given** an Umbraco CMS application, **When** installing the Umbraco integration package, **Then** composers, property editors, and backoffice UI are available.

3. **Given** a platform-specific package is installed, **When** the core package is also present, **Then** they work together without conflicts or duplicate registrations.

---

### Edge Cases

- What happens when the SDK is not yet connected and an operation is attempted?
  - Operations wait for connection with configurable timeout, then throw descriptive exception.
- How does the system handle payment amounts at the minimum/maximum thresholds?
  - SDK enforces limits with clear validation errors before attempting operations.
- What happens when the same invoice is paid twice (overpayment)?
  - Events are raised for each payment; consuming code decides business logic.
- How does the system behave when storage is unavailable?
  - Operations requiring persistence fail with storage-specific exceptions; SDK operations continue.
- What happens when configuration changes at runtime?
  - Core SDK configuration is immutable after startup; runtime settings can be updated.
- How does the system handle SDK version mismatches?
  - Package includes version compatibility checks with clear upgrade guidance.

## Requirements *(mandatory)*

### Functional Requirements

**Core SDK Wrapper**
- **FR-001**: Package MUST provide platform-agnostic BreezSDK integration via a single NuGet package.
- **FR-002**: Package MUST support .NET 8 and higher as the minimum target framework.
- **FR-003**: Package MUST provide extension methods for dependency injection registration.
- **FR-004**: Package MUST expose all BreezSDK capabilities through well-typed .NET interfaces.
- **FR-005**: Package MUST support both connected (live network) and offline (development/testing) modes via configuration.

**Invoice and Payment Operations**
- **FR-006**: System MUST support creating BOLT11 invoices with configurable amount, description, and expiry.
- **FR-007**: System MUST support parsing and validating BOLT11 invoices and Lightning addresses.
- **FR-008**: System MUST support sending payments to BOLT11 invoices and Lightning addresses. *(Note: v1 focuses on receiving payments; send payment implementation is foundational but secondary priority)*
- **FR-009**: System MUST provide real-time payment event notifications via .NET events or channels.
- **FR-010**: System MUST support querying payment history with filtering and pagination.

**Persistence Layer**
- **FR-011**: Package MUST define a payment repository interface for pluggable persistence.
- **FR-012**: Package MUST provide in-memory implementation as default for development/testing.
- **FR-013**: Package MUST support extension packages for SQL Server, PostgreSQL, and SQLite persistence.
- **FR-014**: Payment state MUST include: payment hash, status, amount, timestamps, and metadata.

**Configuration and Security**
- **FR-015**: Configuration MUST support standard .NET configuration sources (appsettings, environment variables, user secrets, Azure Key Vault).
- **FR-016**: System MUST validate configuration at startup and fail fast with clear error messages.
- **FR-017**: Sensitive configuration (mnemonic, API keys) MUST be redacted in logs and diagnostics.
- **FR-018**: System MUST support secure working directory configuration with appropriate file permissions.
- **FR-018a**: Webhook endpoints MUST validate incoming requests using HMAC signature verification with a shared secret.

**Resilience and Reliability**
- **FR-019**: SDK operations MUST implement retry policies with configurable backoff for transient failures.
- **FR-020**: System MUST provide circuit breaker pattern to prevent cascade failures during SDK issues. Default thresholds: 5 failures in 60 seconds triggers a 30-second break; thresholds are configurable.
- **FR-021**: System MUST expose health check implementations compatible with .NET health check infrastructure.
- **FR-022**: System MUST handle SDK disconnection/reconnection gracefully without losing pending payments.

**Observability**
- **FR-023**: System MUST emit OpenTelemetry traces for all SDK operations with relevant attributes.
- **FR-024**: System MUST emit OpenTelemetry metrics for payment counts, amounts, and operation latencies.
- **FR-025**: System MUST use structured logging with correlation IDs and sanitized payment context.
- **FR-026**: System MUST support activity/trace context propagation for distributed tracing.

**Extensibility**
- **FR-027**: Package MUST support dependency injection of custom implementations for all core interfaces.
- **FR-028**: Package MUST provide clear extension points for custom event handlers and middleware.
- **FR-029**: Package MUST support platform-specific integration packages as separate NuGet packages.

### Key Entities

- **PaymentState**: Represents the current state of a payment including hash, status (pending, confirmed, failed, expired), amount in satoshis, creation timestamp, confirmation timestamp, and custom metadata dictionary. Records are retained indefinitely; consuming applications manage their own deletion/archival policies.

- **Invoice**: Represents a BOLT11 invoice with properties for invoice string, payment hash, amount, description, expiry time, and creation timestamp.

- **PaymentEvent**: Represents a payment lifecycle event with event type (created, received, confirmed, failed), payment hash, amount, timestamp, and optional error details.

- **SdkConfiguration**: Represents SDK configuration with mnemonic reference (not value), network type (mainnet, testnet, signet), API key reference, working directory path, and feature flags.

- **OperationResult<T>**: Represents the outcome of an SDK operation with success/failure status, result value, error category, error message, and retry eligibility.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Developers can integrate Lightning payments into a new .NET project in under 15 minutes with no prior BreezSDK experience.

- **SC-002**: Package supports at least 3 .NET application types (Console, ASP.NET Core, MAUI) with identical core API usage patterns.

- **SC-003**: 95% of SDK operations complete successfully within 5 seconds under normal network conditions.

- **SC-004**: System maintains payment state consistency even when process restarts occur during payment processing.

- **SC-005**: All sensitive configuration values are provably never written to logs (verified via log output analysis).

- **SC-006**: Health check endpoint correctly reports unhealthy status within 10 seconds of SDK disconnection.

- **SC-007**: Unit test coverage for core package exceeds 80% line coverage with all critical paths tested.

- **SC-008**: Integration tests verify end-to-end payment flow on testnet completes successfully.

- **SC-009**: Package documentation includes working examples for each supported .NET application type.

- **SC-010**: NuGet package passes all security scans with no high or critical vulnerabilities.

## Assumptions

- The consuming application uses .NET 8 or higher.
- Developers have basic familiarity with .NET dependency injection patterns.
- Network connectivity to BreezSDK infrastructure is generally available (transient failures handled).
- The mnemonic seed is securely stored and managed by the consuming application.
- SQLite is acceptable for single-instance deployments; multi-instance requires SQL Server/PostgreSQL.
- The existing BreezSDK Liquid .NET bindings (`Breez.Sdk.Liquid`) are stable and feature-complete.
- Platform-specific integrations (Umbraco, etc.) remain in separate packages to keep core dependencies minimal.
- Package naming follows `Breez.Sdk.Liquid.Extensions.*` convention (e.g., `.Core`, `.AspNetCore`, `.Umbraco`, `.SqlServer`, `.PostgreSQL`).

## Scope Boundaries

### In Scope

- Core SDK wrapper as platform-agnostic NuGet package
- Persistence abstractions and in-memory implementation
- SQL Server and PostgreSQL persistence packages
- ASP.NET Core integration package (health checks, middleware)
- Umbraco integration package (migration of existing functionality)
- Comprehensive test suite (unit and integration)
- OpenTelemetry instrumentation
- Package documentation and examples
- Migration guide documenting old → new API mappings (clean break, no backward compatibility)

### Out of Scope

- Mobile-specific optimizations (background service restrictions on iOS/Android)
- Custom wallet UI components
- Multi-tenancy support (single SDK instance per application)
- On-chain Bitcoin operations (Lightning-only)
- Payment dispute resolution or refund workflows
- Fiat currency conversion or pricing
- Admin dashboard or backoffice UI (beyond existing Umbraco integration)

## Dependencies

- `Breez.Sdk.Liquid` - Core BreezSDK Liquid bindings
- `Polly` - Resilience and transient fault handling
- `OpenTelemetry.Api` - Observability instrumentation
- `Microsoft.Extensions.DependencyInjection` - DI abstractions
- `Microsoft.Extensions.Configuration` - Configuration abstractions
- `Microsoft.Extensions.Logging` - Logging abstractions
- `Microsoft.Extensions.Options` - Options pattern
