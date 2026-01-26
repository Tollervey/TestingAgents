# Tasks: Umbraco Lightning Payments Code Review & Improvements

**Input**: Design documents from `/specs/001-umbraco-lightning-review/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Per Constitution Article III (Test-First Imperative), tests are **MANDATORY** and must be written BEFORE implementation. No production code may be written until corresponding tests exist and fail (Red-Green-Refactor). This is NON-NEGOTIABLE.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Backend**: `src/Umbraco.Community.Bitcoin.LightningPayments.Core/`
- **Tests**: `tests/Umbraco.Community.Bitcoin.LightningPayments.CoreTests/`
- **Frontend**: `src/Umbraco.Community.Bitcoin.LightningPayments.Core/BackofficeUI/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization, configuration, and shared exception hierarchy

- [x] T001 Create unified exception hierarchy `LightningPaymentsException` base class in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/Exceptions/LightningPaymentsException.cs`
- [x] T002 [P] Create `PaymentNotFoundException` exception in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/Exceptions/PaymentNotFoundException.cs`
- [x] T003 [P] Create `InsufficientBalanceException` exception in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/Exceptions/InsufficientBalanceException.cs`
- [x] T004 [P] Create `BreezSdkConnectionException` exception in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/Exceptions/BreezSdkConnectionException.cs`
- [x] T005 [P] Create `InvalidInvoiceException` exception in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/Exceptions/InvalidInvoiceException.cs`
- [x] T006 [P] Create `RefundExceedsOriginalException` exception in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/Exceptions/RefundExceedsOriginalException.cs`
- [x] T007 Create `NotificationOptions` configuration class in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Configuration/NotificationOptions.cs`
- [x] T008 [P] Create `ExchangeRateOptions` configuration class in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Configuration/ExchangeRateOptions.cs`
- [x] T008a [P] Configure FluentValidation for API request validation in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Extensions/LightningPaymentsExtensions.cs` and create base validator class in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Validation/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure and database entities that MUST be complete before ANY user story can be implemented

**CRITICAL**: No user story work can begin until this phase is complete

### Database Entities (from data-model.md)

- [x] T009 Create `Bolt12Offer` entity in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Data/Models/Bolt12Offer.cs`
- [x] T010 [P] Create `RefundTransaction` entity and `RefundStatus` enum in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Data/Models/RefundTransaction.cs`
- [x] T011 [P] Create `PaymentNotification` entity with `NotificationType`, `NotificationEvent`, `NotificationStatus` enums in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Data/Models/PaymentNotification.cs`
- [x] T012 [P] Create `ExchangeRate` entity in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Data/Models/ExchangeRate.cs`
- [x] T013 Update `PaymentState` entity to add `Bolt12OfferId` FK and navigation properties in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Data/Models/PaymentState.cs`
- [x] T014 Update `PaymentDbContext` with new DbSets and entity configurations in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Data/PaymentDbContext.cs`
- [x] T015 Create EF Core migration `AddBolt12NotificationsRefunds` in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Data/Migrations/`

### Shared API DTOs

- [x] T016 [P] Create `ProblemDetails` response DTO in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Shared/ProblemDetails.cs`
- [x] T017 [P] Create `FiatAmount` DTO in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Shared/FiatAmount.cs`

### Frontend Infrastructure

- [x] T018 Create BackofficeUI project structure with `package.json`, `tsconfig.json`, `vite.config.ts` in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/BackofficeUI/`
- [x] T019 Create `umbraco-package.json` manifest in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/BackofficeUI/`
- [x] T020 [P] Create shared TypeScript types in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/BackofficeUI/src/shared/types.ts`
- [x] T021 [P] Create `lightning-api-client.ts` API wrapper in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/BackofficeUI/src/shared/lightning-api-client.ts`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Enhanced Backoffice Payment Dashboard (Priority: P1)

**Goal**: Provide a comprehensive Lightning payment dashboard in Umbraco backoffice for monitoring payment activity, troubleshooting issues, and viewing system health.

