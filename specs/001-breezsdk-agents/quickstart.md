# Quickstart: Using BreezSDK Expert Agents

This guide explains how to use the five BreezSDK expert agents for Claude Code.

## Prerequisites

1. Claude Code CLI installed and configured
2. Project with BreezSDK integration needs
3. BreezSDK Liquid package (`Breez.Sdk.Liquid`) installed in your project

## Available Agents

| Agent | Purpose | Best For |
|-------|---------|----------|
| `breezsdk-developer` | C# implementation | Writing SDK integration code |
| `breezsdk-architect` | Architecture design | Planning integration structure |
| `breezsdk-reviewer` | Code review | Validating SDK usage patterns |
| `breezsdk-ux` | UX guidance | Payment flow design |
| `breezsdk-test-engineer` | Testing patterns | Writing integration tests |

## How to Invoke Agents

### Via Task Tool (Recommended)

```
Use the Task tool with subagent_type="breezsdk-developer" to implement...
```

### Example Prompts

**For Implementation Help**:
```
I need to implement a payment receiving flow using BreezSDK.
The user should see a Lightning invoice QR code.
Use breezsdk-developer to help implement this.
```

**For Architecture Review**:
```
I'm integrating BreezSDK into my Clean Architecture application.
Use breezsdk-architect to recommend how to structure the integration layer.
```

**For Code Review**:
```
Review my BreezSDK integration code for best practice compliance.
Use breezsdk-reviewer to check the PaymentService.cs file.
```

**For UX Guidance**:
```
I need to design the send payment screen.
Use breezsdk-ux to recommend the UX flow.
```

**For Testing Help**:
```
I need to write tests for my payment preparation logic.
Use breezsdk-test-engineer to create mock patterns and test cases.
```

## Agent Usage by Spec-Kit Phase

| Phase | Recommended Agent |
|-------|-------------------|
| `/speckit.plan` | breezsdk-architect |
| `/speckit.implement` | breezsdk-developer, breezsdk-test-engineer |
| Post-implement | breezsdk-reviewer, breezsdk-ux |

## Knowledge File Reference

All agents reference `.claude/skills/breezsdk-knowledge.md` for:
- C# code examples
- SDK patterns (two-step payment flow)
- UX guidelines
- Production requirements

## Common Tasks

### 1. Initialize BreezSDK Connection

Ask `breezsdk-developer`:
> "Help me implement the BreezSDK connection with proper configuration and error handling"

### 2. Implement Payment Receiving

Ask `breezsdk-developer`:
> "Implement a BOLT11 invoice receiving flow with fee display"

### 3. Review Integration Code

Ask `breezsdk-reviewer`:
> "Review my PaymentService.cs for BreezSDK best practices"

### 4. Design Payment UX

Ask `breezsdk-ux`:
> "What's the recommended UX for the receive payment screen?"

### 5. Write Payment Tests

Ask `breezsdk-test-engineer`:
> "Create test patterns for mocking PrepareSendPayment responses"

## Tips

1. **Always check limits first** - Before any payment operation, call `FetchLightningLimits()` or `FetchOnchainLimits()`

2. **Use two-step pattern** - Always prepare, then execute payments

3. **Clean up event listeners** - Store and use the listener ID for removal

4. **Follow UX guidelines** - Lightning first, transparent fees, progressive disclosure

5. **Log at DEBUG level** - Required for production troubleshooting

## Troubleshooting

**Agent not recognized?**
- Verify agent files exist in `.claude/agents/`
- Check CLAUDE.md includes the agent in Available Agents table

**Knowledge file not loaded?**
- Ensure `.claude/skills/breezsdk-knowledge.md` exists
- Verify globs pattern matches your file types

**Wrong model being used?**
- Check the `model` field in agent frontmatter
- Opus for architect, Sonnet for developer/ux/test, Haiku for reviewer
