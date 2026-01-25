# Implementation Plan: Umbraco MCP Integration for Spec-Kit Workflow

**Branch**: `002-umbraco-mcp-integration` | **Date**: 2026-01-25 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-umbraco-mcp-integration/spec.md`

## Summary

This feature integrates Umbraco v17 MCP documentation support into the spec-kit workflow by creating five specialized agents for Umbraco development, documenting the MCP integration in external-plugins.md, updating CLAUDE.md with agent listings, and configuring WebFetch permissions for docs.umbraco.com fallback access. The implementation follows the established BreezSDK agent patterns and is primarily a documentation/configuration task with no production code changes.

## Technical Context

**Language/Version**: Markdown (agent definitions, documentation) + JSON (settings)
**Primary Dependencies**: Umbraco MCP Documentation Server (GitBook-based at `https://docs.umbraco.com/~gitbook/mcp`)
**Storage**: N/A (documentation/configuration only)
**Testing**: Manual verification of agent invocation and MCP connectivity
**Target Platform**: Claude Code CLI
**Project Type**: Documentation/configuration (no source code changes)
**Performance Goals**: N/A
**Constraints**: Must follow existing BreezSDK agent patterns for consistency
**Scale/Scope**: 5 agent definitions, 3 file updates (CLAUDE.md, external-plugins.md, settings.local.json)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Article | Principle | Status | Notes |
|---------|-----------|--------|-------|
| I.3 | Modular Decomposition | ✅ PASS | Each agent is an independent, loosely-coupled module |
| II.3 | Explicit Over Implicit | ✅ PASS | All MCP tools and endpoints explicitly documented |
| II.4 | Self-Documenting Code | ✅ PASS | Agents include clear expertise descriptions |
| III.4 | Automated Validation Gates | ⚠️ N/A | No production code; manual verification only |
| VIII.3 | Accessibility Compliance | ✅ PASS | Frontend reviewer includes WCAG 2.1 AA guidance |
| IX.1 | YAGNI Enforcement | ✅ PASS | Only creating agents for documented use cases |
| IX.3 | Dependency Scrutiny | ✅ PASS | MCP is a GitBook-provided standard integration |
| X.1 | Living Documentation | ✅ PASS | Documentation updated with feature |
| XII.1 | Task Independence | ✅ PASS | Agent files can be created in parallel |

**Gate Status**: PASS - All applicable constitutional principles satisfied. This is a documentation/configuration feature with no production code.

## Project Structure

### Documentation (this feature)

```text
specs/002-umbraco-mcp-integration/
├── plan.md              # This file
├── research.md          # Phase 0 output - MCP research findings
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Configuration & Agent Files (repository root)

```text
.claude/
├── agents/
│   ├── umbraco-architect.md        # NEW: Architecture decisions (Opus, read-only)
│   ├── umbraco-backend-developer.md # NEW: C# implementation (Sonnet, write)
│   ├── umbraco-frontend-developer.md # NEW: Backoffice UI (Sonnet, write)
│   ├── umbraco-backend-reviewer.md  # NEW: C# code review (Haiku, read-only)
│   └── umbraco-frontend-reviewer.md # NEW: Lit/TS review (Haiku, read-only)
├── skills/
│   └── external-plugins.md         # UPDATE: Add Umbraco MCP section
└── settings.local.json             # UPDATE: Add WebFetch permission

