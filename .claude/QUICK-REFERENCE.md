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

## Available Skills (invoke with `/skill-name`)

### .NET Backend
| Skill | Description |
|-------|-------------|
| `/add-endpoint <desc>` | Add new API endpoint with DTOs, validation |
| `/add-entity <name>` | Create EF Core entity with configuration |
| `/add-service <name>` | Create service with interface and DI |
| `/add-migration <name>` | Create and apply EF Core migration |
| `/optimize-query <desc>` | Optimize EF Core queries |

### Frontend
| Skill | Description |
|-------|-------------|
| `/add-component <name>` | Create accessible, responsive component |
| `/fix-ui <issue>` | Fix UI issues with visual comparison |

### Full Stack
| Skill | Description |
|-------|-------------|
| `/implement-feature <desc>` | Full-stack feature implementation |
| `/add-tests <target>` | Add .NET or frontend tests |
| `/debug <issue>` | Systematic debugging workflow |
| `/fix-issue <number>` | Fix GitHub issue end-to-end |
| `/code-review <file>` | Review code for quality |
| `/refactor <target>` | Refactor with verification |
| `/create-pr` | Create well-structured PR |

## Available Subagents (invoke with "use a subagent to...")

### Code Quality
| Agent | Description |
|-------|-------------|
| `code-reviewer` | .NET and frontend code quality |
| `security-reviewer` | OWASP vulnerabilities, .NET security |
| `performance-analyzer` | EF Core, async, frontend performance |
| `architecture-reviewer` | Solution structure, design patterns |

### Specialized
| Agent | Description |
|-------|-------------|
| `database-expert` | EF Core, queries, migrations, indexes |
| `ui-expert` | Accessibility (WCAG), responsive design |
| `api-designer` | REST conventions, .NET Web API patterns |
| `test-writer` | xUnit, integration tests, React Testing Library |
| `documentation-writer` | Technical documentation |

## Best Practice Workflows

### New Feature (Full Stack)
```
1. Enter Plan Mode (Shift+Tab)
2. "/implement-feature user registration"
3. Follow the checklist through all layers
4. "use a subagent to review this code for security"
5. "/create-pr"
```

### Database Change
```
1. "/add-entity Order"
2. "/add-migration CreateOrdersTable"
3. "use a subagent to review database design"
```

### Performance Investigation
```
"use a subagent to analyze performance of the orders API"
"/optimize-query GetOrdersWithItems"
```

### Comprehensive Code Review
```
"use subagents in parallel to review this for:
- security vulnerabilities
- performance issues
- code quality
- accessibility"
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
