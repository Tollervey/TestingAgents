---
description: Execute the implementation plan by processing and executing all tasks defined in tasks.md
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Outline

1. Run `.specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks` from repo root and parse FEATURE_DIR and AVAILABLE_DOCS list. All paths must be absolute. For single quotes in args like "I'm Groot", use escape syntax: e.g 'I'\''m Groot' (or double-quote if possible: "I'm Groot").

1a. **Verify plugin availability** (optional but recommended):
   - Run `/plugins` to list installed plugins
   - Check if recommended plugins for implementation are available:
     - `superpowers` — TDD workflow, debugging
     - `dotnet-claude-code-skills` — DDD patterns, EF Core (for .NET projects)
   - If plugins are missing, inform user:
     ```
     ⚠️ Recommended plugins not installed:
     - superpowers: `/plugin marketplace add obra/superpowers-marketplace`
     - dotnet-claude-code-skills: `/plugin marketplace add anthropics/dotnet-claude-code-skills`
     
     Continue without plugins? (yes/no)
     ```
   - If user says "no", halt and let them install plugins first
   - If user says "yes" or plugins are installed, proceed to step 2

2. **Check checklists status** (if FEATURE_DIR/checklists/ exists):
   - Scan all checklist files in the checklists/ directory
   - For each checklist, count:
     - Total items: All lines matching `- [ ]` or `- [X]` or `- [x]`
     - Completed items: Lines matching `- [X]` or `- [x]`
     - Incomplete items: Lines matching `- [ ]`
   - Create a status table:

     ```text
     | Checklist | Total | Completed | Incomplete | Status |
     |-----------|-------|-----------|------------|--------|
     | ux.md     | 12    | 12        | 0          | ✓ PASS |
     | test.md   | 8     | 5         | 3          | ✗ FAIL |
     | security.md | 6   | 6         | 0          | ✓ PASS |
     ```

   - Calculate overall status:
     - **PASS**: All checklists have 0 incomplete items
     - **FAIL**: One or more checklists have incomplete items

   - **If any checklist is incomplete**:
     - Display the table with incomplete item counts
     - **STOP** and ask: "Some checklists are incomplete. Do you want to proceed with implementation anyway? (yes/no)"
     - Wait for user response before continuing
     - If user says "no" or "wait" or "stop", halt execution
     - If user says "yes" or "proceed" or "continue", proceed to step 3

   - **If all checklists are complete**:
     - Display the table showing all checklists passed
     - Automatically proceed to step 3

3. Load and analyze the implementation context:
   - **REQUIRED**: Read tasks.md for the complete task list and execution plan
   - **REQUIRED**: Read plan.md for tech stack, architecture, and file structure
   - **IF EXISTS**: Read data-model.md for entities and relationships
   - **IF EXISTS**: Read contracts/ for API specifications and test requirements
   - **IF EXISTS**: Read research.md for technical decisions and constraints
   - **IF EXISTS**: Read quickstart.md for integration scenarios