**Independent Test**: Log into Umbraco backoffice, navigate to Lightning Payments section, verify real-time payment statistics, recent transactions, and system health indicators display correctly.

### Tests for User Story 1 (MANDATORY - Constitution Article III)

> **TDD REQUIRED: Write tests FIRST, verify they FAIL (RED), then implement until GREEN**

- [ ] T022 [P] [US1] Unit tests for `DashboardStatsService` in `tests/Umbraco.Community.Bitcoin.LightningPayments.CoreTests/Features/DashboardStatsServiceTests.cs`
- [ ] T023 [P] [US1] Unit tests for `DashboardController` in `tests/Umbraco.Community.Bitcoin.LightningPayments.CoreTests/Api/DashboardControllerTests.cs`
- [ ] T024 [P] [US1] Contract tests for `/dashboard/stats` and `/dashboard/chart` endpoints in `tests/Umbraco.Community.Bitcoin.LightningPayments.CoreTests/Api/Contract/DashboardContractTests.cs`

### Implementation for User Story 1

#### Backend Services

- [ ] T025 [US1] Create `DashboardStats` model in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Features/Dashboard/DashboardStats.cs`
- [ ] T026 [P] [US1] Create `ChartData` and `ChartDataPoint` models in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Features/Dashboard/ChartData.cs`
- [ ] T027 [P] [US1] Create `WalletBalance` model in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Features/Dashboard/WalletBalance.cs`
- [ ] T028 [P] [US1] Create `WalletLimits` model in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Features/Dashboard/WalletLimits.cs`
- [ ] T029 [US1] Create `IDashboardStatsService` interface in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Features/Dashboard/IDashboardStatsService.cs`
- [ ] T030 [US1] Implement `DashboardStatsService` in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Features/Dashboard/DashboardStatsService.cs`

#### Backend API (from management-api.yaml)

- [ ] T031 [US1] Create `DashboardController` with `getDashboardStats`, `getPaymentChart`, `getWalletBalance`, `getWalletLimits` endpoints in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Management/DashboardController.cs`
- [ ] T032 [US1] Create `PaymentsController` with `listPayments`, `getPayment` endpoints in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Management/PaymentsController.cs`

#### Backend DTOs

- [ ] T033 [P] [US1] Create `PaymentListResponse`, `PaymentSummary`, `PaymentDetails` DTOs in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Management/Dto/PaymentDtos.cs`

#### Frontend Components (Bellissima/Lit)

- [ ] T034 [US1] Create `lightning-dashboard.element.ts` main dashboard component in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/BackofficeUI/src/dashboard/lightning-dashboard.element.ts`
- [ ] T035 [P] [US1] Create `payment-history-table.element.ts` component in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/BackofficeUI/src/dashboard/payment-history-table.element.ts`
- [ ] T036 [P] [US1] Create `connection-status.element.ts` component in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/BackofficeUI/src/dashboard/connection-status.element.ts`
- [ ] T037 [P] [US1] Create `payment-chart.element.ts` component in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/BackofficeUI/src/dashboard/payment-chart.element.ts`

#### DI Registration

- [ ] T038 [US1] Register `IDashboardStatsService` and dashboard-related services in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Extensions/LightningPaymentsExtensions.cs`

**Checkpoint**: User Story 1 complete - Dashboard is fully functional and testable independently

---

## Phase 4: User Story 2 - Improved Paywall Content Editor Experience (Priority: P1)

**Goal**: Provide an intuitive paywall configuration UI directly in the content editor so content editors can easily set up paid content without technical knowledge.

**Independent Test**: Create new content with paywall, configure tiered pricing (1h/8h/24h/7d) through visual UI, verify paywall enforcement and validation feedback.

### Tests for User Story 2 (MANDATORY - Constitution Article III)

> **TDD REQUIRED: Write tests FIRST, verify they FAIL (RED), then implement until GREEN**

