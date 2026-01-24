# Tasks: BreezSDK Production-Ready NuGet Package

**Input**: Design documents from `/specs/001-breezsdk-production-ready/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Per Constitution Article III (Test-First Imperative), tests are **MANDATORY** and must be written BEFORE implementation. No production code may be written until corresponding tests exist and fail (Red-Green-Refactor). This is NON-NEGOTIABLE.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Based on plan.md structure:
- **Source packages**: `src/Breez.Sdk.Liquid.Extensions.*/`
- **Test packages**: `tests/Breez.Sdk.Liquid.Extensions.*.Tests/`
- **Integration tests**: `tests/Breez.Sdk.Liquid.Extensions.Integration.Tests/`
- **Test utilities**: `tests/Breez.Sdk.Liquid.Extensions.TestUtilities/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Solution and project initialization

- [x] T001 Create solution file `Breez.Sdk.Liquid.Extensions.sln` at repository root
- [x] T002 Create Core package project `src/Breez.Sdk.Liquid.Extensions.Core/Breez.Sdk.Liquid.Extensions.Core.csproj` with net8.0;net9.0 targets
- [x] T003 [P] Create AspNetCore package project `src/Breez.Sdk.Liquid.Extensions.AspNetCore/Breez.Sdk.Liquid.Extensions.AspNetCore.csproj`
- [x] T004 [P] Create SqlServer package project `src/Breez.Sdk.Liquid.Extensions.SqlServer/Breez.Sdk.Liquid.Extensions.SqlServer.csproj`
- [x] T005 [P] Create PostgreSql package project `src/Breez.Sdk.Liquid.Extensions.PostgreSql/Breez.Sdk.Liquid.Extensions.PostgreSql.csproj`
- [x] T006 [P] Create Sqlite package project `src/Breez.Sdk.Liquid.Extensions.Sqlite/Breez.Sdk.Liquid.Extensions.Sqlite.csproj`
- [x] T007 [P] Create Umbraco package project `src/Breez.Sdk.Liquid.Extensions.Umbraco/Breez.Sdk.Liquid.Extensions.Umbraco.csproj`
- [x] T008 [P] Create Core.Tests project `tests/Breez.Sdk.Liquid.Extensions.Core.Tests/Breez.Sdk.Liquid.Extensions.Core.Tests.csproj`
- [x] T009 [P] Create AspNetCore.Tests project `tests/Breez.Sdk.Liquid.Extensions.AspNetCore.Tests/Breez.Sdk.Liquid.Extensions.AspNetCore.Tests.csproj`
- [x] T010 [P] Create Integration.Tests project `tests/Breez.Sdk.Liquid.Extensions.Integration.Tests/Breez.Sdk.Liquid.Extensions.Integration.Tests.csproj`
- [x] T011 [P] Create TestUtilities project `tests/Breez.Sdk.Liquid.Extensions.TestUtilities/Breez.Sdk.Liquid.Extensions.TestUtilities.csproj`
- [x] T012 Configure Directory.Build.props for shared package metadata in repository root
- [x] T013 Configure Directory.Packages.props for central package versioning in repository root
- [x] T014 [P] Add .editorconfig for code style enforcement in repository root
- [x] T015 [P] Configure NuGet package properties in Directory.Build.props (PackageId, Authors, License, etc.)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**CRITICAL**: No user story work can begin until this phase is complete

### Domain Layer (Core Package)

- [ ] T016 [P] Create PaymentStatus enum in `src/Breez.Sdk.Liquid.Extensions.Core/Domain/PaymentStatus.cs`
- [ ] T017 [P] Create PaymentKind enum in `src/Breez.Sdk.Liquid.Extensions.Core/Domain/PaymentKind.cs`
- [ ] T018 [P] Create BreezErrorCode enum in `src/Breez.Sdk.Liquid.Extensions.Core/Domain/BreezErrorCode.cs`
- [ ] T019 [P] Create InvoiceType enum in `src/Breez.Sdk.Liquid.Extensions.Core/Domain/InvoiceType.cs`
- [ ] T020 [P] Create PaymentState entity in `src/Breez.Sdk.Liquid.Extensions.Core/Domain/PaymentState.cs`
- [ ] T021 [P] Create Invoice record in `src/Breez.Sdk.Liquid.Extensions.Core/Domain/Invoice.cs`
- [ ] T022 Create OperationResult<T> and OperationError in `src/Breez.Sdk.Liquid.Extensions.Core/Domain/OperationResult.cs`

