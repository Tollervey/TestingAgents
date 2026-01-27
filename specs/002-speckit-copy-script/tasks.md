# Tasks: Spec-Kit Project Scaffolding Script (Copy-SpecKit.ps1)

**Input**: Design documents from `/specs/002-speckit-copy-script/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/cli-interface.md

**Tests**: Per Constitution Article III (Test-First Imperative), tests are **MANDATORY** and must be written BEFORE implementation. No production code may be written until corresponding tests exist and fail (Red-Green-Refactor). This is NON-NEGOTIABLE.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Script**: `.specify/scripts/powershell/Copy-SpecKit.ps1`
- **Tests**: `.specify/scripts/powershell/Copy-SpecKit.tests.ps1`

---

## Phase 1: Setup

**Purpose**: Create script file with parameter definitions, comment-based help, and basic structure

- [x] T001 Create script file with CmdletBinding, parameter block, and comment-based help at .specify/scripts/powershell/Copy-SpecKit.ps1
- [x] T002 Create Pester 5 test file with Describe/Context skeleton organized by user story at .specify/scripts/powershell/Copy-SpecKit.tests.ps1

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Internal functions that ALL user stories depend on: source validation, file manifest building, domain discovery, and output helpers

**CRITICAL**: No user story work can begin until this phase is complete

### Tests for Foundational (MANDATORY - Constitution Article III)

> **TDD REQUIRED: Write tests FIRST, verify they FAIL (RED), then implement until GREEN**

- [x] T003 [P] Write Pester tests for source path validation (valid source, missing directories, missing CLAUDE.md, same source/dest) in .specify/scripts/powershell/Copy-SpecKit.tests.ps1
- [x] T004 [P] Write Pester tests for file manifest building (Tier 1 framework files enumerated, Tier 2 core agents enumerated, excluded files not present) in .specify/scripts/powershell/Copy-SpecKit.tests.ps1
- [x] T005 [P] Write Pester tests for domain module auto-discovery (detects umbraco and breezsdk prefixes, returns DomainModule objects with correct file lists) in .specify/scripts/powershell/Copy-SpecKit.tests.ps1

### Implementation for Foundational

- [x] T006 Implement source path validation function (Test-SpecKitSource) that checks for .claude/, .specify/, CLAUDE.md and detects same source/dest (FR-014, FR-015) in .specify/scripts/powershell/Copy-SpecKit.ps1
- [x] T007 Implement file manifest builder function (Build-FileManifest) that returns FileEntry objects for Tier 1 and Tier 2 files using declarative hashtable with glob patterns (RT-001) in .specify/scripts/powershell/Copy-SpecKit.ps1
- [x] T008 Implement domain module auto-discovery function (Get-DomainModules) that scans .claude/agents/ and .claude/skills/ for non-core prefixes and returns DomainModule objects (FR-010, RT-002) in .specify/scripts/powershell/Copy-SpecKit.ps1
- [x] T009 Implement rich console output helper functions (Write-Header, Write-Section, Write-FileEntry) using Write-Host -ForegroundColor and Unicode symbols (FR-020, RT-004) in .specify/scripts/powershell/Copy-SpecKit.ps1

**Checkpoint**: Foundation ready - all internal functions implemented and tested. User story implementation can now begin.

---

## Phase 3: User Story 1 - Copy Spec-Kit Framework to a New Project (Priority: P1) MVP

**Goal**: A developer runs the script with a destination path and all Tier 1 + Tier 2 files are copied preserving directory structure. Domain-specific files are excluded by default. Missing destination directories are created. Invalid source produces clear error.

**Independent Test**: Run script with valid source and temp destination, verify all expected framework files exist at destination with correct directory structure.

### Tests for User Story 1 (MANDATORY - Constitution Article III)

> **TDD REQUIRED: Write tests FIRST, verify they FAIL (RED), then implement until GREEN**

- [x] T010 [P] [US1] Write Pester tests for core copy operation: all Tier 1 files copied preserving structure, all Tier 2 core agents copied, domain files excluded, destination directories auto-created in .specify/scripts/powershell/Copy-SpecKit.tests.ps1
- [x] T011 [P] [US1] Write Pester tests for placeholder directory creation: .specify/memory/, .specify/metrics/, .specify/plans/, specs/ created empty at destination (FR-007) in .specify/scripts/powershell/Copy-SpecKit.tests.ps1
- [x] T012 [P] [US1] Write Pester tests for error handling: source not found exits with code 1, missing source structure exits with code 1 and lists missing items (FR-014, FR-018) in .specify/scripts/powershell/Copy-SpecKit.tests.ps1

### Implementation for User Story 1

- [x] T013 [US1] Implement main copy engine function (Invoke-SpecKitCopy) that iterates FileEntry manifest, creates directories via New-Item, copies files via Copy-Item, and returns CopyResult object in .specify/scripts/powershell/Copy-SpecKit.ps1
- [x] T014 [US1] Implement placeholder directory creation for .specify/memory/, .specify/metrics/, .specify/plans/, specs/ (FR-007) in .specify/scripts/powershell/Copy-SpecKit.ps1
- [x] T015 [US1] Wire up main script entry point: parameter processing, source validation call, manifest building, copy engine call, exit code setting (FR-001, FR-002, FR-018) in .specify/scripts/powershell/Copy-SpecKit.ps1
- [x] T016 [US1] Implement copy operation summary output showing files copied, directories created, grouped by tier with rich formatting (FR-020) in .specify/scripts/powershell/Copy-SpecKit.ps1

**Checkpoint**: User Story 1 complete. Script copies all framework files to a new destination with directory structure preserved. MVP is functional.

---

## Phase 4: User Story 2 - Preview Mode / Dry Run (Priority: P2)

**Goal**: Developer runs script with -Preview flag and sees categorized list of planned operations without any filesystem changes.

**Independent Test**: Run script in preview mode, verify output lists all planned operations and no files/directories are created.

### Tests for User Story 2 (MANDATORY - Constitution Article III)

> **TDD REQUIRED: Write tests FIRST, verify they FAIL (RED), then implement until GREEN**

- [x] T017 [P] [US2] Write Pester tests for preview mode: no filesystem changes occur, output lists files grouped by tier with source/destination paths and inclusion/exclusion reasons in .specify/scripts/powershell/Copy-SpecKit.tests.ps1

### Implementation for User Story 2

- [x] T018 [US2] Implement preview mode logic in Invoke-SpecKitCopy that skips all filesystem operations and outputs planned operations grouped by tier (FR-008) in .specify/scripts/powershell/Copy-SpecKit.ps1
- [x] T019 [US2] Implement preview output formatting showing each file with source path, destination path, action (Copy/Skip/CreateDir), and tier classification in .specify/scripts/powershell/Copy-SpecKit.ps1

**Checkpoint**: User Story 2 complete. Preview mode shows full operation plan without making changes.

---

## Phase 5: User Story 3 - Include Domain-Specific Agents Selectively (Priority: P2)

**Goal**: Developer specifies -IncludeDomains parameter to include domain-specific agents and skills alongside core framework. Unknown domains produce warning.

**Independent Test**: Run script with -IncludeDomains umbraco, verify umbraco-* agents and skills are copied in addition to core files.

### Tests for User Story 3 (MANDATORY - Constitution Article III)

> **TDD REQUIRED: Write tests FIRST, verify they FAIL (RED), then implement until GREEN**

- [x] T020 [P] [US3] Write Pester tests for domain inclusion: specifying -IncludeDomains umbraco copies umbraco-* agents, specifying multiple domains copies all, unknown domain produces warning and continues in .specify/scripts/powershell/Copy-SpecKit.tests.ps1

### Implementation for User Story 3

- [x] T021 [US3] Implement domain filtering in Build-FileManifest that marks domain FileEntry objects as Included when their domain matches -IncludeDomains parameter in .specify/scripts/powershell/Copy-SpecKit.ps1
- [x] T022 [US3] Implement unknown domain warning that lists available domains and continues with copy (FR-009, FR-010) in .specify/scripts/powershell/Copy-SpecKit.ps1
- [x] T023 [US3] Implement "Available domain modules" display section in rich output showing discovered domains with file counts in .specify/scripts/powershell/Copy-SpecKit.ps1

**Checkpoint**: User Story 3 complete. Domain modules can be selectively included.

---

## Phase 6: User Story 4 - Post-Copy Initialization Guidance (Priority: P3)

**Goal**: After copy completes, script outputs summary with file counts and numbered next-steps checklist.

**Independent Test**: Run script and verify post-copy output includes numbered checklist referencing CLAUDE.project.md, constitution, settings.local.json.

### Tests for User Story 4 (MANDATORY - Constitution Article III)

> **TDD REQUIRED: Write tests FIRST, verify they FAIL (RED), then implement until GREEN**

- [x] T024 [P] [US4] Write Pester tests for post-copy output: summary shows file counts (copied, skipped, directories created, placeholders), next-steps checklist includes all 6 items from quickstart.md in .specify/scripts/powershell/Copy-SpecKit.tests.ps1

### Implementation for User Story 4

- [x] T025 [US4] Implement post-copy summary section with file counts and statistics formatted with rich console output in .specify/scripts/powershell/Copy-SpecKit.ps1
- [x] T026 [US4] Implement next-steps checklist output with 6 numbered steps (CLAUDE.project.md, constitution, settings.json, settings.local.json, hooks.json, first feature) in .specify/scripts/powershell/Copy-SpecKit.ps1

**Checkpoint**: User Story 4 complete. Post-copy guidance is displayed.

---

## Phase 7: User Story 5 - Force Overwrite Existing Files (Priority: P3)

**Goal**: By default, existing files are warned about and skipped. With -Force, they are overwritten. Custom project files at destination are never deleted.

**Independent Test**: Run script twice against same destination - first without -Force (verify skip), then with -Force (verify overwrite). Verify custom files untouched.

### Tests for User Story 5 (MANDATORY - Constitution Article III)

> **TDD REQUIRED: Write tests FIRST, verify they FAIL (RED), then implement until GREEN**

- [x] T027 [P] [US5] Write Pester tests for conflict handling: without -Force existing files are skipped and count reported, with -Force existing files are overwritten and count reported, custom destination files not in manifest are never deleted in .specify/scripts/powershell/Copy-SpecKit.tests.ps1

### Implementation for User Story 5

- [x] T028 [US5] Implement conflict detection in copy engine that checks Test-Path for each destination file before copying, tracks conflicts in CopyOperation objects in .specify/scripts/powershell/Copy-SpecKit.ps1
- [x] T029 [US5] Implement skip-by-default behavior: conflicting files are listed with warning symbol, skip count is reported, exit code 2 when files skipped (FR-011, FR-018) in .specify/scripts/powershell/Copy-SpecKit.ps1
- [x] T030 [US5] Implement -Force overwrite behavior: conflicting files are overwritten, overwrite count reported in summary (FR-012) in .specify/scripts/powershell/Copy-SpecKit.ps1

**Checkpoint**: User Story 5 complete. Force overwrite and skip-by-default behaviors work correctly.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: PassThru output, edge cases, Verbose logging, and final validation

- [ ] T031 [P] Write Pester tests for -PassThru output: returns PSCustomObject with all CopyResult fields matching data-model.md schema in .specify/scripts/powershell/Copy-SpecKit.tests.ps1
- [ ] T032 [P] Write Pester tests for edge cases: paths with spaces handled correctly, Join-Path used for all path construction (FR-016), forward-slash display in output, partial source installation warns but copies available files, permission error on destination returns exit code 3 with clear message (mock UnauthorizedAccessException), verify -Verbose produces per-file operation log entries (FR-017) in .specify/scripts/powershell/Copy-SpecKit.tests.ps1
- [ ] T033 Implement -PassThru switch that returns CopyResult PSCustomObject for pipeline usage (FR-019) in .specify/scripts/powershell/Copy-SpecKit.ps1
- [ ] T034 Add Write-Verbose calls throughout all functions for detailed operation logging (FR-017) in .specify/scripts/powershell/Copy-SpecKit.ps1
- [ ] T035 Add SupportsShouldProcess and -WhatIf/-Confirm support to CmdletBinding for standard PowerShell confirmation alongside -Preview rich manifest mode (per contracts/cli-interface.md) in .specify/scripts/powershell/Copy-SpecKit.ps1
- [ ] T036 Run full Pester test suite and validate all tests pass via Invoke-Pester .specify/scripts/powershell/Copy-SpecKit.tests.ps1 -Output Detailed
- [ ] T037 Run quickstart.md validation: execute all usage examples from quickstart.md against a temporary destination and verify expected behavior

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 (T001, T002) - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Phase 2 completion
- **User Story 2 (Phase 4)**: Depends on Phase 3 (builds on copy engine)
- **User Story 3 (Phase 5)**: Depends on Phase 2 (domain discovery function)
- **User Story 4 (Phase 6)**: Depends on Phase 3 (needs copy result data)
- **User Story 5 (Phase 7)**: Depends on Phase 3 (extends copy engine)
- **Polish (Phase 8)**: Depends on all user stories complete

### User Story Dependencies

- **US1 (P1)**: Depends on Foundational only - no other story dependencies
- **US2 (P2)**: Depends on US1 (preview mode wraps copy engine logic)
- **US3 (P2)**: Depends on Foundational only (domain filtering is independent of copy)
- **US4 (P3)**: Depends on US1 (post-copy output needs copy result data)
- **US5 (P3)**: Depends on US1 (conflict handling extends copy engine)

### Within Each User Story

- Tests MUST be written and FAIL before implementation (RED)
- Implementation until tests pass (GREEN)
- Refactor with test safety net

### Parallel Opportunities

- **Phase 1**: T001 and T002 are sequential (T002 depends on script file existing for dot-source)
- **Phase 2 tests**: T003, T004, T005 can run in parallel (different test contexts)
- **Phase 2 implementation**: T006, T007, T008, T009 can be partially parallelized (different functions in same file)
- **Phase 3 tests**: T010, T011, T012 can run in parallel
- **User stories US3, US4, US5**: Can potentially run in parallel after US1 completes (different concerns)
- **Phase 8**: T031 and T032 can run in parallel (different test contexts)

---

## Parallel Example: User Story 1

```bash
# Launch all tests for US1 together:
& Task: "Write Pester tests for core copy operation" (T010)
& Task: "Write Pester tests for placeholder directory creation" (T011)
& Task: "Write Pester tests for error handling" (T012)

