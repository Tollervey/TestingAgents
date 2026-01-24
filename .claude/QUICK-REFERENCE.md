# Claude Code Quick Reference - .NET Full Stack

> **Full documentation**: See [CLAUDE.md](../CLAUDE.md) for complete governance, conventions, and agent details.

## .NET Commands
| Command | Description |
|---------|-------------|
| `dotnet build` | Build the solution |
| `dotnet test` | Run all tests |
| `dotnet run` | Run the application |
| `dotnet watch run` | Run with hot reload |
| `dotnet ef migrations add <Name>` | Create migration |
| `dotnet ef database update` | Apply migrations |
| `dotnet format` | Format code |

## Spec-Kit Workflow (Abbreviated)

```
/speckit.constitution → /speckit.specify → /speckit.clarify → /speckit.plan
                                                                    ↓
                        /speckit.implement ← /speckit.analyze ← /speckit.tasks
                                                                    ↓
                                                            /speckit.checklist (optional)
```

| Command | Purpose |
|---------|---------|
| `/speckit.constitution` | Establish governance |
| `/speckit.specify` | Define requirements |
| `/speckit.plan` | Technical architecture |
| `/speckit.tasks` | Task breakdown |
| `/speckit.checklist` | Quality gate checklists |
| `/speckit.analyze` | Consistency check |
| `/speckit.implement` | Execute with agents |

## Agents (Quick Lookup)

| Agent | Model | Use For |
|-------|-------|---------|
| `solution-architect` | Opus | Architecture decisions (read-only) |
| `backend-developer` | Sonnet | C# implementation |
| `frontend-developer` | Sonnet | Blazor/UI |
| `test-engineer` | Sonnet | TDD tests (write BEFORE code) |
| `database-architect` | Sonnet | EF Core, migrations |
| `code-reviewer` | Haiku | Quality checks (read-only) |
| `security-auditor` | Haiku | Security scans (read-only) |

**Invocation**: `Use [agent-name] to [task]`

## External Plugins (Quick Lookup)

| Plugin | Use For |
|--------|---------|
| `superpowers` | TDD, debugging, worktrees |
| `dotnet-claude-code-skills` | DDD, EF Core, BDD |
| `engineering-workflow-plugin` | Code review, git |
| `dev-agent-skills` | Commits, PRs |
| `awesome-claude-skills` | Architecture patterns |

**Install**: `/plugin marketplace add <owner>/<plugin-name>`
**List**: `/plugins`
**Details**: See `.claude/skills/external-plugins.md`

## Context & Session

| Command | Purpose |
|---------|---------|
| `/compact` | Compress context (use between waves) |
| `/clear` | Reset context |
| `/rewind` | Return to checkpoint |
| `Esc` | Stop current action |
| `Esc Esc` | Open rewind menu |

## TDD Workflow (Non-Negotiable)

```
1. Use test-engineer to write tests     → RED (tests fail)
2. Use backend-developer to implement   → GREEN (tests pass)
3. Refactor with test safety net        → REFACTOR
```

## Tips
- Run `dotnet build` after every change
- Use `/compact` between implementation waves
- Delegate large tasks to sub-agents (>30k tokens)
- Create checkpoints before each wave