### Event Models (Core Package)

- [ ] T023 Create PaymentEvent base record in `src/Breez.Sdk.Liquid.Extensions.Core/Domain/Events/PaymentEvent.cs`
- [ ] T024 [P] Create InvoiceCreated event in `src/Breez.Sdk.Liquid.Extensions.Core/Domain/Events/InvoiceCreated.cs`
- [ ] T025 [P] Create PaymentReceived event in `src/Breez.Sdk.Liquid.Extensions.Core/Domain/Events/PaymentReceived.cs`
- [ ] T026 [P] Create PaymentConfirmed event in `src/Breez.Sdk.Liquid.Extensions.Core/Domain/Events/PaymentConfirmed.cs`
- [ ] T027 [P] Create PaymentFailed event in `src/Breez.Sdk.Liquid.Extensions.Core/Domain/Events/PaymentFailed.cs`
- [ ] T028 [P] Create InvoiceExpired event in `src/Breez.Sdk.Liquid.Extensions.Core/Domain/Events/InvoiceExpired.cs`

### Exception Hierarchy (Core Package)

- [ ] T029 Create BreezSdkException base in `src/Breez.Sdk.Liquid.Extensions.Core/Exceptions/BreezSdkException.cs`
- [ ] T030 [P] Create ConfigurationException in `src/Breez.Sdk.Liquid.Extensions.Core/Exceptions/ConfigurationException.cs`
- [ ] T031 [P] Create ConnectionException in `src/Breez.Sdk.Liquid.Extensions.Core/Exceptions/ConnectionException.cs`
- [ ] T032 [P] Create PaymentException in `src/Breez.Sdk.Liquid.Extensions.Core/Exceptions/PaymentException.cs`
- [ ] T033 [P] Create TransientException in `src/Breez.Sdk.Liquid.Extensions.Core/Exceptions/TransientException.cs`

### Abstractions (Core Package)

- [ ] T034 Create IBreezSdkService interface in `src/Breez.Sdk.Liquid.Extensions.Core/Abstractions/IBreezSdkService.cs`
- [ ] T035 [P] Create IBreezSdkWrapper interface in `src/Breez.Sdk.Liquid.Extensions.Core/Abstractions/IBreezSdkWrapper.cs`
- [ ] T036 [P] Create IPaymentRepository interface in `src/Breez.Sdk.Liquid.Extensions.Core/Abstractions/IPaymentRepository.cs`
- [ ] T037 [P] Create IPaymentEventHandler interface in `src/Breez.Sdk.Liquid.Extensions.Core/Abstractions/IPaymentEventHandler.cs`
- [ ] T038 [P] Create IBreezHealthCheck interface in `src/Breez.Sdk.Liquid.Extensions.Core/Abstractions/IBreezHealthCheck.cs`

### Test Utilities (Shared)

