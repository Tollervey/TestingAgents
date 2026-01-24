---
name: dotnet-implementation-execution
description: Comprehensive Claude Code execution patterns for .NET implementation. Load this skill during /speckit.implement for multi-agent workflows, background tasks, and quality enforcement.
globs:
  - "**/*.cs"
  - "**/*.razor"
  - "**/*.csproj"
---

# .NET Implementation Execution Skill

This skill provides execution patterns for implementing .NET projects using Claude Code's multi-agent capabilities.

## Execution Patterns

### Pattern 1: Wave-Based Parallel Implementation

Use when implementing multiple independent components:

```
# Wave 1: Infrastructure (sequential)
Implement the solution scaffolding and shared infrastructure

# Create checkpoint
Checkpoint created before Wave 2

# Wave 2: Domain Layer (parallel)
& Use backend-developer to implement the User aggregate with entity, value objects, and domain events
& Use backend-developer to implement the Product aggregate with entity, value objects, and domain events  
& Use backend-developer to implement the Order aggregate with entity, value objects, and domain events

# Monitor all parallel tasks
/tasks

# Wait for completion, then proceed
# Wave 3: Application Layer (parallel, after Wave 2)
& Use backend-developer to implement UserService with all use cases
& Use backend-developer to implement ProductService with all use cases
& Use backend-developer to implement OrderService with all use cases
```

### Pattern 2: Test-First Implementation Loop

Use for every component implementation:

```
# Step 1: Create failing tests
Use test-engineer to create comprehensive unit tests for OrderService including:
- Happy path scenarios
- Edge cases
- Error conditions
- Null handling

# Step 2: Verify RED state
Run: dotnet test --filter "OrderServiceTests"
Expected: Tests should FAIL (no implementation yet)

# Step 3: Implement to GREEN
Use backend-developer to implement OrderService until all tests pass

# Step 4: Verify GREEN state  
Run: dotnet test --filter "OrderServiceTests"
Expected: All tests PASS

# Step 5: Refactor (optional)
Refactor OrderService for clarity while keeping tests green
```

### Pattern 3: Background Build & Test Cycle

Use for long-running operations:

```
# Start implementation
Implement the PaymentProcessingService following the specification in plan.md

# When tests start running (takes >2 minutes)
[Press Ctrl+B to background]

# Continue with independent work
While payment tests run, implement NotificationService which has no dependencies on payment

# Check background status
/tasks

# When background completes, retrieve results
Show me the results from the PaymentProcessingService implementation
```

### Pattern 4: Multi-Agent Feature Implementation

Use for complete feature implementation:

```
# Phase 1: Architecture Review (Opus - complex reasoning)
Use solution-architect to review the checkout flow design and identify architectural concerns before implementation

# Phase 2: Database Layer (Sonnet)
Use database-architect to create the Order and Payment migrations and repository implementations

# Phase 3: Backend Implementation (Sonnet - parallel)
& Use backend-developer to implement CheckoutController and API endpoints
& Use backend-developer to implement PaymentService with Stripe integration
& Use test-engineer to implement integration tests for checkout flow

# Phase 4: Frontend Implementation (Sonnet)
Use frontend-developer to implement the CheckoutPage Blazor component with cart summary and payment form

# Phase 5: Security Audit (Haiku - fast validation)
& Use security-auditor to scan checkout flow for PCI compliance issues
& Use security-auditor to verify no sensitive data logging

# Phase 6: Code Review (Haiku)
Use code-reviewer to verify all checkout code meets constitution standards

# Phase 7: Integration Testing
Run full checkout flow end-to-end tests and fix any integration issues
```

## Context Management

### When to Delegate to Sub-agents

Delegate when:
- Task will consume >30,000 tokens
- Task produces verbose output (test runs, builds)
- Task requires specialist expertise
- Task can run independently

### Sub-agent Instructions Template

```
Use [agent-name] to [specific task].

Requirements:
- [Specific requirement 1]
- [Specific requirement 2]

Return:
- Summary of changes (2-3 sentences)
- List of files created/modified (paths only)
- Test status (passing/failing, count)
- Any blockers or concerns
```

### When to Compact

Compact when:
- Completing a major feature or user story
- Context exceeds 150k tokens
- Switching between unrelated components
- Before starting a new wave

```
/compact
```

### When to Checkpoint

Create checkpoints:
- Before starting each implementation wave
- After completing each user story
- Before any destructive or irreversible operation
- When implementation is in a known good state

```
Create a checkpoint - [description of current state]
```

## Quality Enforcement

### Automated Hooks (Always Active)

These run automatically via .claude/hooks.json:

1. **PostToolUse: Format Check**
   - Runs `dotnet format --verify-no-changes` after .cs/.razor edits
   - Warnings surface immediately

2. **PreToolUse: Dangerous Command Block**
   - Blocks `rm -rf`, `DROP DATABASE`, `TRUNCATE TABLE`
   - Requires explicit confirmation

3. **Stop: Test Verification**
   - Checks for pending tests before completion
   - Prevents completion with failing tests

### Manual Quality Gates

Run at wave boundaries:

```
# After completing Wave N
Use code-reviewer to review all code from Wave N for:
- Constitution compliance
- Code quality standards
- Consistent patterns

Use security-auditor to scan Wave N implementations for:
- Input validation
- Authentication/authorization
- Secrets exposure
```

## Error Recovery

### Build Failure Recovery
```
1. Review the build error output
2. Identify the failing file/component
3. If recent change: fix the specific issue
4. If widespread: /rewind to last checkpoint
5. Re-implement with correction
```

### Test Failure Recovery
```
1. Identify which tests are failing
2. Determine if failure is in new or existing code
3. For new code: fix implementation
4. For existing code: check for unintended side effects
5. If unclear: /rewind and re-approach
```

### Agent Drift Recovery
```
1. Stop current execution (Escape key)
2. Re-read plan.md section for current component
3. Re-read tasks.md for current task requirements
4. Provide explicit correction with plan reference:
   "According to plan.md section X, we need to... Please correct the implementation."
5. Consider using solution-architect (Opus) for complex corrections
```

## Performance Optimization

### Minimize Token Usage

1. Reference files by path, don't paste contents
2. Request summaries from sub-agents, not full outputs
3. Use Haiku for validation tasks
4. Compact after major milestones

### Maximize Parallelization

1. Identify all [P] tasks in current wave
2. Spawn sub-agents for each independent task
3. Use background execution for long operations
4. Continue with unrelated work while waiting

### Model Selection Strategy

| Task Type | Model | Rationale |
|-----------|-------|-----------|
| Architecture decisions | Opus | Complex reasoning required |
| Standard implementation | Sonnet | Good balance of speed/quality |
| Code review | Haiku | Fast, cost-effective |
| Security scanning | Haiku | Pattern matching, fast |
| Test generation | Sonnet | Needs understanding of requirements |
| Refactoring | Sonnet | Balance of reasoning and speed |
