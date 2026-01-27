# Tasks: Genericize SpecKit and Claude Code Configuration for Cross-Project Reuse

**Input**: Design documents from `specs/001-generic-speckit-reuse/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, quickstart.md

**Tests**: This feature modifies configuration files (Markdown, JSON), not runtime code. Per plan.md Constitution Check, TDD is deviated — verification is done via content scanning (grep for domain terms in core files). Validation tasks replace traditional test tasks.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- All paths are relative to repository root
- Configuration files: `.claude/`, `.specify/`, `CLAUDE.md`
- Feature docs: `specs/001-generic-speckit-reuse/`

---

## Phase 1: Setup

**Purpose**: Create the new project-specific addendum file that will receive all moved content

- [ ] T001 Create `CLAUDE.project.md` at repository root with structure for .NET conventions, domain agents, MCP server docs, and technology-specific content (see plan.md D1 content migration map)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: No foundational phase needed — this feature has no shared infrastructure. All user stories operate on independent file sets and can begin after T001.

**Checkpoint**: T001 complete — user story work can begin

---

## Phase 3: User Story 1 — Copy Configuration to a New Project (Priority: P1) MVP

**Goal**: All core configuration files (Tier 1) contain zero references to BreezSDK, Umbraco, Lightning, or other domain-specific content. A developer can copy `.claude/`, `.specify/`, and `CLAUDE.md` to a new project and run SpecKit without encountering domain-specific terms.

**Independent Test**: Copy `.claude/`, `.specify/`, and `CLAUDE.md` into an empty repo. Run grep for domain terms (BreezSDK, Umbraco, Lightning, breezsdk, umbraco). Zero matches expected.

### Validation for User Story 1 (Content Scanning — replaces TDD per plan.md deviation)

- [ ] T002 [US1] Write a PowerShell validation script at `specs/001-generic-speckit-reuse/validate-tier1.ps1` that scans all Tier 1 files for domain-specific terms (BreezSDK, Umbraco, Lightning, breezsdk, umbraco) and reports violations. Run it to establish the RED baseline (current violations).

### Implementation for User Story 1

- [ ] T003 [US1] Genericize `CLAUDE.md` — Apply the full content migration map from plan.md D1. Specifically: **Move to CLAUDE.project.md**: .NET Commands table, Frontend Commands table, Code Conventions (.NET Style), Database (EF Core), Common .NET Gotchas, Test Timing Guidelines, Static State in Tests, domain agent rows from Available Agents table, domain agent rows from Agent Tools & Permissions table, domain agent rows from Agent & Plugin Utilization table. **Keep in CLAUDE.md (genericize)**: Project Configuration header, Governance, Spec-Kit Workflow Order, API Design (generic REST principles), core agent rows (7 agents) in Available Agents/Tools/Utilization tables, Wave Execution Strategy (replace dotnet commands with generic), Workflow Rules (genericize tool commands), Branch Strategy, Resources. Refer to plan.md D1 table for authoritative section-by-section mapping.
- [ ] T004 [P] [US1] Genericize `.specify/memory/constitution.md` — Change title from ".NET Fullstack Development Constitution" to "Software Development Constitution". Replace `dotnet build`, `dotnet test`, `dotnet format` with generic terms (build tool, test runner, formatter). Keep all principle statements unchanged per plan.md D3.
- [ ] T005 [P] [US1] Genericize `.claude/agents/solution-architect.md` — Remove .NET-specific references, replace with generic architecture patterns per plan.md D4.
- [ ] T006 [P] [US1] Genericize `.claude/agents/backend-developer.md` — Remove C# 12+, ASP.NET Core, EF Core references. Replace with generic backend development, ORM, web framework terms per plan.md D4.
- [ ] T007 [P] [US1] Genericize `.claude/agents/frontend-developer.md` — Remove Blazor, Razor references. Replace with generic component framework, template terms per plan.md D4.
- [ ] T008 [P] [US1] Genericize `.claude/agents/test-engineer.md` — Remove xUnit, FluentAssertions, Moq references. Replace with generic test framework, assertion library, mocking framework terms per plan.md D4. Replace domain-specific examples with generic equivalents (e.g., User/Order entities) per plan.md D4 Generic Example Guidelines and FR-016.
- [ ] T009 [P] [US1] Genericize `.claude/agents/database-architect.md` — Remove EF Core, SQL Server references. Replace with generic ORM, relational database terms per plan.md D4.
- [ ] T010 [P] [US1] Genericize `.claude/agents/security-auditor.md` — Remove .NET security pattern references. Replace with generic security patterns per plan.md D4.
- [ ] T011 [P] [US1] Genericize `.claude/agents/code-reviewer.md` — Remove .NET convention references. Replace with generic project conventions (per CLAUDE.md/CLAUDE.project.md) per plan.md D4. Replace domain-specific examples with generic equivalents (e.g., User/Order entities) per plan.md D4 Generic Example Guidelines and FR-016.
- [ ] T012 [P] [US1] Genericize `.claude/commands/speckit.implement.md` — Replace `dotnet build`, `dotnet test`, `dotnet format` with generic build/test/format references. Replace `dotnet-claude-code-skills` plugin reference with generic note. Keep all workflow patterns unchanged per plan.md D5.
- [ ] T013 [P] [US1] Rename `.claude/skills/dotnet-implementation-execution.md` to `.claude/skills/implementation-execution.md` and genericize content — Replace `dotnet build/test/format` with generic build/test/format references. Keep wave execution, checkpoint, context management patterns per plan.md D6.
- [ ] T014 [P] [US1] Genericize `.claude/skills/external-plugins.md` — Replace constitution article number references with principle name references. Separate core plugins from technology-specific plugins per plan.md D6 and FR-015.
- [ ] T015 [P] [US1] Genericize `.specify/templates/plan.md` — Remove references to `dotnet-plan-strategy` skill and .NET-specific agents per plan.md Project Structure.
- [ ] T016 [P] [US1] Genericize `.specify/templates/tasks.md` — Remove references to `dotnet-task-parallelization` skill per plan.md Project Structure.
- [ ] T017 [P] [US1] Genericize `.specify/templates/implement.md` — Remove references to `dotnet-implementation-execution` skill and dotnet commands per plan.md Project Structure.
- [ ] T018 [US1] Run `specs/001-generic-speckit-reuse/validate-tier1.ps1` — Verify zero domain-term violations in all Tier 1 files (GREEN validation).

**Checkpoint**: All Tier 1 core files are domain-free. Copy-to-new-project scenario works.

---

## Phase 4: User Story 2 — Retain Domain-Specific Agents as Optional Add-ons (Priority: P2)

**Goal**: Domain-specific agents (breezsdk-*, umbraco-*) exist as self-contained, optional files. No core configuration file references them. They can be removed entirely without breaking anything.

**Independent Test**: Remove all `breezsdk-*` and `umbraco-*` agent files from `.claude/agents/`. Grep all remaining files for "breezsdk" and "umbraco". Zero matches expected in Tier 1 files.

### Validation for User Story 2 (Content Scanning)

- [ ] T019 [US2] Write a validation script at `specs/001-generic-speckit-reuse/validate-domain-isolation.ps1` that: (1) verifies no Tier 1 file references domain agent filenames (breezsdk-*, umbraco-*), (2) verifies domain agents are only documented in `CLAUDE.project.md`, (3) verifies `hooks.json` contains no hardcoded domain agent names, (4) verifies `settings.local.json` contains no project-specific WebFetch domain permissions, (5) scans domain agent files for cross-references to other domain agent families (e.g., breezsdk-* referencing umbraco-* or vice versa) to ensure each domain family can be removed independently per Edge Case #2. Run for RED baseline.

### Implementation for User Story 2

- [ ] T020 [US2] Update `CLAUDE.project.md` (T001) — Add domain agent documentation (breezsdk-*, umbraco-* agent tables, tools/permissions, utilization phases) moved from CLAUDE.md in T003. Ensure all domain agent docs are self-contained in this file.
- [ ] T021 [P] [US2] Update `.claude/hooks.json` — Remove domain agent names from SessionStart agent list. Remove `dotnet-implementation-execution.md` skill reference (use `implementation-execution.md`). Remove domain-specific SubagentStart guidance per plan.md D2.
- [ ] T022 [P] [US2] Update `.claude/settings.json` and `.claude/settings.local.json` — (1) Remove project-specific MCP server configurations (e.g., umbraco-docs) from `settings.json` per FR-007. (2) Remove project-specific WebFetch domain permissions from `settings.local.json` per FR-008. (3) Document both MCP server and WebFetch domain configuration in `CLAUDE.project.md` with instructions for project-specific setup per plan.md D3.
- [ ] T023 [US2] Run `specs/001-generic-speckit-reuse/validate-domain-isolation.ps1` — Verify zero domain agent references in Tier 1 files (GREEN validation).

**Checkpoint**: Domain agents are fully optional. Removing them breaks nothing in core configuration.

---

## Phase 5: User Story 3 — Technology-Specific Content is Modular (Priority: P2)

**Goal**: All technology-specific content (.NET build commands, code conventions, test patterns) is either in `CLAUDE.project.md` or in clearly marked customizable sections. Core workflow files don't hardcode any technology.

**Independent Test**: Identify all .NET-specific terms (dotnet, csproj, NuGet, xunit, EF Core) in Tier 1 files. Each instance must be generic or in a documented customizable pattern.

### Validation for User Story 3 (Content Scanning)

- [ ] T024 [US3] Write a validation script at `specs/001-generic-speckit-reuse/validate-tech-modularity.ps1` that scans Tier 1 files for hardcoded technology-specific terms (`dotnet`, `.csproj`, `NuGet`, `xunit`, `FluentAssertions`, `Moq`, `EF Core`, `Entity Framework`, `Blazor`, `Razor`) used as requirements (not as customizable examples). Run for RED baseline.

### Implementation for User Story 3

- [ ] T025 [US3] Update `.claude/hooks.json` — Make PostToolUse file extension patterns generic (change `.cs|.razor` to customizable pattern). Replace `dotnet format` with generic format-check. Make build/test detection generic. Make Stop hook file detection generic. Make PreToolUse project file detection generic per plan.md D2/FR-005.
- [ ] T026 [P] [US3] Update `CLAUDE.project.md` — Add .NET-specific hooks examples, build/test commands, code conventions, and customization instructions so `.NET` developers retain full effectiveness.
- [ ] T027 [P] [US3] Update `.specify/memory/constitution.md` — Verify enforcement framework uses generic placeholders (`<build-tool>`, `<test-runner>`, `<formatter>`) not hardcoded `dotnet` commands per plan.md D3. (This may already be done in T004; verify and complete if needed.)
- [ ] T028 [US3] Run `specs/001-generic-speckit-reuse/validate-tech-modularity.ps1` — Verify zero hardcoded technology terms in Tier 1 files (GREEN validation).

**Checkpoint**: Technology-specific content is fully modular. Non-.NET projects can adopt the toolkit without editing core files.

---

## Phase 6: User Story 4 — Core SpecKit Workflow Remains Effective (Priority: P1)

**Goal**: After all genericization, the SpecKit workflow retains full effectiveness. All quality gates, validation checks, agent coordination, wave execution, and TDD enforcement are preserved.

**Independent Test**: Run the full SpecKit workflow mentally/structurally — verify all phases produce output of equivalent quality. All wave execution strategies, context management, and checkpoint patterns are preserved in `CLAUDE.md`.

### Validation for User Story 4 (Structural Verification)

- [ ] T029 [US4] Write a validation script at `specs/001-generic-speckit-reuse/validate-workflow-preservation.ps1` that verifies: (1) all `/speckit.*` command files still exist and are valid, (2) CLAUDE.md still contains wave execution strategy, context management, checkpoint patterns, quality gate sections, (3) constitution still contains all articles and principles, (4) all core agent files exist. Run for RED baseline.

### Implementation for User Story 4

- [ ] T030 [US4] Review `CLAUDE.md` post-genericization — Verify wave execution strategy, context management, checkpoint patterns, phase completion requirements, workflow rules, and agent coordination patterns are preserved and coherent after all content moves per plan.md D1. Fix any gaps.
- [ ] T031 [P] [US4] Review `.claude/commands/speckit.implement.md` post-genericization — Verify TDD enforcement, quality gates, wave execution references, and agent recommendations are intact after genericization in T012. Fix any gaps.
- [ ] T032 [P] [US4] Review `.specify/memory/constitution.md` post-genericization — Verify all articles, principles, and enforcement mechanisms are coherent after genericization in T004/T027. Fix any gaps.
- [ ] T033 [US4] Run `specs/001-generic-speckit-reuse/validate-workflow-preservation.ps1` — Verify all workflow capabilities preserved (GREEN validation).

**Checkpoint**: Full SpecKit workflow effectiveness confirmed after genericization.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final cleanup, cross-file consistency, and comprehensive validation

- [ ] T034 [P] Update `CLAUDE.md` Resources section — Ensure all file references are correct after renames (e.g., `implementation-execution.md` not `dotnet-implementation-execution.md`)
- [ ] T035 [P] Update any cross-references between files — (1) Grep entire repository for `dotnet-implementation-execution` and update all references to `implementation-execution` (renamed in T013). (2) Verify all internal links (skill references in commands, agent references in hooks, skill references in templates) point to correct filenames after renames. (3) Verify `CLAUDE.md` Resources section references match actual file paths.
- [ ] T036 [P] Genericize `.claude/commands/speckit.worktree.md` — Replace project-specific name in example with generic project name per FR-014
- [ ] T039 [P] Separate `QUICK-REFERENCE.md` — Move technology-specific quick reference commands (.NET, npm) to `CLAUDE.project.md` or a clearly marked customizable section. Keep SpecKit workflow reference (command order, phase descriptions) as core content per FR-013.
- [ ] T037 Run comprehensive validation — Execute all four validation scripts (T002, T019, T024, T029) together. All must pass with zero violations. Additionally verify QUICK-REFERENCE.md contains no hardcoded technology commands in core sections.
- [ ] T038 Run quickstart.md validation — Execute the verification steps from `specs/001-generic-speckit-reuse/quickstart.md` (Tests 1-4) to confirm end-to-end quality.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — T001 creates CLAUDE.project.md as the target for moved content
- **US1 (Phase 3)**: Depends on T001 (needs CLAUDE.project.md to exist for content moves)
- **US2 (Phase 4)**: Depends on T003 (CLAUDE.md must be stripped before verifying domain isolation)
- **US3 (Phase 5)**: Depends on T004, T012, T013 (constitution and commands must be genericized first)
- **US4 (Phase 6)**: Depends on Phases 3-5 completion (verifies nothing was lost during genericization)
- **Polish (Phase 7)**: Depends on all user stories complete
- **Constitution Prerequisite**: T004 (genericize constitution title/terms) and T027 (genericize enforcement framework) are blocking prerequisites for all Success Criteria validation. The constitution currently contains `.NET`-specific title and `dotnet` commands in its Enforcement Framework, which directly conflicts with the feature's genericization goal. These tasks MUST complete before SC-001 through SC-006 can be verified.

### User Story Dependencies

- **User Story 1 (P1)**: Depends on T001 only. Core genericization work.
- **User Story 2 (P2)**: Depends on US1 T003 (CLAUDE.md must be cleaned first so domain docs go to CLAUDE.project.md)
- **User Story 3 (P2)**: Depends on US1 T004, T012, T013 (genericization must happen before verifying modularity)
- **User Story 4 (P1)**: Depends on US1, US2, US3 complete (verification of preservation)

### Within Each User Story

- Validation script FIRST (RED baseline)
- Implementation tasks (many parallelizable)
- Final validation (GREEN confirmation)

### Parallel Opportunities

- **Phase 3 (US1)**: T004-T017 are all [P] — 14 tasks can run in parallel (different files, no dependencies)
- **Phase 4 (US2)**: T021-T022 are [P] — 2 tasks can run in parallel
- **Phase 5 (US3)**: T026-T027 are [P] — 2 tasks can run in parallel
- **Phase 6 (US4)**: T031-T032 are [P] — 2 tasks can run in parallel
- **Phase 7**: T034-T036, T039 are [P] — 4 tasks can run in parallel

---

## Parallel Example: User Story 1 (Phase 3)

```bash
# First: Write validation script (sequential)
Task: T002 — Write validate-tier1.ps1 and run RED baseline