- [ ] T039 Create MockBreezSdkBuilder in `tests/Breez.Sdk.Liquid.Extensions.TestUtilities/Builders/MockBreezSdkBuilder.cs`
- [ ] T040 [P] Create FakeBreezSdkWrapper in `tests/Breez.Sdk.Liquid.Extensions.TestUtilities/Fakes/FakeBreezSdkWrapper.cs`
- [ ] T041 [P] Create TestServiceCollectionExtensions in `tests/Breez.Sdk.Liquid.Extensions.TestUtilities/Extensions/TestServiceCollectionExtensions.cs`
- [ ] T042 [P] Create PaymentStateBuilder test helper in `tests/Breez.Sdk.Liquid.Extensions.TestUtilities/Builders/PaymentStateBuilder.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Library Consumer Integration (Priority: P1) MVP

**Goal**: Enable any .NET 8+ developer to add Lightning payment capabilities via a simple NuGet package installation and configuration.

**Independent Test**: Create a minimal .NET console application that references the NuGet package, configures the SDK, and successfully creates a Lightning invoice.

### Tests for User Story 1 (MANDATORY - Constitution Article III)

> **TDD REQUIRED: Write tests FIRST, verify they FAIL (RED), then implement until GREEN**

- [ ] T043 [P] [US1] Unit tests for BreezSdkOptions validation in `tests/Breez.Sdk.Liquid.Extensions.Core.Tests/Configuration/BreezSdkOptionsValidatorTests.cs`
- [ ] T044 [P] [US1] Unit tests for ServiceCollectionExtensions in `tests/Breez.Sdk.Liquid.Extensions.Core.Tests/Extensions/ServiceCollectionExtensionsTests.cs`
- [ ] T045 [P] [US1] Unit tests for BreezSdkService invoice creation in `tests/Breez.Sdk.Liquid.Extensions.Core.Tests/Services/BreezSdkServiceTests.cs`
- [ ] T046 [P] [US1] Unit tests for BreezSdkWrapper in `tests/Breez.Sdk.Liquid.Extensions.Core.Tests/Infrastructure/BreezSdkWrapperTests.cs`
- [ ] T047 [P] [US1] Unit tests for OfflineBreezSdkService in `tests/Breez.Sdk.Liquid.Extensions.Core.Tests/Services/OfflineBreezSdkServiceTests.cs`

### Configuration (Core Package)

- [ ] T048 [US1] Create BreezNetwork enum in `src/Breez.Sdk.Liquid.Extensions.Core/Configuration/BreezNetwork.cs`
- [ ] T049 [US1] Create BreezSdkOptions in `src/Breez.Sdk.Liquid.Extensions.Core/Configuration/BreezSdkOptions.cs`
- [ ] T050 [US1] Create CircuitBreakerOptions in `src/Breez.Sdk.Liquid.Extensions.Core/Configuration/CircuitBreakerOptions.cs`
- [ ] T051 [US1] Create BreezSdkOptionsValidator in `src/Breez.Sdk.Liquid.Extensions.Core/Configuration/BreezSdkOptionsValidator.cs`
- [ ] T052 [US1] Create ConfigurationConstants in `src/Breez.Sdk.Liquid.Extensions.Core/Configuration/ConfigurationConstants.cs`

### Infrastructure (Core Package)

- [ ] T053 [US1] Create BreezSdkWrapper implementation in `src/Breez.Sdk.Liquid.Extensions.Core/Infrastructure/BreezSdkWrapper.cs`
- [ ] T054 [US1] Create BreezSdkService implementation in `src/Breez.Sdk.Liquid.Extensions.Core/Infrastructure/BreezSdkService.cs`
- [ ] T055 [US1] Create OfflineBreezSdkService (mock mode) in `src/Breez.Sdk.Liquid.Extensions.Core/Infrastructure/OfflineBreezSdkService.cs`
- [ ] T056 [US1] Create ResiliencePolicies with Polly pipelines in `src/Breez.Sdk.Liquid.Extensions.Core/Infrastructure/ResiliencePolicies.cs`

### DI Registration (Core Package)

- [ ] T057 [US1] Create ServiceCollectionExtensions with AddBreezSdk() in `src/Breez.Sdk.Liquid.Extensions.Core/Extensions/ServiceCollectionExtensions.cs`
- [ ] T058 [US1] Add AddBreezSdkOffline() extension for development mode in `src/Breez.Sdk.Liquid.Extensions.Core/Extensions/ServiceCollectionExtensions.cs`

**Checkpoint**: User Story 1 complete - developers can integrate BreezSDK into any .NET 8+ application

---

## Phase 4: User Story 2 - Payment Lifecycle Management (Priority: P1)

**Goal**: Enable complete payment lifecycle management including invoices, status tracking, events, and history queries.

**Independent Test**: Create an invoice, simulate/make a payment, verify status transitions from pending to confirmed via event handling.

### Tests for User Story 2 (MANDATORY - Constitution Article III)

> **TDD REQUIRED: Write tests FIRST, verify they FAIL (RED), then implement until GREEN**

- [ ] T059 [P] [US2] Unit tests for InMemoryPaymentRepository in `tests/Breez.Sdk.Liquid.Extensions.Core.Tests/Persistence/InMemoryPaymentRepositoryTests.cs`
- [ ] T060 [P] [US2] Unit tests for payment event handling in `tests/Breez.Sdk.Liquid.Extensions.Core.Tests/Services/PaymentEventProcessorTests.cs`
- [ ] T061 [P] [US2] Unit tests for payment queries in `tests/Breez.Sdk.Liquid.Extensions.Core.Tests/Services/BreezSdkServiceQueryTests.cs`
- [ ] T062 [P] [US2] Unit tests for event channel distribution in `tests/Breez.Sdk.Liquid.Extensions.Core.Tests/Infrastructure/PaymentEventChannelTests.cs`

### Persistence (Core Package)

- [ ] T063 [US2] Create InMemoryPaymentRepository in `src/Breez.Sdk.Liquid.Extensions.Core/Persistence/InMemoryPaymentRepository.cs`

### Event System (Core Package)

- [ ] T064 [US2] Create PaymentEventChannel using System.Threading.Channels in `src/Breez.Sdk.Liquid.Extensions.Core/Infrastructure/PaymentEventChannel.cs`
- [ ] T065 [US2] Create PaymentEventProcessor (IHostedService) in `src/Breez.Sdk.Liquid.Extensions.Core/Infrastructure/PaymentEventProcessor.cs`
- [ ] T066 [US2] Implement payment event subscription in BreezSdkService `src/Breez.Sdk.Liquid.Extensions.Core/Infrastructure/BreezSdkService.cs`

### Query Support (Core Package)

- [ ] T067 [US2] Add GetPaymentByHashAsync to BreezSdkService in `src/Breez.Sdk.Liquid.Extensions.Core/Infrastructure/BreezSdkService.cs`
- [ ] T068 [US2] Add GetPaymentHistoryAsync with pagination to BreezSdkService in `src/Breez.Sdk.Liquid.Extensions.Core/Infrastructure/BreezSdkService.cs`
- [ ] T069 [US2] Add payment history filtering (by status, date range, amount) to IPaymentRepository and implementations in `src/Breez.Sdk.Liquid.Extensions.Core/Abstractions/IPaymentRepository.cs`
- [ ] T070 [US2] Add payment state persistence on status changes in `src/Breez.Sdk.Liquid.Extensions.Core/Infrastructure/BreezSdkService.cs`

**Checkpoint**: User Story 2 complete - full payment lifecycle with events and history queries

---

## Phase 5: User Story 3 - Secure Configuration Management (Priority: P1)

**Goal**: Secure secrets handling with support for multiple configuration sources and fail-fast validation.

**Independent Test**: Configure secrets via environment variables, verify they are never logged and validation fails fast on missing values.

### Tests for User Story 3 (MANDATORY - Constitution Article III)

> **TDD REQUIRED: Write tests FIRST, verify they FAIL (RED), then implement until GREEN**

- [ ] T071 [P] [US3] Unit tests for secrets redaction in `tests/Breez.Sdk.Liquid.Extensions.Core.Tests/Configuration/SecretsRedactionTests.cs`
- [ ] T072 [P] [US3] Unit tests for configuration validation fail-fast in `tests/Breez.Sdk.Liquid.Extensions.Core.Tests/Configuration/ConfigurationValidationTests.cs`
- [ ] T073 [P] [US3] Unit tests for offline mode configuration in `tests/Breez.Sdk.Liquid.Extensions.Core.Tests/Configuration/OfflineModeConfigurationTests.cs`

### Secrets Management (Core Package)

- [ ] T074 [US3] Create SecretsRedactor utility in `src/Breez.Sdk.Liquid.Extensions.Core/Configuration/SecretsRedactor.cs`
- [ ] T075 [US3] Implement ILogger secrets filtering in BreezSdkService `src/Breez.Sdk.Liquid.Extensions.Core/Infrastructure/BreezSdkService.cs`
- [ ] T076 [US3] Add startup validation with fail-fast behavior in `src/Breez.Sdk.Liquid.Extensions.Core/Extensions/ServiceCollectionExtensions.cs`
- [ ] T077 [US3] Create BreezSdkStartupValidator as IHostedService in `src/Breez.Sdk.Liquid.Extensions.Core/Infrastructure/BreezSdkStartupValidator.cs`
- [ ] T078 [US3] Add SDK version compatibility check in `src/Breez.Sdk.Liquid.Extensions.Core/Infrastructure/BreezSdkVersionChecker.cs`

**Checkpoint**: User Story 3 complete - secrets secured, validation enforced

---

## Phase 6: User Story 4 - Extensible Persistence Layer (Priority: P2)

**Goal**: Pluggable persistence with in-memory default and optional SQL Server/PostgreSQL/SQLite packages.

**Independent Test**: Configure with different persistence providers, verify payment state persists correctly in each.

### Tests for User Story 4 (MANDATORY - Constitution Article III)

> **TDD REQUIRED: Write tests FIRST, verify they FAIL (RED), then implement until GREEN**

- [ ] T079 [P] [US4] Integration tests for SqlServer persistence in `tests/Breez.Sdk.Liquid.Extensions.Integration.Tests/SqlServer/SqlServerPaymentRepositoryTests.cs`
- [ ] T080 [P] [US4] Integration tests for PostgreSql persistence in `tests/Breez.Sdk.Liquid.Extensions.Integration.Tests/PostgreSql/PostgreSqlPaymentRepositoryTests.cs`
- [ ] T081 [P] [US4] Integration tests for Sqlite persistence in `tests/Breez.Sdk.Liquid.Extensions.Integration.Tests/Sqlite/SqlitePaymentRepositoryTests.cs`

### SQLite Package

- [ ] T082 [P] [US4] Create SqlitePaymentDbContext in `src/Breez.Sdk.Liquid.Extensions.Sqlite/Data/SqlitePaymentDbContext.cs`
- [ ] T083 [P] [US4] Create SqlitePaymentRepository in `src/Breez.Sdk.Liquid.Extensions.Sqlite/Data/SqlitePaymentRepository.cs`
- [ ] T084 [P] [US4] Create PaymentStateConfiguration for EF Core in `src/Breez.Sdk.Liquid.Extensions.Sqlite/Data/PaymentStateConfiguration.cs`
- [ ] T085 [P] [US4] Create ServiceCollectionExtensions with AddBreezSdkSqlite() in `src/Breez.Sdk.Liquid.Extensions.Sqlite/Extensions/ServiceCollectionExtensions.cs`

### SQL Server Package

- [ ] T086 [P] [US4] Create SqlServerPaymentDbContext in `src/Breez.Sdk.Liquid.Extensions.SqlServer/Data/SqlServerPaymentDbContext.cs`
- [ ] T087 [P] [US4] Create SqlServerPaymentRepository in `src/Breez.Sdk.Liquid.Extensions.SqlServer/Data/SqlServerPaymentRepository.cs`
- [ ] T088 [P] [US4] Create PaymentStateConfiguration for EF Core in `src/Breez.Sdk.Liquid.Extensions.SqlServer/Data/PaymentStateConfiguration.cs`
- [ ] T089 [P] [US4] Create ServiceCollectionExtensions with AddBreezSdkSqlServer() in `src/Breez.Sdk.Liquid.Extensions.SqlServer/Extensions/ServiceCollectionExtensions.cs`

### PostgreSQL Package

- [ ] T090 [P] [US4] Create PostgreSqlPaymentDbContext in `src/Breez.Sdk.Liquid.Extensions.PostgreSql/Data/PostgreSqlPaymentDbContext.cs`
- [ ] T091 [P] [US4] Create PostgreSqlPaymentRepository in `src/Breez.Sdk.Liquid.Extensions.PostgreSql/Data/PostgreSqlPaymentRepository.cs`
- [ ] T092 [P] [US4] Create PaymentStateConfiguration for EF Core in `src/Breez.Sdk.Liquid.Extensions.PostgreSql/Data/PaymentStateConfiguration.cs`
- [ ] T093 [P] [US4] Create ServiceCollectionExtensions with AddBreezSdkPostgreSql() in `src/Breez.Sdk.Liquid.Extensions.PostgreSql/Extensions/ServiceCollectionExtensions.cs`

**Checkpoint**: User Story 4 complete - pluggable persistence with 3 database options

---

## Phase 7: User Story 5 - Robust Error Handling and Resilience (Priority: P2)

**Goal**: Graceful handling of network failures, SDK errors, with automatic retry and circuit breaker patterns.

**Independent Test**: Simulate network failures, verify automatic reconnection and graceful degradation.

### Tests for User Story 5 (MANDATORY - Constitution Article III)

> **TDD REQUIRED: Write tests FIRST, verify they FAIL (RED), then implement until GREEN**

- [ ] T094 [P] [US5] Unit tests for retry policies in `tests/Breez.Sdk.Liquid.Extensions.Core.Tests/Infrastructure/ResiliencePoliciesTests.cs`
- [ ] T095 [P] [US5] Unit tests for circuit breaker in `tests/Breez.Sdk.Liquid.Extensions.Core.Tests/Infrastructure/CircuitBreakerTests.cs`
- [ ] T096 [P] [US5] Unit tests for exception mapping in `tests/Breez.Sdk.Liquid.Extensions.Core.Tests/Exceptions/ExceptionMappingTests.cs`
- [ ] T097 [P] [US5] Unit tests for reconnection behavior in `tests/Breez.Sdk.Liquid.Extensions.Core.Tests/Infrastructure/ReconnectionTests.cs`

### Resilience (Core Package)

- [ ] T098 [US5] Enhance ResiliencePolicies with circuit breaker in `src/Breez.Sdk.Liquid.Extensions.Core/Infrastructure/ResiliencePolicies.cs`
- [ ] T099 [US5] Implement SDK reconnection logic in BreezSdkWrapper `src/Breez.Sdk.Liquid.Extensions.Core/Infrastructure/BreezSdkWrapper.cs`
- [ ] T100 [US5] Add exception categorization and mapping in BreezSdkService `src/Breez.Sdk.Liquid.Extensions.Core/Infrastructure/BreezSdkService.cs`
- [ ] T101 [US5] Implement connection state tracking in BreezSdkWrapper `src/Breez.Sdk.Liquid.Extensions.Core/Infrastructure/BreezSdkWrapper.cs`

**Checkpoint**: User Story 5 complete - production-ready resilience and error handling

---

## Phase 8: User Story 6 - Observability and Diagnostics (Priority: P2)

**Goal**: Full observability with OpenTelemetry traces, metrics, and structured logging.

**Independent Test**: Run SDK with OpenTelemetry exporters, verify metrics, traces, and logs are correctly emitted.

### Tests for User Story 6 (MANDATORY - Constitution Article III)

> **TDD REQUIRED: Write tests FIRST, verify they FAIL (RED), then implement until GREEN**

- [ ] T102 [P] [US6] Unit tests for BreezSdkMetrics in `tests/Breez.Sdk.Liquid.Extensions.Core.Tests/Observability/BreezSdkMetricsTests.cs`
- [ ] T103 [P] [US6] Unit tests for activity/trace instrumentation in `tests/Breez.Sdk.Liquid.Extensions.Core.Tests/Observability/ActivitySourceTests.cs`
- [ ] T104 [P] [US6] Unit tests for structured logging in `tests/Breez.Sdk.Liquid.Extensions.Core.Tests/Observability/StructuredLoggingTests.cs`

### Observability (Core Package)

- [ ] T105 [US6] Create ActivitySources for tracing in `src/Breez.Sdk.Liquid.Extensions.Core/Observability/ActivitySources.cs`
- [ ] T106 [US6] Create BreezSdkMetrics for counters and histograms in `src/Breez.Sdk.Liquid.Extensions.Core/Observability/BreezSdkMetrics.cs`
- [ ] T107 [US6] Add trace instrumentation to BreezSdkService operations in `src/Breez.Sdk.Liquid.Extensions.Core/Infrastructure/BreezSdkService.cs`
- [ ] T108 [US6] Add structured logging with correlation IDs in `src/Breez.Sdk.Liquid.Extensions.Core/Infrastructure/BreezSdkService.cs` and `src/Breez.Sdk.Liquid.Extensions.Core/Infrastructure/PaymentEventProcessor.cs`

**Checkpoint**: User Story 6 complete - full observability for production monitoring

---

## Phase 9: User Story 7 - Platform-Specific Integrations (Priority: P3)

**Goal**: Optional packages for ASP.NET Core (webhooks, health checks) and Umbraco CMS integration.

**Independent Test**: Install Umbraco package, verify backoffice integration and CMS-specific features work.

### Tests for User Story 7 (MANDATORY - Constitution Article III)

> **TDD REQUIRED: Write tests FIRST, verify they FAIL (RED), then implement until GREEN**

- [ ] T109 [P] [US7] Unit tests for BreezSdkHealthCheck in `tests/Breez.Sdk.Liquid.Extensions.AspNetCore.Tests/HealthChecks/BreezSdkHealthCheckTests.cs`
- [ ] T110 [P] [US7] Unit tests for WebhookValidationMiddleware in `tests/Breez.Sdk.Liquid.Extensions.AspNetCore.Tests/Middleware/WebhookValidationMiddlewareTests.cs`
- [ ] T111 [P] [US7] Unit tests for WebhookEndpoints in `tests/Breez.Sdk.Liquid.Extensions.AspNetCore.Tests/Endpoints/WebhookEndpointsTests.cs`
- [ ] T112 [P] [US7] Integration tests for ASP.NET Core integration in `tests/Breez.Sdk.Liquid.Extensions.Integration.Tests/AspNetCore/AspNetCoreIntegrationTests.cs`

### ASP.NET Core Package

- [ ] T113 [US7] Create BreezSdkHealthCheck implementing IHealthCheck in `src/Breez.Sdk.Liquid.Extensions.AspNetCore/HealthChecks/BreezSdkHealthCheck.cs`
- [ ] T114 [US7] Create WebhookValidator utility in `src/Breez.Sdk.Liquid.Extensions.AspNetCore/Middleware/WebhookValidator.cs`
- [ ] T115 [US7] Create WebhookValidationMiddleware in `src/Breez.Sdk.Liquid.Extensions.AspNetCore/Middleware/WebhookValidationMiddleware.cs`
- [ ] T116 [US7] Create WebhookEndpoints with MapBreezSdkEndpoints() in `src/Breez.Sdk.Liquid.Extensions.AspNetCore/Endpoints/WebhookEndpoints.cs`
- [ ] T117 [US7] Create WebApplicationBuilderExtensions with AddBreezSdk() for WebApplicationBuilder in `src/Breez.Sdk.Liquid.Extensions.AspNetCore/Extensions/WebApplicationBuilderExtensions.cs`
- [ ] T118 [US7] Create HealthCheckBuilderExtensions with AddBreezSdkHealthCheck() in `src/Breez.Sdk.Liquid.Extensions.AspNetCore/Extensions/HealthCheckBuilderExtensions.cs`

### Umbraco Package

- [ ] T119 [US7] Create BreezSdkComposer in `src/Breez.Sdk.Liquid.Extensions.Umbraco/Composers/BreezSdkComposer.cs`
- [ ] T120 [US7] Create BreezSdkComponent in `src/Breez.Sdk.Liquid.Extensions.Umbraco/Components/BreezSdkComponent.cs`
- [ ] T121 [US7] Create UmbracoBuilderExtensions in `src/Breez.Sdk.Liquid.Extensions.Umbraco/Extensions/UmbracoBuilderExtensions.cs`

**Checkpoint**: User Story 7 complete - platform-specific packages ready

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Final quality improvements, documentation, and packaging

- [ ] T122 [P] Create ADR for package structure decision in `docs/adr/001-package-structure.md`
- [ ] T123 [P] Create migration guide from Umbraco-coupled code in `docs/migration-guide.md`
- [ ] T124 [P] Run security audit with security-auditor agent
- [ ] T125 [P] Run code review with code-reviewer agent for constitution compliance
- [ ] T126 [P] Run breezsdk-reviewer agent for SDK pattern compliance
- [ ] T127 Validate quickstart.md scenarios work end-to-end
- [ ] T128 Configure CI/CD for NuGet package publishing
- [ ] T129 Add XML documentation to all public APIs across all packages
- [ ] T130 Run final dotnet build, dotnet test, dotnet format validation
- [ ] T131 [P] End-to-end testnet integration test in `tests/Breez.Sdk.Liquid.Extensions.Integration.Tests/EndToEnd/TestnetPaymentFlowTests.cs`

### Sample Applications (SC-009 Compliance)

- [ ] T132 [P] Create Console sample application in `samples/ConsoleApp/`
- [ ] T133 [P] Create ASP.NET Core Web API sample application in `samples/WebApi/`
- [ ] T134 [P] Create Blazor sample application in `samples/BlazorApp/`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies - can start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 - BLOCKS all user stories
- **Phases 3-5 (US1-US3 P1)**: All depend on Phase 2 completion
  - US1 (P1): No dependencies on other stories
  - US2 (P1): Builds on US1 (IBreezSdkService)
  - US3 (P1): Builds on US1 (configuration)
- **Phases 6-8 (US4-US6 P2)**: Depend on US1-US3 completion
  - US4 (P2): Can start after US2 (IPaymentRepository)
  - US5 (P2): Can start after US1 (core service)
  - US6 (P2): Can start after US1 (core service)
- **Phase 9 (US7 P3)**: Depends on US1-US6 completion
- **Phase 10 (Polish)**: Depends on all user stories being complete

### User Story Dependencies

```
Phase 1: Setup
    ↓
