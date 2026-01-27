# Project-Specific Configuration

This file contains technology-specific conventions, domain agent documentation, and project-specific settings for this .NET project. It extends the core SpecKit configuration in `CLAUDE.md`.

> **Note**: This file is project-specific. When copying SpecKit to a new project, replace this file with your project's technology conventions, or delete it if not needed.

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

### API Design (.NET)
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

## Domain Agents

### Available Domain Agents

| Agent | Model | Invoke For |
|-------|-------|------------|
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

### Domain Agent Tools & Permissions

| Agent | Tools | Can Modify Files? |
|-------|-------|-------------------|
| `breezsdk-developer` | Read, Write, Edit, Bash, Glob, Grep | Yes |
| `breezsdk-architect` | Read, Glob, Grep | Read-only |
| `breezsdk-reviewer` | Read, Grep, Glob | Read-only |
| `breezsdk-ux` | Read, Grep, Glob | Read-only |
| `breezsdk-test-engineer` | Read, Write, Edit, Bash, Glob, Grep | Yes |
| `umbraco-architect` | Read, Glob, Grep | Read-only |
| `umbraco-backend-developer` | Read, Write, Edit, Bash, Glob, Grep | Yes |
| `umbraco-frontend-developer` | Read, Write, Edit, Bash, Glob, Grep | Yes |
| `umbraco-backend-reviewer` | Read, Glob, Grep | Read-only |
| `umbraco-frontend-reviewer` | Read, Glob, Grep | Read-only |

### Domain Agent Utilization by Phase

| Phase | Domain Agent(s) | Role |
|-------|----------------|------|
| `/speckit.plan` | breezsdk-architect, umbraco-architect | Domain-specific architecture decisions |
| `/speckit.implement` | breezsdk-developer, breezsdk-test-engineer, umbraco-backend-developer, umbraco-frontend-developer | Domain implementation |
| Post-implement | breezsdk-reviewer, breezsdk-ux, umbraco-backend-reviewer, umbraco-frontend-reviewer | Domain-specific review |

---

## MCP Server Configuration

This project uses project-specific MCP servers. Add these to `.claude/settings.json` under `mcpServers`:

```json
{
  "mcpServers": {
    "umbraco-docs": {
      "url": "https://docs.umbraco.com/~gitbook/mcp",
      "transport": "sse",
      "autoSearch": "auto:3",
      "description": "Umbraco v17 LTS documentation via GitBook MCP"
    }
  }
}
```

### WebFetch Domain Permissions

Add these to `.claude/settings.local.json` under `permissions.allow` for project-specific documentation access:

```json
"WebFetch(domain:sdk-doc-liquid.breez.technology)",
"WebFetch(domain:docs.umbraco.com)",
"WebFetch(domain:gitbook.com)"
```

---

## Technology-Specific Plugins

| Plugin | Use For | Spec-Kit Phase(s) |
|--------|---------|-------------------|
| `dotnet-claude-code-skills` | DDD patterns, EF Core, BDD testing | `/speckit.implement` |

**Installation**: `/plugin marketplace add anthropics/dotnet-claude-code-skills`

---

## Test Timing Guidelines (.NET / Polly)

**Core Principle**: Tests should verify BEHAVIOR, not exact timing. Production delay values are configuration, not logic worth testing.

### Slow Test Anti-Patterns

| Scenario | BAD (Slow) | GOOD (Fast) |
|----------|------------|-------------|
| Timeout behavior | `Task.Delay(30s)` then assert timeout | Use short timeout (100ms), verify TimeoutRejectedException |
| Retry policies | Use production policy with 2s delays | Create test policy with 50ms delays |
| Exponential backoff | Wait for 2s + 4s + 8s = 14s | Use 50ms + 100ms + 200ms = 350ms |
| Circuit breaker | 16 second break duration | 1-2 second break duration |
| Reconnection | Assert exact timing (jitter fails) | Assert retry count or state transitions |
| Transient states | Observe mid-transition | Collect state history via events |

### Fast Test Policy Pattern (Polly)

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

### What to Test vs What to Skip

| Test This (Behavior) | Skip This (Configuration) |
|---------------------|---------------------------|
| Retry count is correct | Exact delay values (2s, 4s, 8s) |
| Backoff pattern (exponential vs constant) | Production timeout duration |
| Jitter is applied (delays vary) | Precise timing measurements |
| Exception propagation after retries | Waiting for real timeouts |

**Rule**: If a test takes >2 seconds due to waiting, create a fast test policy with short delays.

