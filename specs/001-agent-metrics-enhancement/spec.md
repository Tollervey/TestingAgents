# Feature Specification: Enhanced Agent Metrics System

**Feature Branch**: `001-agent-metrics-enhancement`
**Created**: 2026-01-26
**Status**: Draft
**Input**: User description: "Review and improve agent metrics tracking to include model information, extend metrics to all Spec-Kit phases, and add productivity insights"

## Clarifications

### Session 2026-01-26

- Q: When running multiple Spec-Kit phases in sequence, how should metrics sessions be organized? → A: Each phase starts a fresh session; cumulative cross-phase report available on demand
- Q: How should the enhanced system handle existing metrics files that lack new fields (model, category, phase)? → A: Discard old archives; start fresh with new schema
- Q: How long should archived metrics sessions be retained before automatic cleanup? → A: Configurable via settings file, default 7 days
- Q: How should cost be represented in reports? → A: As overall percentage breakdown by model (relative cost distribution, not dollar amounts)
- Q: Should the system track input and output tokens separately? → A: Total tokens only (simpler, matches current data availability)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Comprehensive Agent Performance Data (Priority: P1)

As a developer using Spec-Kit, I want to see detailed performance metrics for every agent invocation including which AI model was used, so I can understand cost implications and make informed decisions about agent selection.

**Why this priority**: Without knowing which model each agent uses, developers cannot accurately estimate costs or optimize their workflow for efficiency vs. quality trade-offs. This is the core missing data point identified in the review.

**Independent Test**: Can be fully tested by running any Spec-Kit phase that invokes agents and verifying the report includes model information for each invocation.

**Acceptance Scenarios**:

1. **Given** an agent completes a task, **When** metrics are recorded, **Then** the model identifier (e.g., "sonnet", "opus", "haiku") is captured and stored alongside the invocation data
2. **Given** metrics have been collected, **When** the report is generated, **Then** each agent entry shows the model used with associated cost calculations
3. **Given** multiple agents use different models, **When** viewing the summary, **Then** I can see model usage distribution and cost breakdown by model

---

### User Story 2 - Track Metrics Across All Spec-Kit Phases (Priority: P1)

As a developer, I want agent metrics collected during all Spec-Kit phases (not just implementation), so I can understand the total effort and cost across the entire development workflow.

**Why this priority**: Currently only `/speckit.implement` tracks metrics, leaving significant agent usage during planning, analysis, and task generation unmeasured. This is explicitly requested functionality.

**Independent Test**: Can be tested by running any Spec-Kit phase (e.g., `/speckit.plan`, `/speckit.analyze`) and verifying metrics are initialized and collected.

**Acceptance Scenarios**:

1. **Given** I run `/speckit.plan`, **When** agents are invoked during planning, **Then** metrics are recorded with the phase name "plan"
2. **Given** I run `/speckit.analyze`, **When** the analysis completes, **Then** a metrics report shows agent usage during that phase
3. **Given** I run multiple phases in sequence, **When** I view the cumulative report, **Then** I can see metrics broken down by phase

---

### User Story 3 - Analyze Productivity Trends Over Time (Priority: P2)

As a team lead, I want to see productivity metrics and trends across multiple sessions, so I can identify improvements or regressions in development efficiency.

**Why this priority**: Single-session metrics provide limited value for understanding long-term productivity; historical comparison enables continuous improvement.

**Independent Test**: Can be tested by archiving multiple sessions and generating a trends report showing changes over time.

**Acceptance Scenarios**:

1. **Given** I have archived metrics from multiple sessions, **When** I request a trends report, **Then** I see metrics compared across sessions (tokens per task, success rates, duration trends)
2. **Given** metrics show increasing token usage over time, **When** viewing the trends report, **Then** I see this flagged as a potential area for optimization
3. **Given** agent failure rates have improved, **When** viewing the trends report, **Then** positive trends are highlighted

---

### User Story 4 - Understand Token Efficiency Per Task Type (Priority: P2)

As a developer, I want to see metrics grouped by task category (test writing, implementation, review), so I can understand which activities consume the most resources.

**Why this priority**: Different task types have different complexity and token requirements; understanding this helps optimize agent prompts and workflow decisions.

**Independent Test**: Can be tested by recording invocations with different task categories and generating a report grouped by category.

**Acceptance Scenarios**:

1. **Given** agents perform different task types (tests, implementation, review), **When** the report is generated, **Then** metrics are grouped by task category
2. **Given** test-writing tasks consistently use more tokens than expected, **When** viewing the category breakdown, **Then** I can identify opportunities for prompt optimization

