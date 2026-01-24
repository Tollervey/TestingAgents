# Research: BreezSDK Expert Agents

**Feature**: 001-breezsdk-agents
**Date**: 2026-01-24
**Sources**: Official BreezSDK Liquid Documentation (sdk-doc-liquid.breez.technology)

## Research Questions & Findings

### Q1: What is the BreezSDK Liquid package and version for C#?

**Decision**: Use `Breez.Sdk.Liquid` NuGet package
**Rationale**: This is the official C# binding published on NuGet.org
**Alternatives Considered**: None - this is the only official package

**Installation**:
```bash
dotnet add package Breez.Sdk.Liquid
```

---

### Q2: What are the core SDK initialization patterns?

**Decision**: Use `BreezSdkLiquidMethods` static class for configuration and connection

**Pattern**:
```csharp
// Configuration
var config = BreezSdkLiquidMethods.DefaultConfig(
    LiquidNetwork.Mainnet,  // or LiquidNetwork.Testnet
    "<your-Breez-API-key>"
) with {
    workingDir = "path/to/existing/directory"
};

// Connection
var connectRequest = new ConnectRequest(config, mnemonic);
var sdk = BreezSdkLiquidMethods.Connect(connectRequest);

// Disconnection (for cleanup)
sdk.Disconnect();
```

**Rationale**: This follows the SDK's documented initialization flow
**Alternatives Considered**: External signer connection via `ConnectWithSigner()` for advanced use cases

---

### Q3: What payment patterns does the SDK use?

**Decision**: Two-step "prepare-then-execute" pattern for all payment operations

**Rationale**: This pattern allows fee preview before committing to transactions, enabling proper UX transparency

**Patterns Identified**:

| Operation | Prepare Method | Execute Method |
|-----------|---------------|----------------|
| Receive Lightning | `PrepareReceivePayment()` | `ReceivePayment()` |
| Send Lightning | `PrepareSendPayment()` | `SendPayment()` |
| On-chain Payment | `PreparePayOnchain()` | `PayOnchain()` |
| LNURL-Pay | `PrepareLnurlPay()` | `LnurlPay()` |

**Important**: Always call `FetchLightningLimits()` or `FetchOnchainLimits()` before payment operations to display limits to users.

---

### Q4: What event handling patterns are required?

**Decision**: Implement `EventListener` interface with proper cleanup via listener ID

**Pattern**:
```csharp
public class SdkListener : EventListener
{
    public void OnEvent(SdkEvent e)
    {
        Console.WriteLine($"Received event {e}");
    }
}

// Add listener
var listenerId = sdk.AddEventListener(listener);

// Remove listener (cleanup)
sdk.RemoveEventListener(listenerId);
```

**Rationale**: Event listeners must be removed to prevent memory leaks and ensure proper resource cleanup
**Alternatives Considered**: None - this is the required pattern

---

### Q5: What logging implementation is required?

**Decision**: Implement `Logger` interface and call `SetLogger()` before connecting

**Pattern**:
```csharp
public class SdkLogger : Logger
{
    public void Log(LogEntry l)
    {
        Console.WriteLine($"[{l.level}]: {l.line}");
    }
}

BreezSdkLiquidMethods.SetLogger(new SdkLogger());
```

**Rationale**: Logging is required for production troubleshooting per SDK documentation
**Note**: DEBUG level logging is recommended for production support

---

### Q6: What payment methods are supported?

**Decision**: Support all SDK payment methods in knowledge file

| Payment Method | Receive | Send | Notes |
|----------------|---------|------|-------|
| BOLT11 Invoice | ✅ | ✅ | Lightning Network |
| BOLT12 Offer | ✅ | ✅ | Reusable, offline capable |
| Bitcoin Address | ✅ | ✅ | On-chain |
| Liquid Address | ✅ | ✅ | Direct L-BTC |
| LNURL-Pay | - | ✅ | Lightning addresses |
| LNURL-Withdraw | ✅ | - | Withdrawal links |
| LNURL-Auth | - | - | Authentication only |
| BIP353 | ✅ | ✅ | Human-readable addresses |

**Rationale**: Complete coverage enables agents to assist with any payment scenario

---

### Q7: What multi-asset support exists?

**Decision**: Document BTC and USDt as default supported assets, plus custom asset configuration