# Then: Launch all file genericization tasks in parallel
Task: T004 — Genericize constitution.md
Task: T005 — Genericize solution-architect.md
Task: T006 — Genericize backend-developer.md
Task: T007 — Genericize frontend-developer.md
Task: T008 — Genericize test-engineer.md
Task: T009 — Genericize database-architect.md
Task: T010 — Genericize security-auditor.md
Task: T011 — Genericize code-reviewer.md
Task: T012 — Genericize speckit.implement.md
Task: T013 — Rename + genericize implementation-execution.md
Task: T014 — Genericize external-plugins.md
Task: T015 — Genericize plan template
Task: T016 — Genericize tasks template
Task: T017 — Genericize implement template

# Finally: Run GREEN validation (sequential)
Task: T018 — Run validate-tier1.ps1, verify zero violations
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001 — create CLAUDE.project.md)
2. Complete Phase 3: User Story 1 (T002-T018 — genericize all Tier 1 files)
3. **STOP and VALIDATE**: Run validate-tier1.ps1 — zero domain-term violations
4. At this point, the toolkit is copyable to new projects

### Incremental Delivery

1. T001 (Setup) -> Foundation ready
2. US1 (Phase 3) -> Core files generic, copyable to new projects (MVP!)
3. US2 (Phase 4) -> Domain agents properly isolated as optional
4. US3 (Phase 5) -> Technology content fully modular
5. US4 (Phase 6) -> Workflow preservation confirmed
6. Polish (Phase 7) -> Cross-file consistency validated

### Single Developer Strategy

Execute phases sequentially: 1 -> 3 -> 4 -> 5 -> 6 -> 7. Within Phase 3, maximize parallel agent execution on the 14 independent file genericization tasks (T004-T017).

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- This feature has no runtime code — "tests" are content validation scripts (grep-based)
- Constitution Article III deviation approved in plan.md for this configuration-only feature
- All genericization must preserve file structure and section headings for CLAUDE.md coherence
- When moving content to CLAUDE.project.md, maintain the same section organization for developer familiarity
- Total files modified: ~19 (7 agents + CLAUDE.md + constitution + hooks.json + settings.json + settings.local.json + 3 templates + 3 skills/commands + CLAUDE.project.md + QUICK-REFERENCE.md)
