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

### Agent Utilization by Phase

| Phase | Primary Agent(s) | Supporting Agent(s) | Parallel? |
|-------|-----------------|---------------------|-----------|
| `/speckit.constitution` | solution-architect | — | No |
| `/speckit.specify` | solution-architect | — | No |
| `/speckit.clarify` | solution-architect | — | No |
| `/speckit.plan` | solution-architect, database-architect | — | No |
| `/speckit.tasks` | (orchestrator) | — | No |
| `/speckit.checklist` | (orchestrator) | — | No |
| `/speckit.analyze` | code-reviewer | — | No |
| `/speckit.implement` | backend-developer, test-engineer, frontend-developer | security-auditor, code-reviewer | ✅ Yes |

### Parallel Execution
Tasks marked `[P]` in tasks.md can run concurrently:
```
& Use backend-developer to implement T004 (User entity)
& Use backend-developer to implement T005 (Product entity)
```
Always respect `depends_on` — never start blocked tasks.

### Background Execution
For operations >5 minutes:
- `Ctrl+B` to background long-running tasks
- `/tasks` to monitor progress
- Maximum 5 concurrent background agents

### Context Management
- `/compact` after completing each user story or wave
- Delegate to sub-agents for tasks >30k tokens
- Sub-agents return summaries, not full outputs

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

### Wave Execution Strategy

1. **Wave 1 (Infrastructure)**: Execute SEQUENTIALLY, checkpoint after completion
2. **Wave 2+ (Domain/Application)**: Execute `[P]` marked tasks in PARALLEL
3. **Quality Gate**: Run `code-reviewer` + `security-auditor` after each wave
4. **Context Management**: `/compact` between waves if context >150k tokens
5. **Validation**: Verify all wave tasks complete before proceeding to next wave

**Wave Execution Example**:
```
# Wave 1: Infrastructure (sequential)
Implement T001 → checkpoint → T002 → checkpoint → T003

# Wave 2: Domain Layer (parallel)
& Use backend-developer to implement T004 (User entity)
& Use backend-developer to implement T005 (Product entity)
& Use backend-developer to implement T006 (Order entity)
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

### Branch Strategy
- Feature branches for new work
- Integration tests for API endpoints
- Unit tests for business logic

---

## Resources
- **Skills**: `.claude/skills/` — Execution patterns and workflows
- **Agents**: `.claude/agents/` — Specialized sub-agent definitions
- **Constitution**: `.specify/memory/constitution.md` — Governance principles

## External Plugins

| Plugin | Use For | Spec-Kit Phase(s) |
|--------|---------|-------------------|
| `superpowers` | TDD workflow, debugging, planning, git worktrees | `/speckit.plan`, `/speckit.implement` |
| `dotnet-claude-code-skills` | DDD patterns, EF Core, BDD testing | `/speckit.implement` |
| `engineering-workflow-plugin` | Code review, git workflows | `/speckit.analyze`, post-implement |
| `dev-agent-skills` | Conventional commits, PR creation/review | Post-implement (PRs, commits) |
| `awesome-claude-skills` | Software architecture, design patterns | `/speckit.plan`, `/speckit.constitution` |
