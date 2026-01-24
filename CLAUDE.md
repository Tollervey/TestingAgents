# Project Configuration

## Quick Reference - .NET
- Build: `dotnet build`
- Test: `dotnet test`
- Run: `dotnet run`
- Watch: `dotnet watch run`
- EF Migrations: `dotnet ef migrations add <Name>` / `dotnet ef database update`
- Format: `dotnet format`
- Publish: `dotnet publish -c Release`

## Quick Reference - Frontend
- Dev server: `npm run dev` (Vite/React/Angular/Vue)
- Build: `npm run build`
- Test: `npm test`
- Lint: `npm run lint`

## .NET Code Style
- Use PascalCase for public members, camelCase for private fields with underscore prefix (_fieldName)
- Prefer `var` when the type is obvious from the right side
- Use `async/await` throughout - never `.Result` or `.Wait()` on tasks
- Use nullable reference types (`string?`) and handle nullability explicitly
- Prefer records for DTOs and immutable data
- Use primary constructors where appropriate (.NET 8+)
- Interfaces go in separate files, prefixed with `I`

## Database Conventions
- Use EF Core with code-first migrations
- Repository pattern for data access when appropriate
- Always use async database operations
- Include `.AsNoTracking()` for read-only queries
- Use transactions for multi-step operations
- Index foreign keys and frequently queried columns

## API Conventions
- RESTful endpoints with consistent naming
- Use `[ApiController]` attribute on controllers
- Return `ActionResult<T>` for type safety
- Use problem details for error responses (RFC 7807)
- Validate input with FluentValidation or Data Annotations
- API versioning in URL path (`/api/v1/`)

## Frontend Conventions
- Component-based architecture
- Separate concerns: components, services, models
- Use TypeScript for type safety
- Accessible HTML (semantic elements, ARIA when needed)
- Mobile-first responsive design
- State management appropriate to app complexity

## Workflow Rules
- ALWAYS run `dotnet build` after code changes to catch errors
- ALWAYS run tests before committing
- Run `dotnet format` before committing
- Create feature branches for new work
- Write integration tests for API endpoints
- Write unit tests for business logic

## Common .NET Gotchas
- Forgetting to register services in DI container
- Not disposing IDisposable resources (use `using` or `await using`)
- Blocking on async code causing deadlocks
- N+1 queries - always check generated SQL
- Missing `[FromBody]` or `[FromQuery]` attributes
- DbContext lifetime issues (scoped, not singleton)

## Imports
See @.claude/skills/ for available skills and workflows.
See @.claude/agents/ for specialized review agents.
See @.claude/QUICK-REFERENCE.md for command reference.

## Claude Code Execution Guidelines

This project uses Claude Code with specialized agents and automated quality enforcement.

### Available Agents

| Agent | Model | Purpose | Invoke For |
|-------|-------|---------|------------|
| `solution-architect` | Opus | System design, architectural decisions | Complex reasoning, architecture changes |
| `backend-developer` | Sonnet | .NET implementation | C# code, APIs, services |
| `frontend-developer` | Sonnet | Blazor/UI implementation | Components, pages, UI logic |
| `test-engineer` | Sonnet | Test implementation | Writing tests (BEFORE code) |
| `database-architect` | Sonnet | Schema, migrations | Database changes |
| `security-auditor` | Haiku | Security scanning | Security reviews |
| `code-reviewer` | Haiku | Quality checks | Code reviews |

### Spec-Kit Integration

When running spec-kit commands, Claude Code automatically:
- `/speckit.plan` → Includes agent architecture, parallel boundaries
- `/speckit.tasks` → Adds execution metadata, dependency markers
- `/speckit.implement` → Uses wave-based parallel execution
- `/speckit.analyze` → Runs multi-agent validation
- `/speckit.checklist` → Executes automated verification gates

### Parallel Execution Rules

Tasks marked `[P]` in tasks.md can run in parallel:
```
& Use backend-developer to implement T004 (User entity)
& Use backend-developer to implement T005 (Product entity)
& Use backend-developer to implement T006 (Order entity)
```

Always respect `depends_on` declarations - never start blocked tasks.

### Background Execution

For operations >5 minutes, use background execution:
- Press `Ctrl+B` to background long-running tasks
- Monitor with `/tasks`
- Maximum 5 concurrent background agents recommended

### Context Management

- **Compact** after completing each user story or wave
- **Delegate** to sub-agents for tasks >30k tokens
- Sub-agents return summaries, not full outputs
- Reference files by path, don't include full contents

### Quality Gates (Automated via Hooks)

These run automatically:
- `.cs`/`.razor` files: Format verification on save
- Bash commands: Destructive command protection
- Task completion: Test verification gate

### Checkpoint Policy

Create checkpoints:
- Before each implementation wave
- After completing user stories
- Before destructive operations

Use `/rewind` for recovery, not manual fixes.

### Test-First Requirement

Per Constitution Article III:
1. Create failing tests (RED)
2. Verify tests fail
3. Implement until tests pass (GREEN)
4. Refactor with test safety net

**No production code without failing tests first.**

### Constitution Location

All development must comply with: `.specify/memory/constitution.md`
