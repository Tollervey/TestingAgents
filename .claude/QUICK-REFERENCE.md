# Claude Code Quick Reference

> **Full documentation**: See [CLAUDE.md](../CLAUDE.md) for complete governance, conventions, and agent details.
> **Project-specific commands**: See [CLAUDE.project.md](../CLAUDE.project.md) for build, test, and technology-specific commands.

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
| `backend-developer` | Sonnet | Backend implementation |
| `frontend-developer` | Sonnet | UI/frontend development |
| `test-engineer` | Sonnet | TDD tests (write BEFORE code) |
| `database-architect` | Sonnet | Schema design, migrations |
| `code-reviewer` | Haiku | Quality checks (read-only) |
| `security-auditor` | Haiku | Security scans (read-only) |

**Invocation**: `Use [agent-name] to [task]`

> **Domain agents**: See `CLAUDE.project.md` for project-specific agent extensions.

## External Plugins (Quick Lookup)

| Plugin | Use For |
|--------|---------|
| `superpowers` | TDD, debugging, worktrees |
| `engineering-workflow-plugin` | Code review, git |
| `dev-agent-skills` | Commits, PRs |
| `awesome-claude-skills` | Architecture patterns |

> **Technology-specific plugins**: See `CLAUDE.project.md` for project-specific plugin recommendations.

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
- Run your build command after every change
- Use `/compact` between implementation waves
- Delegate large tasks to sub-agents (>30k tokens)
- Create checkpoints before each wave
