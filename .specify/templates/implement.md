---
name: implement
description: Execute the implementation plan using Claude Code multi-agent workflows
---

# Implement Command - Full Claude Code Execution Strategy

## Prerequisites
- Read the task breakdown from `/specs/[current-feature]/tasks.md`
- Read the implementation plan from `/specs/[current-feature]/plan.md`
- Read the constitution from `/memory/constitution.md`
- Load the execution skill: `.claude/skills/dotnet-implementation-execution.md`
- Verify all agents are available: `/agents`

## Execution Strategy

### Phase 0: Pre-Implementation Setup

1. **Create Checkpoint**
   ```
   Create a checkpoint before starting implementation
   ```

2. **Initialize Progress Tracking**
   ```
   Initialize TodoWrite with all tasks from tasks.md
   ```

3. **Verify Prerequisites**
   - All dependencies installed
   - Database accessible
   - Build succeeds with empty project

### Phase 1: Wave-Based Execution

Execute tasks wave by wave, respecting the structure in tasks.md:

#### For Sequential Waves (Wave 1: Infrastructure)
```
Execute tasks in order:
1. Start T001, wait for completion
2. Verify T001, create checkpoint
3. Start T002, wait for completion
4. [Continue sequentially]
```

#### For Parallel Waves (Wave 2+)
```
# Spawn parallel sub-agents for [P] marked tasks
& Use backend-developer to implement T004 (User entity)
& Use backend-developer to implement T005 (Product entity)
& Use backend-developer to implement T006 (Order entity)

# Monitor progress
/tasks

# Wait for all to complete before next wave
```

### Phase 2: Agent Selection Rules

Based on task metadata:

| model_tier | Agent | Use For |
|------------|-------|---------|
| opus | solution-architect | Architectural decisions, complex refactoring |
| sonnet | backend-developer | .NET implementation |
| sonnet | frontend-developer | Blazor/UI implementation |
| sonnet | test-engineer | Test implementation (TDD) |
| sonnet | database-architect | Migrations, queries |
| haiku | security-auditor | Security scanning |
| haiku | code-reviewer | Code quality checks |

### Phase 3: Background Execution

For tasks marked `background_eligible: true`:

```
# Start long-running task
Implement [task description]

# When tests/build starts, background it
[Press Ctrl+B]

# Continue with independent work
Meanwhile, implement [another independent task]

# Check status periodically
/tasks
```

### Phase 4: Test-First Enforcement

For every implementation task:

1. **Create Tests First**
   ```
   Use test-engineer to create failing tests for [component]
   ```

2. **Verify Tests Fail**
   ```
   Run dotnet test --filter "[component]" and confirm RED state
   ```

3. **Implement**
   ```
   Use [appropriate-agent] to implement [component] until tests pass
   ```

4. **Verify Tests Pass**
   ```
   Run dotnet test --filter "[component]" and confirm GREEN state
   ```

### Phase 5: Context Management

**When to Compact**:
- After completing each wave
- When context exceeds 150k tokens
- Before starting unrelated component

**When to Delegate**:
- Task estimated >30k tokens
- Task requires specialist expertise
- Verbose output expected

**Sub-agent Return Requirements**:
- Summary of work completed (not full code)
- List of files created/modified (paths only)
- Validation status (tests pass/fail)
- Any blockers or issues

### Phase 6: Quality Gates

Hooks automatically enforce:
- Code formatting on every file save
- Dangerous command blocking
- Test verification before task completion

Manual gates at wave boundaries:
```
# After completing a wave
Use code-reviewer to verify code quality for Wave N
Use security-auditor to scan for vulnerabilities
```

### Phase 7: Checkpoint Strategy

Create checkpoints:
- Before each wave starts
- After each user story completes
- Before any destructive operation

Recovery:
```
# If implementation goes wrong
/rewind

# Select appropriate checkpoint
# Continue from known good state
```

### Phase 8: Completion Verification

After all tasks complete:

```
# Run full validation
& dotnet build --warnaserror
& dotnet test
& Use security-auditor to perform final security scan
& Use code-reviewer to verify constitution compliance

# Check all gates pass
/tasks

# Generate completion report
Summarize implementation status and any outstanding items
```

## Error Recovery

### Build Failures
1. Review error output
2. Identify failing component
3. Fix or rewind to checkpoint

### Test Failures  
1. Identify failing tests
2. Check if new or regression
3. Fix implementation or revert

### Agent Drift
1. Stop current execution
2. Re-read plan.md and tasks.md
3. Provide explicit correction

### Context Overflow
1. /compact to summarize
2. Delegate remaining work to fresh sub-agent
3. Provide summary context to sub-agent
