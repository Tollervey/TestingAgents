#Requires -Modules Pester

<#
.SYNOPSIS
    Pester tests for Copy-SpecKit.ps1

.DESCRIPTION
    Test suite for the Spec-Kit Project Scaffolding Script.
    Organized by user story with foundational function tests first.
    Uses Pester 5 with TestDrive:\ for filesystem isolation.

.NOTES
    Run with: Invoke-Pester -Path .\Copy-SpecKit.tests.ps1 -Output Detailed
#>

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Dot-source the script to import all functions
. "$ScriptDir\Copy-SpecKit.ps1"

# --- Helper: Create a mock Spec-Kit source structure in TestDrive ---
function New-MockSpecKitSource {
    param(
        [string]$Root = (Join-Path $TestDrive 'source')
    )

    # Create required directories
    $dirs = @(
        '.claude/commands'
        '.claude/agents'
        '.claude/skills'
        '.specify/templates'
        '.specify/scripts/powershell'
    )
    foreach ($dir in $dirs) {
        $fullDir = Join-Path $Root $dir
        New-Item -ItemType Directory -Path $fullDir -Force | Out-Null
    }

    # Create CLAUDE.md
    Set-Content -Path (Join-Path $Root 'CLAUDE.md') -Value '# CLAUDE.md'

    # Create framework files
    Set-Content -Path (Join-Path $Root '.claude/settings.json') -Value '{}'
    Set-Content -Path (Join-Path $Root '.claude/hooks.json') -Value '{}'
    Set-Content -Path (Join-Path $Root '.claude/QUICK-REFERENCE.md') -Value '# Quick Reference'

    # Create command files
    @('speckit.plan.md', 'speckit.specify.md', 'speckit.implement.md') | ForEach-Object {
        Set-Content -Path (Join-Path $Root ".claude/commands/$_") -Value "# $_"
    }

    # Create core agent files
    @('backend-developer.md', 'code-reviewer.md', 'database-architect.md',
      'frontend-developer.md', 'security-auditor.md', 'solution-architect.md',
      'test-engineer.md') | ForEach-Object {
        Set-Content -Path (Join-Path $Root ".claude/agents/$_") -Value "# $_"
    }

    # Create domain agent files
    @('umbraco-architect.md', 'umbraco-backend-developer.md', 'umbraco-frontend-developer.md') | ForEach-Object {
        Set-Content -Path (Join-Path $Root ".claude/agents/$_") -Value "# $_"
    }
    @('breezsdk-architect.md', 'breezsdk-developer.md') | ForEach-Object {
        Set-Content -Path (Join-Path $Root ".claude/agents/$_") -Value "# $_"
    }

    # Create core skill files
    @('external-plugins.md', 'implementation-execution.md') | ForEach-Object {
        Set-Content -Path (Join-Path $Root ".claude/skills/$_") -Value "# $_"
    }

    # Create domain skill file
    Set-Content -Path (Join-Path $Root '.claude/skills/breezsdk-knowledge.md') -Value '# breezsdk-knowledge'

    # Create template files
    @('spec-template.md', 'plan-template.md', 'tasks-template.md') | ForEach-Object {
        Set-Content -Path (Join-Path $Root ".specify/templates/$_") -Value "# $_"
    }

    # Create script files
    Set-Content -Path (Join-Path $Root '.specify/scripts/powershell/common.ps1') -Value '# common'

    return $Root
}

Describe 'Copy-SpecKit' {

    #region Phase 2: Foundational Functions

    Context 'Test-SpecKitSource' {
        # T003: Source path validation tests
    }

    Context 'Build-FileManifest' {
        # T004: File manifest building tests
    }

    Context 'Get-DomainModules' {
        # T005: Domain module auto-discovery tests
    }

    #endregion

    #region Phase 3: User Story 1 - Copy Framework to New Project

    Context 'US1: Core copy operation' {
        # T010: All Tier 1 files copied, Tier 2 core agents copied, domain files excluded
    }

    Context 'US1: Placeholder directory creation' {
        # T011: .specify/memory/, .specify/metrics/, .specify/plans/, specs/ created empty
    }

    Context 'US1: Error handling' {
        # T012: Source not found, missing structure exit codes
    }

    #endregion

    #region Phase 4: User Story 2 - Preview / Dry Run

    Context 'US2: Preview mode' {
        # T017: No filesystem changes, output lists files by tier
    }

    #endregion

    #region Phase 5: User Story 3 - Include Domain Modules

    Context 'US3: Domain inclusion' {
        # T020: -IncludeDomains copies domain agents, unknown domain warns
    }

    #endregion

    #region Phase 6: User Story 4 - Post-Copy Guidance

    Context 'US4: Post-copy output' {
        # T024: Summary with file counts and next-steps checklist
    }

    #endregion

    #region Phase 7: User Story 5 - Force Overwrite

    Context 'US5: Conflict handling' {
        # T027: Skip without -Force, overwrite with -Force, custom files untouched
    }

    #endregion

    #region Phase 8: Polish

    Context 'PassThru output' {
        # T031: Returns PSCustomObject with CopyResult fields
    }

    Context 'Edge cases' {
        # T032: Paths with spaces, Join-Path usage, verbose logging, permissions
    }

    #endregion
}
