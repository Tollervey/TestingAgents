# Project Configuration

## Governance

**Constitution**: `.specify/memory/constitution.md` — All development must comply with constitutional principles.

**Spec-Kit Workflow Order**:
1. `/speckit.constitution` → Establish governance principles
2. `/speckit.specify` → Define what to build (technology-agnostic)
3. `/speckit.clarify` → Refine requirements
4. `/speckit.plan` → Technical architecture + Claude Code execution strategy
5. `/speckit.tasks` → Task breakdown with dependencies and parallel markers
6. `/speckit.checklist` → Generate quality gate checklists (OPTIONAL, before implement)
7. `/speckit.analyze` → Multi-agent validation (read-only consistency check)
8. `/speckit.implement` → Wave-based execution with sub-agents (validates checklists first)

**Utility Commands**:
- `/speckit.worktree` → Git worktree management for parallel development

**Command Handoffs** (automatic transition buttons):
- `/speckit.plan` → `/speckit.tasks` or `/speckit.checklist`
- `/speckit.tasks` → `/speckit.analyze` or `/speckit.implement`
- `/speckit.checklist` gates must pass before `/speckit.implement` proceeds

> **Note**: Handoffs are defined in each command's frontmatter (`handoffs:` section in `.claude/commands/speckit.*.md`). They appear as clickable buttons after command completion.

---

<!-- CUSTOMIZABLE: Project-specific commands and conventions belong in CLAUDE.project.md -->
<!-- See CLAUDE.project.md for technology-specific quick reference, code conventions, and gotchas -->

## Quick Reference

See `CLAUDE.project.md` for project-specific build, test, and development commands.

---

## Code Conventions

See `CLAUDE.project.md` for project-specific code style, database conventions, and common gotchas.

### API Design
- RESTful endpoints with proper controller attributes
- Return typed results for type safety
- Problem details for errors (RFC 7807)
- Input validation at API boundaries
- Version in URL path (`/api/v1/`)

---

## Claude Code Execution

### Claude Code 2.1.19 Features

This project leverages the following Claude Code 2.1.19 capabilities:

| Feature | Version | Use Case | Configuration |
|---------|---------|----------|---------------|
| **Native Task Management** | v2.1.16 | Dependency tracking, progress monitoring | Via TodoWrite tool |
| **Session Forking** | v2.1.19 | Explore alternatives without losing main path | During `/speckit.plan` research |
| **Plugin Pinning** | v2.1.14 | Deterministic plugin versions | Pin to Git commit SHA |
| **plansDirectory** | v2.1.9 | Custom plan file location | `.claude/settings.json` |
| **${CLAUDE_SESSION_ID}** | v2.1.9 | Session traceability in artifacts | Embed in generated files |
| **MCP Auto-Search** | v2.1.7 | Automatic documentation lookup | `auto:3` threshold |
| **Inline Agent Responses** | v2.1.7 | Monitor background agents easily | Via `/tasks` command |
| **Context Percentage** | v2.1.6 | Monitor context usage | Status line + hooks |
| **$ARGUMENTS[n]** | v2.1.19 | Indexed command arguments | In custom commands |
| **additionalContext** | v2.1.9 | Enhanced hook context injection | In PreToolUse hooks |

**Key Configurations** (see `.claude/settings.json`):
- Plan files stored in `.specify/plans/`
- Auto-compact warning at 70%, critical at 80%
- Max 5 concurrent background agents
- MCP auto-search enabled for documentation

### Available Agents

| Agent | Model | Invoke For |
|-------|-------|------------|
| `solution-architect` | Opus | Architecture decisions, complex refactoring |
| `backend-developer` | Sonnet | Backend implementation, APIs, services |
| `frontend-developer` | Sonnet | UI components, client-side development |
| `test-engineer` | Sonnet | Writing tests BEFORE implementation |
| `database-architect` | Sonnet | Schema design, migrations |
| `security-auditor` | Haiku | Security scanning, vulnerability checks |
| `code-reviewer` | Haiku | Code quality, constitution compliance |

> **Domain-specific agents**: See `CLAUDE.project.md` for project-specific agent extensions.

### Agent Tools & Permissions