CLAUDE.md                           # UPDATE: Add Umbraco agents to tables
```

**Structure Decision**: Documentation-only feature. No source code structure changes. All changes are to `.claude/` configuration directory and root `CLAUDE.md`.

## Complexity Tracking

> No constitutional violations requiring justification. All gates PASS.

| Item | Decision | Rationale |
|------|----------|-----------|
| 5 agents vs fewer | 5 agents | Matches BreezSDK pattern; separates backend/frontend concerns per spec FR-004 through FR-008 |
| Separate backend/frontend reviewers | 2 reviewers | Different tech stacks (C#/.NET vs Lit/TypeScript) require different expertise |

## Agent Design Decisions

### Model Assignment (matching BreezSDK pattern)

| Agent | Model | Justification |
|-------|-------|---------------|
| `umbraco-architect` | Opus | Complex architecture decisions require deeper reasoning |
| `umbraco-backend-developer` | Sonnet | Implementation work, balanced cost/capability |
| `umbraco-frontend-developer` | Sonnet | Implementation work, balanced cost/capability |
| `umbraco-backend-reviewer` | Haiku | Review is pattern-matching, fast/cheap model sufficient |
| `umbraco-frontend-reviewer` | Haiku | Review is pattern-matching, fast/cheap model sufficient |

### Tool Permissions (matching BreezSDK pattern)

| Agent | Tools | Rationale |
|-------|-------|-----------|
| Architect | Read, Glob, Grep | Analysis only, no file modifications |
| Developers | Read, Write, Edit, Bash, Glob, Grep | Full implementation capability |
| Reviewers | Read, Glob, Grep | Analysis only, no file modifications |

### MCP Integration Strategy

The agents will use the Umbraco MCP Documentation server as their primary documentation source:

1. **MCP Endpoint**: `https://docs.umbraco.com/~gitbook/mcp`
2. **Available Tools**:
   - `search_content`: Search documentation by query
   - `get_page_content`: Get specific page content by ID
   - `get_page_by_path`: Get page content by URL path
   - `get_space_content`: Get space/section overview
3. **Fallback**: WebFetch to `docs.umbraco.com` when MCP unavailable

## Implementation Waves

This feature uses a single wave since all tasks are independent documentation/configuration work.

### Wave 1: Agent Definitions & Configuration [P]

All tasks can run in parallel as they modify independent files.

| Task | File | Agent | Description |
|------|------|-------|-------------|
| T001 | `.claude/agents/umbraco-architect.md` | backend-developer | Create architect agent definition |
| T002 | `.claude/agents/umbraco-backend-developer.md` | backend-developer | Create backend developer agent |
| T003 | `.claude/agents/umbraco-frontend-developer.md` | backend-developer | Create frontend developer agent |
| T004 | `.claude/agents/umbraco-backend-reviewer.md` | backend-developer | Create backend reviewer agent |
| T005 | `.claude/agents/umbraco-frontend-reviewer.md` | backend-developer | Create frontend reviewer agent |
| T006 | `.claude/skills/external-plugins.md` | backend-developer | Add Umbraco MCP section |
| T007 | `.claude/settings.local.json` | backend-developer | Already has permission (verify) |
| T008 | `CLAUDE.md` | backend-developer | Update all three agent tables |

### Wave 2: Verification

| Task | Description | Agent |
|------|-------------|-------|
| T009 | Verify all agent files exist and have correct structure | code-reviewer |
| T010 | Verify CLAUDE.md has all required entries | code-reviewer |

## Agent Definition Templates

Each agent follows this structure (from BreezSDK pattern):

### Architect Agent (Read-Only, Opus)

```markdown
---
name: umbraco-architect
description: [One-line for invocation]
tools: Read, Glob, Grep
model: opus
---

You are [expertise statement]

## Your Expertise
- [Bullet list from FR-014]

## When Invoked
1. Consult Umbraco MCP documentation
2. [Domain-specific steps]

## [Domain Patterns]
[Architecture patterns, document type design, etc.]

## Output Format
[How to structure recommendations]

## Umbraco MCP Integration
This agent uses the Umbraco MCP Documentation server:
- Endpoint: https://docs.umbraco.com/~gitbook/mcp
- Tools: search_content, get_page_content, get_page_by_path, get_space_content
- Fallback: WebFetch to docs.umbraco.com

## Constitutional Compliance
- **Article I: Architectural Foundation**
  - I.1 Clean Architecture Mandate: [How applied]
  - I.3 Modular Decomposition: [How applied]
```

### Developer Agents (Write, Sonnet)

Include minimal code examples (1-2 per topic, ~5-10 lines each) per spec clarification:
- Backend: Composers, Notification Handlers, Services
- Frontend: Lit components, UUI usage, Management API

### Reviewer Agents (Read-Only, Haiku)

Include review checklists for:
- Backend: C# patterns, service usage, Clean Architecture compliance
- Frontend: Lit/TypeScript patterns, UUI usage, WCAG 2.1 AA accessibility

## CLAUDE.md Update Specification

