# Feature Specification: BreezSDK Expert Agents for Claude Code

**Feature Branch**: `001-breezsdk-agents`
**Created**: 2026-01-24
**Status**: Draft
**Input**: User description: "Create a suite of new BreezSDK agents for use within Claude Code and Spec Kit focusing knowledge and learning on the C# implementation of BreezSDK"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - BreezSDK Developer Implementation (Priority: P1)

A developer using Claude Code needs to implement BreezSDK functionality in their C# application. They invoke the BreezSDK developer agent to receive expert guidance on SDK integration, including proper initialization, payment handling, and error management following BreezSDK best practices.

**Why this priority**: This is the core use case - developers need implementation expertise to correctly integrate BreezSDK into their applications. Without proper implementation guidance, developers may create insecure or non-functional integrations.

**Independent Test**: Can be fully tested by having the agent assist with implementing a basic BreezSDK connection and payment flow, delivering working C# code that follows SDK patterns.

**Acceptance Scenarios**:

1. **Given** a developer asks for help implementing BreezSDK connection, **When** the breezsdk-developer agent is invoked, **Then** it provides correct C# code using `BreezSdkLiquidMethods.Connect()` with proper configuration and error handling.
2. **Given** a developer needs to implement payment receiving, **When** the agent assists, **Then** it provides code using the two-step pattern (`PrepareReceivePayment` + `ReceivePayment`) with appropriate payment method selection.
3. **Given** a developer asks about event handling, **When** the agent responds, **Then** it provides a complete `EventListener` implementation with proper listener registration and cleanup.

---

### User Story 2 - BreezSDK Architecture Planning (Priority: P1)

A solution architect planning a new feature that involves BreezSDK integration needs architectural guidance on how to structure their application, handle wallet state, manage multiple assets, and design for production readiness.

**Why this priority**: Architectural decisions made early significantly impact maintainability, security, and scalability. Poor architecture leads to costly rewrites.

**Independent Test**: Can be fully tested by having the agent provide architectural recommendations for a BreezSDK integration scenario, delivering a coherent design that addresses wallet management, event handling, and error recovery.

**Acceptance Scenarios**:

1. **Given** an architect asks about structuring BreezSDK integration in a Clean Architecture application, **When** the breezsdk-architect agent is invoked, **Then** it provides guidance on layering, dependency injection patterns for `BindingLiquidSdk`, and separation of concerns.
2. **Given** a request for production readiness guidance, **When** the agent responds, **Then** it covers logging requirements, payment status handling, swap refund management, and fee transparency.
3. **Given** a multi-asset support question, **When** the agent assists, **Then** it explains asset metadata configuration, asset-specific payments, and fee handling options.

---

### User Story 3 - BreezSDK Code Review (Priority: P2)

A developer has completed BreezSDK integration code and needs it reviewed against SDK best practices, proper error handling, security considerations, and UX guidelines compliance.

**Why this priority**: Code review catches integration mistakes before they reach production. BreezSDK deals with financial transactions where errors can be costly.

**Independent Test**: Can be fully tested by submitting BreezSDK integration code to the agent and receiving a detailed review with specific issues and recommendations.

**Acceptance Scenarios**:

1. **Given** code that calls `PrepareSendPayment` without checking limits first, **When** the breezsdk-reviewer agent reviews it, **Then** it identifies the missing limits check and recommends calling `FetchOnchainLimits()` or similar.
2. **Given** code with bare exception handling (`catch (Exception)`), **When** reviewed, **Then** the agent recommends specific exception handling based on SDK error types.
3. **Given** code that exposes Liquid addresses directly to users, **When** reviewed, **Then** the agent flags this as a UX guideline violation and recommends using Lightning addresses or LNURL-Pay QR codes.

---

### User Story 4 - BreezSDK UX Compliance (Priority: P2)

A frontend developer implementing BreezSDK payment flows needs guidance on UX best practices for receiving payments, sending payments, displaying transaction history, and managing seed phrases according to BreezSDK UX guidelines.

