---
name: tasks
description: Generate task breakdown with dependency management and parallel execution markers
---

# Tasks Command - Enhanced with Claude Code Parallelization

## Prerequisites
- Read the implementation plan from `/specs/[current-feature]/plan.md`
- Read the constitution from `/memory/constitution.md`
- Load the Claude Code tasks skill (if available)

## Execution Steps

1. **Load Context**
   - Read plan.md for technical architecture
   - Read the Claude Code Execution Architecture section from plan.md
   - Read any available task parallelization skills

2. **Generate Task Breakdown**
   
   Each task MUST include these metadata fields:

   ```markdown
   ### T001: [Task Name]
   
   **Description**: What this task accomplishes
   
   **Execution Metadata**:
   - `depends_on`: [List of task IDs that must complete first, e.g., T000]
   - `blocks`: [List of task IDs that cannot start until this completes]
   - `parallel_group`: [Wave identifier, e.g., "wave-2-domain"]
   - `recommended_agent`: [Which agent handles this, e.g., backend-developer]
   - `model_tier`: [opus|sonnet|haiku]
   - `background_eligible`: [true|false - can run in background?]
   - `estimated_duration`: [short|medium|long]
   
   **Files**:
   - Create: [list of files to create]
   - Modify: [list of files to modify]
   
   **Validation**:
   - [ ] [Specific test or verification]
   ```

3. **Organize into Execution Waves**

   Group tasks into waves based on dependencies:

   ```markdown
   ## Wave 1: Infrastructure [SEQUENTIAL]
   Foundation tasks that all subsequent work depends on.
   - T001: Project scaffolding
   - T002: Database setup
   - T003: Core configuration
   
   ## Wave 2: Domain Layer [PARALLEL]
   Independent domain components.
   - T004: User entity [P] - parallel_group: wave-2
   - T005: Product entity [P] - parallel_group: wave-2
   - T006: Order entity [P] - parallel_group: wave-2
   
   ## Wave 3: Application Layer [PARALLEL, depends on Wave 2]
   - T007: User service [P] - depends_on: T004
   - T008: Product service [P] - depends_on: T005
   - T009: Order service [P] - depends_on: T006
   
   [Continue pattern...]
   ```

4. **Mark Parallel Eligibility**
   
   Tasks marked with `[P]` can execute concurrently.
   
   Criteria for parallel eligibility:
   - No file conflicts with other [P] tasks in same wave
   - No data dependencies on other [P] tasks
   - Can be validated independently

5. **Mark Background Eligibility**
   
   Tasks marked `background_eligible: true` can use Ctrl+B.
   
   Criteria for background eligibility:
   - Expected duration >5 minutes
   - Does not require interactive decisions
   - Produces deterministic output
   - Has clear completion criteria

6. **Dependency Validation**
   
   Before finalizing, verify:
   - No circular dependencies
   - All depends_on references exist
   - Parallel tasks have no conflicts
   - Waves can execute in order

## Output Format

Generate `tasks.md` with:
1. Summary table of all tasks with status columns
2. Wave-organized task details
3. Dependency graph (mermaid diagram)
4. Execution instructions for Claude Code
