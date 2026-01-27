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

### 2. Technology-Specific Plugins

Technology-specific plugins are documented in `CLAUDE.project.md`. These plugins vary by project and technology stack.

**Examples** (install per your project's CLAUDE.project.md):
- Check `CLAUDE.project.md` for technology-specific plugin recommendations

**Integration with Spec-Kit**:
- Aligns with Constitution principles for Clean Architecture and Data Layer Governance

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
| `/speckit.implement` | superpowers, (see CLAUDE.project.md) | TDD, technology-specific patterns |
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

Project-specific MCP servers are documented in `CLAUDE.project.md`. Configure MCP servers in `.claude/settings.json` under `mcpServers`.

See `CLAUDE.project.md` for:
- MCP server endpoints and configuration
- Available MCP tools and parameters
- Documentation paths and fallback sequences
- Agent integration details

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

- See `.claude/skills/implementation-execution.md` for:
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