**Why this priority**: Proper UX ensures users can successfully complete payment flows and manage their wallets securely. Poor UX leads to failed transactions and security issues.

**Independent Test**: Can be fully tested by asking for UX guidance on a specific flow and receiving recommendations aligned with BreezSDK documentation.

**Acceptance Scenarios**:

1. **Given** a request for receive payment UX design, **When** the breezsdk-ux agent is invoked, **Then** it recommends showing LNURL-Pay QR by default with human-readable Lightning address and Copy/Share buttons.
2. **Given** a question about send payment interface design, **When** the agent responds, **Then** it recommends a unified entry point supporting paste/scan/upload with fees and limits displayed before confirmation.
3. **Given** a question about seed backup timing, **When** the agent assists, **Then** it recommends deferring backup until after first payment with verification via partial re-entry.

---

### User Story 5 - BreezSDK Test Implementation (Priority: P3)

A test engineer needs to write tests for BreezSDK integration code, including unit tests for payment logic, integration tests for SDK interactions, and test patterns for async event-driven operations.

**Why this priority**: Testing BreezSDK integrations requires specialized knowledge about mocking SDK behaviors, handling async events, and simulating payment flows.

**Independent Test**: Can be fully tested by requesting test implementation for a BreezSDK payment flow and receiving working test code with appropriate mocking patterns.

**Acceptance Scenarios**:

1. **Given** a request to test payment preparation logic, **When** the breezsdk-test-engineer agent is invoked, **Then** it provides tests that mock `PrepareSendPayment` responses and verify fee handling.
2. **Given** a request to test event listener code, **When** the agent assists, **Then** it provides test patterns for verifying event subscription, handling, and cleanup.
3. **Given** code that interacts with fiat rates, **When** test guidance is requested, **Then** the agent provides patterns for mocking `FetchFiatRates()` responses.

---

### Edge Cases

- What happens when an agent is asked about BreezSDK features not available in the C# binding? The agent should note the limitation and suggest alternatives or indicate the feature is only available in other language bindings.
- How does the system handle questions mixing BreezSDK Nodeless (Liquid) with the deprecated Greenlight version? Agents should clarify the distinction and focus on Nodeless/Liquid implementation.
- What if a user asks about unsupported assets or currencies? Agents should reference the supported asset list and explain configuration requirements for custom assets.

**Edge Case Acceptance Scenarios**:

1. **Given** a user asks about a feature only available in Kotlin/Swift bindings (e.g., a platform-specific API), **When** any BreezSDK agent responds, **Then** it clearly states "This feature is not available in the C# binding" and suggests the closest alternative or workaround if one exists.

2. **Given** a user references "BreezSDK" without specifying Liquid vs Greenlight, **When** any agent responds, **Then** it clarifies: "This guidance applies to BreezSDK Liquid (Nodeless). The Greenlight version uses different APIs and is deprecated."

