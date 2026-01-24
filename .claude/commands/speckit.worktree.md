---
description: Manage git worktrees for parallel development of multiple features or branches without context switching.
handoffs:
  - label: Return to Implementation
    agent: speckit.implement
    prompt: Continue implementation in the main worktree
    send: true
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Purpose

Git worktrees enable parallel development by allowing multiple branches to be checked out simultaneously in separate directories. This is particularly useful when:

- Working on a feature while needing to fix a bug on another branch
- Running long tests on one branch while developing on another
- Reviewing PRs without losing current work context
- Enabling parallel sub-agent work on independent features

## Execution Steps

### 1. Parse User Intent

Determine the operation from `$ARGUMENTS`:

| Intent Keywords | Operation |
|----------------|-----------|
| `list`, `show`, `status` | List existing worktrees |
| `add`, `create`, `new` | Create new worktree |
| `remove`, `delete`, `rm` | Remove worktree |
| `switch`, `go`, `cd` | Switch to worktree |
| (empty or `help`) | Show usage help |

### 2. List Worktrees

```bash
git worktree list
```

Output format:
```
| Path | Branch | Status |
|------|--------|--------|
| /main/repo | main | (current) |
| /main/repo-feature-auth | feature/auth | clean |
| /main/repo-bugfix-123 | bugfix/123 | modified |
```

### 3. Create Worktree

**Syntax**: `/speckit.worktree add <branch-name> [base-branch]`

```bash
# Create worktree for existing branch
git worktree add ../$(basename $(pwd))-<branch-name> <branch-name>

# Create worktree with new branch from base
git worktree add -b <new-branch> ../$(basename $(pwd))-<new-branch> <base-branch>
```

**Naming Convention**:
- Worktree directory: `<repo-name>-<branch-short-name>`
- Example: `TestingAgents-feature-auth`

**Post-Creation Steps**:
1. Copy `.env` or local configuration if needed
2. Run `dotnet restore` or `npm install`
3. Report worktree path to user

### 4. Remove Worktree

**Syntax**: `/speckit.worktree remove <branch-name>`

```bash
# Remove worktree (keeps branch)
git worktree remove ../$(basename $(pwd))-<branch-name>

# Force remove if dirty
git worktree remove --force ../$(basename $(pwd))-<branch-name>
```

**Pre-Removal Checks**:
- Warn if worktree has uncommitted changes
- Confirm with user before force removal

### 5. Switch Context

**Syntax**: `/speckit.worktree switch <branch-name>`

Report the worktree path for the user to navigate:
```
Worktree for '<branch-name>' is at: <path>
To work in this worktree, open a new terminal in: <path>
```

## Integration with Spec-Kit Workflow

### Parallel Feature Development

```
# Main worktree: Working on user-auth feature
/speckit.implement  # Continuing user-auth

# Need to fix critical bug without losing context
/speckit.worktree add bugfix/payment-timeout main

# In new terminal at worktree path:
# Fix bug, commit, push, create PR

# Return to main worktree and continue
/speckit.worktree switch user-auth
```

### Parallel Agent Execution

When using parallel sub-agents on independent features:

```
# Create worktrees for parallel work
/speckit.worktree add feature/orders main
/speckit.worktree add feature/inventory main

# In main orchestrator:
& Use backend-developer in worktree feature/orders to implement OrderService
& Use backend-developer in worktree feature/inventory to implement InventoryService
```

### Code Review Without Context Loss

```
# Currently implementing feature
/speckit.implement  # Wave 2 in progress

# Need to review colleague's PR
/speckit.worktree add pr-review/colleague-feature origin/colleague-feature

# Review in separate worktree, then return
/speckit.worktree switch main
```

## Best Practices

### Worktree Hygiene

1. **Clean up completed worktrees**: Remove worktrees after merging branches
2. **Don't share node_modules/bin**: Each worktree needs its own dependencies
3. **Use absolute paths**: When referencing files across worktrees

### Avoid Common Pitfalls

| Problem | Solution |
|---------|----------|
| "fatal: is already checked out" | Branch already in another worktree - use that one |
| Missing dependencies | Run `dotnet restore` / `npm install` in new worktree |
| Configuration mismatch | Copy `.env.local` or user secrets to new worktree |
| Detached HEAD | Specify branch name explicitly when creating |

### Constitution Compliance

- **Article III (Testing)**: Each worktree maintains independent test state
- **Article XII (Workflow)**: Worktrees enable parallel development without branch switching overhead

## External Plugin Integration

The `superpowers` plugin provides enhanced worktree management:

```
# Using superpowers for worktree operations
Use superpowers to create a worktree for feature/payment-integration

# Superpowers can also manage worktree-specific context
Use superpowers to set up the payment-integration worktree with proper dependencies
```

See `.claude/skills/external-plugins.md` for more details on superpowers integration.

## Output Format

After any worktree operation, report:

```markdown
## Worktree Operation: [CREATE|REMOVE|LIST]

**Status**: Success/Failed
**Path**: [worktree path]
**Branch**: [branch name]

### Next Steps
- [Action items based on operation]
```
