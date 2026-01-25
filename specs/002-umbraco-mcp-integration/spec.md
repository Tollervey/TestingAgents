# Feature Specification: Umbraco MCP Integration for Spec-Kit Workflow

**Feature Branch**: `002-umbraco-mcp-integration`
**Created**: 2026-01-25
**Status**: Draft
**Input**: User description: "Integrate Umbraco MCP documentation support into spec-kit workflow with specialized agents"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - MCP Integration Guide (Priority: P1)

When Umbraco agents need to look up documentation, they should use the Umbraco MCP Documentation server (GitBook-based) as their primary knowledge source. A lightweight integration guide tells agents how to effectively query the MCP for Umbraco v17 documentation.

**Why this priority**: The MCP provides live, authoritative Umbraco documentation. Agents need to know how to use it effectively - what tools are available, effective search strategies, and key documentation paths.

**Independent Test**: Can be fully tested by verifying the integration guide documents the MCP endpoint, available tools (search_content, get_page_content, get_page_by_path), and recommended search strategies for common Umbraco topics.

**Acceptance Scenarios**:

1. **Given** an agent needs to find Umbraco API documentation, **When** they consult the integration guide, **Then** they find the MCP endpoint URL and available query tools
2. **Given** an agent needs to search for a specific Umbraco pattern, **When** they consult the integration guide, **Then** they find recommended search terms and documentation paths
3. **Given** an agent needs to understand Umbraco v17 concepts, **When** they query the MCP using the guide's recommendations, **Then** they retrieve relevant, current documentation

---

### User Story 2 - Specialized Agents for Umbraco Development (Priority: P1)

When the spec-kit workflow identifies Umbraco-specific tasks, specialized agents should be available to handle architecture decisions, backend implementation, frontend/backoffice development, and code review with Umbraco domain expertise. These agents use the Umbraco MCP as their primary documentation source.

**Why this priority**: Without specialized agents, Umbraco development tasks would use generic .NET agents that lack CMS-specific knowledge. Separating backend and frontend ensures deep expertise in both .NET/C# patterns AND modern Lit/TypeScript backoffice development.

**Independent Test**: Can be fully tested by verifying each agent file exists with appropriate configuration (model, tools, expertise areas) and that each agent is configured to use the Umbraco MCP for documentation lookup.

**Acceptance Scenarios**:

1. **Given** an Umbraco architecture decision is needed during `/speckit.plan`, **When** the workflow invokes the Umbraco architect agent, **Then** the agent queries the MCP and provides Umbraco-specific architectural guidance
2. **Given** an Umbraco backend feature needs implementation (Composers, Services, Controllers), **When** the workflow invokes the Umbraco backend developer agent, **Then** the agent queries the MCP and writes C# code following Umbraco v17 patterns
3. **Given** an Umbraco backoffice extension needs implementation (dashboards, property editors, workspaces), **When** the workflow invokes the Umbraco frontend developer agent, **Then** the agent queries the MCP and writes Lit/TypeScript code following Bellissima patterns
4. **Given** Umbraco backend code needs review (C#, Composers, Services), **When** the workflow invokes the Umbraco backend reviewer agent, **Then** the agent queries the MCP and validates code against Umbraco C# best practices
5. **Given** Umbraco frontend code needs review (Lit, TypeScript, UUI), **When** the workflow invokes the Umbraco frontend reviewer agent, **Then** the agent queries the MCP and validates code against Bellissima patterns and accessibility standards

---

### User Story 3 - CLAUDE.md Integration (Priority: P2)

When developers or Claude Code consult CLAUDE.md for available agents and workflow guidance, Umbraco agents should be listed with clear descriptions of when to use them and what capabilities they provide.

**Why this priority**: This ensures discoverability. Without CLAUDE.md updates, developers won't know Umbraco agents exist and won't invoke them appropriately during spec-kit phases.

**Independent Test**: Can be fully tested by verifying CLAUDE.md contains Umbraco agents in all three tables (Available Agents, Agent Tools & Permissions, Agent & Plugin Utilization by Phase) with accurate information.

**Acceptance Scenarios**:

1. **Given** a developer reads the Available Agents table in CLAUDE.md, **When** they look for CMS-related agents, **Then** they find all five Umbraco agents listed with clear descriptions
2. **Given** Claude Code is determining which agent to use for a task, **When** the task involves Umbraco backend development, **Then** CLAUDE.md guides it to the backend developer agent
3. **Given** Claude Code is determining which agent to use for a task, **When** the task involves Umbraco backoffice UI development, **Then** CLAUDE.md guides it to the frontend developer agent

---

### User Story 4 - External Plugins Documentation (Priority: P2)

When developers need to understand how Umbraco MCP integrates with the spec-kit workflow, they should find documentation in the external plugins guide explaining capabilities, MCP tools, and invocation patterns.

**Why this priority**: External plugins guide is the reference for all integrations. Including Umbraco MCP ensures consistent documentation patterns and helps developers understand how to leverage the MCP during development.

**Independent Test**: Can be fully tested by verifying external-plugins.md contains an Umbraco MCP section with capabilities, MCP tools documentation, spec-kit phase mapping, and integration examples.

**Acceptance Scenarios**:

1. **Given** a developer wants to use Umbraco MCP during planning, **When** they consult external-plugins.md, **Then** they find Umbraco MCP documented with available tools (search_content, get_page_content, etc.)
2. **Given** a developer needs to understand MCP integration, **When** they consult external-plugins.md, **Then** they find the MCP endpoint and usage examples

---

### User Story 5 - WebFetch Permission for Umbraco Documentation (Priority: P3)

When Claude Code needs to fetch Umbraco documentation directly from docs.umbraco.com (as a fallback or supplement to MCP), the permission should be pre-configured to allow seamless documentation access.

**Why this priority**: This is a configuration convenience providing fallback access if MCP queries need supplementation with direct page fetches.

**Independent Test**: Can be fully tested by verifying settings.local.json contains the WebFetch permission for docs.umbraco.com domain.

**Acceptance Scenarios**:

1. **Given** Claude Code needs to fetch Umbraco documentation directly, **When** it attempts WebFetch for docs.umbraco.com, **Then** the request proceeds without user permission prompts

---

### Edge Cases

- How does the system handle tasks that involve both Umbraco CMS and the existing BreezSDK integration? Agents should be composable; both can be invoked on the same project.
- What happens when the Umbraco MCP server is not running? Agents should fall back to WebFetch for docs.umbraco.com.
- What if MCP search returns no results? Agents should try alternative search terms or browse documentation structure.
- How does the frontend agent handle projects not using Lit? The agent should note that plain JavaScript or other frameworks (React, Vue) are also supported for backoffice extensions.

## Requirements *(mandatory)*

### Functional Requirements

#### MCP Integration
- **FR-001**: System MUST update external-plugins.md to document Umbraco MCP integration including endpoint URL, available tools, and usage examples
- **FR-002**: MCP integration guide MUST document the GitBook MCP tools: search_content, get_page_content, get_page_by_path, get_space_content
- **FR-003**: System MUST update settings.local.json to include WebFetch permission for docs.umbraco.com domain

#### Agent Definitions
- **FR-004**: System MUST provide an `umbraco-architect` agent for architecture decisions with read-only tools (Read, Glob, Grep) and Opus model
- **FR-005**: System MUST provide an `umbraco-backend-developer` agent for C# implementation with write tools (Read, Write, Edit, Bash, Glob, Grep) and Sonnet model
- **FR-006**: System MUST provide an `umbraco-frontend-developer` agent for Backoffice UI/UX implementation with write tools (Read, Write, Edit, Bash, Glob, Grep) and Sonnet model
- **FR-007**: System MUST provide an `umbraco-backend-reviewer` agent for C# code review with read-only tools (Read, Glob, Grep) and Haiku model
- **FR-008**: System MUST provide an `umbraco-frontend-reviewer` agent for Lit/TypeScript code review with read-only tools (Read, Glob, Grep) and Haiku model
- **FR-009**: All agents MUST be configured to use the Umbraco MCP as their primary documentation source
- **FR-010**: All agents MUST include Constitutional Compliance sections referencing relevant articles

#### Agent Expertise Areas
- **FR-011**: Backend developer agent MUST have expertise in: Composers, Notification Handlers, Content/Media/Member Services, Controllers, Dependency Injection patterns
- **FR-012**: Frontend developer agent MUST have expertise in: Lit Web Components, TypeScript, Vite, Umbraco UI Library (UUI), RxJS, Management API, Backoffice extension patterns (dashboards, property editors, workspaces, section views)
- **FR-013**: Frontend developer agent MUST include accessibility guidance (WCAG 2.1 AA) for backoffice extensions
- **FR-014**: Architect agent MUST have expertise in: Document Type design, Composition patterns, Package architecture, Multi-site strategies
- **FR-015**: Backend reviewer agent MUST have expertise in: C# code quality, Composer patterns, Service layer design, Notification Handler best practices, Clean Architecture compliance
- **FR-016**: Frontend reviewer agent MUST have expertise in: Lit/Web Component patterns, TypeScript best practices, UUI usage, accessibility compliance (WCAG 2.1 AA), Management API integration patterns

#### CLAUDE.md Updates
- **FR-017**: System MUST update CLAUDE.md to include all Umbraco agents in the Available Agents table
- **FR-018**: System MUST update CLAUDE.md to include all Umbraco agents in the Agent Tools & Permissions table
- **FR-019**: System MUST update CLAUDE.md to include Umbraco agents in the Agent & Plugin Utilization by Phase table with correct phase mappings

### Key Entities

- **MCP Integration Guide**: A section in external-plugins.md documenting how to use the Umbraco MCP. Key attributes: endpoint URL, available tools, search strategies, fallback options, spec-kit phase mapping.
- **Agent Definition**: A markdown file defining a specialized sub-agent. Key attributes: name, description, tools, model, expertise areas, MCP usage guidance, code examples, Constitutional Compliance section.
- **Backoffice Extension**: UI components for the Umbraco backoffice built with Lit/TypeScript using the Bellissima architecture. Types include: dashboards, property editors, workspaces, section views, header apps.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 5 agent definition files exist and pass structural validation
- **SC-002**: CLAUDE.md contains Umbraco entries in all 3 required tables (Available Agents, Tools & Permissions, Phase Utilization)
- **SC-003**: External-plugins.md contains Umbraco MCP section with endpoint URL, tools documentation, and usage examples
- **SC-004**: Settings.local.json contains WebFetch permission for docs.umbraco.com
- **SC-005**: Each agent definition includes guidance on using the Umbraco MCP for documentation lookup
- **SC-006**: Agent tool assignments match the established pattern (architect=read-only/Opus, developers=write/Sonnet, reviewers=read-only/Haiku)
- **SC-007**: Backend developer agent includes code examples for Composers, Services, and Notification Handlers
- **SC-008**: Frontend developer agent includes code examples for Lit components, UUI usage, and Management API integration
- **SC-009**: Backend reviewer agent includes review criteria for C# patterns and Umbraco service usage
- **SC-010**: Frontend reviewer agent includes review criteria for Lit/TypeScript patterns and accessibility compliance
- **SC-011**: All agents include Constitutional Compliance sections matching the format used by existing agents

## Assumptions

- Umbraco v17 LTS is the target version (built on .NET 10 LTS, released November 2025)
- The Umbraco MCP Documentation server is available at `https://docs.umbraco.com/~gitbook/mcp` (GitBook-hosted)
- The Umbraco MCP for VS Code has been installed and configured separately by the user
- Network access is available for MCP queries and WebFetch fallback
- The MCP provides live, authoritative documentation - no static knowledge skill is needed
- The project follows the existing agent patterns established by BreezSDK integration
- Backoffice extensions use the Bellissima architecture (Lit, TypeScript, Vite, UUI)
- The Umbraco UI Library documentation is available at uui.umbraco.com

## Technology Reference

### Umbraco v17 Backend Stack
- .NET 10 LTS
- C# with modern language features
- Dependency Injection via Composers
- Notification Handlers (replacing legacy events)
- Content Service, Media Service, Member Service APIs

### Umbraco v17 Frontend Stack (Bellissima Backoffice)
- Lit (Web Components library)
- TypeScript
- Vite (build tool)
- Umbraco UI Library (UUI) - uui.umbraco.com
- RxJS (reactivity)
- Management API (backend communication)
- Extension types: Dashboards, Property Editors, Workspaces, Section Views, Header Apps

### Sources
- [Umbraco 17 LTS Release](https://umbraco.com/blog/umbraco-17-lts-release/)
- [Umbraco 17 Bellissima Backoffice](https://www.arroact.com/blogs/umbraco-17-bellissima-backoffice-what-new-compared-to-umbraco-13/)
- [GitBook MCP Servers](https://gitbook.com/docs/publishing-documentation/mcp-servers-for-published-docs)