- [ ] T039 [P] [US2] Unit tests for `PaywallPropertyEditorValueConverter` in `tests/Umbraco.Community.Bitcoin.LightningPayments.CoreTests/Features/PropertyEditor/PaywallPropertyEditorValueConverterTests.cs`
- [ ] T040 [P] [US2] Contract tests for `/paywall/invoice` and `/paywall/status` endpoints in `tests/Umbraco.Community.Bitcoin.LightningPayments.CoreTests/Api/Contract/PaywallContractTests.cs`

### Implementation for User Story 2

#### Backend Services

- [ ] T041 [US2] Create `PaywallConfig` model with tiered pricing in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Features/PropertyEditor/PaywallConfig.cs`
- [ ] T042 [US2] Update/Create `PaywallPropertyEditorValueConverter` in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Features/PropertyEditor/PaywallPropertyEditorValueConverter.cs`

#### Backend API (from public-api.yaml)

- [ ] T043 [US2] Create `PaywallController` with `createPaywallInvoice`, `getPaywallStatus` endpoints in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Public/PaywallController.cs`

#### Backend DTOs

- [ ] T044 [P] [US2] Create `CreatePaywallInvoiceRequest`, `PaywallStatus`, `InvoiceResponse` DTOs in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Public/Dto/PaywallDtos.cs`

#### Frontend Components (Bellissima/Lit)

- [ ] T045 [US2] Create `paywall-editor.element.ts` property editor component in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/BackofficeUI/src/property-editor/paywall-editor.element.ts`
- [ ] T046 [P] [US2] Create `tier-config.element.ts` component for tier pricing in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/BackofficeUI/src/property-editor/tier-config.element.ts`

#### Manifest Updates

- [ ] T047 [US2] Register property editor UI in `umbraco-package.json` manifest in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/BackofficeUI/umbraco-package.json`

**Checkpoint**: User Story 2 complete - Paywall editor is fully functional and testable independently

---

## Phase 5: User Story 3 - Bolt12 Offer Support for Recurring Payments (Priority: P2)

**Goal**: Enable creation and management of Bolt12 offers for recurring payment use cases (subscriptions, donations) through the Management API.

**Independent Test**: Create a Bolt12 offer through the API, share the offer string, receive multiple payments to the same offer, verify payments are tracked individually.

### Tests for User Story 3 (MANDATORY - Constitution Article III)

> **TDD REQUIRED: Write tests FIRST, verify they FAIL (RED), then implement until GREEN**

- [ ] T048 [P] [US3] Unit tests for `Bolt12OfferService` in `tests/Umbraco.Community.Bitcoin.LightningPayments.CoreTests/Services/Bolt12OfferServiceTests.cs`
- [ ] T049 [P] [US3] Unit tests for `Bolt12Controller` in `tests/Umbraco.Community.Bitcoin.LightningPayments.CoreTests/Api/Bolt12ControllerTests.cs`
- [ ] T050 [P] [US3] Contract tests for `/offers` CRUD endpoints in `tests/Umbraco.Community.Bitcoin.LightningPayments.CoreTests/Api/Contract/Bolt12ContractTests.cs`

### Implementation for User Story 3

#### Backend Services

- [ ] T051 [US3] Create `IBolt12OfferService` interface in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/Bolt12/IBolt12OfferService.cs`
- [ ] T052 [US3] Implement `Bolt12OfferService` in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/Bolt12/Bolt12OfferService.cs`

#### Backend API (from management-api.yaml)

- [ ] T053 [US3] Create `Bolt12Controller` with `listOffers`, `createOffer`, `getOffer`, `deactivateOffer`, `listOfferPayments` endpoints in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Management/Bolt12Controller.cs`

#### Backend DTOs

- [ ] T054 [P] [US3] Create `Bolt12Offer`, `Bolt12OfferDetails`, `CreateOfferRequest` DTOs in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Management/Dto/Bolt12Dtos.cs`

#### Public API (from public-api.yaml)

- [ ] T055 [US3] Create `OffersController` with `getOfferInfo` endpoint in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Public/OffersController.cs`
- [ ] T056 [P] [US3] Create `OfferInfo` DTO in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Public/Dto/OfferDtos.cs`

#### DI Registration

- [ ] T057 [US3] Register `IBolt12OfferService` in DI in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Extensions/LightningPaymentsExtensions.cs`

