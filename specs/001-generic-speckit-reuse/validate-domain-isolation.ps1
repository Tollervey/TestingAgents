#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates domain agent isolation from Tier 1 core configuration files.

.DESCRIPTION
    Verifies that domain-specific agents (breezsdk-*, umbraco-*) are:
    1. Not referenced by any Tier 1 core file
    2. Only documented in CLAUDE.project.md
    3. Not hardcoded in hooks.json
    4. Not present as WebFetch domain permissions in settings.local.json
    5. Not cross-referencing other domain agent families (each family removable independently)

.PARAMETER RepoRoot
    Repository root directory. Defaults to script's grandparent directory.

.EXAMPLE
    .\validate-domain-isolation.ps1
    .\validate-domain-isolation.ps1 -RepoRoot "C:\Work\ClaudeCode\Spikes\TestingAgents"

.NOTES
    Exit Code 0: No violations found (PASS)
    Exit Code 1: Violations found (FAIL)
#>

param(
    [string]$RepoRoot = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent)
)

# Domain agent filename patterns
$DomainAgentPatterns = @(
    'breezsdk-',
    'umbraco-'
)

# Domain-specific WebFetch domain permissions (project-specific, not core)
$ProjectSpecificDomains = @(
    'sdk-doc-liquid.breez.technology',
    'docs.umbraco.com'
)

# Domain-specific MCP server names
$ProjectSpecificMcpServers = @(
    'umbraco-docs'
)

# Tier 1 files to scan for domain agent references
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
    '.claude/skills/external-plugins.md',
    '.specify/memory/constitution.md',
    '.specify/templates/plan.md',
    '.specify/templates/tasks.md',
    '.specify/templates/implement.md'
)

# Domain agent files grouped by family
$BreezsdkAgentFiles = @(
    '.claude/agents/breezsdk-architect.md',
    '.claude/agents/breezsdk-developer.md',
    '.claude/agents/breezsdk-reviewer.md',
    '.claude/agents/breezsdk-ux.md',
    '.claude/agents/breezsdk-test-engineer.md'
)

$UmbracoAgentFiles = @(
    '.claude/agents/umbraco-architect.md',
    '.claude/agents/umbraco-backend-developer.md',
    '.claude/agents/umbraco-backend-reviewer.md',
    '.claude/agents/umbraco-frontend-developer.md',
    '.claude/agents/umbraco-frontend-reviewer.md'
)

# ANSI color codes
$Red = "`e[91m"
$Yellow = "`e[93m"
$Green = "`e[92m"
$Cyan = "`e[96m"
$Reset = "`e[0m"

Write-Host "${Cyan}========================================${Reset}"
Write-Host "${Cyan}Domain Agent Isolation Validator${Reset}"
Write-Host "${Cyan}========================================${Reset}"
Write-Host ""
Write-Host "Repository Root: $RepoRoot"
Write-Host ""

$totalViolations = 0
$checkResults = @{}

# ---------------------------------------------------------------------------
# CHECK 1: No Tier 1 file references domain agent filenames
# ---------------------------------------------------------------------------
Write-Host "${Cyan}Check 1: Domain agent filename references in Tier 1 files${Reset}"

$check1Violations = @()

foreach ($relPath in $Tier1Files) {
    $fullPath = Join-Path $RepoRoot $relPath

    if (-not (Test-Path $fullPath)) {
        continue
    }

    $lines = (Get-Content $fullPath -Raw -ErrorAction SilentlyContinue) -split "`r?`n"

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $lineNum = $i + 1
        $line = $lines[$i]

        foreach ($pattern in $DomainAgentPatterns) {
            if ($line -match [regex]::Escape($pattern)) {
                # Exclude references to validation scripts and spec directories
                if ($line -match 'specs[\\/]' -or
                    $line -match 'validate-' -or
                    $line -match '001-generic-speckit-reuse') {
                    continue
                }

                $check1Violations += [PSCustomObject]@{
                    File = $relPath
                    LineNumber = $lineNum
                    Pattern = $pattern
                    Line = $line.Trim()
                }
                $totalViolations++
            }
        }
    }
}

if ($check1Violations.Count -gt 0) {
    Write-Host "${Red}  [FAIL]${Reset} $($check1Violations.Count) domain agent reference(s) found in Tier 1 files:"
    foreach ($v in $check1Violations) {
        Write-Host "    ${Yellow}$($v.File):$($v.LineNumber):${Reset} '$($v.Pattern)' -> $($v.Line)"
    }
} else {
    Write-Host "${Green}  [PASS]${Reset} No domain agent filenames found in Tier 1 files."
}
$checkResults['Check 1'] = $check1Violations.Count

Write-Host ""

# ---------------------------------------------------------------------------
# CHECK 2: Domain agents documented only in CLAUDE.project.md
# ---------------------------------------------------------------------------
Write-Host "${Cyan}Check 2: Domain agents documented only in CLAUDE.project.md${Reset}"

