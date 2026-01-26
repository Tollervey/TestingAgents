# Spec-Kit & Claude Code Demo Briefing

**Prepared for**: Technical Demo Attendees
**Date**: January 2026
**Project**: BreezSDK Production-Ready NuGet Package Suite

---

## Executive Summary

This document provides technical context for a demonstration of **Spec-Kit**, a structured specification-to-implementation workflow, integrated with **Claude Code** multi-agent orchestration. The demo showcases how AI-assisted development can transform an existing codebase into a production-ready, well-architected solution through governance-driven automation.

The subject project delivers a suite of 6 NuGet packages enabling Bitcoin Lightning payment integration for any .NET application, implemented across 10 phases with 170+ tasks using specialized AI agents.

---

## 1. Project Background

### The Problem

An existing Umbraco-coupled BreezSDK integration needed transformation into a reusable, production-ready library. The original implementation had:

- Tight coupling to Umbraco CMS
- No formal test coverage
- Inconsistent error handling
- Missing observability infrastructure
- Single-database dependency

### The Solution

Using Spec-Kit and Claude Code, the codebase was systematically redesigned and implemented as a modular NuGet package suite:

| Package | Purpose |
|---------|---------|
| `Breez.Sdk.Liquid.Extensions.Core` | Platform-agnostic abstractions, domain entities, DI registration |
| `Breez.Sdk.Liquid.Extensions.AspNetCore` | Webhook endpoints, health checks, middleware |
| `Breez.Sdk.Liquid.Extensions.SqlServer` | EF Core persistence (SQL Server) |
| `Breez.Sdk.Liquid.Extensions.PostgreSql` | EF Core persistence (PostgreSQL) |
| `Breez.Sdk.Liquid.Extensions.Sqlite` | EF Core persistence (SQLite) |
| `Breez.Sdk.Liquid.Extensions.Umbraco` | Umbraco CMS integration (backoffice, property editors) |

**Outcomes**:
- 80%+ test coverage with TDD methodology
- Clean Architecture compliance
- Multi-database support
- OpenTelemetry observability
- Polly-based resilience patterns
- Complete API documentation

---

## 2. Spec-Kit Workflow

Spec-Kit implements an 8-phase workflow that transforms natural language requirements into tested, production code.

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  /constitution  │───▶│    /specify     │───▶│    /clarify     │
│  Governance     │    │  Requirements   │    │  Refinement     │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                                       │
       ┌───────────────────────────────────────────────┘
       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│     /plan       │───▶│    /tasks       │───▶│   /checklist    │
│  Architecture   │    │  Task Breakdown │    │  Quality Gates  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                                       │
       ┌───────────────────────────────────────────────┘
       ▼
