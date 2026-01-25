# Research: Umbraco MCP Integration

**Feature**: 002-umbraco-mcp-integration
**Date**: 2026-01-25
**Status**: Complete

## Research Questions

### 1. GitBook MCP Server Integration

**Question**: How does the GitBook MCP server work and what tools does it provide?

**Decision**: Use the standard GitBook MCP protocol with four primary tools.

**Rationale**: GitBook provides a standardized MCP server for all published documentation sites. The Umbraco documentation is hosted on GitBook and therefore follows this protocol.

**Findings**:
- **Endpoint**: `https://docs.umbraco.com/~gitbook/mcp`
- **Protocol**: Model Context Protocol (MCP) - JSON-RPC based
- **Authentication**: None required for public documentation

**Available MCP Tools**:

| Tool | Purpose | Parameters |
|------|---------|------------|
| `search_content` | Search documentation content | `query: string` |
| `get_page_content` | Retrieve page by ID | `pageId: string` |
| `get_page_by_path` | Retrieve page by URL path | `path: string` |
| `get_space_content` | Get space/section overview | `spaceId?: string` |

**Alternatives Considered**:
- Static knowledge skill: Rejected - MCP provides live, authoritative docs
- WebFetch only: Rejected - MCP is more efficient for structured queries

---

### 2. Umbraco v17 Backend Stack

**Question**: What are the key patterns and APIs for Umbraco v17 backend development?

**Decision**: Focus agents on Composers, Notification Handlers, and Service APIs.

**Rationale**: These are the primary extension points in Umbraco v17 LTS.

**Findings**:
- **Framework**: .NET 10 LTS
- **Dependency Injection**: Via Composers (IComposer, IUserComposer)
- **Events**: Notification Handlers replace legacy events
- **Core Services**:
  - IContentService - Content operations
  - IMediaService - Media operations
  - IMemberService - Member management
  - IContentTypeService - Document type operations

**Key Documentation Paths**:
- `/umbraco-cms/reference/extending/extending-overview`
- `/umbraco-cms/reference/notifications`
- `/umbraco-cms/reference/management/services`

**Alternatives Considered**:
- Legacy events: Deprecated, use Notification Handlers
- Custom middleware: For HTTP concerns, not CMS events

---

### 3. Umbraco v17 Frontend Stack (Bellissima Backoffice)

**Question**: What technologies and patterns are used for Umbraco backoffice extensions?

**Decision**: Agents target Lit, TypeScript, Vite, and UUI component library.

**Rationale**: Bellissima (new backoffice) replaces AngularJS with modern web components.

**Findings**:
- **UI Framework**: Lit (Web Components)
- **Language**: TypeScript
- **Build Tool**: Vite
- **Component Library**: Umbraco UI Library (UUI) - https://uui.umbraco.com
- **State Management**: RxJS for reactivity
- **Backend Communication**: Management API

**Extension Types**:
| Type | Purpose | Entry Point |
|------|---------|-------------|
| Dashboards | Custom admin panels | `kind: "dashboard"` manifest |
| Property Editors | Custom field types | `kind: "propertyEditorUi"` manifest |
| Workspaces | Full-page experiences | `kind: "workspace"` manifest |
| Section Views | Navigation sections | `kind: "section"` manifest |
| Header Apps | Top-bar widgets | `kind: "headerApp"` manifest |

**Key Documentation Paths**:
- `/umbraco-cms/extending/backoffice-setup`
- `/umbraco-cms/extending/extension-types`
- `/umbraco-cms/extending/ui-documentation`

**Alternatives Considered**:
- AngularJS extensions: Legacy, not supported in v17+
- React/Vue: Supported but Lit is the primary pattern

---

### 4. Existing Agent Patterns

**Question**: How are agents structured in the existing BreezSDK integration?

**Decision**: Follow the exact same frontmatter, section structure, and Constitutional Compliance format.

**Rationale**: Consistency enables predictable agent behavior and easier maintenance.

**Findings - Agent Structure**:
```markdown
---
name: agent-name
description: One-line description for invocation guidance
tools: Tool1, Tool2, Tool3
model: sonnet|opus|haiku
---

You are [expertise statement]

## Your Expertise
- Bullet list of specific skills

## When Invoked
1. Numbered steps the agent follows

## [Domain-Specific Sections]
- Code examples (minimal: 1-2 per topic, ~5-10 lines each)
- Reference tables
- Patterns

## Output Format
How the agent structures its responses

## [Domain] Compliance
Checklist of patterns the agent enforces

## Constitutional Compliance
This agent enforces and validates:

- **Article X: Title**
  - X.N Principle: How applied
```

**Alternatives Considered**:
- New structure: Rejected - consistency with existing agents is critical

---

### 5. Fallback Strategy

**Question**: What happens when the Umbraco MCP server is unavailable?

**Decision**: Silent fallback to WebFetch for docs.umbraco.com.

**Rationale**: Per spec clarification, agents should not retry or notify users of MCP unavailability.

**Findings**:
- MCP unavailability is transient (server restarts, network issues)
- WebFetch provides equivalent content, just less structured
- User experience should be seamless

**Implementation**:
1. Agent attempts MCP query
2. If MCP fails, use `WebFetch(domain:docs.umbraco.com)`
3. No user notification needed
4. settings.local.json already has WebFetch permission for docs.umbraco.com

---

## Key Decisions Summary

| Decision Area | Choice | Confidence |
|---------------|--------|------------|
| MCP Tools | Use all 4 GitBook tools | High |
| Agent Count | 5 agents (matches BreezSDK pattern) | High |
| Model Assignment | Architect=Opus, Devs=Sonnet, Reviewers=Haiku | High |
| Tool Permissions | Read-only for architects/reviewers, write for developers | High |
| Fallback | Silent WebFetch to docs.umbraco.com | High |
| Code Examples | Minimal (1-2 per topic, ~5-10 lines) | High |
| Constitutional Compliance | Include in all agents | High |

## References

- [Umbraco 17 LTS Release](https://umbraco.com/blog/umbraco-17-lts-release/)
- [Umbraco 17 Bellissima Backoffice](https://www.arroact.com/blogs/umbraco-17-bellissima-backoffice-what-new-compared-to-umbraco-13/)
- [GitBook MCP Servers](https://gitbook.com/docs/publishing-documentation/mcp-servers-for-published-docs)
- [Umbraco Documentation](https://docs.umbraco.com)
- [Umbraco UI Library](https://uui.umbraco.com)