### Non-Transient Errors Before Resilience Pipelines

Configuration validation and argument checks are deterministic — they fail the same way every time. These MUST be called **before** entering a retry pipeline, not inside it. Otherwise tests expecting fast validation failures wait through all retry delays.

```csharp
// BAD: Validation retried 3x with exponential backoff (2s + 4s + 8s = 14s!)
await RetryPolicy.ExecuteAsync(async ct => {
    ValidateConfiguration(); // Deterministic failure retried uselessly
    await ConnectInternalAsync(ct);
}, cancellationToken);

// GOOD: Validate before the pipeline
ValidateConfiguration(); // Fails fast
await RetryPolicy.ExecuteAsync(async ct => {
    await ConnectInternalAsync(ct); // Only transient failures retried
}, cancellationToken);
```

**Corollary**: Make resilience pipelines injectable (constructor parameter) so tests can provide fast policies.

### Async Enumerable Cancellation Tests

Never cancel inside a `foreach` loop body when the source may be empty. `MoveNextAsync()` blocks waiting for data — the loop body never executes, and the test hangs forever.

```csharp
// BAD: Hangs — empty channel blocks on MoveNextAsync
await foreach (var evt in channel.ReadAllAsync(cts.Token))
    cts.Cancel(); // Never reached if channel is empty!

// GOOD: Write data first so the loop body executes
await channel.WriteAsync(testEvent, CancellationToken.None);
await foreach (var evt in channel.ReadAllAsync(cts.Token))
    cts.Cancel(); // Reached because there's data
```

---

## Static State in Tests (.NET)

When testing classes with static state (Meters, ActivitySources, ConcurrentDictionaries):
- Use unique identifiers per test (e.g., `$"test-{Guid.NewGuid():N}"`)
- Don't assert exact counts - filter by your unique identifier
- Static state persists across test runs in the same process

### Metrics Test Pollution (Static Meters)

Static `Meter` instruments (Counters, Histograms, ObservableGauges) are shared across all tests in the same process. Using hardcoded tag values (e.g., `"testnet"`, `"mainnet"`) causes **cross-test pollution** — one test's recorded measurements appear in another test's assertions, causing flaky count/value checks depending on execution order.

**Fix**: Use `Guid.NewGuid()` for tag values that identify test-specific data, then **filter assertions** by that unique tag.

```csharp
// BAD: Hardcoded tag — polluted by other tests using the same value
var network = "testnet";
Metrics.RecordInvoiceCreated(network, "success");
var measurements = _counterMeasurements["breez.invoice.created"];
measurements.Should().HaveCount(1); // FLAKY

// GOOD: Unique tag + filtered assertion
var network = $"invoice-test-{Guid.NewGuid():N}";
Metrics.RecordInvoiceCreated(network, "success");
var measurements = _counterMeasurements["breez.invoice.created"]
    .Where(m => m.Tags.ToArray().Any(t => t.Key == "network" && t.Value?.ToString() == network))
    .ToList();
measurements.Should().HaveCount(1); // STABLE
```

**Rule**: Any test that records metrics via static instruments MUST use unique tag values and filter assertions by those values.

---

## API Verification Before Writing Tests (.NET)

1. Verify the API exists in the target framework
2. Check if it's a standard API or requires an extension package
3. For OpenTelemetry: `Activity` is `System.Diagnostics`, extensions are in `OpenTelemetry.Api`
4. Prefer standard APIs over extension methods for broader compatibility
5. Example: Use `activity.AddEvent()` instead of `activity.RecordException()` (extension method)

---

## Test Execution Best Practices (.NET)

**Avoid full test suite runs during development:**
- Use `--filter "FullyQualifiedName~ClassName"` for targeted tests
- Run new feature tests in isolation first
- Full suite runs can hang on CI-dependent or integration tests

**If tests hang, check for:**
- Blocking calls (`.Wait()`, `.Result`) - use `await` instead
- Infinite loops in async code
- Missing CancellationToken handling
- Tests waiting for real timeouts instead of short test timeouts
- Reading from empty channels/streams with cancellation inside the loop body

---

## Constitution Technology Mapping

The core constitution in `.specify/memory/constitution.md` uses technology-agnostic placeholders. Here's how they map to .NET:

