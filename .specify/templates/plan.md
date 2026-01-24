---
name: plan
description: Create a technical implementation plan from the feature specification with Claude Code execution architecture
---

# Plan Command - Enhanced with Claude Code Integration

## Prerequisites
- Read the feature specification from `/specs/[current-feature]/spec.md`
- Read the constitution from `/memory/constitution.md`
- Load the Claude Code planning skill: `.claude/skills/dotnet-plan-strategy.md`

## Execution Steps

1. **Load Context**
   - Read spec.md for requirements
   - Read constitution.md for governance principles
   - Read the dotnet-plan-strategy skill for Claude Code architecture guidance

2. **Generate Technical Plan**
   Follow the standard spec-kit plan template with these additions:

3. **Claude Code Execution Architecture Section**
   
   The plan MUST include a "Claude Code Execution Architecture" section containing:
   
   ### Agent Specialization Strategy
   Define which specialized sub-agents will handle which components:
   - `solution-architect` (Opus) - architectural decisions, complex refactoring
   - `backend-developer` (Sonnet) - .NET implementation, API development
   - `frontend-developer` (Sonnet) - Blazor/UI implementation
   - `test-engineer` (Sonnet) - test implementation, TDD
   - `database-architect` (Sonnet) - schema design, migrations
   - `security-auditor` (Haiku) - security scanning, vulnerability assessment
   - `code-reviewer` (Haiku) - code quality, style enforcement

   ### Parallel Execution Boundaries
   Identify which components can be developed in parallel:
   - List modules with no shared dependencies
   - Identify components requiring sequential handling
   - Define synchronization requirements at integration points

   ### Context Management Strategy
   Define thresholds for:
   - When to delegate to sub-agents (>30k tokens)
   - When to use background execution (>5 min operations)
   - When to compact (/compact)
   - What summaries sub-agents must return

   ### Quality Gate Automation
   Define hooks for automated enforcement:
   - PostToolUse hooks for formatting/linting
   - PreToolUse hooks for dangerous command protection
   - Stop hooks for test verification

4. **Constitutional Compliance Check**
   Verify all plan elements comply with constitution articles.

5. **Output Files**
   Generate:
   - `plan.md` - Main implementation plan
   - `research.md` - Technical research findings
   - `data-model.md` - Data model specifications
   - `quickstart.md` - Validation scenarios
   - `contracts/` - API contracts if applicable

## User Prompt Integration

When the user provides technical stack details, incorporate them into the plan.
Always include the Claude Code Execution Architecture section regardless of user input.

Example user input:
```
/speckit.plan We are using .NET 8, Blazor Server, PostgreSQL with EF Core.
```

The plan should reflect these technology choices AND include the Claude Code execution strategy.
