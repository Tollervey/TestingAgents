# Project Configuration

## Governance

**Constitution**: `.specify/memory/constitution.md` — All development must comply with constitutional principles.

**Spec-Kit Workflow Order**:
1. `/speckit.constitution` → Establish governance principles
2. `/speckit.specify` → Define what to build (technology-agnostic)
3. `/speckit.clarify` → Refine requirements
4. `/speckit.plan` → Technical architecture + Claude Code execution strategy
5. `/speckit.tasks` → Task breakdown with dependencies and parallel markers
6. `/speckit.implement` → Wave-based execution with sub-agents
7. `/speckit.analyze` → Multi-agent validation
8. `/speckit.checklist` → Automated verification gates

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

| Plugin | Use For |
|--------|---------|
| `superpowers` | TDD workflow, debugging, planning, git worktrees |
| `dotnet-claude-code-skills` | DDD patterns, EF Core, BDD testing |
| `engineering-workflow-plugin` | Code review, git workflows |
| `dev-agent-skills` | Conventional commits, PR creation/review |
| `awesome-claude-skills` | Software architecture, design patterns |
