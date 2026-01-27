# Feature Specification: Genericize SpecKit and Claude Code Configuration for Cross-Project Reuse

**Feature Branch**: `001-generic-speckit-reuse`
**Created**: 2026-01-27
**Status**: Draft
**Input**: User description: "Review and update all Claude/SpecKit md files, ps1 files, skills, agents etc. to be generic enough for cross-project reuse without losing effectiveness."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Copy Configuration to a New Project (Priority: P1)

A developer copies the `.claude/` and `.specify/` directories (plus `CLAUDE.md`) from this project into a brand new project with a different technology stack or business domain. The configuration files work immediately for the SpecKit workflow without requiring edits to remove references to BreezSDK, Umbraco, Lightning payments, or other domain-specific content from core files.

**Why this priority**: This is the fundamental reuse scenario. If core configuration files contain hardcoded domain references, every new project requires manual cleanup, which defeats the purpose of a reusable toolkit.

**Independent Test**: Copy `.claude/`, `.specify/`, and `CLAUDE.md` into an empty repository. Run `/speckit.specify` with a feature description. Verify no errors and no references to BreezSDK, Umbraco, or other domain-specific terms appear in the generated output.

**Acceptance Scenarios**:

1. **Given** a new empty project, **When** the developer copies the SpecKit configuration files, **Then** no file in `.claude/commands/`, `.specify/templates/`, or `.specify/scripts/` contains references to any specific business domain (BreezSDK, Umbraco, Lightning, etc.)
2. **Given** a new project using a non-.NET technology, **When** the developer reviews `CLAUDE.md`, **Then** technology-specific sections are clearly separated and documented as customizable, not embedded in core workflow logic
3. **Given** a new project, **When** the developer runs any `/speckit.*` command, **Then** the command executes without assuming any particular tech stack in its core logic

---

### User Story 2 - Retain Domain-Specific Agents as Optional Add-ons (Priority: P2)

A developer working on an Umbraco project wants to keep the Umbraco-specific agents. A developer working on a BreezSDK project wants the BreezSDK agents. These domain-specific agents exist as clearly separated, optional components that don't pollute the core configuration.

**Why this priority**: Domain-specific agents provide significant value for their respective projects. Removing them entirely would lose effectiveness. They need to be present but clearly separated from core agents.

**Independent Test**: Remove all `breezsdk-*` and `umbraco-*` agent files from `.claude/agents/`. Verify all `/speckit.*` commands still work correctly. Then add them back and verify they enhance but don't break anything.

**Acceptance Scenarios**:

1. **Given** a project that does not use BreezSDK or Umbraco, **When** the developer removes domain-specific agent files, **Then** no other configuration file breaks or references missing agents
2. **Given** the `CLAUDE.md` file, **When** the developer reads it, **Then** domain-specific agents are listed in a clearly marked optional section separate from core agents
3. **Given** the `hooks.json` file, **When** it initializes a session, **Then** it does not contain hardcoded agent names that may not exist in all projects

---

### User Story 3 - Technology-Specific Content is Modular (Priority: P2)

A developer using this toolkit on a non-.NET project can replace technology-specific content (build commands, code conventions, test patterns) with their equivalents without touching the core SpecKit workflow files.

**Why this priority**: The SpecKit workflow (specify, clarify, plan, tasks, implement) is technology-agnostic by design. Technology-specific content mixed into core files forces unnecessary edits when adopting the toolkit.

**Independent Test**: Identify all .NET-specific content in core configuration files. Verify each piece is either in a clearly marked section or in a separate file that can be swapped.

**Acceptance Scenarios**:

1. **Given** `CLAUDE.md`, **When** the developer reads it, **Then** technology-specific sections (build commands, code conventions, test patterns) are clearly marked and separable from SpecKit workflow documentation
2. **Given** the constitution file, **When** the developer reviews it, **Then** universal software principles (architecture, testing discipline, security) are separate from technology-specific rules
3. **Given** `hooks.json`, **When** file-save hooks execute, **Then** technology-specific hooks are clearly documented as customizable examples

---

### User Story 4 - Core SpecKit Workflow Remains Effective (Priority: P1)

After genericization, the SpecKit workflow (specify -> clarify -> plan -> tasks -> implement) retains all its current effectiveness. No quality gates, validation checks, agent coordination patterns, or workflow enforcement is lost.

**Why this priority**: Genericization must not reduce effectiveness. The value of the toolkit is in its structured workflow, not in domain-specific references.

**Independent Test**: Run the full SpecKit workflow on a sample feature after genericization. Verify all phases produce output of equivalent quality and structure to the current implementation.

**Acceptance Scenarios**:

1. **Given** the genericized toolkit, **When** `/speckit.implement` executes, **Then** it still enforces build verification, test verification, and regression checks
2. **Given** the genericized toolkit, **When** any agent is invoked, **Then** it still follows constitution principles, TDD discipline, and quality gates
3. **Given** the genericized `CLAUDE.md`, **When** Claude Code reads it, **Then** all wave execution strategies, context management, and checkpoint patterns are preserved

