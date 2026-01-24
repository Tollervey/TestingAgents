# Claude Code Quick Reference - .NET Full Stack

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

## Spec-Kit Workflow Commands

### Governance & Specification
| Command | Description |
|---------|-------------|
| `/speckit.constitution` | Establish/update governance principles |
| `/speckit.specify <desc>` | Create technology-agnostic feature specification |
| `/speckit.clarify` | Refine requirements with targeted questions |

### Planning & Tasks
| Command | Description |
|---------|-------------|
| `/speckit.plan` | Generate technical architecture and design artifacts |
| `/speckit.tasks` | Break plan into dependency-ordered task list |
| `/speckit.checklist <domain>` | Generate quality gate checklists |

### Execution & Validation
| Command | Description |
|---------|-------------|
| `/speckit.analyze` | Read-only consistency check across artifacts |
| `/speckit.implement` | Wave-based execution with sub-agents |

## Available Agents (invoke with "Use [agent-name] to...")

### Architecture & Review (Read-Only)
| Agent | Model | Description |
|-------|-------|-------------|
| `solution-architect` | Opus | Architecture decisions, complex refactoring analysis |
| `code-reviewer` | Haiku | Code quality, constitution compliance checks |
| `security-auditor` | Haiku | OWASP vulnerabilities, secrets detection |

### Implementation (Can Modify Files)
| Agent | Model | Description |
|-------|-------|-------------|
| `backend-developer` | Sonnet | C# implementation, APIs, services, business logic |
| `frontend-developer` | Sonnet | Blazor/Razor components, UI implementation |
| `test-engineer` | Sonnet | TDD test creation (tests BEFORE implementation) |
| `database-architect` | Sonnet | EF Core, migrations, schema design, queries |

## Best Practice Workflows

### New Feature (Full Spec-Kit Flow)
```
1. /speckit.specify "user registration with email verification"
2. /speckit.clarify (if ambiguities exist)
3. /speckit.plan
4. /speckit.tasks
5. /speckit.checklist security
6. /speckit.analyze
7. /speckit.implement
```

### Database Change
```
1. Use database-architect to design Order entity with relationships
2. Use database-architect to create migration AddOrdersTable
3. Use code-reviewer to verify constitution Article IV compliance
```

### TDD Implementation (Constitution Article III)
```
# Step 1: Create failing tests FIRST
Use test-engineer to write unit tests for OrderService

# Step 2: Verify RED state
dotnet test --filter "OrderServiceTests"  # Should FAIL

# Step 3: Implement to GREEN
Use backend-developer to implement OrderService

# Step 4: Verify GREEN state
dotnet test --filter "OrderServiceTests"  # Should PASS
```

### Quality Gate (After Each Wave)
```
& Use code-reviewer to verify code quality
& Use security-auditor to scan for vulnerabilities
```

## Context Management
- `/clear` - Reset between unrelated tasks
- `/compact` - Compress context when getting full
- `Esc` - Stop Claude mid-action
- `Esc Esc` - Open rewind menu
- `/rewind` - Go back to a previous state

## Session Management
- `claude --continue` - Resume last session
- `claude --resume` - Pick from recent sessions
- `/rename` - Name your session for later

## Tips for .NET Development
- Always run `dotnet build` after changes to catch errors early
- Use `dotnet watch run` during development for hot reload
- Check EF Core SQL with logging before deploying queries
- Use subagents for reviews to keep main context clean
- Run `/clear` frequently between different tasks