**Checkpoint**: User Story 3 complete - Bolt12 offers are fully functional and testable independently

---

## Phase 6: User Story 4 - Payment Notification System (Priority: P2)

**Goal**: Enable email and webhook notifications for payment events with retry logic, so site owners can track revenue and fulfill orders promptly.

**Independent Test**: Configure email/webhook notifications, trigger a test payment, verify notification delivery with correct payload and HMAC signature.

### Tests for User Story 4 (MANDATORY - Constitution Article III)

> **TDD REQUIRED: Write tests FIRST, verify they FAIL (RED), then implement until GREEN**

- [ ] T058 [P] [US4] Unit tests for `NotificationService` in `tests/Umbraco.Community.Bitcoin.LightningPayments.CoreTests/Services/NotificationServiceTests.cs`
- [ ] T059 [P] [US4] Unit tests for `EmailNotificationHandler` in `tests/Umbraco.Community.Bitcoin.LightningPayments.CoreTests/Services/EmailNotificationHandlerTests.cs`
- [ ] T060 [P] [US4] Unit tests for `WebhookNotificationHandler` with HMAC signing in `tests/Umbraco.Community.Bitcoin.LightningPayments.CoreTests/Services/WebhookNotificationHandlerTests.cs`
- [ ] T061 [P] [US4] Unit tests for `NotificationController` in `tests/Umbraco.Community.Bitcoin.LightningPayments.CoreTests/Api/NotificationControllerTests.cs`
- [ ] T062 [P] [US4] Contract tests for `/notifications` endpoints in `tests/Umbraco.Community.Bitcoin.LightningPayments.CoreTests/Api/Contract/NotificationContractTests.cs`

### Implementation for User Story 4

#### Backend Services

- [ ] T063 [US4] Create `INotificationService` interface in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/Notification/INotificationService.cs`
- [ ] T064 [US4] Implement `NotificationService` with Polly retry in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/Notification/NotificationService.cs`
- [ ] T065 [P] [US4] Create `INotificationHandler` interface in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/Notification/INotificationHandler.cs`
- [ ] T066 [P] [US4] Implement `EmailNotificationHandler` with MailKit in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/Notification/EmailNotificationHandler.cs`
- [ ] T067 [P] [US4] Implement `WebhookNotificationHandler` with HMAC-SHA256 signing in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/Notification/WebhookNotificationHandler.cs`
- [ ] T068 [US4] Create `WebhookPayloadBuilder` for constructing webhook payloads in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/Notification/WebhookPayloadBuilder.cs`

#### Backend API (from management-api.yaml)

- [ ] T069 [US4] Create `NotificationController` with `getNotificationConfig`, `updateNotificationConfig`, `listNotifications`, `retryNotification` endpoints in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Management/NotificationController.cs`

#### Backend DTOs

- [ ] T070 [P] [US4] Create `NotificationConfig`, `NotificationListResponse`, `NotificationSummary` DTOs in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Management/Dto/NotificationDtos.cs`

#### Webhook Payloads (from webhook-api.yaml)

- [ ] T071 [P] [US4] Create webhook event models (`PaymentConfirmedEvent`, `PaymentFailedEvent`, `RefundCompletedEvent`) in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/Notification/WebhookEvents.cs`

#### Background Processing

- [ ] T072 [US4] Create `NotificationRetryBackgroundService` for processing retry queue in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/Notification/NotificationRetryBackgroundService.cs`

#### DI Registration

