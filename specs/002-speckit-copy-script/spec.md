# Feature Specification: Spec-Kit Project Scaffolding Script

**Feature Branch**: `002-speckit-copy-script`
**Created**: 2026-01-27
**Status**: Draft
**Input**: User description: "I would like a powershell script that automates this process of copying the files from the analysis above, and places them in a destination path. I would like the powershell script to be professional, robust and intelligent. Use your imagination on features and facilities within the powershell script that will make this powershell script highly impressive and easy to use."

## Clarifications

### Session 2026-01-27

- Q: Should the source path default to the repo containing the script itself, so the user only needs to provide the destination? → A: Yes — source defaults to the repo containing the script; `-Source` is an optional override.
- Q: How strict should source validation be (FR-014)? → A: Moderate — require key directories (`.claude/`, `.specify/`, `CLAUDE.md`) to exist; warn about missing files within them.
- Q: What output formatting style should the script use? → A: Rich console — color-coded sections, Unicode symbols (checkmarks/warnings), grouped by tier.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Copy Spec-Kit Framework to a New Project (Priority: P1)

A developer has an existing project directory and wants to bootstrap it with the full Spec-Kit framework (Claude Code configuration, commands, agents, skills, templates, and scripts). They run a single PowerShell command from within the source Spec-Kit project, provide only the destination path, and the script copies all reusable framework files while skipping project-specific artifacts. The source defaults to the repository containing the script itself.

**Why this priority**: This is the core purpose of the tool. Without this capability, the script has no value. It enables the primary workflow of reusing the Spec-Kit infrastructure across projects.

**Independent Test**: Can be tested by running the script with a valid source and destination path, then verifying all expected framework files exist at the destination with correct directory structure.

**Acceptance Scenarios**:

1. **Given** a source project containing the Spec-Kit framework and an empty destination directory, **When** the user runs the script with both paths, **Then** all Tier 1 (framework) and Tier 2 (core agents) files are copied to the destination preserving directory structure.
2. **Given** a source project containing domain-specific agents (e.g., breezsdk-*, umbraco-*), **When** the script runs with default settings, **Then** domain-specific agents, skills, and project-specific files are NOT copied.
3. **Given** a destination path that does not exist, **When** the user runs the script, **Then** the script creates the destination directory and all required subdirectories before copying.
4. **Given** a source path that does not contain the expected Spec-Kit structure, **When** the user runs the script, **Then** the script reports a clear error explaining what is missing and exits gracefully.

---

### User Story 2 - Preview Mode / Dry Run (Priority: P2)

A developer wants to see what would be copied before committing to the operation. They run the script with a preview flag and receive a detailed manifest of files that would be copied, directories that would be created, and files that would be skipped.

**Why this priority**: Provides confidence and transparency before modifying the filesystem. Prevents mistakes and builds trust in the tool.

**Independent Test**: Can be tested by running the script in preview mode and verifying it produces output listing all planned operations without creating or modifying any files.

**Acceptance Scenarios**:

1. **Given** valid source and destination paths, **When** the user runs the script with the preview flag, **Then** the script outputs a categorized list of files to be copied, directories to be created, and files to be skipped, without making any filesystem changes.
2. **Given** the preview output, **When** the user reviews it, **Then** each entry shows source path, destination path, and the reason for inclusion or exclusion (e.g., "core agent", "domain-specific - excluded").

---

### User Story 3 - Include Domain-Specific Agents Selectively (Priority: P2)

A developer's new project also uses one of the domain-specific frameworks (e.g., Umbraco or BreezSDK) and wants to include those domain agents and skills alongside the core framework. They specify which domain modules to include using a parameter.

**Why this priority**: Increases the script's flexibility and usefulness across different project types without requiring manual post-copy steps for known domains.

**Independent Test**: Can be tested by running the script with a domain inclusion flag and verifying that domain-specific agents and skills for the selected domain are present in the destination.

**Acceptance Scenarios**:

1. **Given** a source project with Umbraco domain agents, **When** the user runs the script specifying the Umbraco domain module, **Then** all `umbraco-*.md` agents and related skills are copied in addition to the core framework.
2. **Given** the user specifies multiple domain modules, **When** the script runs, **Then** all specified domain agents and skills are included.
3. **Given** the user specifies a domain module that does not exist in the source, **When** the script runs, **Then** the script warns the user and continues copying the core framework.

---

### User Story 4 - Post-Copy Initialization Guidance (Priority: P3)

After the copy completes, the developer needs to know what project-specific files to create and what steps to take next. The script outputs a summary of what was copied and a clear checklist of next steps.

**Why this priority**: Reduces cognitive load and ensures the developer does not miss critical post-setup steps. Complements the copy operation with actionable guidance.

**Independent Test**: Can be tested by running the script and verifying the post-copy output includes a numbered checklist referencing required files (CLAUDE.project.md, constitution, settings.local.json).

**Acceptance Scenarios**:

1. **Given** a successful copy operation, **When** the script completes, **Then** it outputs a summary showing: count of files copied, count of directories created, and a numbered checklist of required next steps.
2. **Given** the next-steps checklist, **When** the developer follows each step, **Then** the new project is fully configured for Spec-Kit usage.

---

