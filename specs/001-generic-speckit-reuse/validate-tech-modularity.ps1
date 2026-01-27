#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates Tier 1 core files for hardcoded technology-specific terms.

.DESCRIPTION
    Scans all Tier 1 core SpecKit files for hardcoded technology-specific terms
    (dotnet, .csproj, NuGet, xunit, FluentAssertions, Moq, EF Core, Entity Framework, Blazor, Razor)
    that are used as requirements rather than as customizable examples.

    Terms inside CUSTOMIZABLE comment blocks or generic placeholder patterns (e.g., <build-tool>)
    are excluded. The goal is to ensure core files don't hardcode any specific technology.

.PARAMETER RepoRoot
    Repository root directory. Defaults to script's grandparent directory.

.EXAMPLE
    .\validate-tech-modularity.ps1
    .\validate-tech-modularity.ps1 -RepoRoot "C:\Work\ClaudeCode\Spikes\TestingAgents"

.NOTES
    Exit Code 0: No violations found (PASS)
    Exit Code 1: Violations found (FAIL)
#>

param(
    [string]$RepoRoot = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent)
)

# Technology-specific terms to search for (case-insensitive)
# Each entry: @{ Term = 'regex pattern'; Label = 'display name' }
$TechTerms = @(
    @{ Term = '\bdotnet\b'; Label = 'dotnet' },
    @{ Term = '\.csproj\b'; Label = '.csproj' },
    @{ Term = '\bNuGet\b'; Label = 'NuGet' },
    @{ Term = '\bxunit\b'; Label = 'xunit' },
    @{ Term = '\bxUnit\b'; Label = 'xUnit' },
    @{ Term = '\bFluentAssertions\b'; Label = 'FluentAssertions' },
    @{ Term = '\bFluentValidation\b'; Label = 'FluentValidation' },
    @{ Term = '\bMoq\b'; Label = 'Moq' },
    @{ Term = '\bEF\s*Core\b'; Label = 'EF Core' },
    @{ Term = '\bEntity\s*Framework\b'; Label = 'Entity Framework' },
    @{ Term = '\bBlazor\b'; Label = 'Blazor' },
    @{ Term = '\bRazor\b'; Label = 'Razor' },
    @{ Term = '\b\.sln\b'; Label = '.sln' },
    @{ Term = '\bASP\.NET\b'; Label = 'ASP.NET' },
    @{ Term = '\bdotnet\s+(build|test|run|watch|format|ef|publish)\b'; Label = 'dotnet CLI command' },
    @{ Term = '\bSerilog\b'; Label = 'Serilog' },
    @{ Term = '\bIOptions<'; Label = 'IOptions<T>' },
    @{ Term = '\bActionResult<'; Label = 'ActionResult<T>' },
    @{ Term = '\b\[ApiController\]'; Label = '[ApiController]' },
    @{ Term = '\bMicrosoft\.Extensions\.'; Label = 'Microsoft.Extensions.*' }
)

# Tier 1 files to scan (relative to repo root)
$Tier1Files = @(
    'CLAUDE.md',
    '.claude/settings.json',
    # NOTE: .claude/settings.local.json is excluded — it is gitignored, not tracked,
    # and accumulates user-specific permissions. It is not a distributable Tier 1 file.
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

# Patterns that indicate the term is used in a customizable/example context (false positive exclusions)
$ExclusionPatterns = @(
    '<!-- CUSTOMIZABLE',           # Inside a CUSTOMIZABLE comment block
    '<!-- END CUSTOMIZABLE',       # End of customizable block
    'CUSTOMIZABLE:',               # Customizable marker
    '_comment',                    # JSON comment field
    'See `CLAUDE\.project\.md`',   # Reference to project-specific file
    'See CLAUDE\.project\.md',     # Reference to project-specific file
    '<build-tool>',                # Generic placeholder
    '<test-runner>',               # Generic placeholder
    '<formatter>',                 # Generic placeholder
    '<dependency-audit-tool>',     # Generic placeholder
    'specs[\\/]',                  # References to spec files
    'validate-.*\.ps1',            # Validation script references
    '001-generic-speckit-reuse'    # Feature spec references
)

# ANSI color codes
$Red = "`e[91m"
$Yellow = "`e[93m"
$Green = "`e[92m"
$Cyan = "`e[96m"
$Reset = "`e[0m"

Write-Host "${Cyan}========================================${Reset}"
Write-Host "${Cyan}Tier 1 Technology Modularity Validator${Reset}"
Write-Host "${Cyan}========================================${Reset}"
Write-Host ""
Write-Host "Repository Root: $RepoRoot"
Write-Host ""

# Validation counters
$filesScanned = 0
$filesMissing = 0
$totalViolations = 0
$violationsByFile = @{}

# Track CUSTOMIZABLE block state per file
function Test-InCustomizableBlock {
    param([string[]]$Lines, [int]$CurrentIndex)

    # Look backwards from current line for CUSTOMIZABLE markers
    for ($j = $CurrentIndex; $j -ge 0; $j--) {
        if ($Lines[$j] -match '<!-- END CUSTOMIZABLE') {
            return $false  # We're after a closed block
        }
        if ($Lines[$j] -match '<!-- CUSTOMIZABLE') {
            return $true  # We're inside an open block
        }
    }
    return $false
}

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

        foreach ($termEntry in $TechTerms) {
            $termPattern = $termEntry.Term
            $termLabel = $termEntry.Label

            # Case-insensitive search
            if ($line -match "(?i)$termPattern") {
                # Check exclusion patterns
                $excluded = $false
                foreach ($exclusion in $ExclusionPatterns) {
                    if ($line -match $exclusion) {
                        $excluded = $true
                        break
                    }
                }
                if ($excluded) { continue }

                # Check if inside a CUSTOMIZABLE block
                if (Test-InCustomizableBlock -Lines $lines -CurrentIndex $i) {
                    continue
                }

                # Check if the line is a comment documenting what to replace
                # e.g., "Replace `dotnet build` with..." or "Remove .NET-specific..."
                if ($line -match '(?i)(replace|remove|move|moved|strip|change|instead of|rather than|no longer|was renamed)') {
                    continue
                }

                # Check if line references project-specific file
                if ($line -match 'CLAUDE\.project\.md') {
                    continue
                }

                $fileViolations += [PSCustomObject]@{
                    LineNumber = $lineNum
                    Term = $termLabel
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
    Write-Host "${Red}[FAIL]${Reset} Found $totalViolations technology-specific term violation(s) in Tier 1 files:"
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
    Write-Host "${Green}[PASS]${Reset} No hardcoded technology-specific terms found in Tier 1 files."
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
    Write-Host "Tier 1 files must not hardcode technology-specific terms."
    Write-Host "Use generic placeholders (<build-tool>, <test-runner>, <formatter>) or"
    Write-Host "move technology-specific content to CLAUDE.project.md."
    exit 1
}
