# Data Model: BreezSDK Expert Agents

**Feature**: 001-breezsdk-agents
**Date**: 2026-01-24

## Overview

This feature creates knowledge artifacts (not traditional data entities). The "data model" describes the structure of agent files and the knowledge reference file.

---

## Entity: Agent File

Agent files define specialized Claude Code sub-agents for BreezSDK expertise.

### Schema

```yaml
# Frontmatter (YAML)
---
name: string          # Unique identifier (kebab-case)
description: string   # One-line purpose with invocation trigger
tools: string         # Comma-separated tool list
model: string         # opus | sonnet | haiku
---

# Content (Markdown)
## Sections:
- Role Introduction     # Single paragraph establishing expertise
- Your Expertise        # Bulleted list of 5-7 competencies
- When Invoked          # Numbered workflow steps
- Code Standards        # Optional: code examples (if applicable)
- Output Format         # Expected output structure
- {Domain Sections}     # Role-specific content
- Constitutional Compliance  # Optional: article mappings
```

### Agent Instances

| Name | Tools | Model | Can Write |
|------|-------|-------|-----------|
| breezsdk-developer | Read, Write, Edit, Bash, Glob, Grep | sonnet | Yes |
| breezsdk-architect | Read, Glob, Grep | opus | No |
| breezsdk-reviewer | Read, Grep, Glob | haiku | No |
| breezsdk-ux | Read, Grep, Glob | sonnet | No |
| breezsdk-test-engineer | Read, Write, Edit, Bash, Glob, Grep | sonnet | Yes |

### Validation Rules

1. `name` must be kebab-case and unique
2. `tools` must only include valid Claude Code tools
3. `model` must be one of: opus, sonnet, haiku
4. Must include "Your Expertise" section
5. Must include "When Invoked" section
6. Must include "Output Format" section

---

## Entity: Knowledge Reference File

The knowledge reference file contains comprehensive SDK documentation for agent consultation.

### Schema

```yaml
# Frontmatter (YAML)
---
name: string          # Skill identifier
description: string   # Purpose description
globs: string[]       # File patterns for activation
---

# Content (Markdown)
## Sections:
- Quick Reference
- Connection & Configuration
- Event Handling
- Logging
- Wallet State
- Payment Operations (Receiving, Sending, On-Chain, Listing)
- Refunds & Recovery
- LNURL Operations
- Multi-Asset Support
- Fiat Currencies
- Message Signing
- UX Guidelines Summary
- Production Checklist
```

### Content Requirements

Each section must include:
1. **Concept explanation** - What the feature does
2. **C# code example** - Complete, runnable code
3. **Key points** - Important notes and gotchas

### Code Example Format

```csharp
// Purpose comment
try
{
    // Implementation
    var result = sdk.Method(new Request(...));
}
catch (Exception)
{
    // Handle error
}
```

### Validation Rules

1. Must include all SDK operations listed in spec FR-011
2. All code examples must be C# (not other language bindings)
3. Must include UX guidelines summary
4. Must include production checklist

---

## Entity: CLAUDE.md Updates

Updates to the main project configuration file.

### Available Agents Table Entry

```markdown
| Agent | Model | Invoke For |
|-------|-------|------------|
| `{agent-name}` | {Model} | {Use case description} |
```

### Agent Tools & Permissions Table Entry

```markdown
| Agent | Tools | Can Modify Files? |
|-------|-------|-------------------|
| `{agent-name}` | {tool list} | {Yes/No} |
```

### Phase Mapping Table Entry

```markdown
| Phase | Primary Agent(s) | Supporting Agent(s) |
|-------|-----------------|---------------------|
| {phase} | {agents} | {agents} |
```

---

## Relationships

```
CLAUDE.md
    │
    ├── references → .claude/agents/breezsdk-*.md (5 files)
    │
    └── references → .claude/skills/breezsdk-knowledge.md

.claude/agents/breezsdk-*.md
    │
    └── depends on → .claude/skills/breezsdk-knowledge.md
```

---

## State Transitions

### Agent Usage Lifecycle

```
Not Invoked → Invoked → Processing → Response
```

### Knowledge File Usage

```
File Loaded (on C# file access) → Referenced by Agent → Pattern Applied
```

---

## File Locations

| Entity | Path |
|--------|------|
| breezsdk-developer | `.claude/agents/breezsdk-developer.md` |
| breezsdk-architect | `.claude/agents/breezsdk-architect.md` |
| breezsdk-reviewer | `.claude/agents/breezsdk-reviewer.md` |
| breezsdk-ux | `.claude/agents/breezsdk-ux.md` |
| breezsdk-test-engineer | `.claude/agents/breezsdk-test-engineer.md` |
| Knowledge File | `.claude/skills/breezsdk-knowledge.md` |
| Configuration | `CLAUDE.md` |

---

## Success Metrics Mapping

| Metric (from spec) | Validation Method |
|-------------------|-------------------|
| SC-001: All agents created | File existence check |
| SC-002: 100% SDK coverage | Knowledge file section count |
| SC-003: CLAUDE.md updated | Table entry verification |
| SC-004: Agent invocation works | Manual Task tool test |
| SC-005: Reviewer identifies issues | Manual test with flawed code |
| SC-006: Developer produces valid code | Manual implementation test |
| SC-007: UX alignment | Guideline comparison |