**Supported Assets (Default)**:
| Name | Ticker | Asset ID | Precision |
|------|--------|----------|-----------|
| Bitcoin | BTC | `6f0279e9ed041c3d710a9f57d0c02928416460c4b722ae3457a11eec381c526d` | 8 |
| Tether USD | USDt | `ce091c998b83c78bb71a632313ba3760f1763d9cfcffae02258ffa9865a37bd2` | 8 |

**Custom Asset Configuration**:
```csharp
var config = BreezSdkLiquidMethods.DefaultConfig(...) with {
    assetMetadata = new List<AssetMetadata> {
        new(
            assetId: "...",
            name: "PEGx EUR",
            ticker: "EURx",
            precision: 8,
            fiatId: "EUR"
        )
    }
};
```

**Rationale**: Multi-asset support is a key SDK capability that must be documented

---

### Q8: What are the UX guidelines?

**Decision**: Document all four UX guideline areas from official documentation

**Core Principles**:
1. **Simplicity over choice** - Don't make users pick protocols/rails
2. **Transparency without jargon** - Show fees/limits in plain language
3. **Progressive disclosure** - Advanced details tucked away
4. **Lightning priority** - Present Lightning as primary option

**Receiving Payments UX**:
- Show LNURL-Pay QR code by default
- Provide human-readable Lightning address
- Primary actions: Copy (address) and Share (LNURL string)
- Display limits and fees before payment

**Sending Payments UX**:
- Unified entry point for all payment types
- Support paste/scan/upload input methods
- Display fees and limits before confirmation
- Offer "Use all funds" option

**Payment Display UX**:
- Separate fees visually from amounts
- Use Lightning addresses as titles over invoice descriptions
- Show pending/succeeded/failed status clearly
- Progressive disclosure for technical details

**Seed Management UX**:
- Defer backup until after first received payment
- Verify backup via partial re-entry
- Consider encrypted cloud backup options

**Rationale**: UX compliance is a key acceptance criterion per spec

---

### Q9: What are production readiness requirements?

**Decision**: Document four critical production requirements

1. **Logging**: DEBUG level minimum for troubleshooting
2. **Payment Status**: Monitor status fields to distinguish completed vs pending
3. **Swap Refunds**: Handle `Refundable` state, enable retry with adjustable fees
4. **Fee Transparency**: Display fees prominently alongside amounts

**Rationale**: These are explicitly required per SDK production documentation

---

### Q10: What existing agent format should be followed?

**Decision**: Follow exact format from existing agents

**Required Sections**:
```markdown
---
name: {agent-name}
description: {one-line description with invocation trigger}
tools: {comma-separated list}
model: {opus|sonnet|haiku}
---

{Role introduction paragraph}

## Your Expertise
- {bulleted list of 5-7 competencies}

## When Invoked
1. **{Phase}**
   - Step
   - Step

## {Agent-specific sections}

## Output Format
{Expected output structure}

## Constitutional Compliance (if applicable)
- **Article X**: {How enforced}
```

**Model Assignment**:
- `opus`: Complex reasoning (architect)
- `sonnet`: Implementation (developer, test-engineer, ux)
- `haiku`: Fast reviews (reviewer)

**Tool Permissions**:
- Read-only: `Read, Grep, Glob`
- Write-capable: `Read, Write, Edit, Bash, Glob, Grep`

**Rationale**: Consistency with existing agents ensures predictable behavior

---

## Unresolved Questions

None - all technical questions resolved through documentation review.

---

## SDK Documentation Coverage

| Category | Pages Reviewed | Coverage |
|----------|---------------|----------|
| API Overview | 32 pages | Complete |
| Notifications | 16 pages | Reviewed (mobile-focused) |
| UX Guidelines | 5 pages | Complete |

**Key Documentation URLs Reviewed**:
- About, Getting Started, Installation
- Connection, Wallet State, Events, Logging, Configuration
- Payments, Parse, Receive, Send, List, Refund
- On-chain, LNURL (Pay, Withdraw, Auth)
- Fiat, Assets, Production, Fees
- UX Guidelines (Overview, Receive, Send, Display, Seed)

---

## Research Status: COMPLETE

All NEEDS CLARIFICATION items resolved. Ready for implementation planning.