Phase 2: Foundational (BLOCKING)
    ↓
    ├─→ US1 (P1): Library Consumer Integration (MVP)
    │       ↓
    ├─→ US2 (P1): Payment Lifecycle (depends on US1)
    │       ↓
    └─→ US3 (P1): Secure Configuration (depends on US1)
            ↓
    ┌───────┴───────┐
    ↓               ↓
US4 (P2)        US5 (P2)        US6 (P2)
Persistence     Resilience      Observability
    └───────────────┼───────────────┘
                    ↓
            US7 (P3): Platform Integrations
                    ↓
            Phase 10: Polish
```

### Within Each User Story

1. Tests MUST be written and FAIL before implementation (RED)
2. Implement until tests pass (GREEN)
3. Refactor with test safety net
4. Story complete before moving to next priority

### Parallel Opportunities

**Phase 1 (Setup)**:
- T003-T011 can all run in parallel after T001-T002
- T014-T015 can run in parallel with T003-T011

**Phase 2 (Foundational)**:
- T016-T019 (enums) can all run in parallel
- T020-T022 (entities) can run after enums
- T023-T028 (events) can run in parallel
- T029-T033 (exceptions) can run in parallel
- T034-T038 (abstractions) can run in parallel
- T039-T042 (test utilities) can run in parallel

**User Stories**:
- All tests within a story marked [P] can run in parallel
- US4, US5, US6 can run in parallel after US1-US3 complete
- Within US4: All persistence packages (SQLite/SQL Server/PostgreSQL) can run in parallel

---

## Parallel Example: Phase 2 Foundational

```bash
# Wave 1: Create all enums in parallel
& Use backend-developer to implement T016 (PaymentStatus)
& Use backend-developer to implement T017 (PaymentKind)
& Use backend-developer to implement T018 (BreezErrorCode)
& Use backend-developer to implement T019 (InvoiceType)