### User Story 5 - Force Overwrite Existing Files (Priority: P3)

A developer has previously scaffolded a project and wants to update it with the latest framework files from the source. By default, the script warns about existing files and skips them. With a force flag, existing files are overwritten.

**Why this priority**: Supports the upgrade/update workflow which is a natural follow-on to initial scaffolding.

**Independent Test**: Can be tested by running the script twice against the same destination, first without force (verifying skip behavior) and then with force (verifying overwrite behavior).

**Acceptance Scenarios**:

1. **Given** a destination that already contains some framework files, **When** the user runs the script without the force flag, **Then** the script lists conflicting files, skips them, and reports how many were skipped.
2. **Given** the same scenario, **When** the user runs the script with the force flag, **Then** existing files are overwritten and the script reports how many files were updated.
3. **Given** a destination with files that do not exist in the source (custom project files), **When** the script runs with force, **Then** those custom files are NOT deleted or modified.

---

### Edge Cases

- What happens when the source and destination paths are the same? The script detects this and exits with a clear error.
- What happens when the user lacks write permissions on the destination? The script detects the permission error and reports it clearly.
- What happens when the source is missing some expected framework files (partial installation)? The script copies what is available and warns about missing files.
- What happens when paths contain spaces or special characters? The script handles these correctly using proper path quoting.
- What happens when run on a non-Windows platform? The script uses PowerShell 7+ compatible syntax and handles path separator differences.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Script MUST default the source path to the repository containing the script itself; an optional `-Source` parameter allows override.
- **FR-002**: Script MUST accept a destination path parameter for the target project.
- **FR-003**: Script MUST copy all Tier 1 framework files (commands, skills, settings, hooks, templates, scripts, CLAUDE.md) preserving directory structure.
- **FR-004**: Script MUST copy all Tier 2 core agent files (7 technology-agnostic agents).
- **FR-005**: Script MUST skip project-specific files by default (domain agents, domain skills, constitution, metrics, plans, specs, CLAUDE.project.md, settings.local.json).
- **FR-006**: Script MUST create all required destination directories if they do not exist.
- **FR-007**: Script MUST create empty placeholder directories for generated content (.specify/memory, .specify/metrics, .specify/plans, specs).
- **FR-008**: Script MUST support a preview/dry-run mode that shows planned operations without executing them.
- **FR-009**: Script MUST support an optional parameter to include specific domain modules (e.g., `-IncludeDomains umbraco,breezsdk`).
- **FR-010**: Script MUST detect and auto-discover available domain modules from the source project.
- **FR-011**: Script MUST warn about and skip existing files at the destination by default.
- **FR-012**: Script MUST support a force flag to overwrite existing files.
- **FR-013**: Script MUST output a post-copy summary with file counts and a next-steps checklist.
- **FR-014**: Script MUST validate the source path contains key directories (`.claude/`, `.specify/`) and `CLAUDE.md`; warn about missing files within those directories rather than failing.
- **FR-015**: Script MUST detect when source and destination are the same path and exit with an error.
- **FR-016**: Script MUST use PowerShell 7+ compatible syntax for cross-platform support.
- **FR-017**: Script MUST support `-Verbose` for detailed operation logging.
- **FR-018**: Script MUST return a structured exit code (0 for success, non-zero for errors).
- **FR-019**: Script MUST support a `-PassThru` switch that returns a summary object for pipeline usage.
- **FR-020**: Script MUST use rich console output by default: color-coded sections, Unicode symbols (checkmarks, warnings, arrows), and file listings grouped by tier.

### Key Entities

- **File Manifest**: The categorized list of all files eligible for copying, grouped by tier (Tier 1 Framework, Tier 2 Core Agents, Domain Modules) with inclusion/exclusion status.
- **Domain Module**: A named group of related domain-specific agents and skills (e.g., "umbraco" maps to `umbraco-*.md` agents and related skills).
- **Copy Operation Result**: The outcome of the script execution, including counts of files copied, skipped, directories created, and warnings encountered.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can scaffold a new project with the full Spec-Kit framework in a single command invocation.
- **SC-002**: The script completes the copy operation for a typical framework (approximately 30 files) without errors on Windows, macOS, and Linux.
- **SC-003**: Preview mode accurately reflects 100% of the operations that would occur in a real run.
- **SC-004**: The post-copy checklist covers all required manual steps, enabling a developer to have a fully functional Spec-Kit project after following it.
- **SC-005**: Existing destination files are never overwritten unless the user explicitly opts in via the force flag.
- **SC-006**: The script provides clear, actionable error messages for all failure scenarios (invalid paths, missing source structure, permission errors).

## Assumptions

- The source project follows the standard Spec-Kit directory layout as documented in the analysis (`.claude/`, `.specify/`, `CLAUDE.md` at root).
- Domain modules are identified by filename prefix patterns (e.g., `umbraco-*.md`, `breezsdk-*.md`) in the agents and skills directories.
- The script does not need to handle version migration between different Spec-Kit framework versions. It performs a direct file copy.
- PowerShell 7+ is available on the target machine (ships with modern Windows and is installable on macOS/Linux).
- The script will be placed in the `.specify/scripts/powershell/` directory of the source project for discoverability.
