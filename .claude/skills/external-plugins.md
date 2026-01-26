---
name: external-plugins
description: Integration guide for external Claude Code plugins used in the Spec-Kit workflow. Reference this skill when invoking plugin capabilities during /speckit.plan and /speckit.implement phases.
globs:
  - "**/*"
---

# External Plugins Integration Guide

This skill documents how to invoke and integrate external Claude Code plugins with the Spec-Kit workflow.

> **Claude Code 2.1.19 Features**: This guide leverages plugin pinning (v2.1.14), MCP auto-search (v2.1.7), and session forking (v2.1.19) for enhanced workflow reliability.

## Plugin Version Pinning (v2.1.14+)

For deterministic, reproducible builds, pin plugins to specific Git commit SHAs:

```bash
# Pinned installation (RECOMMENDED for production)
/plugin marketplace add obra/superpowers-marketplace@<commit-sha>
/plugin marketplace add anthropics/dotnet-claude-code-skills@<commit-sha>

# To find current commit SHA
/plugins  # Lists installed plugins with versions

# To update to latest while maintaining pin
/plugin marketplace remove obra/superpowers-marketplace
/plugin marketplace add obra/superpowers-marketplace@<new-commit-sha>
```

**Why Pin?**
- Ensures all team members use identical plugin versions
- Prevents breaking changes from upstream updates
- Required for CI/CD reproducibility

## Available Plugins

### 1. superpowers (obra/superpowers-marketplace)

**Use For**: TDD workflow, debugging, planning, git worktrees
**Spec-Kit Phases**: `/speckit.plan`, `/speckit.implement`

**Capabilities**:
- Advanced TDD workflow automation
- Debugging assistance with context awareness
- Planning and task breakdown helpers
- Git worktree management for parallel development

**Invocation Examples**:
```
# TDD workflow assistance
Use superpowers to guide the TDD cycle for OrderService

# Debugging with context
Use superpowers to debug the failing authentication test

# Git worktree for parallel work
Use superpowers to create a worktree for feature/payment-integration
```

**Integration with Spec-Kit**:
- During `/speckit.plan`: Use for complex planning decisions
- During `/speckit.implement`: Use for TDD enforcement and debugging

---

### 2. dotnet-claude-code-skills

**Use For**: DDD patterns, EF Core best practices, BDD testing
**Spec-Kit Phases**: `/speckit.implement`

**Capabilities**:
- Domain-Driven Design tactical patterns (aggregates, entities, value objects)
- Entity Framework Core code generation and optimization
- BDD-style test scenario generation
- Repository pattern implementation

**Invocation Examples**:
```
# DDD aggregate design
Use dotnet-claude-code-skills to create the Order aggregate with OrderLine value objects

# EF Core optimization
Use dotnet-claude-code-skills to optimize the customer query with proper includes

# BDD test generation
Use dotnet-claude-code-skills to generate BDD scenarios for the checkout feature
```

**Integration with Spec-Kit**:
- Aligns with Constitution Article I (Clean Architecture, DDD)
- Aligns with Constitution Article IV (Data Layer Governance)

---

### 3. engineering-workflow-plugin

**Use For**: Code review workflows, git best practices
**Spec-Kit Phases**: `/speckit.analyze`, post-implement

**Capabilities**:
- Structured code review checklists
- Git workflow automation
- Branch management strategies
- PR description generation

**Invocation Examples**:
```
# Code review workflow
Use engineering-workflow-plugin to perform a structured code review of the OrderService

# Git workflow
Use engineering-workflow-plugin to prepare the feature branch for merge

# PR creation
Use engineering-workflow-plugin to generate a PR description for the authentication feature
```

**Integration with Spec-Kit**:
- Use after `/speckit.implement` waves complete
- Complements `code-reviewer` and `security-auditor` agents

---

### 4. dev-agent-skills

**Use For**: Conventional commits, PR creation and review
**Spec-Kit Phases**: Post-implement (commits, PRs)

**Capabilities**:
- Conventional commit message generation
- PR creation with proper formatting
- PR review assistance
- Changelog generation

**Invocation Examples**:
```
# Conventional commit
Use dev-agent-skills to create a conventional commit for the user authentication feature

# PR creation
Use dev-agent-skills to create a PR with proper description and checklist

# Changelog
Use dev-agent-skills to update the changelog for v1.2.0
```