$check2Violations = @()

# Files where domain agents should NOT be documented (exclude CLAUDE.project.md)
$nonProjectFiles = $Tier1Files | Where-Object { $_ -ne 'CLAUDE.project.md' }

foreach ($relPath in $nonProjectFiles) {
    $fullPath = Join-Path $RepoRoot $relPath

    if (-not (Test-Path $fullPath)) {
        continue
    }

    $content = Get-Content $fullPath -Raw -ErrorAction SilentlyContinue
    if (-not $content) { continue }

    # Check for domain agent documentation patterns (tables, lists mentioning domain agents)
    foreach ($pattern in $DomainAgentPatterns) {
        $matches = [regex]::Matches($content, "(?i)$([regex]::Escape($pattern))")
        foreach ($m in $matches) {
            # Find line number
            $beforeMatch = $content.Substring(0, $m.Index)
            $lineNum = ($beforeMatch -split "`r?`n").Count

            $lines = $content -split "`r?`n"
            $line = if ($lineNum -le $lines.Count) { $lines[$lineNum - 1].Trim() } else { "" }

            # Exclude spec/validation references
            if ($line -match 'specs[\\/]' -or $line -match 'validate-' -or $line -match '001-generic-speckit-reuse') {
                continue
            }

            # Already counted in check 1 for Tier 1 files, but this specifically checks non-project docs
            $check2Violations += [PSCustomObject]@{
                File = $relPath
                LineNumber = $lineNum
                Pattern = $pattern
                Line = $line
            }
        }
    }
}

# Deduplicate with check 1 (don't double-count, but report separately)
$uniqueCheck2 = $check2Violations | Select-Object -Unique File, LineNumber, Pattern
$check2Count = ($uniqueCheck2 | Measure-Object).Count

if ($check2Count -gt 0) {
    Write-Host "${Red}  [FAIL]${Reset} $check2Count domain agent documentation reference(s) found outside CLAUDE.project.md:"
    foreach ($v in $check2Violations) {
        Write-Host "    ${Yellow}$($v.File):$($v.LineNumber):${Reset} '$($v.Pattern)' -> $($v.Line)"
    }
} else {
    Write-Host "${Green}  [PASS]${Reset} Domain agents documented only in CLAUDE.project.md."
}
$checkResults['Check 2'] = $check2Count

Write-Host ""

# ---------------------------------------------------------------------------
# CHECK 3: hooks.json contains no hardcoded domain agent names
# ---------------------------------------------------------------------------
Write-Host "${Cyan}Check 3: No domain agent names in hooks.json${Reset}"

$check3Violations = @()
$hooksPath = Join-Path $RepoRoot '.claude/hooks.json'

if (Test-Path $hooksPath) {
    $hooksContent = Get-Content $hooksPath -Raw
    $hooksLines = $hooksContent -split "`r?`n"

    for ($i = 0; $i -lt $hooksLines.Count; $i++) {
        $lineNum = $i + 1
        $line = $hooksLines[$i]

        foreach ($pattern in $DomainAgentPatterns) {
            if ($line -match [regex]::Escape($pattern)) {
                $check3Violations += [PSCustomObject]@{
                    LineNumber = $lineNum
                    Pattern = $pattern
                    Line = $line.Trim()
                }
                $totalViolations++
            }
        }
    }

    if ($check3Violations.Count -gt 0) {
        Write-Host "${Red}  [FAIL]${Reset} $($check3Violations.Count) domain agent name(s) in hooks.json:"
        foreach ($v in $check3Violations) {
            Write-Host "    ${Yellow}Line $($v.LineNumber):${Reset} '$($v.Pattern)' -> $($v.Line)"
        }
    } else {
        Write-Host "${Green}  [PASS]${Reset} No domain agent names in hooks.json."
    }
} else {
    Write-Host "${Yellow}  [SKIP]${Reset} hooks.json not found."
}
$checkResults['Check 3'] = $check3Violations.Count

Write-Host ""

# ---------------------------------------------------------------------------
# CHECK 4: settings.local.json contains no project-specific WebFetch domain permissions
# ---------------------------------------------------------------------------
Write-Host "${Cyan}Check 4: No project-specific WebFetch domains in settings.local.json${Reset}"

$check4Violations = @()
$settingsLocalPath = Join-Path $RepoRoot '.claude/settings.local.json'

