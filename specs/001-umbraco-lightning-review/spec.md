# Feature Specification: Umbraco Lightning Payments Code Review & Improvements

**Feature Branch**: `001-umbraco-lightning-review`
**Created**: 2026-01-26
**Status**: Draft
**Input**: Code review of Umbraco.Community.Bitcoin.LightningPayments.Core project with BreezSDK cross-reference for improvements

## Executive Summary

This specification documents findings from a comprehensive code review of the Umbraco.Community.Bitcoin.LightningPayments.Core project and related BreezSDK integration projects. The review identifies areas for improvement, missing functionality, and enhancement opportunities based on Umbraco v17 best practices and BreezSDK capabilities.

## Clarifications

### Session 2026-01-26

- Q: Who should be able to access the Lightning Payments dashboard? → A: Administrators and configurable user groups
- Q: Which exchange rate data source should be used? → A: CoinGecko API (with optional Pro upgrade path)
- Q: How long should paid sessions remain valid? → A: Tiered pricing with 4 duration options (1h, 8h, 24h, 7d) at different costs per content item
- Q: What should the notification retry policy be? → A: 5 retries over ~30 minutes with exponential backoff, then mark as failed
- Q: Who can initiate refunds? → A: Administrators only (highest security)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Enhanced Backoffice Payment Dashboard (Priority: P1)

As an Umbraco administrator, I want a comprehensive Lightning payment dashboard in the backoffice so I can monitor payment activity, troubleshoot issues, and manage paywall configurations without leaving Umbraco.

**Why this priority**: Currently, payment monitoring requires API calls or database queries. A visual dashboard provides immediate value to site administrators and reduces support burden.

**Independent Test**: Can be fully tested by logging into Umbraco backoffice and viewing real-time payment statistics, recent transactions, and system health indicators.

**Acceptance Scenarios**:

1. **Given** I am logged into Umbraco backoffice, **When** I navigate to the Lightning Payments section, **Then** I see a dashboard showing total payments received, pending payments, and recent transaction history
2. **Given** the Breez SDK is disconnected, **When** I view the dashboard, **Then** I see a clear connection status indicator with troubleshooting guidance
3. **Given** payments have been received today, **When** I view the dashboard, **Then** I can see a chart of payment volume over time

---

### User Story 2 - Improved Paywall Content Editor Experience (Priority: P1)

As a content editor, I want an intuitive paywall configuration UI directly in the content editor so I can easily set up paid content without technical knowledge.

**Why this priority**: The current paywall property editor requires understanding JSON configuration. A user-friendly UI dramatically improves content editor adoption.

**Independent Test**: Can be tested by creating new content with paywall, configuring amount and description through UI, and verifying paywall enforcement.

**Acceptance Scenarios**:

1. **Given** I am editing content with a paywall property, **When** I toggle paywall enabled, **Then** I see fields for tiered pricing (1h/8h/24h/7d durations with amounts in sats) and description with validation feedback
2. **Given** I enter an invalid amount (negative or exceeds maximum), **When** I try to save, **Then** I receive a clear validation error message
3. **Given** I have configured a paywall, **When** I preview the content, **Then** I see how the paywall will appear to visitors

---

### User Story 3 - Bolt12 Offer Support for Recurring Payments (Priority: P2)

As a site owner, I want to create Bolt12 offers for recurring payment use cases (subscriptions, donations) so I can accept payments without generating new invoices each time.

**Why this priority**: Bolt12 is the next-generation Lightning invoice format. The BreezSDK supports it but it's not fully exposed in the Umbraco integration.

**Independent Test**: Can be tested by creating a Bolt12 offer through the API, sharing the offer string, and receiving multiple payments to the same offer.

**Acceptance Scenarios**:

1. **Given** I configure a Bolt12 offer, **When** a user pays the offer, **Then** the payment is recorded and associated with the offer
2. **Given** I have an active Bolt12 offer, **When** multiple users pay the same offer, **Then** each payment is tracked individually
3. **Given** I want to disable an offer, **When** I mark it as inactive, **Then** no new payments are accepted to that offer

---

### User Story 4 - Payment Notification System (Priority: P2)

As a site owner, I want to receive notifications when payments are received so I can track revenue and fulfill orders promptly.

**Why this priority**: Real-time payment awareness is critical for e-commerce and service-based use cases. Current implementation broadcasts via SSE but lacks persistent notifications.

**Independent Test**: Can be tested by configuring email/webhook notifications and receiving alerts when test payments arrive.

**Acceptance Scenarios**:

1. **Given** I have configured email notifications, **When** a payment is confirmed, **Then** I receive an email with payment details (amount, content, timestamp)
2. **Given** I have configured a webhook URL, **When** a payment is confirmed, **Then** my endpoint receives a signed payload with payment information
3. **Given** notification delivery fails, **When** the system retries, **Then** failed deliveries are logged and can be manually retried

---

### User Story 5 - Payment Refund/Return Capability (Priority: P3)

As an Administrator, I want to issue refunds for mistaken or disputed payments so I can maintain good customer relationships while ensuring only authorized personnel can disburse funds.

**Why this priority**: While Lightning payments are typically final, the BreezSDK supports sending payments which enables refund workflows. This is important for customer service.

**Independent Test**: Can be tested by initiating a refund for a received payment and verifying funds are sent to the customer's Lightning address.

**Acceptance Scenarios**:

1. **Given** I have received a payment, **When** I initiate a refund with a Lightning invoice, **Then** the refund is processed and linked to the original payment
2. **Given** I attempt to refund more than the original amount, **When** I submit the refund, **Then** I receive a validation error
3. **Given** a refund fails due to insufficient balance, **When** I view the refund status, **Then** I see a clear error message with balance information

---

### User Story 6 - Multi-Currency Display (Priority: P3)

As a site visitor, I want to see payment amounts in my preferred fiat currency so I can understand the real cost before paying.

**Why this priority**: Bitcoin/satoshi amounts are unfamiliar to most users. Showing fiat equivalents reduces friction and improves conversion rates.

**Independent Test**: Can be tested by viewing a paywall with configured fiat display and verifying accurate conversion based on current rates.

**Acceptance Scenarios**:

1. **Given** I am viewing paywalled content, **When** the paywall displays, **Then** I see the amount in both satoshis and a configured fiat currency
2. **Given** exchange rates change, **When** I refresh the page, **Then** the fiat amount reflects current rates
3. **Given** the exchange rate service is unavailable, **When** I view the paywall, **Then** I still see the satoshi amount with a note that fiat conversion is unavailable

---

### Edge Cases

- What happens when the Breez SDK loses connection mid-payment?
- How does the system handle expired invoices that receive late payments?
- What happens if the database becomes unavailable while a payment is processing?
- How are duplicate webhook deliveries handled?
- What happens when paywall content is unpublished while payments are pending?
- How does the system behave when rate limits are exceeded?

## Requirements *(mandatory)*

### Functional Requirements

#### Backoffice Dashboard

- **FR-001**: System MUST provide a dashboard section in Umbraco backoffice showing payment statistics, accessible to Administrators and configurable user groups
- **FR-002**: System MUST display real-time connection status of the Breez SDK
- **FR-003**: System MUST show recent payment history with filtering and search capabilities
- **FR-004**: System MUST display current wallet balance and receiving limits

#### Paywall Enhancements

- **FR-005**: System MUST provide a user-friendly property editor for paywall configuration
- **FR-006**: System MUST validate paywall amounts against configured minimum and maximum limits
- **FR-007**: System MUST support content preview with paywall simulation
- **FR-008**: System MUST track payment completion per user session with tiered duration options (1 hour, 8 hours, 24 hours, 7 days) at content-editor-configurable prices per tier

#### Bolt12 Support

- **FR-009**: System MUST support creating Bolt12 offers through the Management API
- **FR-010**: System MUST track multiple payments received against a single Bolt12 offer
- **FR-011**: System MUST allow deactivating Bolt12 offers to prevent new payments

#### Notification System

- **FR-012**: System MUST support email notifications for payment events
- **FR-013**: System MUST support outbound webhook notifications with HMAC signing
- **FR-014**: System MUST retry failed notification deliveries with exponential backoff (5 retries over ~30 minutes, then mark as permanently failed)
- **FR-015**: System MUST log all notification attempts for troubleshooting

#### Refund Capability

- **FR-016**: System MUST support initiating refunds via Lightning invoice payment, restricted to Administrators only
- **FR-017**: System MUST validate refund amounts do not exceed original payment
- **FR-018**: System MUST link refund transactions to original payments for auditing

#### Multi-Currency Display

- **FR-019**: System MUST support configuring a display fiat currency
- **FR-020**: System MUST fetch exchange rates from CoinGecko API (free tier by default, with optional Pro upgrade path for higher rate limits)
- **FR-021**: System MUST cache exchange rates to minimize API calls
- **FR-022**: System MUST gracefully degrade when exchange rate service is unavailable

#### Code Quality Improvements (From Review)

- **FR-023**: System MUST have consistent exception handling across all layers
- **FR-024**: System MUST implement comprehensive input validation using FluentValidation
- **FR-025**: System MUST have consistent logging patterns with correlation IDs
- **FR-026**: System MUST expose OpenTelemetry metrics for all payment operations