# Wave 2: Create entities after enums
& Use backend-developer to implement T020 (PaymentState)
& Use backend-developer to implement T021 (Invoice)
& Use backend-developer to implement T022 (OperationResult)

# Wave 3: Events and exceptions in parallel
& Use backend-developer to implement T023-T028 (events)
& Use backend-developer to implement T029-T033 (exceptions)

# Wave 4: Abstractions and test utilities in parallel
& Use backend-developer to implement T034-T038 (abstractions)
& Use breezsdk-test-engineer to implement T039-T042 (test utilities)
```

---

## Parallel Example: User Story 4 (Persistence Packages)

```bash
# All persistence packages can be developed in parallel:
& Use database-architect to implement T080-T083 (SQLite)
& Use database-architect to implement T084-T087 (SQL Server)
& Use database-architect to implement T088-T091 (PostgreSQL)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (Library Consumer Integration)
4. **STOP and VALIDATE**: Test User Story 1 independently with a console app
5. Deploy/demo if ready - developers can now create invoices

### Incremental Delivery

1. **Setup + Foundational** → Foundation ready
2. **Add US1** → Test independently → Demo (MVP: invoice creation)
3. **Add US2** → Test independently → Demo (payment tracking)
4. **Add US3** → Test independently → Demo (production-ready security)
5. **Add US4-US6** → Test independently → Demo (full production features)
6. **Add US7** → Test independently → Demo (platform packages)
7. **Polish** → Final quality validation → NuGet publish