3. **Given** a user asks about an unsupported asset (e.g., a token not in the default asset list), **When** the breezsdk-developer or breezsdk-architect agent responds, **Then** it explains the asset configuration requirements and references `AssetMetadata` configuration patterns from the knowledge file.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST include a `breezsdk-developer` agent that provides C# implementation guidance for all core SDK operations (connection, payments, events, logging, configuration).
- **FR-002**: System MUST include a `breezsdk-architect` agent that provides architectural guidance for BreezSDK integration including Clean Architecture patterns, production readiness, and multi-asset support.
- **FR-003**: System MUST include a `breezsdk-reviewer` agent that reviews code against BreezSDK best practices, security considerations, and UX guidelines.
- **FR-004**: System MUST include a `breezsdk-ux` agent that provides UX guidance aligned with official BreezSDK UX guidelines for payment flows and key management.
- **FR-005**: System MUST include a `breezsdk-test-engineer` agent that provides testing patterns for BreezSDK integration code.
- **FR-006**: System MUST create a comprehensive knowledge reference file containing all BreezSDK C# patterns, code examples, and guidelines for agent reference.
- **FR-007**: All BreezSDK agents MUST follow the existing agent file format and structure used by other agents in the project (frontmatter with name, description, tools, model; expertise section; when invoked section; output format; compliance section).
- **FR-008**: System MUST update CLAUDE.md to include BreezSDK agents in the Available Agents table with appropriate model assignments and phase mappings.
- **FR-009**: System MUST update the Agent & Plugin Utilization by Phase table in CLAUDE.md to specify when BreezSDK agents should be invoked during spec kit phases.
- **FR-010**: Each agent MUST have clearly defined tool access permissions consistent with their role (read-only for reviewers, write access for developers).
- **FR-011**: The knowledge reference file MUST include C# code snippets for all major SDK operations: connection, receiving payments, sending payments, LNURL operations, on-chain transactions, event handling, logging, and multi-asset support.
- **FR-012**: Agents MUST reference the knowledge file for domain-specific guidance while following existing project constitution and code standards.
- **FR-013**: When an SDK operation is not explicitly covered in the knowledge file, agents MUST indicate the gap to the user and apply general SDK principles (prepare-then-execute pattern, proper error handling, event cleanup) rather than guessing or fabricating patterns.

### Key Entities

- **BreezSDK Agent**: A specialized Claude Code agent with domain expertise in BreezSDK C# implementation, defined in `.claude/agents/` directory.
- **Knowledge Reference File**: A comprehensive markdown file in `.claude/skills/` containing BreezSDK patterns, code examples, and guidelines that agents can reference.
- **Spec Kit Phase Integration**: Configuration in CLAUDE.md that maps BreezSDK agents to appropriate spec kit workflow phases.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All five BreezSDK agents are created with complete documentation following the existing agent template structure.
- **SC-002**: Knowledge reference file covers 100% of SDK operations documented in the official BreezSDK Liquid documentation with C# code examples.
- **SC-003**: CLAUDE.md is updated with all BreezSDK agents listed in the Available Agents table and Agent & Plugin Utilization by Phase table.
- **SC-004**: Each agent can be successfully invoked via Claude Code Task tool with the appropriate subagent_type.
- **SC-005**: Code review agent correctly identifies at least 3 common BreezSDK integration mistakes when presented with intentionally flawed code.
- **SC-006**: Developer agent produces working C# code for a basic payment flow that follows SDK patterns (prepare-then-execute, proper error handling, event listener cleanup).
- **SC-007**: UX agent recommendations align with the 4 core UX principles documented in BreezSDK: simplicity over choice, transparency without jargon, progressive disclosure, and Lightning priority.

## Clarifications

### Session 2026-01-24

- Q: Which BreezSDK documentation source(s) should be used as the authoritative reference for building the knowledge file? → A: Use official BreezSDK GitHub docs (https://github.com/breez/breez-sdk-liquid-docs) pinned to latest stable release.
- Q: How should knowledge file completeness be validated and how should agents handle SDK operations not explicitly covered? → A: Agents should reference knowledge file first; if operation not found, indicate gap and use general SDK principles.

## Assumptions

- The BreezSDK NuGet package `Breez.Sdk.Liquid` is the target C# implementation (not the deprecated Greenlight version).
- Agents will operate within the existing Claude Code and Spec Kit infrastructure without requiring new tooling.
- The project constitution (`.specify/memory/constitution.md`) applies to BreezSDK implementations alongside SDK-specific guidelines.
- Agent model assignments follow the existing pattern: Opus for architectural decisions, Sonnet for implementation, Haiku for reviews.
- The knowledge reference file will be maintained alongside SDK documentation updates but is not automatically synchronized.
- **Documentation Source**: The knowledge reference file MUST be built from the official BreezSDK GitHub documentation repository (https://github.com/breez/breez-sdk-liquid-docs), pinned to the latest stable release at time of implementation.