- [ ] T073 [US4] Register notification services and handlers in DI in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Extensions/LightningPaymentsExtensions.cs`

**Checkpoint**: User Story 4 complete - Notification system is fully functional and testable independently

---

## Phase 7: User Story 5 - Payment Refund/Return Capability (Priority: P3)

**Goal**: Enable Administrators to issue refunds for mistaken or disputed payments via Lightning invoice payment.

**Independent Test**: Initiate a refund for a received payment, provide customer's Lightning invoice, verify funds are sent and refund is linked to original payment.

### Tests for User Story 5 (MANDATORY - Constitution Article III)

> **TDD REQUIRED: Write tests FIRST, verify they FAIL (RED), then implement until GREEN**

- [ ] T074 [P] [US5] Unit tests for `RefundService` in `tests/Umbraco.Community.Bitcoin.LightningPayments.CoreTests/Services/RefundServiceTests.cs`
- [ ] T075 [P] [US5] Unit tests for `RefundController` in `tests/Umbraco.Community.Bitcoin.LightningPayments.CoreTests/Api/RefundControllerTests.cs`
- [ ] T076 [P] [US5] Contract tests for `/refunds` CRUD and `/refunds/prepare` endpoints in `tests/Umbraco.Community.Bitcoin.LightningPayments.CoreTests/Api/Contract/RefundContractTests.cs`

### Implementation for User Story 5

#### Backend Services

- [ ] T077 [US5] Extend `IBreezSdkService` to add `PrepareSendPaymentAsync`, `SendPaymentAsync`, `GetWalletBalanceAsync`, `ParseInvoiceAsync` in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/IBreezSdkService.cs`
- [ ] T078 [US5] Implement send payment methods in `BreezSdkService` in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/BreezSdkService.cs`
- [ ] T079 [US5] Create `IRefundService` interface in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/Refund/IRefundService.cs`
- [ ] T080 [US5] Implement `RefundService` in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/Refund/RefundService.cs`

#### Backend API (from management-api.yaml)

- [ ] T081 [US5] Create `RefundController` with `listRefunds`, `initiateRefund`, `getRefund`, `prepareRefund` endpoints in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Management/RefundController.cs`

#### Backend DTOs

- [ ] T082 [P] [US5] Create `RefundTransaction`, `RefundSummary`, `RefundListResponse`, `InitiateRefundRequest`, `PrepareRefundRequest`, `PrepareRefundResponse` DTOs in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Management/Dto/RefundDtos.cs`

#### Authorization

- [ ] T083 [US5] Add Administrator-only authorization policy for refund endpoints in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Management/RefundController.cs`

#### DI Registration

- [ ] T084 [US5] Register `IRefundService` in DI in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Extensions/LightningPaymentsExtensions.cs`

**Checkpoint**: User Story 5 complete - Refund capability is fully functional and testable independently

---

## Phase 8: User Story 6 - Multi-Currency Display (Priority: P3)

**Goal**: Enable site visitors to see payment amounts in their preferred fiat currency to reduce friction and improve conversion rates.

**Independent Test**: View a paywall with configured fiat display, verify accurate conversion based on current CoinGecko rates, verify graceful degradation when service unavailable.

### Tests for User Story 6 (MANDATORY - Constitution Article III)

> **TDD REQUIRED: Write tests FIRST, verify they FAIL (RED), then implement until GREEN**

- [ ] T085 [P] [US6] Unit tests for `ExchangeRateService` in `tests/Umbraco.Community.Bitcoin.LightningPayments.CoreTests/Services/ExchangeRateServiceTests.cs`
- [ ] T086 [P] [US6] Unit tests for `CoinGeckoClient` in `tests/Umbraco.Community.Bitcoin.LightningPayments.CoreTests/Services/CoinGeckoClientTests.cs`
- [ ] T087 [P] [US6] Unit tests for `ExchangeRateController` in `tests/Umbraco.Community.Bitcoin.LightningPayments.CoreTests/Api/ExchangeRateControllerTests.cs`
- [ ] T088 [P] [US6] Contract tests for `/exchange-rates` and `/convert` endpoints in `tests/Umbraco.Community.Bitcoin.LightningPayments.CoreTests/Api/Contract/ExchangeRateContractTests.cs`

### Implementation for User Story 6

#### Backend Services

- [ ] T089 [US6] Create `IExchangeRateService` interface in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/ExchangeRate/IExchangeRateService.cs`
- [ ] T090 [US6] Implement `CoinGeckoClient` HTTP client in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/ExchangeRate/CoinGeckoClient.cs`
- [ ] T091 [US6] Implement `ExchangeRateService` with caching in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/ExchangeRate/ExchangeRateService.cs`