---

### Edge Cases

- What happens when a project uses multiple technology stacks (e.g., .NET backend + React frontend)? Technology-specific sections should support multi-stack projects.
- What happens when domain-specific agents reference other domain-specific agents? No cross-references should exist that would break if some domain agents are removed.
- What happens when the constitution contains both universal and technology-specific principles? The separation must be clean and both parts remain coherent independently.

## Clarifications

### Session 2026-01-27

- Q: How should domain-specific content within shared files (CLAUDE.md, settings.json, settings.local.json, hooks.json) be handled? → A: Move domain content to a separate addendum file (e.g., `CLAUDE.project.md`). Core files become clean and portable; the addendum holds project-specific additions (domain agents, MCP servers, WebFetch domains).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: All files in `.claude/commands/` MUST be free of references to specific business domains (BreezSDK, Umbraco, Lightning payments) in their core logic
- **FR-002**: `CLAUDE.md` MUST separate domain-specific agent documentation into a clearly marked optional section
- **FR-003**: `CLAUDE.md` MUST separate technology-specific content (build commands, code conventions, test patterns) into clearly marked customizable sections
- **FR-004**: `hooks.json` MUST NOT contain hardcoded agent lists; agent availability should not be assumed
- **FR-005**: `hooks.json` technology-specific hooks MUST be clearly documented as customizable examples
- **FR-006**: The constitution (`constitution.md`) MUST separate universal software principles from technology-specific rules
- **FR-007**: `settings.json` MUST NOT contain project-specific MCP server configurations; these MUST be moved to a project-specific addendum file
- **FR-008**: `settings.local.json` MUST NOT contain project-specific WebFetch domain permissions; these MUST be moved to a project-specific addendum file
- **FR-009**: Domain-specific agent files (breezsdk-*, umbraco-*) MUST be self-contained with no references from core configuration files; their documentation MUST reside in a project-specific addendum (e.g., `CLAUDE.project.md`), not in the core `CLAUDE.md`
- **FR-010**: All `.specify/templates/` files MUST remain technology-agnostic (already compliant)
- **FR-011**: All `.specify/scripts/powershell/` files MUST remain technology-agnostic (already compliant)
- **FR-012**: The `speckit.implement.md` command MUST reference only core agents, with domain-specific agents documented as optional extensions
- **FR-013**: `QUICK-REFERENCE.md` MUST separate technology-specific quick reference from SpecKit workflow reference
- **FR-014**: The `speckit.worktree.md` example MUST use a generic project name rather than a project-specific name
- **FR-015**: `external-plugins.md` skill MUST reference principles generically rather than citing specific constitution article numbers
- **FR-016**: Generic agents (`code-reviewer.md`, `test-engineer.md`) MUST use generic examples rather than domain-specific ones for illustrating universal patterns

### Key Entities

- **Core Configuration**: Files that define the SpecKit workflow and must be fully generic (commands, templates, scripts, core agents)
- **Technology Profile**: Customizable sections/files that define tech-stack-specific behavior (build commands, linting, conventions)
- **Domain Extensions**: Optional agent files and settings for specific business domains

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can copy `.claude/`, `.specify/`, and `CLAUDE.md` to a new empty repository and run `/speckit.specify` with a sample feature description — the generated output contains zero references to BreezSDK, Umbraco, Lightning, or other project-specific domains
- **SC-002**: After removing all `breezsdk-*` and `umbraco-*` files from `.claude/agents/`, running `grep -r "breezsdk\|umbraco" .claude/ .specify/ CLAUDE.md` returns zero matches
- **SC-003**: Domain-specific content in `CLAUDE.md` is confined to clearly marked sections that can be removed without breaking the document's structure
- **SC-004**: A developer can copy the toolkit to a new project and run `/speckit.specify` without encountering any project-specific references in generated output
- **SC-005**: All existing SpecKit workflow capabilities are preserved after genericization (no feature regression)
- **SC-006**: All four validation scripts (`validate-tier1.ps1`, `validate-domain-isolation.ps1`, `validate-tech-modularity.ps1`, `validate-workflow-preservation.ps1`) pass with zero violations

## Assumptions

- The primary target audience includes .NET developers, so .NET remains a useful default technology profile; but it is documented as a customizable default, not a hardcoded requirement
- Domain-specific agents (BreezSDK, Umbraco) remain in the repository as reference examples of how to create domain extensions; their configuration is documented in a project-specific addendum file (`CLAUDE.project.md`) rather than the core `CLAUDE.md`
- The PowerShell scripts and templates are already generic and require no changes
- The `speckit.implement.md` command's technology-specific build/test examples are acceptable as defaults, provided they are framed as customizable rather than mandatory
- `hooks.json` technology detection patterns are documented as customizable examples