**Commit Message Format** (per Conventional Commits):
```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

Types: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`

---

### 5. awesome-claude-skills

**Use For**: Software architecture patterns, design decisions
**Spec-Kit Phases**: `/speckit.plan`, `/speckit.constitution`

**Capabilities**:
- Architecture pattern recommendations
- Design pattern application guidance
- Trade-off analysis
- Technology selection assistance

**Invocation Examples**:
```
# Architecture pattern
Use awesome-claude-skills to evaluate CQRS vs traditional layered architecture for this project

# Design pattern
Use awesome-claude-skills to recommend a pattern for the notification system

# Trade-off analysis
Use awesome-claude-skills to analyze the trade-offs between SQL Server and PostgreSQL
```

**Integration with Spec-Kit**:
- During `/speckit.constitution`: Architecture principle decisions
- During `/speckit.plan`: Technical architecture choices

---

## Plugin Invocation by Spec-Kit Phase

| Phase | Recommended Plugins | Purpose |
|-------|-------|---------|
| `/speckit.constitution` | awesome-claude-skills | Architecture principles |
| `/speckit.plan` | superpowers, awesome-claude-skills | Planning, architecture |
| `/speckit.implement` | superpowers, dotnet-claude-code-skills | TDD, DDD, EF Core |
| `/speckit.analyze` | engineering-workflow-plugin | Code review workflows |
| Post-implement | dev-agent-skills, engineering-workflow-plugin | Commits, PRs |

## Installation Verification

Before invoking plugins, verify they are installed:

```bash
# Check installed plugins
/plugins

# Install a plugin from marketplace
/plugin marketplace add <owner>/<plugin-name>

# Examples:
/plugin marketplace add obra/superpowers-marketplace
/plugin marketplace add anthropics/dotnet-claude-code-skills
```

## Combining Plugins with Agents

Plugins complement the built-in agents:

```
# Example: Full TDD cycle with plugin + agent
1. Use superpowers to plan the TDD approach for OrderService
2. Use test-engineer to write failing tests (RED)
3. Use backend-developer to implement until tests pass (GREEN)
4. Use superpowers to guide refactoring
5. Use engineering-workflow-plugin to create PR
```

## Troubleshooting

If a plugin command fails:
1. Verify plugin is installed: `/plugins`
2. Check plugin documentation for correct invocation syntax
3. Ensure you're in the correct directory context
4. Try reinstalling: `/plugin marketplace remove <owner>/<name>` then `/plugin marketplace add <owner>/<name>`

---

## MCP Servers

### Umbraco MCP (GitBook)

**Use For**: Accessing official Umbraco v17 LTS documentation for backend and frontend development
**Spec-Kit Phases**: `/speckit.plan`, `/speckit.implement`

**Prerequisite Setup**:

Before agents can use the Umbraco MCP, ensure the GitBook MCP server is configured:

1. **VS Code**: Install the GitBook MCP extension or configure manually in `.vscode/mcp.json`
2. **Claude Code CLI**: The MCP endpoint is accessed via the configured MCP tools

If MCP is not available, agents will automatically fall back to WebFetch.

**Endpoint**: `https://docs.umbraco.com/~gitbook/mcp`

**Available MCP Tools**:

| Tool | Purpose | Parameters |
|------|---------|------------|
| `search_content` | Search documentation content | `query: string` |
| `get_page_content` | Retrieve page by ID | `pageId: string` |
| `get_page_by_path` | Retrieve page by URL path | `path: string` |
| `get_space_content` | Get space/section overview | `spaceId?: string` |

**Key Documentation Paths**:

| Path | Content |
|------|---------|
| `/umbraco-cms/reference/extending/extending-overview` | Extension points overview |
| `/umbraco-cms/reference/notifications` | Notification Handlers (events) |
| `/umbraco-cms/reference/management/services` | Core services (IContentService, etc.) |
| `/umbraco-cms/extending/backoffice-setup` | Bellissima backoffice setup |
| `/umbraco-cms/extending/extension-types` | Dashboard, Property Editor, Workspace types |
| `/umbraco-cms/extending/ui-documentation` | UUI component library usage |

**No-Results Fallback Sequence**:

When MCP search returns no results or is unavailable:

1. **Retry with broader terms**: Simplify the query (e.g., "Composer" instead of "IUserComposer registration")
2. **Browse via `get_space_content`**: Navigate the documentation structure to find relevant sections
3. **WebFetch fallback**: Use `WebFetch(domain:docs.umbraco.com)` to fetch specific documentation pages directly

**Invocation Examples**:

```
# Search for Composer patterns
Use Umbraco MCP search_content to find "Composer dependency injection"

# Get specific page by path
Use Umbraco MCP get_page_by_path for "/umbraco-cms/reference/notifications"

# Browse extension types
Use Umbraco MCP get_space_content to explore the extending section

# Fallback when MCP unavailable
Use WebFetch to retrieve https://docs.umbraco.com/umbraco-cms/reference/notifications
```

**Integration with Spec-Kit**:

| Phase | Usage |
|-------|-------|
| `/speckit.plan` | Research Umbraco architecture patterns and extension points |
| `/speckit.implement` | Look up API details, code examples, and best practices |

**Agent Integration**:

The following agents are configured to use Umbraco MCP:

- `umbraco-architect`: Architecture decisions, Document Type design
- `umbraco-backend-developer`: C# implementation, Composers, Services
- `umbraco-frontend-developer`: Lit/TypeScript, UUI, Management API
- `umbraco-backend-reviewer`: C# code review, pattern compliance
- `umbraco-frontend-reviewer`: Frontend review, accessibility

---

## Claude Code 2.1.19 Features for Spec-Kit

### Session Forking (v2.1.19)

Fork sessions to explore alternatives without losing your main path:

```
# During /speckit.plan research phase
Fork session to explore Option A (CQRS architecture)
Fork session to explore Option B (Traditional layered)
Compare results in main session, select best approach

# During /speckit.implement for risky changes
Fork session before attempting complex refactoring
If fork succeeds, apply changes to main session
If fork fails, main session remains unaffected
```

**Use Cases**:
- Research phase: Compare multiple architecture options in parallel
- Plan phase: Evaluate different technical approaches
- Implement phase: Test risky changes before committing

### Native Task Management (v2.1.16)

Claude Code now has built-in task management with dependency tracking:

```
# Register tasks from tasks.md into native system
Use TodoWrite to create structured task list

# Monitor parallel execution
/tasks  # Shows all running/pending tasks with dependencies

# Native dependency blocking
# Tasks with depends_on are automatically blocked until dependencies complete
```

**Benefits**:
- Automatic dependency resolution
- Visual progress tracking
- Background task monitoring
- Inline response previews in notifications

### Inline Agent Responses (v2.1.7)

Agent final responses now appear inline in task notifications:

```
# When running background agents
& Use backend-developer to implement T004
& Use backend-developer to implement T005

# Results visible inline without reading transcript files
# Check /tasks for status and inline summaries
```

### Context Percentage Monitoring (v2.1.6)

Status line shows context usage:

```
# Monitor via status line
context_window.used_percentage: 65%
context_window.remaining_percentage: 35%

# Automated thresholds (configured in .claude/settings.json)
# - Warning at 70%
# - Auto-compact suggestion at 80%
# - Critical at 85%
```

### $ARGUMENTS Indexed Access (v2.1.19)

Commands can access individual arguments:

```markdown
# Old syntax
$ARGUMENTS  # "feature-name wave-2"

# New syntax (v2.1.19)
$ARGUMENTS[0]  # "feature-name"
$ARGUMENTS[1]  # "wave-2"
```

### ${CLAUDE_SESSION_ID} Substitution (v2.1.9)

Embed session ID in artifacts for traceability:

```markdown
<!-- Generated by Spec-Kit | Session: ${CLAUDE_SESSION_ID} -->
```

---

## Related Skills

- See `.claude/skills/dotnet-implementation-execution.md` for:
  - Wave-based execution patterns (sequential vs parallel)
  - Checkpoint strategies and recovery
  - Multi-agent orchestration patterns
  - Context management (`/compact`, sub-agent delegation)
  - Quality gate enforcement after each wave

- See `.claude/settings.json` for:
  - plansDirectory configuration
  - MCP server auto-search settings
  - Plugin pinning preferences
  - Context threshold configurations