#### Backend API (from public-api.yaml)

- [ ] T092 [US6] Create `ExchangeRateController` with `getExchangeRates`, `convertAmount` endpoints in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Public/ExchangeRateController.cs`

#### Backend DTOs

- [ ] T093 [P] [US6] Create `ExchangeRatesResponse`, `ConversionResponse` DTOs in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Public/Dto/ExchangeRateDtos.cs`

#### Integration Points

- [ ] T094 [US6] Update `InvoiceResponse` to include `amountFiat` field in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Public/Dto/PaywallDtos.cs`
- [ ] T095 [US6] Update paywall invoice creation to fetch and include fiat amount in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Public/PaywallController.cs`

#### DI Registration

- [ ] T096 [US6] Register `IExchangeRateService`, `CoinGeckoClient`, and HttpClient in DI in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Extensions/LightningPaymentsExtensions.cs`

**Checkpoint**: User Story 6 complete - Multi-currency display is fully functional and testable independently

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T097 [P] Add OpenTelemetry metrics instrumentation across all services in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/`
- [ ] T098 [P] Add correlation ID logging to all controllers in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/`
- [ ] T099 [P] Create `FeatureFlagsController` with `getFeatureFlags` endpoint in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Public/FeatureFlagsController.cs`
- [ ] T100 [P] Create `FeatureFlags` DTO in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Public/Dto/FeatureFlagsDtos.cs`
- [ ] T100a [P] [Edge] Implement and test late payment handler for expired invoices (edge case from spec.md:123) in `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Services/PaymentStateService.cs` - define behavior: reject, accept with warning, or queue for manual review
- [ ] T100b [P] [Edge] Implement and test unpublished content payment handling (edge case from spec.md:126) - when paywalled content is unpublished while payments are pending, define behavior: refund automatically, complete payment but deny access, or notify admin
- [ ] T100c [P] [Edge] Document and test rate limiter exceeded behavior (edge case from spec.md:127) - verify 429 response with Retry-After header per public-api.yaml, add integration test in `tests/Umbraco.Community.Bitcoin.LightningPayments.CoreTests/Api/RateLimiterIntegrationTests.cs`
- [ ] T101 Run quickstart.md validation to verify development setup
- [ ] T102 Build and verify BackofficeUI components compile with `npm run build`
- [ ] T103 Run full test suite with `dotnet test` and verify 80%+ coverage for new code
- [ ] T104 Security review: Verify HMAC webhook signing, admin-only refund authorization, input validation

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion - BLOCKS all user stories
- **User Stories (Phases 3-8)**: All depend on Foundational phase (Phase 2) completion
  - User stories can proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Phase 9)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Dashboard - Can start after Phase 2 - No dependencies on other stories
- **User Story 2 (P1)**: Paywall Editor - Can start after Phase 2 - No dependencies on other stories
- **User Story 3 (P2)**: Bolt12 Offers - Can start after Phase 2 - No dependencies on other stories
- **User Story 4 (P2)**: Notifications - Can start after Phase 2 - No dependencies on other stories
- **User Story 5 (P3)**: Refunds - Can start after Phase 2 - No dependencies on other stories
- **User Story 6 (P3)**: Exchange Rates - Can start after Phase 2 - No dependencies on other stories

### Within Each User Story

- Tests (MANDATORY) MUST be written and FAIL before implementation
- Models/DTOs before services
- Services before controllers
- Backend before frontend components
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel (T002-T006, T007-T008)
- All Foundational entity tasks marked [P] can run in parallel (T010-T012)
- All Foundational frontend tasks marked [P] can run in parallel (T020-T021)
- Once Foundational phase completes, ALL user stories can start in parallel
- Within each story, tests marked [P] can run in parallel
- Within each story, DTOs marked [P] can run in parallel
- Different user stories can be worked on in parallel by different agents/developers

---

## Parallel Example: User Story 1 (Dashboard)

```bash
# Phase 3: Launch all tests for User Story 1 together:
& Use test-engineer to implement T022 (DashboardStatsServiceTests)
& Use test-engineer to implement T023 (DashboardControllerTests)
& Use test-engineer to implement T024 (DashboardContractTests)

