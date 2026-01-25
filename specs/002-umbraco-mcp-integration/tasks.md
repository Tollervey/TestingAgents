# Tasks: Umbraco MCP Integration for Spec-Kit Workflow

**Input**: Design documents from `/specs/002-umbraco-mcp-integration/`
**Prerequisites**: plan.md (required), spec.md (required), research.md

**Verification**: This is a documentation/configuration feature with no production code. Per plan.md Section "Technical Context", testing is manual verification of agent invocation and file structure. Verification tasks replace automated tests.

**Organization**: Tasks are grouped by user story to enable independent implementation and verification of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

This feature only modifies configuration files:
- **Agents**: `.claude/agents/`
- **Skills**: `.claude/skills/`
- **Settings**: `.claude/settings.local.json`
- **Root**: `CLAUDE.md`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Verify prerequisites and existing structure

- [X] T001 Verify `.claude/agents/` directory exists and review existing agent patterns (e.g., breezsdk-*.md)
- [X] T002 Verify `.claude/skills/external-plugins.md` exists and review existing plugin documentation patterns
- [X] T003 [P] Verify `.claude/settings.local.json` has WebFetch permission for docs.umbraco.com (plan.md notes line 72)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: No foundational blocking tasks - this feature adds new configuration without dependencies on existing code

**Note**: This phase is empty because all user stories can proceed directly after Setup verification. Agent files and documentation updates are independent additions.

**Checkpoint**: Setup complete - user story implementation can begin in parallel

---

## Phase 3: User Story 1 - MCP Integration Guide (Priority: P1) 🎯 MVP

**Goal**: Document Umbraco MCP integration including endpoint URL, available tools, and usage guidance for all agents

**Independent Test**: Verify external-plugins.md contains Umbraco MCP section with endpoint, tools documentation, and recommended search strategies

### Verification for User Story 1 (Manual Validation)

- [X] T004 [US1] Create verification checklist for MCP Integration Guide content in `.specify/checklists/us1-mcp-guide.md`

### Implementation for User Story 1

- [X] T005 [US1] Add Umbraco MCP section to `.claude/skills/external-plugins.md` with:
  - Endpoint URL: `https://docs.umbraco.com/~gitbook/mcp`
  - MCP tools table (search_content, get_page_content, get_page_by_path, get_space_content)
  - Key documentation paths from research.md
  - Fallback guidance (WebFetch to docs.umbraco.com)
  - **Prerequisite setup**: Document that Umbraco MCP for VS Code must be installed and configured
  - **No-results fallback sequence**: (1) retry with broader terms, (2) browse via get_space_content, (3) WebFetch fallback
  - Spec-Kit phase mapping
  - Agent integration guidance

**Checkpoint**: MCP Integration Guide complete - agents now have documentation on how to use Umbraco MCP

---

## Phase 4: User Story 2 - Specialized Agents for Umbraco Development (Priority: P1)

**Goal**: Create 5 specialized agents (architect, backend-developer, frontend-developer, backend-reviewer, frontend-reviewer) configured to use Umbraco MCP

**Independent Test**: Verify all 5 agent files exist with correct frontmatter (name, description, tools, model) and MCP integration section

### Verification for User Story 2 (Manual Validation)

- [X] T006 [US2] Create verification checklist for agent structure and content in `.specify/checklists/us2-agents.md`

### Implementation for User Story 2

- [X] T007 [P] [US2] Create `.claude/agents/umbraco-architect.md` with:
  - Frontmatter: name, description, tools (Read, Glob, Grep), model (opus)
  - Expertise: Document Type design, Composition patterns, Package architecture, Multi-site strategies
  - MCP Integration section with endpoint, tools, and **no-results fallback sequence**
  - Constitutional Compliance section
  - Output format guidance

- [X] T008 [P] [US2] Create `.claude/agents/umbraco-backend-developer.md` with:
  - Frontmatter: name, description, tools (Read, Write, Edit, Bash, Glob, Grep), model (sonnet)
  - Expertise: Composers, Notification Handlers, Content/Media/Member Services, Controllers, DI patterns
  - Minimal code examples (1-2 per topic, ~5-10 lines): Composers, Services, Notification Handlers
  - MCP Integration section with endpoint, tools, and **no-results fallback sequence**
  - Constitutional Compliance section

