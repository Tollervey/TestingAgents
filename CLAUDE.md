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

## Quick Reference

### .NET Commands
| Action | Command |
|--------|---------|
| Build | `dotnet build` |
| Test | `dotnet test` |
| Run | `dotnet run` |
| Watch | `dotnet watch run` |
| Format | `dotnet format` |
| Add Migration | `dotnet ef migrations add <Name>` |
| Update Database | `dotnet ef database update` |
| Publish | `dotnet publish -c Release` |

### Frontend Commands
| Action | Command |
|--------|---------|
| Dev server | `npm run dev` |
| Build | `npm run build` |
| Test | `npm test` |
| Lint | `npm run lint` |

---

## Code Conventions

### .NET Style
- **Naming**: PascalCase for public members; for private fields use an underscore followed by camelCase starting with a lowercase letter (e.g., `_myPrivateField`)
- **Types**: Prefer `var` when type is obvious; use nullable references (`string?`)
- **Async**: Use `async/await` throughout — never `.Result` or `.Wait()`
- **DTOs**: Prefer records for immutable data
- **Constructors**: Use primary constructors where appropriate (.NET 8+)
- **Interfaces**: Separate files, prefixed with `I`

### Database (EF Core)
- Code-first migrations
- Repository pattern for data access
- Always async operations
- `.AsNoTracking()` for read-only queries
- Transactions for multi-step operations
- Index foreign keys and frequently queried columns

### API Design
- RESTful with `[ApiController]` attribute
- Return `ActionResult<T>` for type safety
- Problem details for errors (RFC 7807)
- Validate with FluentValidation or Data Annotations
- Version in URL path (`/api/v1/`)

### Frontend
- Component-based architecture
- TypeScript for type safety
- Semantic HTML + ARIA for accessibility
- Mobile-first responsive design

---

## Common .NET Gotchas
- Forgetting to register services in DI container
- Not disposing `IDisposable` (use `using` or `await using`)
- Blocking on async causing deadlocks
- N+1 queries — check generated SQL
- Missing `[FromBody]` or `[FromQuery]` attributes
- DbContext lifetime issues (scoped, not singleton)

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
| `backend-developer` | Sonnet | C# implementation, APIs, services |
| `frontend-developer` | Sonnet | Blazor/UI components |
| `test-engineer` | Sonnet | Writing tests BEFORE implementation |
| `database-architect` | Sonnet | Schema design, migrations |
| `security-auditor` | Haiku | Security scanning, vulnerability checks |
| `code-reviewer` | Haiku | Code quality, constitution compliance |
| `breezsdk-developer` | Sonnet | BreezSDK C# implementation, payment flows, event handling |
| `breezsdk-architect` | Opus | BreezSDK integration architecture, production readiness |
| `breezsdk-reviewer` | Haiku | BreezSDK code review, SDK pattern compliance |
| `breezsdk-ux` | Sonnet | BreezSDK UX guidelines, payment flow design |
| `breezsdk-test-engineer` | Sonnet | BreezSDK testing patterns, mock strategies |
| `umbraco-architect` | Opus | Umbraco architecture, Document Type design, package architecture |
| `umbraco-backend-developer` | Sonnet | Umbraco C# development, Composers, Services, Notification Handlers |
| `umbraco-frontend-developer` | Sonnet | Umbraco backoffice UI, Lit/TypeScript, UUI components |
| `umbraco-backend-reviewer` | Haiku | Umbraco C# code review, pattern compliance |
| `umbraco-frontend-reviewer` | Haiku | Umbraco frontend review, accessibility compliance |

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
| `breezsdk-developer` | Read, Write, Edit, Bash, Glob, Grep | ✅ Yes |
| `breezsdk-architect` | Read, Glob, Grep | ❌ Read-only |
| `breezsdk-reviewer` | Read, Grep, Glob | ❌ Read-only |
| `breezsdk-ux` | Read, Grep, Glob | ❌ Read-only |
| `breezsdk-test-engineer` | Read, Write, Edit, Bash, Glob, Grep | ✅ Yes |
| `umbraco-architect` | Read, Glob, Grep | ❌ Read-only |
| `umbraco-backend-developer` | Read, Write, Edit, Bash, Glob, Grep | ✅ Yes |
| `umbraco-frontend-developer` | Read, Write, Edit, Bash, Glob, Grep | ✅ Yes |
| `umbraco-backend-reviewer` | Read, Glob, Grep | ❌ Read-only |
| `umbraco-frontend-reviewer` | Read, Glob, Grep | ❌ Read-only |

### Agent & Plugin Utilization by Phase

This is the **authoritative** mapping of which agents and plugins apply to each Spec-Kit phase.