### Available Agents Table
Add after breezsdk-test-engineer row:
```markdown
| `umbraco-architect` | Opus | Umbraco architecture, Document Type design, package architecture |
| `umbraco-backend-developer` | Sonnet | Umbraco C# development, Composers, Services, Notification Handlers |
| `umbraco-frontend-developer` | Sonnet | Umbraco backoffice UI, Lit/TypeScript, UUI components |
| `umbraco-backend-reviewer` | Haiku | Umbraco C# code review, pattern compliance |
| `umbraco-frontend-reviewer` | Haiku | Umbraco frontend review, accessibility compliance |
```

### Agent Tools & Permissions Table
Add after breezsdk-test-engineer row:
```markdown
| `umbraco-architect` | Read, Glob, Grep | ❌ Read-only |
| `umbraco-backend-developer` | Read, Write, Edit, Bash, Glob, Grep | ✅ Yes |
| `umbraco-frontend-developer` | Read, Write, Edit, Bash, Glob, Grep | ✅ Yes |
| `umbraco-backend-reviewer` | Read, Glob, Grep | ❌ Read-only |
| `umbraco-frontend-reviewer` | Read, Glob, Grep | ❌ Read-only |
```

### Agent & Plugin Utilization by Phase Table
Update existing rows:
- `/speckit.plan`: Add `umbraco-architect` to Primary Agents
- `/speckit.implement`: Add `umbraco-backend-developer`, `umbraco-frontend-developer` to Primary Agents
- Post-implement: Add `umbraco-backend-reviewer`, `umbraco-frontend-reviewer` to Supporting Agents

## External Plugins Update Specification

Add new section after existing plugins (section 6):

```markdown
### 6. Umbraco MCP Documentation

**Use For**: Umbraco v17 documentation lookup, CMS patterns, backoffice extension development
**Spec-Kit Phases**: `/speckit.plan`, `/speckit.implement`

**Endpoint**: `https://docs.umbraco.com/~gitbook/mcp`

**MCP Tools**:
| Tool | Purpose | Example |
|------|---------|---------|
| `search_content` | Search documentation | Query: "notification handlers" |
| `get_page_content` | Get page by ID | After search returns page IDs |
| `get_page_by_path` | Get page by URL path | Path: "/umbraco-cms/reference/notifications" |
| `get_space_content` | Get section overview | For navigation/exploration |

**Key Documentation Paths**:
- `/umbraco-cms/reference/extending/extending-overview` - Extension points
- `/umbraco-cms/reference/notifications` - Notification Handlers
- `/umbraco-cms/reference/management/services` - Core services
- `/umbraco-cms/extending/backoffice-setup` - Backoffice extensions
- `/umbraco-cms/extending/extension-types` - Extension type manifests

**Fallback**: If MCP unavailable, use `WebFetch(domain:docs.umbraco.com)`

**Integration with Spec-Kit**:
- During `/speckit.plan`: Use umbraco-architect for Document Type and package design
- During `/speckit.implement`: Use umbraco-backend-developer or umbraco-frontend-developer
- Post-implement: Use umbraco-backend-reviewer or umbraco-frontend-reviewer

**Integration with Agents**:
All Umbraco agents (umbraco-architect, umbraco-backend-developer, umbraco-frontend-developer, umbraco-backend-reviewer, umbraco-frontend-reviewer) use this MCP as their primary documentation source.
```

## Success Criteria Verification

| Criterion | Verification Method |
|-----------|-------------------|
| SC-001: 5 agent files exist | `ls .claude/agents/umbraco-*.md` |
| SC-002: CLAUDE.md has all 3 tables updated | Grep for umbraco entries in each table |
| SC-003: external-plugins.md has MCP section | Grep for "Umbraco MCP" |
| SC-004: settings.local.json has WebFetch | Grep for "docs.umbraco.com" |
| SC-005: Agents reference MCP | Grep for "~gitbook/mcp" in each agent |
| SC-006: Tool assignments match pattern | Verify frontmatter in each agent |
| SC-007-010: Code examples and review criteria | Manual review of agent content |
| SC-011: Constitutional Compliance sections | Grep for "Constitutional Compliance" in each agent |

## Notes

- settings.local.json already has `WebFetch(domain:docs.umbraco.com)` permission (line 72)
- No data-model.md or contracts/ needed - this is documentation-only
- No quickstart.md needed - agents are self-documenting
