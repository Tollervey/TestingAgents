# Implementation Plan: BreezSDK Expert Agents for Claude Code

**Branch**: `001-breezsdk-agents` | **Date**: 2026-01-24 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-breezsdk-agents/spec.md`

## Summary

Create a suite of five specialized BreezSDK agents for Claude Code focusing on C# implementation expertise. The agents (developer, architect, reviewer, ux, test-engineer) will reference a comprehensive knowledge file containing all BreezSDK Liquid C# patterns and code examples. CLAUDE.md will be updated to register these agents in the Available Agents table and phase mapping.

## Technical Context

**Language/Version**: C# 12+ / .NET 8+ (target environment for BreezSDK integrations)
**Primary Dependencies**: Breez.Sdk.Liquid NuGet package
**Storage**: N/A (this is a knowledge/agent creation feature, not a code implementation)
**Testing**: Manual verification of agent invocation and response quality
**Target Platform**: Claude Code CLI (Windows/macOS/Linux)
**Project Type**: Single (knowledge files and agent definitions)
**Performance Goals**: N/A (documentation artifacts)
**Constraints**: Agents must follow existing file format; knowledge file must cover all SDK operations
**Scale/Scope**: 5 agent files, 1 knowledge reference file, CLAUDE.md updates

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| I.1 Clean Architecture | ✅ PASS | N/A - no code implementation |
| II.3 Explicit Over Implicit | ✅ PASS | Agent files use explicit YAML frontmatter |
| II.4 Self-Documenting Code | ✅ PASS | Knowledge file will include documented examples |
| III.1 Test-First Imperative | ⚠️ DEFER | Agents are documentation; verification via manual testing |
| IX.1 YAGNI Enforcement | ✅ PASS | Only implementing required agents per spec |
| X.1 Living Documentation | ✅ PASS | Knowledge file serves as living documentation |

**Gate Decision**: PASS - This feature creates documentation artifacts (agents, knowledge file) rather than production code. TDD is not applicable but verification criteria are defined in spec.md.

## Project Structure

### Documentation (this feature)

```text
specs/001-breezsdk-agents/
├── plan.md              # This file
├── research.md          # Phase 0 output (BreezSDK documentation analysis)
├── data-model.md        # Phase 1 output (agent structure and knowledge schema)
├── quickstart.md        # Phase 1 output (how to use BreezSDK agents)
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
.claude/
├── agents/
│   ├── breezsdk-developer.md    # New: C# implementation agent
│   ├── breezsdk-architect.md    # New: Architecture guidance agent
│   ├── breezsdk-reviewer.md     # New: Code review agent
│   ├── breezsdk-ux.md           # New: UX guidelines agent
│   └── breezsdk-test-engineer.md # New: Testing patterns agent
└── skills/
    └── breezsdk-knowledge.md    # New: Comprehensive SDK reference

CLAUDE.md                        # Update: Add agents to tables
```

**Structure Decision**: Single project structure using existing `.claude/agents/` and `.claude/skills/` directories for agent definitions and knowledge files respectively. No new directories needed.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | This is a documentation feature with minimal complexity |

---

## Phase 0: Research & Knowledge Gathering

### Research Summary

All BreezSDK Liquid documentation has been reviewed and C# code examples extracted. Key findings:

#### SDK Overview
- **Package**: `Breez.Sdk.Liquid` (NuGet)
- **Network**: Liquid sidechain with Lightning via submarine swaps
- **Key Feature**: Self-custodial, keys held by users only
- **API Key**: Required for SDK operation (different from Greenlight version)

#### Core Patterns Identified

1. **Connection Pattern**
   - `BreezSdkLiquidMethods.DefaultConfig()` for configuration
   - `BreezSdkLiquidMethods.Connect()` for connection
   - `sdk.Disconnect()` for cleanup
   - Working directory configuration required

2. **Two-Step Payment Pattern** (prepare-then-execute)
   - `PrepareReceivePayment()` → `ReceivePayment()`
   - `PrepareSendPayment()` → `SendPayment()`
   - `PreparePayOnchain()` → `PayOnchain()`
   - `PrepareLnurlPay()` → `LnurlPay()`
   - Always check limits first (`FetchLightningLimits()`, `FetchOnchainLimits()`)

3. **Event Handling Pattern**
   - Implement `EventListener` interface
   - `sdk.AddEventListener()` returns listener ID
   - `sdk.RemoveEventListener(listenerId)` for cleanup
   - Proper cleanup is essential

4. **Logging Pattern**
   - Implement `Logger` interface
   - `BreezSdkLiquidMethods.SetLogger()` before connect
   - LogEntry contains level and line properties

5. **Input Parsing Pattern**
   - `sdk.Parse(input)` returns typed InputType
   - Switch on InputType variants (BitcoinAddress, Bolt11, Bolt12Offer, LnUrlPay, LnUrlWithdraw)

#### Payment Methods Supported
- Lightning: BOLT11 invoices, BOLT12 offers
- Bitcoin: On-chain addresses
- Liquid: Direct addresses
- LNURL: Pay, Withdraw, Auth
- BIP353: Human-readable Lightning addresses

#### UX Principles Documented
1. **Simplicity over choice**: Don't make users pick protocols
2. **Transparency without jargon**: Show fees/limits in plain language
3. **Progressive disclosure**: Advanced details tucked away
4. **Lightning priority**: Present Lightning as primary, on-chain as fallback

#### Production Requirements
1. Logging implementation (DEBUG level minimum)
2. Payment status monitoring
3. Swap refund management (Refundable state handling)
4. Fee transparency (display fees prominently)

---

## Phase 1: Design & Contracts

### Agent Design Schema

Each agent follows the established format:

```yaml
---
name: breezsdk-{role}
description: {one-line purpose and invocation trigger}
tools: {Read, Write, Edit, Bash, Glob, Grep as appropriate}
model: {opus|sonnet|haiku}
---