# After tests are RED, launch parallel model creation:
& Use backend-developer to implement T025 (DashboardStats model)
& Use backend-developer to implement T026 (ChartData models)
& Use backend-developer to implement T027 (WalletBalance model)
& Use backend-developer to implement T028 (WalletLimits model)

# Services depend on models, then controllers depend on services (sequential)
# Frontend components can run in parallel:
& Use umbraco-frontend-developer to implement T034 (lightning-dashboard)
& Use umbraco-frontend-developer to implement T035 (payment-history-table)
& Use umbraco-frontend-developer to implement T036 (connection-status)
& Use umbraco-frontend-developer to implement T037 (payment-chart)
```

---

## Parallel Example: Multiple User Stories

```bash
# After Phase 2 (Foundational) is complete, start P1 stories in parallel:
& Use backend-developer to implement US1 (Dashboard) Phase 3
& Use backend-developer to implement US2 (Paywall Editor) Phase 4

# Monitor progress:
/tasks

# Once P1 stories complete, start P2 stories in parallel:
& Use backend-developer to implement US3 (Bolt12) Phase 5
& Use backend-developer to implement US4 (Notifications) Phase 6

# Once P2 stories complete, start P3 stories in parallel:
& Use backend-developer to implement US5 (Refunds) Phase 7
& Use backend-developer to implement US6 (Exchange Rates) Phase 8
```

---

## Implementation Strategy

### MVP First (User Stories 1 & 2 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (Dashboard)
4. Complete Phase 4: User Story 2 (Paywall Editor)
5. **STOP and VALIDATE**: Test US1 and US2 independently
6. Deploy/demo if ready - this is MVP!

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo
3. Add User Story 2 → Test independently → Deploy/Demo (MVP!)
4. Add User Story 3 → Test independently → Deploy/Demo
5. Add User Story 4 → Test independently → Deploy/Demo
6. Add User Story 5 → Test independently → Deploy/Demo
7. Add User Story 6 → Test independently → Deploy/Demo (Full Feature)
8. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple agents/developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Agent A: User Story 1 (Dashboard)
   - Agent B: User Story 2 (Paywall Editor)
   - Agent C: User Story 3 (Bolt12)
   - Agent D: User Story 4 (Notifications)
3. Stories complete and integrate independently
4. After P1/P2 complete, reassign to P3 stories

---

## Summary

| Category | Count |
|----------|-------|
| **Total Tasks** | 104 |
| **Phase 1 (Setup)** | 8 tasks |
| **Phase 2 (Foundational)** | 13 tasks |
| **User Story 1 (Dashboard)** | 17 tasks |
| **User Story 2 (Paywall)** | 9 tasks |
| **User Story 3 (Bolt12)** | 10 tasks |
| **User Story 4 (Notifications)** | 16 tasks |
| **User Story 5 (Refunds)** | 11 tasks |
| **User Story 6 (Exchange)** | 12 tasks |
| **Phase 9 (Polish)** | 8 tasks |
| **Parallel Opportunities** | ~45 tasks marked [P] |

### MVP Scope (Recommended)

- **Minimum**: Phase 1 + Phase 2 + User Story 1 (Dashboard) = 38 tasks
- **Full MVP**: Phase 1 + Phase 2 + User Stories 1-2 = 47 tasks
- **Full Feature**: All 104 tasks

---

## Notes

- [P] tasks = different files, no dependencies within the same phase
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- **Tests MUST fail (RED) before implementing** - this is non-negotiable per Constitution Article III
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Run `code-reviewer` and `security-auditor` agents after each story phase

---

*Generated: 2026-01-26*
