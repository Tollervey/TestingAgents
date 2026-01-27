#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates that the SpecKit workflow remains fully effective after genericization.

.DESCRIPTION
    Verifies structural preservation of:
    (1) All /speckit.* command files exist and are non-empty
    (2) CLAUDE.md contains wave execution strategy, context management, checkpoint patterns, quality gate sections
    (3) Constitution contains all 13 articles and 40 principles
    (4) All 7 core agent files exist and are non-empty

.PARAMETER RepoRoot
    Repository root directory. Defaults to script's grandparent directory.

.EXAMPLE
    .\validate-workflow-preservation.ps1
    .\validate-workflow-preservation.ps1 -RepoRoot "C:\Work\ClaudeCode\Spikes\TestingAgents"

.NOTES
    Exit Code 0: All checks pass (PASS)
    Exit Code 1: One or more checks fail (FAIL)
#>

param(
    [string]$RepoRoot = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent)
)

# ANSI color codes
$Red = "`e[91m"
$Yellow = "`e[93m"
$Green = "`e[92m"
$Cyan = "`e[96m"
$Reset = "`e[0m"

Write-Host "${Cyan}========================================${Reset}"
Write-Host "${Cyan}Workflow Preservation Validator${Reset}"
Write-Host "${Cyan}========================================${Reset}"
Write-Host ""
Write-Host "Repository Root: $RepoRoot"
Write-Host ""

$totalChecks = 0
$passedChecks = 0
$failedChecks = 0
$failures = @()

function Test-Check {
    param(
        [string]$Category,
        [string]$Description,
        [bool]$Condition
    )
    $script:totalChecks++
    if ($Condition) {
        $script:passedChecks++
        Write-Host "${Green}  [PASS]${Reset} $Description"
    } else {
        $script:failedChecks++
        $script:failures += "${Category}: $Description"
        Write-Host "${Red}  [FAIL]${Reset} $Description"
    }
}

# ============================================================
# CHECK 1: All /speckit.* command files exist and are non-empty
# ============================================================
Write-Host "${Cyan}--- Check 1: SpecKit Command Files ---${Reset}"

$speckitCommands = @(
    'speckit.specify.md',
    'speckit.clarify.md',
    'speckit.plan.md',
    'speckit.tasks.md',
    'speckit.checklist.md',
    'speckit.analyze.md',
    'speckit.implement.md',
    'speckit.constitution.md',
    'speckit.worktree.md',
    'speckit.taskstoissues.md'
)

foreach ($cmd in $speckitCommands) {
    $path = Join-Path $RepoRoot ".claude/commands/$cmd"
    $exists = Test-Path $path
    $nonEmpty = $false
    if ($exists) {
        $content = Get-Content $path -Raw -ErrorAction SilentlyContinue
        $nonEmpty = ($null -ne $content -and $content.Trim().Length -gt 0)
    }
    Test-Check "Commands" "$cmd exists and is non-empty" ($exists -and $nonEmpty)
}

Write-Host ""

# ============================================================
# CHECK 2: CLAUDE.md contains essential workflow sections
# ============================================================
Write-Host "${Cyan}--- Check 2: CLAUDE.md Workflow Sections ---${Reset}"

$claudeMdPath = Join-Path $RepoRoot "CLAUDE.md"
$claudeContent = ""
if (Test-Path $claudeMdPath) {
    $claudeContent = Get-Content $claudeMdPath -Raw -ErrorAction SilentlyContinue
}