| Constitution Placeholder | .NET Implementation |
|-------------------------|---------------------|
| `<build-tool>` | `dotnet build` |
| `<test-runner>` | `dotnet test` |
| `<formatter>` | `dotnet format` |
| `<dependency-audit-tool>` | `dotnet list package --vulnerable` |
| Typed configuration | `IOptions<T>` pattern from `Microsoft.Extensions.Options` |
| Validation library | FluentValidation or Data Annotations |
| Structured logging | Serilog or `Microsoft.Extensions.Logging` with structured providers |
| Health check middleware | `Microsoft.Extensions.Diagnostics.HealthChecks` |
| Repository pattern | EF Core DbContext with repository interfaces |
| Dependency injection | Built-in `Microsoft.Extensions.DependencyInjection` |

**Phase Completion Commands**:
```bash
# Build verification (blocking)
dotnet build <solution-file>  # Must exit with code 0

# Test verification (blocking)
dotnet test <solution-file> --no-build  # Must not regress

# Format check
dotnet format <solution-file> --verify-no-changes
```

---

## .NET-Specific Hooks Configuration

The core `.claude/hooks.json` uses technology-agnostic patterns. .NET projects should add project-specific hooks to `.claude/settings.local.json` under the `hooks` key.

### Hooks Customization Guide

**Core vs Project Hooks**:
- **Core hooks** (`.claude/hooks.json`): Technology-agnostic patterns (generic file extensions, common command blocking)
- **Project hooks** (`.claude/settings.local.json`): .NET-specific patterns (`.cs`, `.razor`, `dotnet` CLI permissions)

**Key Customizations for .NET**:
1. The PostToolUse Write|Edit hook in core checks for common web extensions (`ts`, `tsx`, `js`, `jsx`). .NET projects should add detection for `.cs` and `.razor` files.
2. The core hook permissions block dangerous commands generically. .NET projects should auto-approve safe `dotnet` CLI commands.

### .NET File Extension Patterns

For .NET projects, source file patterns should include:
- `.cs` — C# source files
- `.razor` — Razor component files
- `.csproj` — Project files
- `.sln` — Solution files

### Build/Test/Format Commands

Add these to `.claude/settings.local.json`:

**Build Command**:
```bash
dotnet build <solution-file>
```

**Test Command**:
```bash
dotnet test <solution-file> --no-build
```

**Format Command**:
```bash
dotnet format <solution-file>
```

### PostToolUse Hook Examples

Add these to `.claude/settings.local.json` under `hooks`:

**Format Reminder After Edit**:
```json
{
  "matcher": "Edit",
  "hooks": [
    {
      "type": "command",
      "command": "echo '[Reminder] Run dotnet build to check for errors'"
    }
  ]
}
```

**Auto-Format .cs Files After Write**:
```json
{
  "matcher": "Write(*.cs)",
  "hooks": [
    {
      "type": "command",
      "command": "dotnet format <solution-file> --include ${file_path}"
    }
  ]
}
```

### dotnet CLI Permission Patterns

Add these to `.claude/settings.local.json` under `permissions.allow` for auto-approved dotnet operations:

```json
"Bash(dotnet build*)",
"Bash(dotnet test*)",
"Bash(dotnet format*)",
"Bash(dotnet run*)",
"Bash(dotnet watch*)",
"Bash(dotnet ef migrations*)",
"Bash(dotnet ef database update*)",
"Bash(dotnet list package --vulnerable)",
"Bash(dotnet add package*)",
"Bash(dotnet remove package*)"
```

**Do NOT auto-approve**:
- `dotnet clean` — deletes build artifacts
- `dotnet ef database drop` — destructive operation
- `dotnet publish` — production deployment command

### Complete .claude/settings.local.json Example

```json
{
  "hooks": [
    {
      "matcher": "Edit",
      "hooks": [
        {
          "type": "command",
          "command": "echo '[Reminder] Run dotnet build to check for errors'"
        }
      ]
    },
    {
      "matcher": "Write(*.cs)",
      "hooks": [
        {
          "type": "command",
          "command": "dotnet format --include ${file_path}"
        }
      ]
    }
  ],
  "permissions": {
    "allow": [
      "Bash(dotnet build*)",
      "Bash(dotnet test*)",
      "Bash(dotnet format*)",
      "Bash(dotnet run*)",
      "Bash(dotnet watch*)",
      "Bash(dotnet ef migrations*)",
      "Bash(dotnet ef database update*)",
      "Bash(dotnet list package --vulnerable)",
      "Bash(dotnet add package*)",
      "Bash(dotnet remove package*)",
      "WebFetch(domain:sdk-doc-liquid.breez.technology)",
      "WebFetch(domain:docs.umbraco.com)",
      "WebFetch(domain:gitbook.com)"
    ]
  }
}