┌─────────────────┐    ┌─────────────────┐
│    /analyze     │───▶│   /implement    │
│  Validation     │    │  Execution      │
└─────────────────┘    └─────────────────┘
```

### Phase Descriptions

| Phase | Command | Purpose | Output |
|-------|---------|---------|--------|
| 1 | `/speckit.constitution` | Establish immutable governance principles | `constitution.md` |
| 2 | `/speckit.specify` | Define requirements as user stories | `spec.md` |
| 3 | `/speckit.clarify` | Resolve ambiguities via targeted questions | Updated `spec.md` |
| 4 | `/speckit.plan` | Technical architecture and execution strategy | `plan.md`, `data-model.md`, contracts |
| 5 | `/speckit.tasks` | Dependency-ordered task breakdown | `tasks.md` |
| 6 | `/speckit.checklist` | Quality gate criteria | `checklists/*.md` |
| 7 | `/speckit.analyze` | Cross-artifact consistency check | Validation report |
| 8 | `/speckit.implement` | Wave-based execution with agents | Production code |

### Handoff Mechanism

Each phase produces artifacts that feed the next. Commands include `handoffs:` metadata that generates clickable transition buttons, ensuring workflow continuity.

---

## 3. Constitutional Governance

The Constitution (`constitution.md`) defines 13 articles with 40+ principles that govern all development. It acts as the "law" that all agents must follow.

### Articles Overview

| Article | Domain | Key Principle |
|---------|--------|---------------|
| I | Architecture | Clean Architecture with inward dependencies |
| II | Code Quality | SRP, interface segregation, explicit over implicit |
| III | Testing | **Test-First Imperative (NON-NEGOTIABLE)** |
| IV | Data Layer | Repository pattern, migration-first |
| V | API Design | Contract-first, RESTful, versioning |
| VI | Security | Defence in depth, secrets management |
| VII | Observability | Structured logging, health checks |
| VIII | Frontend | Component-based, WCAG 2.1 AA |
| IX | Simplicity | YAGNI, dependency scrutiny |
| X | Documentation | ADRs, living documentation |
| XI | Performance | Async-first, caching strategy |
| XII | Workflow | Task independence, incremental validation |
| XIII | Governance | Constitution supremacy, amendment protocol |

### Gate Classification

Each principle has enforcement gates:

| Status | Action |
|--------|--------|
| **PASS** | Proceed |
| **WARNING** | Document deviation and proceed |
| **CRITICAL** | **BLOCK** until resolved |

### Example: Article III.1 - Test-First Imperative

```markdown
**Statement**: All production code MUST be preceded by failing tests that define
expected behavior. The Red-Green-Refactor cycle is mandatory, not optional.

**Gate Status**:
- PASS: Tests written before implementation; coverage >80%
- WARNING: Tests written after, but comprehensive
- CRITICAL: Production code without corresponding tests
```

This principle is marked **NON-NEGOTIABLE**. Every agent enforces it.

---

## 4. Multi-Agent Architecture

Claude Code orchestrates 17 specialized agents, each with defined expertise, model assignment, and tool permissions.

### Agent Categories

```
┌──────────────────────────────────────────────────────────────────┐
│                        ARCHITECTS (Read-Only)                     │
│  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐    │
│  │ solution-       │ │ breezsdk-       │ │ umbraco-        │    │
│  │ architect       │ │ architect       │ │ architect       │    │
│  │ (Opus)          │ │ (Opus)          │ │ (Opus)          │    │
│  └─────────────────┘ └─────────────────┘ └─────────────────┘    │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│                       DEVELOPERS (Write Access)                   │
│  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐    │
│  │ backend-        │ │ breezsdk-       │ │ umbraco-backend-│    │
│  │ developer       │ │ developer       │ │ developer       │    │
│  │ (Sonnet)        │ │ (Sonnet)        │ │ (Sonnet)        │    │
│  └─────────────────┘ └─────────────────┘ └─────────────────┘    │
│  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐    │
│  │ test-           │ │ database-       │ │ frontend-       │    │
│  │ engineer        │ │ architect       │ │ developer       │    │
│  │ (Sonnet)        │ │ (Sonnet)        │ │ (Sonnet)        │    │
│  └─────────────────┘ └─────────────────┘ └─────────────────┘    │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│                       REVIEWERS (Read-Only)                       │
│  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐    │
│  │ code-           │ │ security-       │ │ breezsdk-       │    │
│  │ reviewer        │ │ auditor         │ │ reviewer        │    │
│  │ (Haiku)         │ │ (Haiku)         │ │ (Haiku)         │    │
│  └─────────────────┘ └─────────────────┘ └─────────────────┘    │
└──────────────────────────────────────────────────────────────────┘
```

### Agent Configuration

Each agent is defined in `.claude/agents/<name>.md` with:

```yaml
---
name: backend-developer
description: .NET backend specialist for APIs, services, business logic
tools: Read, Write, Edit, Bash, Glob, Grep
model: sonnet
---

# Your Expertise
- Clean Architecture implementation
- EF Core repositories and Unit of Work
- Async/await patterns throughout
- Primary constructors (.NET 8+)

# When Invoked
1. Read task requirements
2. Verify test-engineer has written failing tests
3. Implement until tests pass
4. Run `dotnet format`

# Constitutional Compliance
- Article III.1: Never write production code without tests
- Article I.1: Dependencies flow inward only
```

### Model Assignment Strategy

| Model | Cost | Use For |
|-------|------|---------|
| **Opus** | High | Architecture decisions, complex reasoning |
| **Sonnet** | Medium | Implementation, test writing |
| **Haiku** | Low | Code review, security scanning |

---

## 5. Parallel Execution & Dependencies

### Task Markers

Tasks in `tasks.md` use markers to indicate execution strategy:

```markdown
- [x] T001 Create solution file (sequential)
- [x] T002 [P] Create Core project (parallel-capable)
- [x] T003 [P] Create AspNetCore project (parallel-capable)
```

The `[P]` marker indicates the task can run concurrently with other `[P]` tasks.

### Wave Execution Pattern

```
Wave 1: Infrastructure (Sequential)
├── T001 Solution file
├── T002 Directory.Build.props
└── T003 Directory.Packages.props
    │
    ▼ [Checkpoint]

Wave 2: Domain Layer (Parallel)
├── T016 [P] PaymentStatus enum
├── T017 [P] PaymentKind enum
├── T018 [P] BreezErrorCode enum
└── T019 [P] InvoiceType enum
    │
    ▼ [Quality Gate: code-reviewer + security-auditor]

Wave 3: Application Layer (Parallel with Dependencies)
├── T034 IBreezSdkService interface
│   └── T045 BreezSdkService (depends_on: T034)
└── T036 [P] IPaymentRepository interface
```

### Invocation Syntax

```bash
# Sequential execution
Use test-engineer to write tests for PaymentService

# Parallel execution (single message, multiple agents)
& Use backend-developer to implement T004 (User entity)
& Use backend-developer to implement T005 (Product entity)
```

---

## 6. Skills & Knowledge Base

Skills (`.claude/skills/*.md`) provide domain knowledge and execution patterns.

### Available Skills

| Skill | Purpose |
|-------|---------|
| `breezsdk-knowledge.md` | BreezSDK Liquid C# API reference, patterns, examples |
| `dotnet-implementation-execution.md` | Wave execution strategy, TDD workflow, checkpointing |
| `external-plugins.md` | Integration guide for third-party plugins |

### Example: BreezSDK Knowledge

```markdown
## Quick Reference

| Item | Value |
|------|-------|
| NuGet Package | Breez.Sdk.Liquid |
| Networks | Mainnet, Testnet |
| Working Directory | User-configurable |

## Connection Pattern

```csharp
var config = BreezSdkConfig.Production(mnemonic, workingDir);
var sdk = await BreezSdk.ConnectAsync(config);
```

## Payment Flow

1. Create invoice: `sdk.ReceivePaymentAsync(amount, description)`
2. Subscribe to events: `sdk.PaymentReceived += OnPayment`
3. Query history: `sdk.ListPaymentsAsync(filter)`
```

---

## 7. External Plugins

Plugins extend Claude Code capabilities for specialized workflows.

| Plugin | Source | Use For |
|--------|--------|---------|
| `superpowers` | obra/superpowers-marketplace | TDD automation, git worktrees, debugging |
| `dotnet-claude-code-skills` | Community | DDD patterns, EF Core, BDD |
| `engineering-workflow-plugin` | Community | Code review workflows |
| `dev-agent-skills` | Community | Conventional commits, PR creation |
| `awesome-claude-skills` | Community | Architecture patterns |

### Installation

```bash
/plugin marketplace add obra/superpowers-marketplace
/plugins  # List installed plugins
```

---

## 8. Hooks & Automation

Hooks (`.claude/settings.local.json`) inject automated guidance at key workflow points.

### Hook Types

| Hook | Trigger | Purpose |
|------|---------|---------|
| `SessionStart` | Session begins | Display context, remind about TDD |
| `UserPromptSubmit` | Before processing | Inject phase-specific guidance |
| `PreToolUse` | Before tool execution | Block dangerous commands |
| `PostToolUse` | After tool execution | Verify formatting, detect failures |
| `SubagentStart` | Agent spawned | Inject agent-specific reminders |
| `SubagentStop` | Agent completes | Suggest verification steps |
| `Stop` | Session ends | Pre-commit checklist |

### Example: Dangerous Command Blocking

```json
{
  "hooks": {
    "PreToolUse": {
      "Bash": {
        "blocked_patterns": [
          "rm -rf",
          "DROP DATABASE",
          "git push --force"
        ],
        "message": "Checkpoint required before destructive operations"
      }
    }
  }
}
```

### Example: TDD Enforcement

```json
{
  "SubagentStart": {
    "backend-developer": {
      "message": "TDD required per Article III.1. Verify test-engineer has written failing tests before implementation."
    }
  }
}
```

---

## 9. MCP Server Integration

The project integrates MCP (Model Context Protocol) servers for enhanced IDE and external service interaction.

### Configured Servers

| Server | Purpose |
|--------|---------|
| VS Code Diagnostics | Language server diagnostics via `mcp__ide__getDiagnostics` |
| Umbraco Documentation | Access to Umbraco v17 documentation |

### Usage Example

```typescript
// Get diagnostics for a specific file
mcp__ide__getDiagnostics({ uri: "file:///path/to/file.cs" })

// Get all diagnostics
mcp__ide__getDiagnostics({})
```

---

## 10. CLAUDE.md: The Orchestration Hub

`CLAUDE.md` at the repository root ties everything together:

```markdown
# Project Configuration

## Governance
**Constitution**: `.specify/memory/constitution.md`
**Spec-Kit Workflow Order**: [8 phases listed]

## Quick Reference
| Action | Command |
| Build | `dotnet build` |
| Test | `dotnet test` |

## Available Agents
| Agent | Model | Invoke For |
| solution-architect | Opus | Architecture decisions |
| backend-developer | Sonnet | C# implementation |

## Workflow Rules
### Before Every Commit
1. `dotnet build` — must pass
2. `dotnet test` — must pass
3. `dotnet format` — must be clean

### Test-First (Non-Negotiable)
Per Constitution Article III:
1. Write tests for new behavior
2. Run tests and verify they fail (RED)
3. Implement until tests pass (GREEN)
4. Refactor with test safety net
```

This file is automatically loaded by Claude Code, ensuring consistent behavior across all sessions.

---

## 11. Implementation Journey

### Phase Progression

| Phase | Tasks | Focus |
|-------|-------|-------|
| 1 | 15 | Solution structure, project scaffolding |
| 2 | 27 | Domain entities, abstractions, test utilities |
| 3 | 20 | US1: Library consumer integration |
| 4 | 18 | US2: Payment lifecycle management |
| 5 | 12 | US3: Secure configuration |
| 6 | 22 | US4: Extensible persistence |
| 7 | 16 | US5: Error handling & resilience |
| 8 | 18 | US6: Observability |
| 9 | 15 | US7: Platform integrations |
| 10 | 12 | Sample applications, integration tests |

### TDD in Practice

Every feature followed the Red-Green-Refactor cycle:

```
1. test-engineer writes failing tests
   └── Validates requirements captured correctly

2. backend-developer implements
   └── Minimum code to pass tests

3. code-reviewer validates
   └── Constitution compliance check

4. Refactor with test safety net
   └── Improve design without changing behavior
```

---

## 12. Key Takeaways

### What This Setup Enables

1. **Governance Without Overhead**: The constitution automates quality enforcement
2. **Scalable Parallelism**: Multiple agents work concurrently on independent tasks
3. **Consistent Quality**: Every agent enforces the same principles
4. **Traceable Decisions**: Specifications flow through to implementation
5. **Reduced Cognitive Load**: Domain-specific agents handle specialized concerns

### When to Use This Approach

- Large features requiring architectural planning
- Projects with strict quality requirements
- Codebases needing systematic modernization
- Teams wanting to establish consistent practices

### Demo Highlights to Watch For

1. **Phase Handoffs**: Automatic transition between Spec-Kit phases
2. **Agent Specialization**: Different agents for architecture vs implementation
3. **Parallel Execution**: Multiple tasks running concurrently
4. **Constitutional Enforcement**: Gates blocking non-compliant code
5. **TDD Workflow**: Tests always written before implementation
6. **Hook Automation**: Contextual guidance injected at key points

---

## Appendix: File Structure Reference

```
.
├── .claude/
│   ├── agents/                 # 17 specialized agent definitions
│   │   ├── backend-developer.md
│   │   ├── breezsdk-architect.md
│   │   └── ...
│   ├── commands/               # Spec-Kit phase commands
│   │   ├── speckit.constitution.md
│   │   ├── speckit.specify.md
│   │   └── ...
│   ├── skills/                 # Knowledge base
│   │   ├── breezsdk-knowledge.md
│   │   ├── dotnet-implementation-execution.md
│   │   └── external-plugins.md
│   └── settings.local.json     # Hooks and permissions
├── .specify/
│   ├── memory/
│   │   └── constitution.md     # Governance principles
│   └── templates/              # Spec-Kit templates
├── specs/
│   └── 001-breezsdk-production-ready/
│       ├── spec.md             # Feature requirements
│       ├── plan.md             # Technical architecture
│       ├── tasks.md            # Task breakdown
│       ├── data-model.md       # Domain entities
│       └── contracts/          # API specifications
├── src/                        # 6 NuGet packages
├── tests/                      # 4 test projects
├── samples/                    # 3 sample applications
├── CLAUDE.md                   # Orchestration hub
├── Directory.Build.props       # Shared build config
└── Directory.Packages.props    # Central versioning
```

---

## Questions?

This document provides the foundational context for the demo. Technical questions about specific components can be addressed during the demonstration.