# Required sections/patterns in CLAUDE.md
$claudeRequiredPatterns = @(
    @{ Name = "Governance section"; Pattern = "(?i)##\s*Governance" },
    @{ Name = "Spec-Kit Workflow Order"; Pattern = "(?i)Spec-Kit Workflow Order" },
    @{ Name = "Wave Execution Strategy"; Pattern = "(?i)Wave.*(Execution|Based)" },
    @{ Name = "Context Management"; Pattern = "(?i)Context\s*(Management|Percentage)" },
    @{ Name = "Checkpoint Strategy/Patterns"; Pattern = "(?i)Checkpoint" },
    @{ Name = "Quality Gate (after wave)"; Pattern = "(?i)Quality\s*Gate" },
    @{ Name = "Phase Completion Requirements"; Pattern = "(?i)Phase\s*Completion\s*Requirements" },
    @{ Name = "Workflow Rules section"; Pattern = "(?i)##\s*Workflow\s*Rules" },
    @{ Name = "Available Agents table"; Pattern = "(?i)Available\s*Agents" },
    @{ Name = "Agent Tools & Permissions"; Pattern = "(?i)Agent\s*Tools\s*&?\s*Permissions" },
    @{ Name = "Agent & Plugin Utilization"; Pattern = "(?i)Agent\s*&?\s*Plugin\s*Utilization" },
    @{ Name = "Parallel Execution section"; Pattern = "(?i)Parallel\s*Execution" },
    @{ Name = "Background Execution section"; Pattern = "(?i)Background\s*Execution" },
    @{ Name = "Test-First (Non-Negotiable)"; Pattern = "(?i)Test-First" },
    @{ Name = "Branch Strategy section"; Pattern = "(?i)Branch\s*Strategy" },
    @{ Name = "Resources section"; Pattern = "(?i)##\s*Resources" },
    @{ Name = "Before Every Commit section"; Pattern = "(?i)Before\s*Every\s*Commit" },
    @{ Name = "API Design section"; Pattern = "(?i)API\s*Design" }
)

foreach ($req in $claudeRequiredPatterns) {
    $found = $claudeContent -match $req.Pattern
    Test-Check "CLAUDE.md" "$($req.Name)" $found
}

Write-Host ""

# ============================================================
# CHECK 3: Constitution contains all 13 articles and principles
# ============================================================
Write-Host "${Cyan}--- Check 3: Constitution Articles & Principles ---${Reset}"

$constitutionPath = Join-Path $RepoRoot ".specify/memory/constitution.md"
$constitutionContent = ""
if (Test-Path $constitutionPath) {
    $constitutionContent = Get-Content $constitutionPath -Raw -ErrorAction SilentlyContinue
}

Test-Check "Constitution" "Constitution file exists" (Test-Path $constitutionPath)

# Check all 13 articles exist
$articles = @(
    @{ Num = "I";    Name = "Architectural Foundation" },
    @{ Num = "II";   Name = "Code Quality Standards" },
    @{ Num = "III";  Name = "Testing Philosophy" },
    @{ Num = "IV";   Name = "Data Layer Governance" },
    @{ Num = "V";    Name = "API Design Principles" },
    @{ Num = "VI";   Name = "Security Framework" },
    @{ Num = "VII";  Name = "Error Handling" },
    @{ Num = "VIII"; Name = "Frontend Architecture" },
    @{ Num = "IX";   Name = "Simplicity" },
    @{ Num = "X";    Name = "Documentation Standards" },
    @{ Num = "XI";   Name = "Performance" },
    @{ Num = "XII";  Name = "Development Workflow" },
    @{ Num = "XIII"; Name = "Governance" }
)

foreach ($article in $articles) {
    $pattern = "(?i)Article\s*$($article.Num)\s*:"
    $found = $constitutionContent -match $pattern
    Test-Check "Constitution" "Article $($article.Num): $($article.Name)" $found
}

# Check key principles exist (spot-check subset of all 40)
$principles = @(
    @{ Id = "I.1";    Name = "Clean Architecture Mandate" },
    @{ Id = "I.2";    Name = "Domain-Driven Design" },
    @{ Id = "I.3";    Name = "Modular Decomposition" },
    @{ Id = "III.1";  Name = "Test-First Imperative" },
    @{ Id = "III.4";  Name = "Automated Validation Gates" },
    @{ Id = "VI.1";   Name = "Defence in Depth" },
    @{ Id = "VI.3";   Name = "Secrets Management" },
    @{ Id = "IX.1";   Name = "YAGNI Enforcement" },
    @{ Id = "XII.1";  Name = "Task Independence" },
    @{ Id = "XIII.1"; Name = "Constitution Supremacy" },
    @{ Id = "XIII.2"; Name = "Amendment Protocol" },
    @{ Id = "XIII.3"; Name = "Deviation Documentation" }
)