# After tests written and RED verified, implement:
Task: "Implement main copy engine" (T013)
Task: "Implement placeholder directory creation" (T014)
Task: "Wire up main entry point" (T015)
Task: "Implement summary output" (T016)
```

## Parallel Example: Post-US1 Stories

```bash
# After US1 complete, these stories can run in parallel:
& Task: "US3 - Domain inclusion" (T020-T023)
& Task: "US4 - Post-copy guidance" (T024-T026)
& Task: "US5 - Force overwrite" (T027-T030)

# US2 depends on US1 copy engine but can overlap with US3-US5:
& Task: "US2 - Preview mode" (T017-T019)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T002)
2. Complete Phase 2: Foundational (T003-T009)
3. Complete Phase 3: User Story 1 (T010-T016)
4. **STOP and VALIDATE**: Run full test suite, test manually with temp directory
5. Script is functional for basic scaffolding

### Incremental Delivery

1. Setup + Foundational -> Foundation ready
2. Add US1 -> Test independently -> MVP functional
3. Add US2 (Preview) + US3 (Domains) in parallel -> Enhanced usability
4. Add US4 (Guidance) + US5 (Force) in parallel -> Full feature set
5. Polish phase -> Production quality

---

## Notes

- All code lives in 2 files: Copy-SpecKit.ps1 (script) and Copy-SpecKit.tests.ps1 (tests)
- [P] tasks = different test contexts or functions, no conflicting edits
- Tests use Pester 5 with TestDrive:\ for filesystem isolation (RT-006)
- Cross-platform paths via Join-Path exclusively (RT-003)
- Rich output via Write-Host -ForegroundColor + Unicode symbols (RT-004)
- Domain auto-discovery via filename prefix scanning (RT-002)
- Exit codes: 0=success, 1=validation error, 2=partial, 3=filesystem error (RT-008)