### Agent Assignments

| Wave | Task Type | Agent | Rationale |
|------|-----------|-------|-----------|
| Setup | Project scaffolding | backend-developer | .csproj creation |
| Foundation | Domain models | backend-developer | Entity patterns |
| Foundation | Test utilities | breezsdk-test-engineer | SDK mocking |
| US1-US3 | Core services | breezsdk-developer | SDK-specific patterns |
| US1-US3 | Unit tests | breezsdk-test-engineer | SDK mocking expertise |
| US4 | Persistence | database-architect | EF Core migrations |
| US5 | Resilience | backend-developer | Polly patterns |
| US6 | Observability | backend-developer | OpenTelemetry |
| US7 | ASP.NET Core | backend-developer | Middleware patterns |
| US7 | Umbraco | backend-developer | CMS patterns |
| Polish | Quality | code-reviewer, security-auditor | Compliance |
| Polish | SDK patterns | breezsdk-reviewer | SDK compliance |

---

## Summary

| Metric | Count |
|--------|-------|
| **Total Tasks** | 134 |
| **Phase 1 (Setup)** | 15 tasks |
| **Phase 2 (Foundational)** | 27 tasks |
| **US1 (P1) Library Integration** | 16 tasks |
| **US2 (P1) Payment Lifecycle** | 12 tasks |
| **US3 (P1) Configuration** | 8 tasks |
| **US4 (P2) Persistence** | 15 tasks |
| **US5 (P2) Resilience** | 8 tasks |
| **US6 (P2) Observability** | 7 tasks |
| **US7 (P3) Platform** | 13 tasks |
| **Phase 10 (Polish)** | 13 tasks |
| **Parallel Opportunities** | 75+ tasks marked [P] |

### MVP Scope (User Story 1 Only)

- Phases 1-3 (58 tasks)
- Delivers: NuGet package that enables Lightning invoice creation
- Independent validation: Console app creates invoice successfully

### Notes

- [P] tasks = different files, no dependencies, can run in parallel
- [Story] label maps task to specific user story
- Each user story is independently completable and testable
- Verify tests FAIL before implementing (TDD)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
