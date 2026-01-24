# Tasks: BreezSDK Expert Agents for Claude Code

**Input**: Design documents from `/specs/001-breezsdk-agents/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, quickstart.md ✅

**Tests**: Per Constitution Article III (Test-First Imperative), tests are normally **MANDATORY**. However, this feature creates documentation artifacts (agents, knowledge files) rather than production code. Verification is performed via manual testing per plan.md Gate Decision.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

This feature creates documentation artifacts in:
- Agent files: `.claude/agents/`
- Knowledge file: `.claude/skills/`
- Configuration: `CLAUDE.md` (repository root)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and ensure directories exist

- [ ] T001 Verify `.claude/agents/` directory exists (create if needed)
- [ ] T002 Verify `.claude/skills/` directory exists (create if needed)

---

## Phase 2: Foundational (Knowledge Reference File)

**Purpose**: Create the knowledge reference file that ALL agents depend on - MUST complete before any agent can be created

**⚠️ CRITICAL**: No agent work can begin until this phase is complete - all agents reference this file

- [ ] T003 Create comprehensive BreezSDK knowledge reference file in `.claude/skills/breezsdk-knowledge.md` containing:
  - Quick Reference (package, API key, network options)
  - Connection & Configuration (DefaultConfig, Connect, Disconnect, working directory, external signer)
  - Event Handling (EventListener implementation, Add/Remove listener pattern, event types)
  - Logging (Logger implementation, SetLogger before connect)
  - Wallet State (GetInfo, balance properties)
  - Payment Operations - Receiving (BOLT11, BOLT12, Bitcoin, Liquid, PrepareReceivePayment → ReceivePayment)
  - Payment Operations - Sending (PrepareSendPayment → SendPayment, drain funds, LNURL-Pay)
  - Payment Operations - On-Chain (PreparePayOnchain → PayOnchain, fee rate customization)
  - Payment Listing & Retrieval (ListPayments with filters, GetPayment by hash/swap ID)
  - Refunds & Recovery (ListRefundables, Refund pattern, RescanOnchainSwaps)
  - LNURL Operations (Parse input types, LNURL-Pay, LNURL-Withdraw, LNURL-Auth)
  - Multi-Asset Support (asset configuration, asset-specific payments, asset exchange)
  - Fiat Currencies (ListFiatCurrencies, FetchFiatRates)
  - Message Signing (SignMessage, CheckMessage)
  - UX Guidelines Summary (core principles, receive/send/display patterns, seed management)
  - Production Checklist (logging requirements, status handling, refund management, fee transparency)

**Checkpoint**: Knowledge file ready - agent creation can now begin in parallel

---

## Phase 3: User Story 1 - BreezSDK Developer Implementation (Priority: P1) 🎯 MVP

**Goal**: Developer can invoke breezsdk-developer agent to receive C# implementation guidance

**Independent Test**: Invoke agent via Task tool, ask for help implementing BreezSDK connection, verify working C# code is returned with proper patterns

### Implementation for User Story 1

- [ ] T004 [US1] Create breezsdk-developer agent in `.claude/agents/breezsdk-developer.md` with:
  - Frontmatter: name, description, tools (Read, Write, Edit, Bash, Glob, Grep), model (sonnet)
  - Role introduction paragraph
  - Your Expertise section (7 competencies: SDK bindings, two-step patterns, event listeners, configuration, error handling, multi-asset, LNURL)
  - When Invoked workflow (5 steps: read knowledge file, apply two-step pattern, implement event cleanup, use async patterns, follow SDK error handling)
  - Code Standards section with C# examples
  - Output Format section
  - BreezSDK Compliance section (SDK-specific guidelines)
  - Constitutional Compliance section

**Checkpoint**: User Story 1 complete - breezsdk-developer agent functional

---

## Phase 4: User Story 2 - BreezSDK Architecture Planning (Priority: P1)

**Goal**: Architect can invoke breezsdk-architect agent for integration architecture guidance

**Independent Test**: Invoke agent, ask for Clean Architecture integration guidance, verify coherent design recommendations

### Implementation for User Story 2

- [ ] T005 [US2] Create breezsdk-architect agent in `.claude/agents/breezsdk-architect.md` with:
  - Frontmatter: name, description, tools (Read, Glob, Grep), model (opus)
  - Role introduction paragraph
  - Your Expertise section (6 competencies: Clean Architecture patterns, DI registration, wallet state management, multi-asset architecture, production readiness, offline payment architecture)
  - When Invoked workflow (5 steps: analyze existing architecture, recommend SDK integration layer, design event handling strategy, plan error recovery, address production readiness)
  - Architecture Patterns section
  - Output Format section
  - BreezSDK Compliance section

**Checkpoint**: User Story 2 complete - breezsdk-architect agent functional

---

## Phase 5: User Story 3 - BreezSDK Code Review (Priority: P2)

**Goal**: Developer can invoke breezsdk-reviewer agent to review code against SDK best practices

**Independent Test**: Submit intentionally flawed BreezSDK code, verify agent identifies missing limit checks, bare exception handling, UX violations

### Implementation for User Story 3

- [ ] T006 [US3] Create breezsdk-reviewer agent in `.claude/agents/breezsdk-reviewer.md` with:
  - Frontmatter: name, description, tools (Read, Grep, Glob), model (haiku)
  - Role introduction paragraph
  - Your Expertise section (5 competencies: SDK pattern compliance, common integration mistakes, UX guideline violations, security considerations, error handling review)
  - When Invoked workflow (5 steps: check for missing limit checks, verify two-step pattern usage, review event listener cleanup, check for UX violations, identify security issues)
  - Common Issues to Flag section (missing FetchLightningLimits/FetchOnchainLimits, Liquid address exposure, missing event listener cleanup, generic exception handling, missing fee transparency)
  - Output Format section
  - BreezSDK Compliance section

**Checkpoint**: User Story 3 complete - breezsdk-reviewer agent functional

---

## Phase 6: User Story 4 - BreezSDK UX Compliance (Priority: P2)

**Goal**: Frontend developer can invoke breezsdk-ux agent for payment flow UX guidance

**Independent Test**: Ask for receive payment UX design, verify recommendations align with 4 core UX principles

### Implementation for User Story 4

- [ ] T007 [US4] Create breezsdk-ux agent in `.claude/agents/breezsdk-ux.md` with:
  - Frontmatter: name, description, tools (Read, Grep, Glob), model (sonnet)
  - Role introduction paragraph
  - Your Expertise section (5 competencies: receive payment UX, send payment UX, payment display UX, seed/key management UX, core UX principles)
  - When Invoked workflow (steps for UX guidance)
  - UX Guidelines section covering:
    - Core principles (simplicity over choice, transparency without jargon, progressive disclosure, Lightning priority)
    - Receive payment patterns (LNURL-Pay QR, Lightning address, Copy/Share buttons)
    - Send payment patterns (unified entry, paste/scan/upload, fees before confirmation)
    - Seed management (defer backup until after first payment, verification via partial re-entry)
  - Output Format section
  - BreezSDK Compliance section

**Checkpoint**: User Story 4 complete - breezsdk-ux agent functional

---

## Phase 7: User Story 5 - BreezSDK Test Implementation (Priority: P3)

**Goal**: Test engineer can invoke breezsdk-test-engineer agent for testing patterns

**Independent Test**: Request test patterns for payment preparation logic, verify working mock patterns returned

### Implementation for User Story 5

- [ ] T008 [US5] Create breezsdk-test-engineer agent in `.claude/agents/breezsdk-test-engineer.md` with:
  - Frontmatter: name, description, tools (Read, Write, Edit, Bash, Glob, Grep), model (sonnet)
  - Role introduction paragraph
  - Your Expertise section (6 competencies: mocking SDK responses, testing payment preparation, event listener testing, async operation testing, fiat rate mocking, integration test strategies)
  - When Invoked workflow (5 steps: design mock interfaces, test prepare-then-execute flows, verify event subscription/unsubscription, test error handling paths, validate fee calculations)
  - Test Patterns section with code examples
  - Output Format section
  - Constitutional Compliance section (Article III: Test-First Imperative)

**Checkpoint**: User Story 5 complete - breezsdk-test-engineer agent functional

---

## Phase 8: Integration (CLAUDE.md Updates)

**Purpose**: Register all agents in project configuration

**Dependencies**: All 5 agents must be created first (T004-T008)

- [ ] T009 Update CLAUDE.md Available Agents table to add:
  - `breezsdk-developer` | Sonnet | BreezSDK C# implementation, payment flows, event handling
  - `breezsdk-architect` | Opus | BreezSDK integration architecture, production readiness
  - `breezsdk-reviewer` | Haiku | BreezSDK code review, SDK pattern compliance
  - `breezsdk-ux` | Sonnet | BreezSDK UX guidelines, payment flow design
  - `breezsdk-test-engineer` | Sonnet | BreezSDK testing patterns, mock strategies

- [ ] T010 Update CLAUDE.md Agent Tools & Permissions table to add:
  - `breezsdk-developer` | Read, Write, Edit, Bash, Glob, Grep | ✅ Yes
  - `breezsdk-architect` | Read, Glob, Grep | ❌ Read-only
  - `breezsdk-reviewer` | Read, Grep, Glob | ❌ Read-only
  - `breezsdk-ux` | Read, Grep, Glob | ❌ Read-only
  - `breezsdk-test-engineer` | Read, Write, Edit, Bash, Glob, Grep | ✅ Yes

- [ ] T011 Update CLAUDE.md Agent & Plugin Utilization by Phase table to add BreezSDK agent usage:
  - `/speckit.plan` phase: breezsdk-architect for integration design
  - `/speckit.implement` phase: breezsdk-developer (primary), breezsdk-test-engineer
  - Post-implement: breezsdk-reviewer, breezsdk-ux

**Checkpoint**: All agents registered in CLAUDE.md

---

## Phase 9: Validation

**Purpose**: Verify all agents work correctly

- [ ] T012 [P] Verify breezsdk-developer agent invocation works via Task tool
- [ ] T013 [P] Verify breezsdk-architect agent invocation works via Task tool
- [ ] T014 [P] Verify breezsdk-reviewer agent invocation works via Task tool
- [ ] T015 [P] Verify breezsdk-ux agent invocation works via Task tool
- [ ] T016 [P] Verify breezsdk-test-engineer agent invocation works via Task tool
- [ ] T017 Verify knowledge file is referenced correctly by agents

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and documentation

- [ ] T018 Run quickstart.md validation - verify all usage examples work
- [ ] T019 Verify SC-005: breezsdk-reviewer identifies at least 3 common mistakes when given flawed code
- [ ] T020 Verify SC-006: breezsdk-developer produces working C# code for basic payment flow
- [ ] T021 Verify SC-007: breezsdk-ux recommendations align with 4 core UX principles

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup - BLOCKS all agent creation
- **User Stories (Phases 3-7)**: All depend on Foundational phase completion
  - US1 (P1) and US2 (P1) can proceed in parallel
  - US3 (P2) and US4 (P2) can proceed in parallel
  - US5 (P3) can proceed in parallel with any story
- **Integration (Phase 8)**: Depends on ALL user stories complete (T004-T008)
- **Validation (Phase 9)**: Depends on Integration complete
- **Polish (Phase 10)**: Depends on Validation complete

### User Story Dependencies

- **User Story 1 (P1)**: Depends only on T003 (knowledge file) - no dependencies on other stories
- **User Story 2 (P1)**: Depends only on T003 (knowledge file) - no dependencies on other stories
- **User Story 3 (P2)**: Depends only on T003 (knowledge file) - no dependencies on other stories
- **User Story 4 (P2)**: Depends only on T003 (knowledge file) - no dependencies on other stories
- **User Story 5 (P3)**: Depends only on T003 (knowledge file) - no dependencies on other stories

### Parallel Opportunities

- T001 and T002 can run in parallel (Setup)
- T004, T005, T006, T007, T008 can ALL run in parallel after T003 completes
- T009, T010, T011 must be sequential (same file: CLAUDE.md)
- T012-T016 can run in parallel (Validation - different agents)

---

## Parallel Example: Agent Creation Wave

```bash
# After T003 (knowledge file) completes, launch all agent creation in parallel:
& Task: "Create breezsdk-developer agent in .claude/agents/breezsdk-developer.md" [T004]
& Task: "Create breezsdk-architect agent in .claude/agents/breezsdk-architect.md" [T005]
& Task: "Create breezsdk-reviewer agent in .claude/agents/breezsdk-reviewer.md" [T006]
& Task: "Create breezsdk-ux agent in .claude/agents/breezsdk-ux.md" [T007]
& Task: "Create breezsdk-test-engineer agent in .claude/agents/breezsdk-test-engineer.md" [T008]
```

## Parallel Example: Validation Wave

```bash
# After CLAUDE.md updates complete, launch all validation in parallel:
& Task: "Verify breezsdk-developer agent invocation" [T012]
& Task: "Verify breezsdk-architect agent invocation" [T013]
& Task: "Verify breezsdk-reviewer agent invocation" [T014]
& Task: "Verify breezsdk-ux agent invocation" [T015]
& Task: "Verify breezsdk-test-engineer agent invocation" [T016]
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T002)
2. Complete Phase 2: Foundational (T003 - knowledge file)
3. Complete Phase 3: User Story 1 (T004 - breezsdk-developer)
4. **STOP and VALIDATE**: Test breezsdk-developer agent independently
5. Deploy/demo if ready - developer can now get C# implementation guidance