# Content sections:
- Role introduction
- ## Your Expertise (bulleted competencies)
- ## When Invoked (numbered workflow)
- ## Code Standards (if applicable)
- ## Output Format
- ## {Role-specific sections}
- ## BreezSDK Compliance (SDK-specific guidelines)
- ## Constitutional Compliance (if applicable)
```

### Agent Specifications

#### 1. breezsdk-developer (Sonnet, Write-capable)

**Purpose**: C# implementation guidance for all SDK operations
**Tools**: Read, Write, Edit, Bash, Glob, Grep
**Model**: sonnet

**Expertise Areas**:
- BreezSDK Liquid C# bindings
- Two-step payment patterns (prepare-then-execute)
- Event listener implementation and cleanup
- Configuration and connection management
- Error handling for SDK operations
- Multi-asset support (BTC, USDt, custom assets)
- LNURL/Lightning address integration

**Key Workflow**:
1. Read knowledge file for SDK patterns
2. Apply two-step payment pattern
3. Implement proper event listener cleanup
4. Use async patterns throughout
5. Follow SDK error handling conventions

#### 2. breezsdk-architect (Opus, Read-only)

**Purpose**: Architectural guidance for BreezSDK integration
**Tools**: Read, Glob, Grep
**Model**: opus

**Expertise Areas**:
- Clean Architecture integration patterns
- DI registration for `BindingLiquidSdk`
- Wallet state management strategies
- Multi-asset architecture considerations
- Production readiness planning
- Offline payment architecture (LNURL-pay, BOLT12)

**Key Workflow**:
1. Analyze existing architecture
2. Recommend SDK integration layer placement
3. Design event handling strategy
4. Plan error recovery mechanisms
5. Address production readiness concerns

#### 3. breezsdk-reviewer (Haiku, Read-only)

**Purpose**: Code review against SDK best practices
**Tools**: Read, Grep, Glob
**Model**: haiku

**Expertise Areas**:
- SDK pattern compliance
- Common integration mistakes
- UX guideline violations
- Security considerations
- Error handling review

**Key Workflow**:
1. Check for missing limit checks
2. Verify two-step pattern usage
3. Review event listener cleanup
4. Check for UX guideline violations
5. Identify security issues

**Common Issues to Flag**:
- Missing `FetchLightningLimits()` / `FetchOnchainLimits()` before payment
- Direct Liquid address exposure to users
- Missing event listener cleanup
- Generic exception handling
- Missing fee transparency

#### 4. breezsdk-ux (Sonnet, Read-only)

**Purpose**: UX guidance per official BreezSDK guidelines
**Tools**: Read, Grep, Glob
**Model**: sonnet

**Expertise Areas**:
- Receive payment UX (QR codes, Lightning addresses)
- Send payment UX (unified entry, fee display)
- Payment display UX (status, history, progressive disclosure)
- Seed/key management UX (backup timing, verification)
- Core UX principles application

**Key Guidelines**:
1. Show LNURL-Pay QR by default
2. Provide human-readable Lightning address
3. Unified send entry point (paste/scan/upload)
4. Display fees and limits before confirmation
5. Defer seed backup until after first payment
6. Progressive disclosure for technical details

#### 5. breezsdk-test-engineer (Sonnet, Write-capable)

**Purpose**: Testing patterns for SDK integrations
**Tools**: Read, Write, Edit, Bash, Glob, Grep
**Model**: sonnet

**Expertise Areas**:
- Mocking SDK responses
- Testing payment preparation logic
- Event listener testing patterns
- Async operation testing
- Fiat rate mocking
- Integration test strategies

**Key Workflow**:
1. Design mock interfaces for SDK types
2. Test prepare-then-execute flows
3. Verify event subscription/unsubscription
4. Test error handling paths
5. Validate fee calculations

### Knowledge File Schema

The `breezsdk-knowledge.md` file will be organized as:

```markdown
---
name: breezsdk-knowledge
description: Comprehensive BreezSDK Liquid C# reference for agent consultation
globs:
  - "**/*.cs"
  - "**/*.razor"