if (Test-Path $settingsLocalPath) {
    $settingsContent = Get-Content $settingsLocalPath -Raw
    $settingsLines = $settingsContent -split "`r?`n"

    for ($i = 0; $i -lt $settingsLines.Count; $i++) {
        $lineNum = $i + 1
        $line = $settingsLines[$i]

        foreach ($domain in $ProjectSpecificDomains) {
            if ($line -match [regex]::Escape($domain)) {
                $check4Violations += [PSCustomObject]@{
                    LineNumber = $lineNum
                    Domain = $domain
                    Line = $line.Trim()
                }
                $totalViolations++
            }
        }
    }

    # Also check settings.json for project-specific MCP servers
    $settingsJsonPath = Join-Path $RepoRoot '.claude/settings.json'
    if (Test-Path $settingsJsonPath) {
        $settingsJsonContent = Get-Content $settingsJsonPath -Raw
        $settingsJsonLines = $settingsJsonContent -split "`r?`n"

        for ($i = 0; $i -lt $settingsJsonLines.Count; $i++) {
            $lineNum = $i + 1
            $line = $settingsJsonLines[$i]

            foreach ($server in $ProjectSpecificMcpServers) {
                if ($line -match [regex]::Escape($server)) {
                    $check4Violations += [PSCustomObject]@{
                        LineNumber = $lineNum
                        Domain = "$server (MCP server in settings.json)"
                        Line = $line.Trim()
                    }
                    $totalViolations++
                }
            }
        }
    }

    if ($check4Violations.Count -gt 0) {
        Write-Host "${Red}  [FAIL]${Reset} $($check4Violations.Count) project-specific domain/MCP reference(s) found:"
        foreach ($v in $check4Violations) {
            Write-Host "    ${Yellow}Line $($v.LineNumber):${Reset} '$($v.Domain)' -> $($v.Line)"
        }
    } else {
        Write-Host "${Green}  [PASS]${Reset} No project-specific WebFetch domains or MCP servers in settings files."
    }
} else {
    Write-Host "${Yellow}  [SKIP]${Reset} settings.local.json not found."
}
$checkResults['Check 4'] = $check4Violations.Count

Write-Host ""

# ---------------------------------------------------------------------------
# CHECK 5: Domain agent files have no cross-references to other families
# ---------------------------------------------------------------------------
Write-Host "${Cyan}Check 5: Domain agent family independence (no cross-references)${Reset}"

$check5Violations = @()

# Check breezsdk agents for umbraco references
foreach ($relPath in $BreezsdkAgentFiles) {
    $fullPath = Join-Path $RepoRoot $relPath

    if (-not (Test-Path $fullPath)) { continue }

    $lines = (Get-Content $fullPath -Raw -ErrorAction SilentlyContinue) -split "`r?`n"

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $lineNum = $i + 1
        $line = $lines[$i]

        if ($line -match '(?i)umbraco') {
            $check5Violations += [PSCustomObject]@{
                File = $relPath
                LineNumber = $lineNum
                CrossRef = 'umbraco (in breezsdk agent)'
                Line = $line.Trim()
            }
            $totalViolations++
        }
    }
}

# Check umbraco agents for breezsdk references
foreach ($relPath in $UmbracoAgentFiles) {
    $fullPath = Join-Path $RepoRoot $relPath

    if (-not (Test-Path $fullPath)) { continue }

    $lines = (Get-Content $fullPath -Raw -ErrorAction SilentlyContinue) -split "`r?`n"

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $lineNum = $i + 1
        $line = $lines[$i]

        if ($line -match '(?i)breezsdk|(?i)breez') {
            $check5Violations += [PSCustomObject]@{
                File = $relPath
                LineNumber = $lineNum
                CrossRef = 'breezsdk/breez (in umbraco agent)'
                Line = $line.Trim()
            }
            $totalViolations++
        }
    }
}

if ($check5Violations.Count -gt 0) {
    Write-Host "${Red}  [FAIL]${Reset} $($check5Violations.Count) cross-family reference(s) found:"
    foreach ($v in $check5Violations) {
        Write-Host "    ${Yellow}$($v.File):$($v.LineNumber):${Reset} $($v.CrossRef) -> $($v.Line)"
    }
} else {
    Write-Host "${Green}  [PASS]${Reset} Domain agent families are independent (no cross-references)."
}
$checkResults['Check 5'] = $check5Violations.Count

Write-Host ""

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------
Write-Host "${Cyan}========================================${Reset}"
Write-Host "${Cyan}Summary${Reset}"
Write-Host "${Cyan}========================================${Reset}"

foreach ($check in $checkResults.Keys | Sort-Object) {
    $count = $checkResults[$check]
    $status = if ($count -eq 0) { "${Green}PASS${Reset}" } else { "${Red}FAIL ($count)${Reset}" }
    Write-Host "  $check`: $status"
}

Write-Host ""
Write-Host "Total violations: $totalViolations"
Write-Host ""

if ($totalViolations -eq 0) {
    Write-Host "${Green}RESULT: PASS${Reset}"
    Write-Host "Domain agents are properly isolated as optional add-ons."
    exit 0
} else {
    Write-Host "${Red}RESULT: FAIL${Reset}"
    Write-Host ""
    Write-Host "Domain agents must be self-contained. Core files should not reference them."
    Write-Host "Move domain agent documentation to CLAUDE.project.md."
    exit 1
}