### Incremental Delivery

1. Setup + Foundational (T001-T003) → Knowledge base ready
2. Add User Story 1 (T004) → breezsdk-developer available
3. Add User Story 2 (T005) → breezsdk-architect available
4. Add User Story 3 (T006) → breezsdk-reviewer available
5. Add User Story 4 (T007) → breezsdk-ux available
6. Add User Story 5 (T008) → breezsdk-test-engineer available
7. Integration (T009-T011) → All agents registered
8. Each agent adds value without breaking previous agents

### Recommended Execution Order

1. **Wave 1** (Sequential): T001 → T002 → T003
2. **Wave 2** (Parallel): T004, T005, T006, T007, T008 (all 5 agents)
3. **Wave 3** (Sequential): T009 → T010 → T011 (CLAUDE.md updates)
4. **Wave 4** (Parallel): T012-T016 (validation)
5. **Wave 5** (Sequential): T017 → T018 → T019 → T020 → T021 (polish)

---

## Summary

| Category | Count |
|----------|-------|
| Total Tasks | 21 |
| Setup Tasks | 2 |
| Foundational Tasks | 1 |
| User Story 1 Tasks | 1 |
| User Story 2 Tasks | 1 |
| User Story 3 Tasks | 1 |
| User Story 4 Tasks | 1 |
| User Story 5 Tasks | 1 |
| Integration Tasks | 3 |
| Validation Tasks | 6 |
| Polish Tasks | 4 |
| Parallel Opportunities | 10 tasks can run in parallel (T004-T008, T012-T016) |

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story creates one agent file that can be tested independently
- Knowledge file (T003) is the critical foundation - all agents depend on it
- CLAUDE.md updates (T009-T011) must be sequential (same file)
- Validation can be highly parallelized once integration is complete