| Phase | Primary Agent(s) | Supporting Agent(s) | Recommended Plugins | Parallel? |
|-------|-----------------|---------------------|---------------------|-----------|
| `/speckit.constitution` | solution-architect | — | awesome-claude-skills | No |
| `/speckit.specify` | solution-architect | — | — | No |
| `/speckit.clarify` | solution-architect | — | — | No |
| `/speckit.plan` | solution-architect, database-architect, breezsdk-architect, umbraco-architect | — | superpowers, awesome-claude-skills | No |
| `/speckit.tasks` | (orchestrator) | — | — | No |
| `/speckit.checklist` | (orchestrator) | — | — | No |
| `/speckit.analyze` | code-reviewer | — | engineering-workflow-plugin | No |
| `/speckit.implement` | backend-developer, test-engineer, frontend-developer, breezsdk-developer, breezsdk-test-engineer, umbraco-backend-developer, umbraco-frontend-developer | security-auditor, code-reviewer | superpowers, dotnet-claude-code-skills | ✅ Yes |
| Post-implement | — | code-reviewer, security-auditor, breezsdk-reviewer, breezsdk-ux, umbraco-backend-reviewer, umbraco-frontend-reviewer | dev-agent-skills, engineering-workflow-plugin | ✅ Yes |

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

> **Canonical reference**: See `.claude/skills/dotnet-implementation-execution.md` for detailed patterns.

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

**No production code without failing tests first.**

### Test Timing Guidelines (CRITICAL)

**Core Principle**: Tests should verify BEHAVIOR, not exact timing. Production delay values are configuration, not logic worth testing.

#### Slow Test Anti-Patterns

| Scenario | BAD (Slow) | GOOD (Fast) |
|----------|------------|-------------|
| Timeout behavior | `Task.Delay(30s)` then assert timeout | Use short timeout (100ms), verify TimeoutRejectedException |
| Retry policies | Use production policy with 2s delays | Create test policy with 50ms delays |
| Exponential backoff | Wait for 2s + 4s + 8s = 14s | Use 50ms + 100ms + 200ms = 350ms |
| Circuit breaker | 16 second break duration | 1-2 second break duration |
| Reconnection | Assert exact timing (jitter fails) | Assert retry count or state transitions |
| Transient states | Observe mid-transition | Collect state history via events |

#### Fast Test Policy Pattern (Polly)

When testing resilience policies, create test-specific versions with short delays:

```csharp
// SLOW: Using production policy (2s base delay, 14s total for 3 retries)
await ResiliencePolicies.ConnectPolicy.ExecuteAsync(...);

// FAST: Create test policy with 50ms base delay (350ms total)
private static ResiliencePipeline CreateFastTestPolicy() =>
    new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,           // Same count as production
            Delay = TimeSpan.FromMilliseconds(50),  // Fast delay for tests
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        })
        .Build();
```

#### What to Test vs What to Skip

| Test This (Behavior) | Skip This (Configuration) |
|---------------------|---------------------------|
| Retry count is correct | Exact delay values (2s, 4s, 8s) |
| Backoff pattern (exponential vs constant) | Production timeout duration |
| Jitter is applied (delays vary) | Precise timing measurements |
| Exception propagation after retries | Waiting for real timeouts |

**Rule**: If a test takes >2 seconds due to waiting, create a fast test policy with short delays.

### Test Execution Best Practices

**Avoid full test suite runs during development:**
- Use `--filter "FullyQualifiedName~ClassName"` for targeted tests
- Run new feature tests in isolation first
- Full suite runs can hang on CI-dependent or integration tests

**If tests hang, check for:**
- Blocking calls (`.Wait()`, `.Result`) - use `await` instead
- Infinite loops in async code
- Missing CancellationToken handling
- Tests waiting for real timeouts instead of short test timeouts

### Static State in Tests

When testing classes with static state (Meters, ActivitySources, ConcurrentDictionaries):
- Use unique identifiers per test (e.g., `$"test-{Guid.NewGuid():N}"`)
- Don't assert exact counts - filter by your unique identifier
- Static state persists across test runs in the same process

### API Verification Before Writing Tests

1. Verify the API exists in the target framework
2. Check if it's a standard API or requires an extension package
3. For OpenTelemetry: `Activity` is `System.Diagnostics`, extensions are in `OpenTelemetry.Api`
4. Prefer standard APIs over extension methods for broader compatibility
5. Example: Use `activity.AddEvent()` instead of `activity.RecordException()` (extension method)

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

---

## External Plugins

External plugins extend Claude Code capabilities. See `.claude/skills/external-plugins.md` for detailed usage, installation, and troubleshooting.

| Plugin | Use For | Spec-Kit Phase(s) |
|--------|---------|-------------------|
| `superpowers` | TDD workflow, debugging, planning, git worktrees | `/speckit.plan`, `/speckit.implement` |
| `dotnet-claude-code-skills` | DDD patterns, EF Core, BDD testing | `/speckit.implement` |
| `engineering-workflow-plugin` | Code review, git workflows | `/speckit.analyze`, post-implement |
| `dev-agent-skills` | Conventional commits, PR creation/review | Post-implement (PRs, commits) |
| `awesome-claude-skills` | Software architecture, design patterns | `/speckit.plan`, `/speckit.constitution` |

**Installation**: `/plugin marketplace add <owner>/<plugin-name>`
**Pinned Installation** (v2.1.14): `/plugin marketplace add <owner>/<plugin-name>@<commit-sha>`
**List installed**: `/plugins`

> **Recommended**: Pin plugins to specific commits for reproducible builds. See `.claude/skills/external-plugins.md` for pinning guide.
