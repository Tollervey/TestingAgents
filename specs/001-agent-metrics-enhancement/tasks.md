# Tasks: Enhanced Agent Metrics System

**Input**: Design documents from `/specs/001-agent-metrics-enhancement/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Per Constitution Article III (Test-First Imperative), tests are **MANDATORY** and must be written BEFORE implementation. No production code may be written until corresponding tests exist and fail (Red-Green-Refactor). This is NON-NEGOTIABLE.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Scripts**: `.specify/scripts/powershell/`
- **Tests**: `.specify/scripts/powershell/agent-metrics.tests.ps1`
- **Config**: `.specify/metrics/settings.json`
- **Commands**: `.claude/commands/speckit.*.md`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create test infrastructure and base configuration

- [X] T001 Create Pester test file structure in `.specify/scripts/powershell/agent-metrics.tests.ps1`
- [X] T002 Create settings.json with default configuration in `.specify/metrics/settings.json`
- [X] T003 [P] Add helper function `Get-MetricsSettings` for loading settings in `.specify/scripts/powershell/agent-metrics.ps1`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core schema and validation infrastructure that ALL user stories depend on

**CRITICAL**: No user story work can begin until this phase is complete

- [X] T004 Update schema to v2.0.0 with new fields (model, phase, category, parallel) in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T005 [P] Implement schema version validation function in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T006 [P] Implement aggregate calculation helpers (per-model, per-category, parallel groups) in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T007 Add ValidateSet constraints for model, status, phase, category parameters in `.specify/scripts/powershell/agent-metrics.ps1`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - View Comprehensive Agent Performance Data (Priority: P1)

**Goal**: Capture AI model identifiers for every invocation and display cost distribution by model

**Independent Test**: Run any Spec-Kit phase that invokes agents and verify the report includes model information for each invocation with cost percentages

### Tests for User Story 1 (MANDATORY - Constitution Article III)

> **TDD REQUIRED: Write tests FIRST, verify they FAIL (RED), then implement until GREEN**

- [X] T008 [P] [US1] Unit test: Init action creates session with v2.0.0 schema in `.specify/scripts/powershell/agent-metrics.tests.ps1`
- [X] T009 [P] [US1] Unit test: Record action captures model parameter in `.specify/scripts/powershell/agent-metrics.tests.ps1`
- [X] T010 [P] [US1] Unit test: Record action updates per-model aggregates in `.specify/scripts/powershell/agent-metrics.tests.ps1`
- [X] T011 [P] [US1] Unit test: Report action calculates cost distribution by model (1:3:5 weights) in `.specify/scripts/powershell/agent-metrics.tests.ps1`
- [X] T012 [P] [US1] Unit test: Report action displays model distribution section in `.specify/scripts/powershell/agent-metrics.tests.ps1`

### Implementation for User Story 1

- [X] T013 [US1] Enhance Init action with v2.0.0 schema, phase, featureBranch parameters in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T014 [US1] Enhance Record action with -Model parameter and model validation in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T015 [US1] Implement per-model aggregate updates in Record action in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T016 [US1] Enhance Report action with MODEL DISTRIBUTION section in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T017 [US1] Implement cost percentage calculation using weighted tokens in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T018 [US1] Enhance Reset action with auto-cleanup per retention settings in `.specify/scripts/powershell/agent-metrics.ps1`

**Checkpoint**: US1 complete - model tracking and cost distribution fully functional

---

## Phase 4: User Story 2 - Track Metrics Across All Spec-Kit Phases (Priority: P1)

**Goal**: Metrics collected during all Spec-Kit phases with phase name tracking and Spec-Kit command integration

**Independent Test**: Run `/speckit.plan` and verify metrics are initialized, collected, and reported with phase="plan"

### Tests for User Story 2 (MANDATORY - Constitution Article III)

> **TDD REQUIRED: Write tests FIRST, verify they FAIL (RED), then implement until GREEN**

- [X] T019 [P] [US2] Unit test: Record action captures phase name from session in `.specify/scripts/powershell/agent-metrics.tests.ps1`
- [X] T020 [P] [US2] Unit test: Record action captures category parameter in `.specify/scripts/powershell/agent-metrics.tests.ps1`
- [X] T021 [P] [US2] Unit test: Report action displays category breakdown section in `.specify/scripts/powershell/agent-metrics.tests.ps1`
- [X] T022 [P] [US2] Unit test: Init archives incomplete previous session in `.specify/scripts/powershell/agent-metrics.tests.ps1`

### Implementation for User Story 2

- [X] T023 [US2] Enhance Record action with -Category parameter and category aggregates in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T024 [US2] Implement per-category aggregate updates in Record action in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T025 [US2] Enhance Report action with CATEGORY BREAKDOWN section in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T026 [US2] Implement incomplete session archiving in Init action in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T027 [P] [US2] Add metrics integration to speckit.specify.md in `.claude/commands/speckit.specify.md`
- [X] T028 [P] [US2] Add metrics integration to speckit.clarify.md in `.claude/commands/speckit.clarify.md`
- [X] T029 [P] [US2] Add metrics integration to speckit.plan.md in `.claude/commands/speckit.plan.md`
- [X] T030 [P] [US2] Add metrics integration to speckit.tasks.md in `.claude/commands/speckit.tasks.md`
- [X] T031 [P] [US2] Add metrics integration to speckit.checklist.md in `.claude/commands/speckit.checklist.md`
- [X] T032 [P] [US2] Add metrics integration to speckit.analyze.md in `.claude/commands/speckit.analyze.md`
- [X] T033 [US2] Update speckit.implement.md with enhanced model capture in `.claude/commands/speckit.implement.md`

**Checkpoint**: US2 complete - all 7 Spec-Kit phases track metrics with phase/category breakdown

---

## Phase 5: User Story 3 - Analyze Productivity Trends Over Time (Priority: P2)

**Goal**: Generate trend reports comparing metrics across archived sessions

**Independent Test**: Archive 3+ sessions, run Trends action, verify trend analysis report with insights

### Tests for User Story 3 (MANDATORY - Constitution Article III)

> **TDD REQUIRED: Write tests FIRST, verify they FAIL (RED), then implement until GREEN**

- [X] T034 [P] [US3] Unit test: Trends action loads archives from archive directory in `.specify/scripts/powershell/agent-metrics.tests.ps1`
- [X] T035 [P] [US3] Unit test: Trends action filters by date range and feature branch in `.specify/scripts/powershell/agent-metrics.tests.ps1`
- [X] T036 [P] [US3] Unit test: Trends action calculates token usage trend in `.specify/scripts/powershell/agent-metrics.tests.ps1`
- [X] T037 [P] [US3] Unit test: Trends action calculates success rate trend in `.specify/scripts/powershell/agent-metrics.tests.ps1`
- [X] T038 [P] [US3] Unit test: Trends action generates insights for concerning trends in `.specify/scripts/powershell/agent-metrics.tests.ps1`
- [X] T039 [P] [US3] Unit test: Trends action ignores legacy schema archives in `.specify/scripts/powershell/agent-metrics.tests.ps1`

### Implementation for User Story 3

- [X] T040 [US3] Implement Trends action with -Days and -FeatureBranch parameters in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T041 [US3] Implement archive loading and filtering logic for Trends in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T042 [US3] Implement trend calculation algorithms (token, success, duration) in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T043 [US3] Implement insight generation (warnings, positives) in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T044 [US3] Implement Trends report formatting with colored output in `.specify/scripts/powershell/agent-metrics.ps1`

**Checkpoint**: US3 complete - trend analysis available for archived sessions

---

## Phase 6: User Story 4 - Understand Token Efficiency Per Task Type (Priority: P2)

**Goal**: Parallelization metrics and session comparison capabilities

**Independent Test**: Record parallel invocations, generate report with parallelization metrics showing efficiency percentage

### Tests for User Story 4 (MANDATORY - Constitution Article III)

> **TDD REQUIRED: Write tests FIRST, verify they FAIL (RED), then implement until GREEN**

- [X] T045 [P] [US4] Unit test: Record action captures parallel execution fields in `.specify/scripts/powershell/agent-metrics.tests.ps1`
- [X] T046 [P] [US4] Unit test: Record action updates parallel group metrics in `.specify/scripts/powershell/agent-metrics.tests.ps1`
- [X] T047 [P] [US4] Unit test: Report action displays parallelization metrics section in `.specify/scripts/powershell/agent-metrics.tests.ps1`
- [X] T048 [P] [US4] Unit test: Compare action loads two sessions from archive in `.specify/scripts/powershell/agent-metrics.tests.ps1`
- [X] T049 [P] [US4] Unit test: Compare action calculates deltas between sessions in `.specify/scripts/powershell/agent-metrics.tests.ps1`
- [X] T050 [P] [US4] Unit test: Compare action rejects legacy schema sessions in `.specify/scripts/powershell/agent-metrics.tests.ps1`

### Implementation for User Story 4

- [X] T051 [US4] Enhance Record action with -IsParallel, -ParallelGroupId, -GroupSize parameters in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T052 [US4] Implement parallel group aggregate updates in Record action in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T053 [US4] Enhance Report action with PARALLELIZATION METRICS section in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T054 [US4] Implement parallelization efficiency calculation in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T055 [US4] Implement Compare action with -Session1 and -Session2 parameters in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T056 [US4] Implement session loading by filename, timestamp, or date in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T057 [US4] Implement delta calculation and comparison report formatting in `.specify/scripts/powershell/agent-metrics.ps1`

**Checkpoint**: US4 complete - parallelization tracking and session comparison functional

---

## Phase 7: User Story 5 - Export Metrics for External Analysis (Priority: P3)

**Goal**: CSV/JSON export and cumulative cross-phase reporting

**Independent Test**: Export to CSV, open in spreadsheet; run Cumulative action for feature branch

### Tests for User Story 5 (MANDATORY - Constitution Article III)

> **TDD REQUIRED: Write tests FIRST, verify they FAIL (RED), then implement until GREEN**

- [X] T058 [P] [US5] Unit test: Export action generates valid CSV with correct columns in `.specify/scripts/powershell/agent-metrics.tests.ps1`
- [X] T059 [P] [US5] Unit test: Export action generates valid JSON format in `.specify/scripts/powershell/agent-metrics.tests.ps1`
- [X] T060 [P] [US5] Unit test: Export action includes archives when -IncludeArchives specified in `.specify/scripts/powershell/agent-metrics.tests.ps1`
- [X] T061 [P] [US5] Unit test: Cumulative action aggregates all sessions for feature branch in `.specify/scripts/powershell/agent-metrics.tests.ps1`
- [X] T062 [P] [US5] Unit test: Cumulative action displays phase breakdown in `.specify/scripts/powershell/agent-metrics.tests.ps1`

### Implementation for User Story 5

- [X] T063 [US5] Implement Export action with -Format parameter (CSV, JSON) in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T064 [US5] Implement CSV export with standardized columns per contract in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T065 [US5] Implement JSON export with formatted array structure in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T066 [US5] Implement -IncludeArchives and -FeatureBranch filtering for Export in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T067 [US5] Implement Cumulative action with -FeatureBranch parameter in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T068 [US5] Implement cross-phase aggregation logic for Cumulative in `.specify/scripts/powershell/agent-metrics.ps1`
- [X] T069 [US5] Implement Cumulative report formatting with phase breakdown in `.specify/scripts/powershell/agent-metrics.ps1`

**Checkpoint**: US5 complete - export and cumulative reporting functional

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, edge cases, and performance verification

- [X] T070 [P] Verify all 8 actions pass Pester tests in `.specify/scripts/powershell/agent-metrics.tests.ps1`
- [X] T071 [P] Verify performance: Report generation <2 seconds with 100+ invocations
- [X] T072 [P] Verify edge case handling per FR-010 (missing data gracefully handled)
- [X] T073 Run quickstart.md validation scenarios in `specs/001-agent-metrics-enhancement/quickstart.md`
- [X] T074 [P] Verify colored console output works correctly on Windows PowerShell
- [X] T075 Final code cleanup and inline documentation

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-7)**: All depend on Foundational phase completion
  - US1 and US2 are P1 priority and should be completed first (can run in parallel)
  - US3 and US4 are P2 priority (can run in parallel after US1/US2)
  - US5 is P3 priority (can start after US1/US2)
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - Benefits from US1 being complete for model data
- **User Story 4 (P2)**: Can start after Foundational (Phase 2) - Benefits from US1/US2 for parallel data
- **User Story 5 (P3)**: Can start after Foundational (Phase 2) - Benefits from US3/US4 for export completeness

### Within Each User Story

- Tests MUST be written and FAIL before implementation (TDD)
- Unit tests before action implementation
- Core action implementation before report formatting
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel
- Once Foundational phase completes:
  - US1 and US2 can run in parallel (different concerns)
  - US3 and US4 can run in parallel (after US1/US2)
- All tests for a user story marked [P] can run in parallel
- Spec-Kit command modifications (T027-T033) can run in parallel

---

## Parallel Example: User Story 1

```bash
# Launch all tests for US1 together:
Task: "Unit test: Init action creates session with v2.0.0 schema"
Task: "Unit test: Record action captures model parameter"
Task: "Unit test: Record action updates per-model aggregates"
Task: "Unit test: Report action calculates cost distribution"
Task: "Unit test: Report action displays model distribution section"
```

## Parallel Example: User Story 2 Command Integration

```bash
# Launch all Spec-Kit command modifications together:
Task: "Add metrics integration to speckit.specify.md"
Task: "Add metrics integration to speckit.clarify.md"
Task: "Add metrics integration to speckit.plan.md"
Task: "Add metrics integration to speckit.tasks.md"
Task: "Add metrics integration to speckit.checklist.md"
Task: "Add metrics integration to speckit.analyze.md"
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2 Only)

1. Complete Phase 1: Setup (test infrastructure, settings)
2. Complete Phase 2: Foundational (schema v2.0.0, validation)
3. Complete Phase 3: User Story 1 (model tracking, cost distribution)
4. Complete Phase 4: User Story 2 (all phases, category tracking)
5. **STOP and VALIDATE**:
   - Run any Spec-Kit phase, verify metrics captured with model/phase/category
   - Generate report, verify MODEL DISTRIBUTION and CATEGORY BREAKDOWN sections
6. Deploy/demo if ready - core functionality complete

### Incremental Delivery

1. Setup + Foundational -> Schema v2.0.0 ready
2. Add User Story 1 -> Model tracking works -> Test independently
3. Add User Story 2 -> All phases track metrics -> Test independently (MVP!)
4. Add User Story 3 -> Trends analysis available -> Test independently
5. Add User Story 4 -> Parallelization metrics + Compare -> Test independently
6. Add User Story 5 -> Export + Cumulative -> Test independently
7. Each story adds value without breaking previous stories

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- All tasks modify `.specify/scripts/powershell/agent-metrics.ps1` except where noted
- Tests are in single file `.specify/scripts/powershell/agent-metrics.tests.ps1` organized by Context blocks
