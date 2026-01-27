#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates Tier 1 core files for domain-specific terminology violations.

.DESCRIPTION
    Scans all Tier 1 core SpecKit files for domain-specific terms (BreezSDK, Umbraco, Lightning Network).
    These files should be generic and reusable across projects.

.PARAMETER RepoRoot
    Repository root directory. Defaults to script's grandparent directory.

.EXAMPLE
    .\validate-tier1.ps1
    .\validate-tier1.ps1 -RepoRoot "C:\Work\ClaudeCode\Spikes\TestingAgents"

.NOTES
    Exit Code 0: No violations found (PASS)
    Exit Code 1: Violations found (FAIL)
#>

param(
    [string]$RepoRoot = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent)
)

# Domain-specific terms to search for (case-insensitive)
$DomainTerms = @(
    'BreezSDK',
    'breez',
    'breezsdk',
    'Umbraco',
    'umbraco',
    'Lightning'  # In context of Lightning Network
)

# Tier 1 files to scan (relative to repo root)
$Tier1Files = @(
    'CLAUDE.md',
    '.claude/settings.json',
    '.claude/settings.local.json',
    '.claude/hooks.json',
    '.claude/commands/speckit.implement.md',
    '.claude/agents/solution-architect.md',
    '.claude/agents/backend-developer.md',
    '.claude/agents/frontend-developer.md',
    '.claude/agents/test-engineer.md',
    '.claude/agents/database-architect.md',
    '.claude/agents/security-auditor.md',
    '.claude/agents/code-reviewer.md',
    '.claude/skills/implementation-execution.md',
    '.claude/skills/implementation-execution.md',  # Alternative name after rename
    '.claude/skills/external-plugins.md',
    '.specify/memory/constitution.md',
    '.specify/templates/plan.md',
    '.specify/templates/tasks.md',
    '.specify/templates/implement.md'
)

# ANSI color codes
$Red = "`e[91m"
$Yellow = "`e[93m"
$Green = "`e[92m"
$Cyan = "`e[96m"
$Reset = "`e[0m"

Write-Host "${Cyan}========================================${Reset}"
Write-Host "${Cyan}Tier 1 Domain-Specific Term Validator${Reset}"
Write-Host "${Cyan}========================================${Reset}"
Write-Host ""
Write-Host "Repository Root: $RepoRoot"
Write-Host ""

# Validation counters
$filesScanned = 0
$filesMissing = 0
$totalViolations = 0
$violationsByFile = @{}

# Process each Tier 1 file
foreach ($relPath in $Tier1Files) {
    $fullPath = Join-Path $RepoRoot $relPath

    if (-not (Test-Path $fullPath)) {
        Write-Host "${Yellow}[WARN]${Reset} File not found (may have been renamed): $relPath"
        $filesMissing++
        continue
    }

    $filesScanned++
    $content = Get-Content $fullPath -Raw -ErrorAction SilentlyContinue

    if (-not $content) {
        continue
    }

    # Split into lines for line number reporting
    $lines = $content -split "`r?`n"
    $fileViolations = @()

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $lineNum = $i + 1
        $line = $lines[$i]

        foreach ($term in $DomainTerms) {
            # Case-insensitive search
            if ($line -match "(?i)$term") {
                # Exclude false positives:
                # 1. References to validation scripts (specs/001-generic-speckit-reuse/validate-tier1.ps1)
                # 2. File paths containing 'specs/' or validation script names
                # 3. References to spec directories
                if ($line -match 'specs[\\/]' -or
                    $line -match 'validate-tier\d+\.ps1' -or
                    $line -match '001-generic-speckit-reuse') {
                    continue
                }

                # Special handling for 'breez' - exclude if it's part of a filename/path
                if ($term -eq 'breez' -and $line -match '[\\/]breez') {
                    continue
                }

                $fileViolations += [PSCustomObject]@{
                    LineNumber = $lineNum
                    Term = $term
                    Line = $line.Trim()
                }
                $totalViolations++
            }
        }
    }

    if ($fileViolations.Count -gt 0) {
        $violationsByFile[$relPath] = $fileViolations
    }
}

# Report violations
if ($totalViolations -gt 0) {
    Write-Host "${Red}[FAIL]${Reset} Found $totalViolations domain-specific term violation(s) in Tier 1 files:"
    Write-Host ""

    foreach ($file in $violationsByFile.Keys | Sort-Object) {
        Write-Host "${Red}File:${Reset} $file"

        foreach ($violation in $violationsByFile[$file]) {
            Write-Host "  ${Yellow}Line $($violation.LineNumber):${Reset} Found '$($violation.Term)'"
            Write-Host "    ${Cyan}$($violation.Line)${Reset}"
        }
        Write-Host ""
    }
} else {
    Write-Host "${Green}[PASS]${Reset} No domain-specific terms found in Tier 1 files."
    Write-Host ""
}

# Summary
Write-Host "${Cyan}========================================${Reset}"
Write-Host "${Cyan}Summary${Reset}"
Write-Host "${Cyan}========================================${Reset}"
Write-Host "Files scanned:     $filesScanned"
Write-Host "Files missing:     $filesMissing"
Write-Host "Total violations:  $totalViolations"
Write-Host ""

if ($totalViolations -eq 0) {
    Write-Host "${Green}RESULT: PASS${Reset}"
    exit 0
} else {
    Write-Host "${Red}RESULT: FAIL${Reset}"
    Write-Host ""
    Write-Host "Tier 1 files must be domain-agnostic and reusable across projects."
    Write-Host "Move domain-specific content to Tier 2 (.claude/project.config.md) or Tier 3 (spec files)."
    exit 1
}