foreach ($principle in $principles) {
    $pattern = "(?i)$([regex]::Escape($principle.Id))\s"
    $found = $constitutionContent -match $pattern
    Test-Check "Constitution" "Principle $($principle.Id): $($principle.Name)" $found
}

# Check Enforcement Framework section
Test-Check "Constitution" "Enforcement Framework section" ($constitutionContent -match "(?i)Enforcement\s*Framework")
Test-Check "Constitution" "Amendment Log section" ($constitutionContent -match "(?i)Amendment\s*Log")
Test-Check "Constitution" "Generic placeholders (<build-tool>)" ($constitutionContent -match "<build-tool>")
Test-Check "Constitution" "Generic placeholders (<test-runner>)" ($constitutionContent -match "<test-runner>")
Test-Check "Constitution" "Generic placeholders (<formatter>)" ($constitutionContent -match "<formatter>")

Write-Host ""

# ============================================================
# CHECK 4: All 7 core agent files exist and are non-empty
# ============================================================
Write-Host "${Cyan}--- Check 4: Core Agent Files ---${Reset}"

$coreAgents = @(
    'solution-architect.md',
    'backend-developer.md',
    'frontend-developer.md',
    'test-engineer.md',
    'database-architect.md',
    'security-auditor.md',
    'code-reviewer.md'
)

foreach ($agent in $coreAgents) {
    $path = Join-Path $RepoRoot ".claude/agents/$agent"
    $exists = Test-Path $path
    $nonEmpty = $false
    if ($exists) {
        $content = Get-Content $path -Raw -ErrorAction SilentlyContinue
        $nonEmpty = ($null -ne $content -and $content.Trim().Length -gt 0)
    }
    Test-Check "Agents" "$agent exists and is non-empty" ($exists -and $nonEmpty)
}

Write-Host ""

# ============================================================
# CHECK 5: Core skill files exist
# ============================================================
Write-Host "${Cyan}--- Check 5: Core Skill Files ---${Reset}"

$coreSkills = @(
    '.claude/skills/implementation-execution.md',
    '.claude/skills/external-plugins.md'
)

foreach ($skill in $coreSkills) {
    $path = Join-Path $RepoRoot $skill
    $exists = Test-Path $path
    $nonEmpty = $false
    if ($exists) {
        $content = Get-Content $path -Raw -ErrorAction SilentlyContinue
        $nonEmpty = ($null -ne $content -and $content.Trim().Length -gt 0)
    }
    Test-Check "Skills" "$skill exists and is non-empty" ($exists -and $nonEmpty)
}

Write-Host ""

# ============================================================
# Summary
# ============================================================
Write-Host "${Cyan}========================================${Reset}"
Write-Host "${Cyan}Summary${Reset}"
Write-Host "${Cyan}========================================${Reset}"
Write-Host "Total checks:   $totalChecks"
Write-Host "Passed:         $passedChecks"
Write-Host "Failed:         $failedChecks"
Write-Host ""

if ($failedChecks -eq 0) {
    Write-Host "${Green}RESULT: PASS${Reset}"
    Write-Host "All workflow capabilities preserved after genericization."
    exit 0
} else {
    Write-Host "${Red}RESULT: FAIL${Reset}"
    Write-Host ""
    Write-Host "Failed checks:"
    foreach ($f in $failures) {
        Write-Host "  ${Red}-${Reset} $f"
    }
    Write-Host ""
    Write-Host "Workflow capabilities must be preserved during genericization."
    Write-Host "Review the failed checks and fix any gaps."
    exit 1
}