| Agent | Tools | Can Modify Files? |
|-------|-------|-------------------|
| `solution-architect` | Read, Glob, Grep | ❌ Read-only |
| `backend-developer` | Read, Write, Edit, Bash, Glob, Grep | ✅ Yes |
| `frontend-developer` | Read, Write, Edit, Bash, Glob, Grep | ✅ Yes |
| `test-engineer` | Read, Write, Edit, Bash, Glob, Grep | ✅ Yes |
| `database-architect` | Read, Write, Edit, Bash, Glob, Grep | ✅ Yes |
| `security-auditor` | Read, Grep, Glob, Bash | ❌ Read-only |
| `code-reviewer` | Read, Grep, Glob | ❌ Read-only |

### Agent & Plugin Utilization by Phase

This is the **authoritative** mapping of which core agents and plugins apply to each Spec-Kit phase.

| Phase | Primary Agent(s) | Supporting Agent(s) | Recommended Plugins | Parallel? |
|-------|-----------------|---------------------|---------------------|-----------|
| `/speckit.constitution` | solution-architect | — | awesome-claude-skills | No |
| `/speckit.specify` | solution-architect | — | — | No |
| `/speckit.clarify` | solution-architect | — | — | No |
| `/speckit.plan` | solution-architect, database-architect | — | superpowers, awesome-claude-skills | No |
| `/speckit.tasks` | (orchestrator) | — | — | No |
| `/speckit.checklist` | (orchestrator) | — | — | No |
| `/speckit.analyze` | code-reviewer | — | engineering-workflow-plugin | No |
| `/speckit.implement` | backend-developer, test-engineer, frontend-developer | security-auditor, code-reviewer | superpowers | ✅ Yes |
| Post-implement | — | code-reviewer, security-auditor | dev-agent-skills, engineering-workflow-plugin | ✅ Yes |

> **Domain-specific agent utilization**: See `CLAUDE.project.md` for project-specific agent phase mappings.

### Parallel Execution
Tasks marked `[P]` in tasks.md can run concurrently:
```
& Use backend-developer to implement T004 (User entity)
& Use backend-developer to implement T005 (Product entity)
/tasks  # Monitor with inline responses (v2.1.7)
```
Always respect `depends_on` — never start blocked tasks.

**Native Dependency Tracking** (v2.1.16): Use TodoWrite to register tasks with dependencies. Blocked tasks automatically wait for their dependencies to complete.

### Background Execution
For operations >5 minutes:
- `Ctrl+B` to background long-running tasks
- `/tasks` to monitor progress
- Maximum 5 concurrent background agents

**When to use background agents:**
- Long-running tasks (>2 minutes expected)
- Multiple independent tasks (3+) that can run in parallel

**When to use foreground agents:**
- Quick tasks (<2 minutes)
- When you need results immediately
- When task count is ≤2 (parallelism benefit is minimal)

**Timeout strategy:**
- Use 120000ms (2 min) timeout for simple implementation tasks
- Use 300000ms (5 min) timeout for complex multi-file changes
- If agent times out 3x consecutively, read the output file directly with Read tool

### Context Management

**Context Percentage Monitoring** (v2.1.6+):
- Monitor via status line: `context_window.used_percentage`
- Warning threshold: 70% (consider planning compaction)
- Critical threshold: 80% (run `/compact` immediately)
- Auto-notification via hooks when thresholds exceeded

**Best Practices**:
- `/compact` after completing each user story or wave
- Delegate to sub-agents for tasks >30k tokens
- Sub-agents return summaries, not full outputs
- Use session forking for exploratory work (preserves main context)

### Checkpoints
Create checkpoints before:
- Each implementation wave
- Destructive operations

Use `/rewind` for recovery.

**Checkpoint Strategy by Phase**:
| Phase | Checkpoint When |
|-------|----------------|
| `/speckit.plan` | Before starting research phase |
| `/speckit.implement` | Before each wave starts |
| `/speckit.analyze` | Not needed (read-only) |

**Session Forking vs Checkpoints** (v2.1.19):
- **Checkpoints**: Roll back code changes while preserving conversation
- **Session Forking**: Explore alternatives in parallel, compare results, keep main path intact
- **Use Forking When**: Exploring multiple approaches, testing risky changes, architecture comparisons

### Wave Execution Strategy

> **Canonical reference**: See `.claude/skills/implementation-execution.md` for detailed patterns.

**Summary**:
1. **Wave 1 (Infrastructure)**: Execute SEQUENTIALLY, checkpoint after completion
2. **Wave 2+ (Domain/Application)**: Execute `[P]` marked tasks in PARALLEL
3. **Quality Gate**: Run `code-reviewer` + `security-auditor` after each wave
4. **Context Management**: `/compact` between waves if context >70% (warning) or >80% (critical)
5. **Validation**: Verify all wave tasks complete before proceeding to next wave
6. **Session Forking** (v2.1.19): Fork before risky refactoring or experimental approaches