- [X] T009 [P] [US2] Create `.claude/agents/umbraco-frontend-developer.md` with:
  - Frontmatter: name, description, tools (Read, Write, Edit, Bash, Glob, Grep), model (sonnet)
  - Expertise: Lit Web Components, TypeScript, Vite, UUI, RxJS, Management API, Extension types
  - Minimal code examples (1-2 per topic, ~5-10 lines): Lit component, UUI usage, Management API
  - Accessibility guidance (WCAG 2.1 AA)
  - MCP Integration section with endpoint, tools, and **no-results fallback sequence**
  - Constitutional Compliance section

- [X] T010 [P] [US2] Create `.claude/agents/umbraco-backend-reviewer.md` with:
  - Frontmatter: name, description, tools (Read, Glob, Grep), model (haiku)
  - Expertise: C# code quality, Composer patterns, Service layer design, Notification Handler best practices
  - Review checklist for C# patterns and Clean Architecture compliance
  - MCP Integration section with endpoint, tools, and **no-results fallback sequence**
  - Constitutional Compliance section

- [X] T011 [P] [US2] Create `.claude/agents/umbraco-frontend-reviewer.md` with:
  - Frontmatter: name, description, tools (Read, Glob, Grep), model (haiku)
  - Expertise: Lit/Web Component patterns, TypeScript best practices, UUI usage, accessibility
  - Review checklist for Lit/TypeScript patterns and WCAG 2.1 AA compliance
  - MCP Integration section with endpoint, tools, and **no-results fallback sequence**
  - Constitutional Compliance section

**Checkpoint**: All 5 Umbraco agents created and ready for invocation

---

## Phase 5: User Story 3 - CLAUDE.md Integration (Priority: P2)

**Goal**: Update CLAUDE.md to include Umbraco agents in all three agent tables (Available Agents, Tools & Permissions, Phase Utilization)

**Independent Test**: Verify CLAUDE.md contains all 5 Umbraco agents in each of the 3 tables with accurate information

### Verification for User Story 3 (Manual Validation)

- [X] T012 [US3] Create verification checklist for CLAUDE.md updates in `.specify/checklists/us3-claudemd.md`

### Implementation for User Story 3

- [X] T013 [US3] Update Available Agents table in `CLAUDE.md` to add all 5 Umbraco agents:
  - umbraco-architect | Opus | Umbraco architecture, Document Type design, package architecture
  - umbraco-backend-developer | Sonnet | Umbraco C# development, Composers, Services, Notification Handlers
  - umbraco-frontend-developer | Sonnet | Umbraco backoffice UI, Lit/TypeScript, UUI components
  - umbraco-backend-reviewer | Haiku | Umbraco C# code review, pattern compliance
  - umbraco-frontend-reviewer | Haiku | Umbraco frontend review, accessibility compliance

- [X] T014 [US3] Update Agent Tools & Permissions table in `CLAUDE.md` to add all 5 Umbraco agents:
  - umbraco-architect | Read, Glob, Grep | ❌ Read-only
  - umbraco-backend-developer | Read, Write, Edit, Bash, Glob, Grep | ✅ Yes
  - umbraco-frontend-developer | Read, Write, Edit, Bash, Glob, Grep | ✅ Yes
  - umbraco-backend-reviewer | Read, Glob, Grep | ❌ Read-only
  - umbraco-frontend-reviewer | Read, Glob, Grep | ❌ Read-only

- [X] T015 [US3] Update Agent & Plugin Utilization by Phase table in `CLAUDE.md`:
  - `/speckit.plan`: Add umbraco-architect to Primary Agents
  - `/speckit.implement`: Add umbraco-backend-developer, umbraco-frontend-developer to Primary Agents
  - Post-implement: Add umbraco-backend-reviewer, umbraco-frontend-reviewer to Supporting Agents

**Checkpoint**: CLAUDE.md fully updated - developers can discover Umbraco agents

---

## Phase 6: User Story 4 - External Plugins Documentation (Priority: P2)

**Goal**: Already completed in User Story 1 - MCP section added to external-plugins.md

**Note**: This user story is satisfied by T005 in Phase 3. The MCP Integration Guide (US1) fulfills the External Plugins Documentation requirement (US4).

**Independent Test**: Same as US1 - verify external-plugins.md has Umbraco MCP section

**Checkpoint**: No additional tasks needed - US4 is a subset of US1

---

## Phase 7: User Story 5 - WebFetch Permission (Priority: P3)

**Goal**: Verify/configure WebFetch permission for docs.umbraco.com in settings.local.json

**Independent Test**: Verify settings.local.json contains `WebFetch(domain:docs.umbraco.com)` or equivalent permission

### Verification for User Story 5 (Manual Validation)