4. **Project Setup Verification**:
   - **REQUIRED**: Create/verify ignore files based on actual project setup:

   **Detection & Creation Logic**:
   - Check if the following command succeeds to determine if the repository is a git repo (create/verify .gitignore if so):

     ```sh
     git rev-parse --git-dir 2>/dev/null
     ```

   - Check if Dockerfile* exists or Docker in plan.md → create/verify .dockerignore
   - Check if .eslintrc* exists → create/verify .eslintignore
   - Check if eslint.config.* exists → ensure the config's `ignores` entries cover required patterns
   - Check if .prettierrc* exists → create/verify .prettierignore
   - Check if .npmrc or package.json exists → create/verify .npmignore (if publishing)
   - Check if terraform files (*.tf) exist → create/verify .terraformignore
   - Check if .helmignore needed (helm charts present) → create/verify .helmignore

   **If ignore file already exists**: Verify it contains essential patterns, append missing critical patterns only
   **If ignore file missing**: Create with full pattern set for detected technology

   **Common Patterns by Technology** (from plan.md tech stack):
   - **Node.js/JavaScript/TypeScript**: `node_modules/`, `dist/`, `build/`, `*.log`, `.env*`
   - **Python**: `__pycache__/`, `*.pyc`, `.venv/`, `venv/`, `dist/`, `*.egg-info/`
   - **Java**: `target/`, `*.class`, `*.jar`, `.gradle/`, `build/`
   - **C#/.NET**: `bin/`, `obj/`, `*.user`, `*.suo`, `packages/`
   - **Go**: `*.exe`, `*.test`, `vendor/`, `*.out`
   - **Ruby**: `.bundle/`, `log/`, `tmp/`, `*.gem`, `vendor/bundle/`
   - **PHP**: `vendor/`, `*.log`, `*.cache`, `*.env`
   - **Rust**: `target/`, `debug/`, `release/`, `*.rs.bk`, `*.rlib`, `*.prof*`, `.idea/`, `*.log`, `.env*`
   - **Kotlin**: `build/`, `out/`, `.gradle/`, `.idea/`, `*.class`, `*.jar`, `*.iml`, `*.log`, `.env*`
   - **C++**: `build/`, `bin/`, `obj/`, `out/`, `*.o`, `*.so`, `*.a`, `*.exe`, `*.dll`, `.idea/`, `*.log`, `.env*`
   - **C**: `build/`, `bin/`, `obj/`, `out/`, `*.o`, `*.a`, `*.so`, `*.exe`, `Makefile`, `config.log`, `.idea/`, `*.log`, `.env*`
   - **Swift**: `.build/`, `DerivedData/`, `*.swiftpm/`, `Packages/`
   - **R**: `.Rproj.user/`, `.Rhistory`, `.RData`, `.Ruserdata`, `*.Rproj`, `packrat/`, `renv/`
   - **Universal**: `.DS_Store`, `Thumbs.db`, `*.tmp`, `*.swp`, `.vscode/`, `.idea/`

   **Tool-Specific Patterns**:
   - **Docker**: `node_modules/`, `.git/`, `Dockerfile*`, `.dockerignore`, `*.log*`, `.env*`, `coverage/`
   - **ESLint**: `node_modules/`, `dist/`, `build/`, `coverage/`, `*.min.js`
   - **Prettier**: `node_modules/`, `dist/`, `build/`, `coverage/`, `package-lock.json`, `yarn.lock`, `pnpm-lock.yaml`
   - **Terraform**: `.terraform/`, `*.tfstate*`, `*.tfvars`, `.terraform.lock.hcl`
   - **Kubernetes/k8s**: `*.secret.yaml`, `secrets/`, `.kube/`, `kubeconfig*`, `*.key`, `*.crt`

5. Parse tasks.md structure and extract:
   - **Task phases**: Setup, Tests, Core, Integration, Polish
   - **Task dependencies**: Sequential vs parallel execution rules
   - **Task details**: ID, description, file paths, parallel markers [P]
   - **Execution flow**: Order and dependency requirements

6. **Checkpoint Strategy** (CRITICAL for recovery):

   **When to Create Checkpoints**:
   - **BEFORE each wave starts**: Create checkpoint with wave identifier
   - **AFTER completing Setup phase**: Baseline checkpoint before domain work
   - **Before destructive operations**: Database migrations, file deletions
   - **When implementation is in known good state**: Tests passing, build succeeding

   **Checkpoint Commands**:
   ```
   # Create checkpoint before Wave 2
   Create a checkpoint - Wave 2 starting, Setup phase complete, all tests passing
   
   # Recovery if needed
   /rewind
   ```

   **Checkpoint Naming Convention**:
   - `Wave-N-start`: Before beginning wave N
   - `Phase-complete-[name]`: After completing a major phase
   - `Pre-migration-[name]`: Before database migrations
   - `Known-good-[description]`: Stable state with passing tests