**Quick Example**:
```
# Wave 1: Infrastructure (sequential)
Implement T001 → checkpoint → T002 → checkpoint → T003

# Wave 2: Domain Layer (parallel)
& Use backend-developer to implement T004 (User entity)
& Use backend-developer to implement T005 (Product entity)
/tasks  # Monitor progress

# Quality gate after Wave 2
& Use code-reviewer to verify Wave 2 code quality
& Use security-auditor to scan for vulnerabilities
```

### Automatic Hooks

Claude Code hooks (`.claude/hooks.json`) automatically enforce:
- Code formatting on every file save
- Dangerous command blocking
- Test verification before task completion

---

## Workflow Rules

### Phase Completion Requirements (MANDATORY)

**CRITICAL**: No implementation phase may be marked complete until these exit criteria pass:

<!-- CUSTOMIZABLE: Replace <build-tool> and <test-runner> with your project's commands in CLAUDE.project.md -->
1. **Build Verification** (blocking):
   ```bash
   <build-tool> build  # Must exit with code 0
   ```
   - If build fails, fix ALL errors before completing the phase
   - Pre-existing errors discovered during implementation MUST be fixed
   - Do NOT suppress warnings as errors without explicit user approval

2. **Test Verification** (blocking):
   ```bash
   <test-runner>  # Must not regress
   ```
   - All previously passing tests must still pass
   - New tests written in this phase must pass (GREEN phase of TDD)
   - If tests fail, investigate and fix before completing

3. **Regression Check**:
   - Compare test count before/after implementation
   - No decrease in passing test count allowed
   - Document any tests that were intentionally modified

**If pre-existing issues block the build:**
1. Document the pre-existing issues found
2. Fix them as part of the implementation phase
3. Clearly separate pre-existing fixes from new implementation in commit messages
4. Never suppress errors without documenting why

**Exit Criteria Checklist** (verify before marking phase complete):
- [ ] Build exits with code 0
- [ ] Tests show no regressions
- [ ] All new implementation code compiles
- [ ] All new tests pass (TDD GREEN)

### Before Every Commit
1. Build — must pass
2. Tests — must pass
3. Format check — must be clean

### Test-First (Non-Negotiable)
Per Constitution Article III:
1. Write tests for new behavior
2. Run tests and verify they fail (RED)
3. Implement until tests pass (GREEN)
4. Refactor with test safety net

**No production code without failing tests first.**

### Branch Strategy
- Feature branches for new work
- Integration tests for API endpoints
- Unit tests for business logic

---

## Resources
- **Skills**: `.claude/skills/` — Execution patterns and workflows
- **Agents**: `.claude/agents/` — Specialized sub-agent definitions
- **Constitution**: `.specify/memory/constitution.md` — Governance principles
- **Plugins**: `.claude/skills/external-plugins.md` — Third-party plugin integration guide
- **Settings**: `.claude/settings.json` — Claude Code 2.1.19 configuration (plans directory, MCP, thresholds)
- **Hooks**: `.claude/hooks.json` — Automatic enforcement and context injection
- **Project Config**: `CLAUDE.project.md` — Technology-specific conventions and domain agents

---

## External Plugins

External plugins extend Claude Code capabilities. See `.claude/skills/external-plugins.md` for detailed usage, installation, and troubleshooting.

<!-- CUSTOMIZABLE: Add technology-specific plugins in CLAUDE.project.md -->
| Plugin | Use For | Spec-Kit Phase(s) |
|--------|---------|-------------------|
| `superpowers` | TDD workflow, debugging, planning, git worktrees | `/speckit.plan`, `/speckit.implement` |
| `engineering-workflow-plugin` | Code review, git workflows | `/speckit.analyze`, post-implement |
| `dev-agent-skills` | Conventional commits, PR creation/review | Post-implement (PRs, commits) |
| `awesome-claude-skills` | Software architecture, design patterns | `/speckit.plan`, `/speckit.constitution` |

> **Technology-specific plugins**: See `CLAUDE.project.md` for project-specific plugin recommendations.

**Installation**: `/plugin marketplace add <owner>/<plugin-name>`
**Pinned Installation** (v2.1.14): `/plugin marketplace add <owner>/<plugin-name>@<commit-sha>`
**List installed**: `/plugins`

> **Recommended**: Pin plugins to specific commits for reproducible builds. See `.claude/skills/external-plugins.md` for pinning guide.