- [X] T016 [US5] Verify `.claude/settings.local.json` has WebFetch permission for docs.umbraco.com domain
  - Per plan.md: "settings.local.json already has `WebFetch(domain:docs.umbraco.com)` permission (line 72)"
  - If present: Document verification in task completion
  - If missing: Add permission entry

**Checkpoint**: WebFetch fallback is configured for all Umbraco agents

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final validation across all user stories

- [X] T017 [P] Verify all 5 agent files exist: `ls .claude/agents/umbraco-*.md`
- [X] T018 [P] Verify CLAUDE.md has all 3 tables updated: Search for "umbraco-" entries in each table
- [X] T019 [P] Verify external-plugins.md has Umbraco MCP section: Search for "Umbraco MCP" heading
- [X] T020 [P] Verify each agent references MCP: Search for "~gitbook/mcp" in each agent file
- [X] T021 [P] Verify tool assignments match BreezSDK pattern: Compare frontmatter across all agents
- [X] T022 [P] Verify Constitutional Compliance sections exist in all agents

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Empty - no blocking prerequisites for this feature
- **User Stories (Phases 3-7)**: Can proceed after Setup verification
  - US1 (Phase 3) and US2 (Phase 4) can run in **parallel** - independent files
  - US3 (Phase 5) depends on US2 completion (agents must exist to be listed)
  - US4 (Phase 6) is completed by US1 - no additional work
  - US5 (Phase 7) can run in parallel with any phase
- **Polish (Phase 8)**: Depends on US1, US2, US3 completion

### User Story Dependencies

- **User Story 1 (MCP Guide)**: Independent - can start after Setup
- **User Story 2 (Agents)**: Independent - can start after Setup
- **User Story 3 (CLAUDE.md)**: Depends on US2 (agents must exist to document them)
- **User Story 4 (External Plugins)**: Satisfied by US1 - no separate work
- **User Story 5 (WebFetch)**: Independent - can start after Setup

### Within Each User Story

- Verification checklist before implementation (where applicable)
- All agent files (T007-T011) can run in parallel
- CLAUDE.md updates (T013-T015) can run sequentially or parallel (same file but different sections)
- Core implementation before cross-cutting validation

### Parallel Opportunities

- All Setup tasks (T001-T003) can run in parallel
- US1 (T005) and US2 (T007-T011) can run in parallel
- All agent creation tasks (T007-T011) can run in parallel
- All Polish verification tasks (T017-T022) can run in parallel
- US5 (T016) can run in parallel with any other phase

---

## Parallel Example: User Story 2 (Agents)

```bash
# Launch all agent creation tasks together:
& Use backend-developer to create umbraco-architect.md (T007)
& Use backend-developer to create umbraco-backend-developer.md (T008)
& Use backend-developer to create umbraco-frontend-developer.md (T009)
& Use backend-developer to create umbraco-backend-reviewer.md (T010)
& Use backend-developer to create umbraco-frontend-reviewer.md (T011)
```

---

## Parallel Example: Polish Phase

```bash
# Launch all verification tasks together:
& Use code-reviewer to verify agent files exist (T017)
& Use code-reviewer to verify CLAUDE.md updates (T018)
& Use code-reviewer to verify external-plugins.md (T019)
& Use code-reviewer to verify MCP references (T020)
& Use code-reviewer to verify tool assignments (T021)
& Use code-reviewer to verify Constitutional Compliance (T022)
```

---

## Implementation Strategy

### MVP First (User Story 1 + 2)

1. Complete Phase 1: Setup verification
2. Complete Phase 3: User Story 1 (MCP Integration Guide) **AND**
3. Complete Phase 4: User Story 2 (Agent Definitions) **IN PARALLEL**
4. **STOP and VALIDATE**: Test agent invocation with MCP queries
5. Agents are functional with MCP documentation access

### Incremental Delivery

1. Setup verification → Prerequisites confirmed
2. US1 + US2 in parallel → Agents created with MCP guide (MVP!)
3. US3 → CLAUDE.md updated → Agents discoverable
4. US5 → WebFetch verified → Fallback configured
5. Polish → All success criteria validated

### Parallel Team Strategy

With multiple agents:

1. Complete Setup verification together
2. Once Setup is done:
   - Agent A: User Story 1 (external-plugins.md)
   - Agent B-F: User Story 2 (5 agent files in parallel)
3. After US2: User Story 3 (CLAUDE.md updates)
4. Final: Polish verification in parallel

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- This is a documentation-only feature - no production code, no automated tests
- Verification is manual file inspection per plan.md
- All agents follow BreezSDK pattern for consistency
- settings.local.json already has WebFetch permission (verify only)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