### Key Entities

- **PaymentDashboardStats**: Aggregated statistics (total received, pending count, daily volume)
- **Bolt12Offer**: Reusable payment offer (OfferId, Description, AmountSat, Active, CreatedAt)
- **PaymentNotification**: Notification delivery record (PaymentHash, NotificationType, DeliveryStatus, Attempts)
- **RefundTransaction**: Refund record (RefundId, OriginalPaymentHash, AmountSat, DestinationInvoice, Status)
- **ExchangeRate**: Cached rate (Currency, RatePerBtc, FetchedAt, Source)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Administrators can view complete payment history within 3 clicks from Umbraco backoffice
- **SC-002**: Content editors can configure a paywall in under 1 minute without documentation
- **SC-003**: System supports 100 concurrent payment sessions without performance degradation
- **SC-004**: Notification delivery succeeds within 30 seconds of payment confirmation for 99% of payments
- **SC-005**: Refund processing completes within 60 seconds of initiation
- **SC-006**: Exchange rate display updates within 5 minutes of rate changes
- **SC-007**: All new code has 80% or higher test coverage
- **SC-008**: Zero security vulnerabilities identified in security audit

## Code Review Findings & Recommendations

### Strengths Identified

1. **Clean Architecture**: Good separation between BreezSDK core abstractions and Umbraco-specific code
2. **Resilience Patterns**: Comprehensive Polly policies for retries, timeouts, and circuit breakers
3. **Security Awareness**: HMAC webhook validation, ACL application, sensitive data handling
4. **Testability**: Interface-based design with mock builders in test utilities
5. **Configuration Validation**: FluentValidation on settings with clear error messages

### Areas for Improvement

#### 1. Umbraco Backoffice Integration (High Priority)

**Current State**: No backoffice UI - all management through API or database
**Recommendation**: Implement Bellissima (Lit/TypeScript) dashboard components following Umbraco v17 patterns

#### 2. Property Editor Enhancement (High Priority)

**Current State**: Basic JSON property for paywall configuration
**Recommendation**: Create a custom property editor with visual UI for paywall setup

#### 3. Missing Bolt12 Features (Medium Priority)

**Current State**: `CreateBolt12OfferAsync` exists in service but limited exposure
**Recommendation**: Full Bolt12 offer lifecycle management through API and UI

#### 4. Notification System Gaps (Medium Priority)

**Current State**: SSE broadcast only, SMTP settings exist but not connected
**Recommendation**: Complete notification pipeline with email and webhook support

#### 5. Exception Hierarchy Inconsistency (Medium Priority)

**Current State**: Mix of `BreezSdkException` subtypes and separate `PaymentException`, `WebhookException`
**Recommendation**: Consolidate under unified exception hierarchy with consistent error codes

#### 6. Missing Refund Capability (Medium Priority)

**Current State**: No refund/send payment functionality exposed
**Recommendation**: Expose BreezSDK send payment capability for refund workflows

#### 7. No Fiat Display (Low Priority)

**Current State**: All amounts in satoshis only
**Recommendation**: Add exchange rate service and fiat display option

#### 8. Test Coverage Gaps (Medium Priority)

**Current State**: Good unit test coverage, limited integration tests
**Recommendation**: Add more integration tests, especially for paywall middleware and webhook flows

## Assumptions

- Umbraco v17 with Bellissima backoffice is the target platform
- BreezSDK Liquid is the payment backend (not Greenlight)
- SQLite is acceptable for default storage (with PostgreSQL/SQL Server options)
- Single-tenant deployment (one wallet per Umbraco instance)
- CoinGecko API is the exchange rate source (free tier default, Pro upgrade optional)

## Out of Scope

- Multi-tenant/multi-wallet support
- On-chain Bitcoin payments (Lightning only)
- Fiat payment gateway integration
- Subscription management/recurring billing logic
- Advanced fraud detection
- Mobile app integration

## Dependencies

- Umbraco CMS v17+
- BreezSDK Liquid .NET bindings
- .NET 9.0
- Entity Framework Core 9.0
- Polly v8 for resilience
- OpenTelemetry for observability

## Risks

- **BreezSDK API Changes**: SDK is evolving; interface changes may require updates
- **Exchange Rate Service Reliability**: Third-party dependency for fiat display
- **Backoffice UI Complexity**: Bellissima development requires TypeScript/Lit expertise
- **Testing Real Payments**: Integration testing requires testnet setup

---

*This specification is based on a comprehensive code review conducted on 2026-01-26. Implementation should follow the Spec-Kit workflow: `/speckit.plan` → `/speckit.tasks` → `/speckit.implement`*