---

# BreezSDK Liquid C# Reference

## Quick Reference
- Package, API key requirement, network options

## Connection & Configuration
- DefaultConfig, Connect, Disconnect
- Working directory setup
- External signer connection

## Event Handling
- EventListener implementation
- Add/Remove listener pattern
- Event types

## Logging
- Logger implementation
- SetLogger before connect

## Wallet State
- GetInfo, balance properties

## Payment Operations
### Receiving Payments
- BOLT11, BOLT12, Bitcoin, Liquid methods
- PrepareReceivePayment → ReceivePayment
- Fee acceptance for Bitcoin

### Sending Payments
- PrepareSendPayment → SendPayment
- Drain funds pattern
- LNURL-Pay flow

### On-Chain Payments
- PreparePayOnchain → PayOnchain
- Fee rate customization

### Payment Listing & Retrieval
- ListPayments with filters
- GetPayment by hash or swap ID

## Refunds & Recovery
- ListRefundables
- Refund pattern
- RescanOnchainSwaps

## LNURL Operations
- Parse input types
- LNURL-Pay, LNURL-Withdraw, LNURL-Auth

## Multi-Asset Support
- Asset configuration
- Asset-specific payments
- Asset exchange (self-payment swap)

## Fiat Currencies
- ListFiatCurrencies, FetchFiatRates

## Message Signing
- SignMessage, CheckMessage

## UX Guidelines Summary
- Core principles
- Receive/Send/Display patterns
- Seed management

## Production Checklist
- Logging requirements
- Status handling
- Refund management
- Fee transparency
```

### CLAUDE.md Updates

**Available Agents Table** additions:

| Agent | Model | Invoke For |
|-------|-------|------------|
| `breezsdk-developer` | Sonnet | BreezSDK C# implementation, payment flows, event handling |
| `breezsdk-architect` | Opus | BreezSDK integration architecture, production readiness |
| `breezsdk-reviewer` | Haiku | BreezSDK code review, SDK pattern compliance |
| `breezsdk-ux` | Sonnet | BreezSDK UX guidelines, payment flow design |
| `breezsdk-test-engineer` | Sonnet | BreezSDK testing patterns, mock strategies |

**Agent Tools & Permissions Table** additions:

| Agent | Tools | Can Modify Files? |
|-------|-------|-------------------|
| `breezsdk-developer` | Read, Write, Edit, Bash, Glob, Grep | ✅ Yes |
| `breezsdk-architect` | Read, Glob, Grep | ❌ Read-only |
| `breezsdk-reviewer` | Read, Grep, Glob | ❌ Read-only |
| `breezsdk-ux` | Read, Grep, Glob | ❌ Read-only |
| `breezsdk-test-engineer` | Read, Write, Edit, Bash, Glob, Grep | ✅ Yes |

**Agent & Plugin Utilization by Phase** additions:

| Phase | BreezSDK Agent Usage |
|-------|---------------------|
| `/speckit.plan` | breezsdk-architect for integration design |
| `/speckit.implement` | breezsdk-developer (primary), breezsdk-test-engineer |
| Post-implement | breezsdk-reviewer, breezsdk-ux |

---

## Implementation Waves

### Wave 1: Foundation (Sequential)
1. **T001**: Create knowledge reference file (`breezsdk-knowledge.md`)
   - Comprehensive SDK documentation
   - All C# code examples
   - UX guidelines summary

### Wave 2: Core Agents (Parallel)
2. **T002**: Create `breezsdk-developer.md` [P]
3. **T003**: Create `breezsdk-architect.md` [P]
4. **T004**: Create `breezsdk-reviewer.md` [P]

### Wave 3: Supporting Agents (Parallel)
5. **T005**: Create `breezsdk-ux.md` [P]
6. **T006**: Create `breezsdk-test-engineer.md` [P]

### Wave 4: Integration (Sequential)
7. **T007**: Update CLAUDE.md with all agent registrations
8. **T008**: Create quickstart.md for this feature

### Wave 5: Validation
9. **T009**: Verify agent invocation works
10. **T010**: Verify knowledge file is referenced correctly

---

## Risk Assessment

| Risk | Mitigation |
|------|------------|
| SDK documentation gaps | Agents will indicate gaps per FR-013 |
| Agent format mismatch | Use existing agents as exact templates |
| Knowledge file size | Organize by topic with clear headers |
| Model selection wrong | Follow existing pattern (Opus=arch, Sonnet=impl, Haiku=review) |

---

## Next Steps

1. Run `/speckit.tasks` to generate detailed task breakdown
2. Or run `/speckit.checklist` for quality gates before implementation
3. Execute implementation with parallel agent invocation for Wave 2/3

---

**Plan Status**: ✅ READY FOR TASKS GENERATION
