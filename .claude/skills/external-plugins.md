---
name: external-plugins
description: Integration guide for external Claude Code plugins used in the Spec-Kit workflow. Reference this skill when invoking plugin capabilities during /speckit.plan and /speckit.implement phases.
globs:
  - "**/*"
---

# External Plugins Integration Guide

This skill documents how to invoke and integrate external Claude Code plugins with the Spec-Kit workflow.

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
claude plugins list

# Install a plugin if missing
claude plugin add <plugin-name>
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
1. Verify plugin is installed: `claude plugins list`
2. Check plugin documentation for correct invocation syntax
3. Ensure you're in the correct directory context
4. Try reinstalling: `claude plugin remove <name> && claude plugin add <name>`