7. **Wave-Based Execution** (from CLAUDE.md):

   **Agent Recommendations by Wave**:

   | Wave | Primary Agent(s) | Supporting Agent(s) | When to Use |
   |------|-----------------|---------------------|-------------|
   | Wave 1 (Infrastructure) | `backend-developer`, `database-architect` | — | Project scaffolding, config, migrations |
   | Wave 2+ (Domain) | `backend-developer`, `test-engineer` | `frontend-developer` | Entity/service implementation (parallel [P]) |
   | Quality Gate | `code-reviewer`, `security-auditor` | — | After each wave completion |
   | Complex Decisions | `solution-architect` | — | Architecture questions, refactoring decisions |

   **Agent Invocation Examples**:
   ```
   # Infrastructure (Wave 1)
   Use database-architect to create the initial EF Core migrations for User and Order entities
   Use backend-developer to implement the repository interfaces in src/Domain/Interfaces/
   
   # Domain Layer (Wave 2 - parallel)
   & Use test-engineer to write unit tests for OrderService in tests/Unit/OrderServiceTests.cs
   & Use backend-developer to implement the User aggregate in src/Domain/Entities/User.cs
   & Use backend-developer to implement the Product aggregate in src/Domain/Entities/Product.cs
   
   # Quality Gate
   & Use code-reviewer to review Wave 2 code for constitution compliance
   & Use security-auditor to scan for vulnerabilities in authentication code
   
   # Complex Decisions (when needed)
   Use solution-architect to evaluate CQRS vs traditional layered approach for the Order module
   ```

   **TDD Workflow (Non-Negotiable per Constitution Article III)**:
   ```
   # Step 1: RED - Write failing tests FIRST
   Use test-engineer to create unit tests for ProductService including happy path, edge cases, and error conditions
   
   # Step 2: Verify tests fail
   Run: dotnet test --filter "ProductServiceTests"
   Expected: Tests FAIL (no implementation yet)
   
   # Step 3: GREEN - Implement until tests pass
   Use backend-developer to implement ProductService until all tests pass
   
   # Step 4: Verify tests pass
   Run: dotnet test --filter "ProductServiceTests"
   Expected: All tests PASS
   ```

   **Wave 1 (Infrastructure)**: Execute SEQUENTIALLY
   ```
   # Sequential execution with checkpoints
   Implement T001 → checkpoint → T002 → checkpoint → T003
   ```
   - No parallel execution in infrastructure phase
   - Checkpoint after each critical task
   - Verify build passes before proceeding

   **Wave 2+ (Domain/Application)**: Execute `[P]` marked tasks in PARALLEL
   ```
   # Parallel execution using & operator
   & Use backend-developer to implement T004 (User entity)
   & Use backend-developer to implement T005 (Product entity)
   & Use backend-developer to implement T006 (Order entity)
   /tasks  # Monitor progress
   ```
   - Only parallelize tasks marked with [P]
   - Respect `depends_on` constraints
   - Maximum 5 concurrent background agents

   **Quality Gate After Each Wave**:
   ```
   # Run after wave completion
   & Use code-reviewer to verify Wave N code quality
   & Use security-auditor to scan for vulnerabilities
   
   # Verify tests pass
   dotnet test
   
   # Verify build is clean
   dotnet build --warnaserror
   ```

   **Context Management Between Waves**:
   - Run `/compact` if context exceeds 150k tokens
   - Delegate large tasks (>30k tokens) to sub-agents
   - Sub-agents return summaries, not full outputs

8. Execute implementation following the task plan:
   - **Phase-by-phase execution**: Complete each phase before moving to the next
   - **Respect dependencies**: Run sequential tasks in order, parallel tasks [P] can run together  
   - **Follow TDD approach**: Execute test tasks before their corresponding implementation tasks
   - **File-based coordination**: Tasks affecting the same files must run sequentially
   - **Validation checkpoints**: Verify each phase completion before proceeding

9. Implementation execution rules:
   - **Setup first**: Initialize project structure, dependencies, configuration
   - **Tests before code**: Write tests for contracts, entities, and integration scenarios FIRST
   - **Core development**: Implement models, services, CLI commands, endpoints
   - **Integration work**: Database connections, middleware, logging, external services
   - **Polish and validation**: Additional tests, performance optimization, documentation

10. Progress tracking and error handling:
   - Report progress after each completed task
   - Halt execution if any non-parallel task fails
   - For parallel tasks [P], continue with successful tasks, report failed ones
   - Provide clear error messages with context for debugging
   - Suggest next steps if implementation cannot proceed
   - **IMPORTANT** For completed tasks, make sure to mark the task off as [X] in the tasks file.

11. Completion validation:
   - Verify all required tasks are completed
   - Check that implemented features match the original specification
   - Validate that tests pass and coverage meets requirements
   - Confirm the implementation follows the technical plan
   - Report final status with summary of completed work

Note: This command assumes a complete task breakdown exists in tasks.md. If tasks are incomplete or missing, suggest running `/speckit.tasks` first to regenerate the task list.