---

### User Story 5 - Export Metrics for External Analysis (Priority: P3)

As a project manager, I want to export metrics in multiple formats (JSON, CSV), so I can integrate with external dashboards and reporting tools.

**Why this priority**: Enables integration with existing project management and analytics tools for broader visibility.

**Independent Test**: Can be tested by generating exports and verifying they open correctly in spreadsheet applications and JSON parsers.

**Acceptance Scenarios**:

1. **Given** metrics have been collected, **When** I request a CSV export, **Then** I receive a well-formatted file that opens in spreadsheet applications
2. **Given** metrics have been collected, **When** I request a JSON export, **Then** I receive a structured JSON file suitable for API consumption

---

### Edge Cases

- What happens when an agent invocation provides no token count information? System records "0" or "unknown" and still captures other available metrics without failing.
- How does the system handle interrupted sessions where metrics weren't properly closed? System marks the session as "incomplete" during archival and includes partial data.
- What happens if the metrics directory doesn't exist or lacks write permissions? System provides a clear error message and fails gracefully without crashing the Spec-Kit phase.
- How are concurrent agent invocations tracked to avoid race conditions? Each invocation is uniquely identified by timestamp and task ID; file operations use atomic writes.
- What if the model information is not available from the agent response? System defaults to the configured model for that agent type from CLAUDE.md.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST capture the AI model identifier (sonnet, opus, haiku) for every agent invocation
- **FR-002**: System MUST record agent metrics during ALL Spec-Kit phases: specify, clarify, plan, tasks, checklist, analyze, implement
- **FR-003**: System MUST display cost distribution as percentages by model (e.g., "Opus: 65%, Sonnet: 30%, Haiku: 5%") using relative model cost weights
- **FR-004**: System MUST archive completed session metrics with timestamps for historical analysis
- **FR-005**: System MUST generate trend analysis reports comparing metrics across archived sessions
- **FR-006**: System MUST support exporting metrics to CSV format for spreadsheet analysis
- **FR-007**: System MUST categorize tasks by type (implementation, testing, review, planning, analysis) for grouped reporting
- **FR-008**: System MUST provide efficiency indicators (tokens per minute, cost per task, success rate by model)
- **FR-009**: System MUST track parallelization metrics (concurrent agents count, parallel vs sequential efficiency)
- **FR-010**: System MUST handle missing or incomplete data gracefully without crashing
- **FR-011**: System MUST support a "compare" action to compare two archived sessions side-by-side
- **FR-012**: System MUST display model distribution in summary reports (percentage of invocations per model)
- **FR-013**: System MUST track the Spec-Kit phase name for each invocation for phase-level reporting
- **FR-014**: System MUST store the feature branch name associated with each session for project traceability
- **FR-015**: System MUST provide a "Trends" action that analyzes all archived sessions
- **FR-016**: System MUST start a fresh metrics session for each Spec-Kit phase, archiving the previous session automatically
- **FR-017**: System MUST provide a "Cumulative" action to generate a cross-phase report combining all sessions for a feature branch
- **FR-018**: System MUST NOT attempt to migrate or interpret legacy metrics archives; trend analysis applies only to new-format data
- **FR-019**: System MUST support configurable archive retention period via settings file, defaulting to 7 days
- **FR-020**: System MUST automatically purge archived sessions older than the configured retention period during Reset or Init actions

### Key Entities

- **Invocation**: A single agent execution - timestamp, agent name, model, task ID, status, tokens (total), duration, description, phase, task category
- **Session**: A collection of invocations for a Spec-Kit phase run - phase name, feature branch, start/end times, summary statistics by agent and model
- **Archive**: A stored session with metadata for historical analysis - preserved as timestamped JSON file
- **TrendReport**: An aggregated analysis of multiple archived sessions showing patterns, improvements, and regressions over time

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Developers can identify which AI model was used for any agent invocation within 10 seconds of viewing the report
- **SC-002**: All 7 Spec-Kit phases that can invoke agents (specify, clarify, plan, tasks, checklist, analyze, implement) automatically capture metrics when agents are used
- **SC-003**: Historical trend comparisons are available within 5 seconds when analyzing 10+ archived sessions
- **SC-004**: Cost distribution percentages correctly reflect relative model pricing weights (Opus > Sonnet > Haiku)
- **SC-005**: 95% of users can successfully export metrics to CSV format on first attempt without needing documentation
- **SC-006**: Report generation completes within 2 seconds even with 100+ invocations per session
- **SC-007**: Metric data persists correctly across session restarts and is recoverable from archive files
