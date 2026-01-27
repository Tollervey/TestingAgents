<#
.SYNOPSIS
    Agent Metrics Tracker for Spec-Kit Implementation Phases

.DESCRIPTION
    Tracks and reports metrics on sub-agent usage during implementation phases.
    Collects: invocation count, tokens, timing, success/failure rates.

.PARAMETER Action
    The action to perform: Init, Record, Report, Reset

.PARAMETER AgentName
    The name of the agent (e.g., "backend-developer", "test-engineer")

.PARAMETER TaskId
    The task ID returned by the agent invocation

.PARAMETER Status
    The completion status: "completed", "failed", "timeout"

.PARAMETER TokensUsed
    The number of tokens used by the agent

.PARAMETER DurationMs
    The duration in milliseconds

.PARAMETER PhaseName
    The name of the current phase (e.g., "US2-Paywall-Implementation")

.EXAMPLE
    # Initialize metrics for a new phase
    .\agent-metrics.ps1 -Action Init -PhaseName "US2-Implementation"

    # Record an agent completion
    .\agent-metrics.ps1 -Action Record -AgentName "backend-developer" -TaskId "a620108" -Status "completed" -TokensUsed 45000 -DurationMs 120000

    # Generate report at phase end
    .\agent-metrics.ps1 -Action Report

    # Reset metrics for next phase
    .\agent-metrics.ps1 -Action Reset
#>

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Init", "Record", "Report", "Reset", "Trends", "Compare", "Cumulative", "Export", "")]
    [string]$Action = "",

    [string]$AgentName,
    [string]$TaskId,
    [ValidateSet("completed", "failed", "timeout", "")]
    [string]$Status,
    [int]$TokensUsed = 0,
    [int]$DurationMs = 0,
    [string]$PhaseName = "Unknown",
    [string]$Description = "",

    # New parameters for v2.0.0 schema
    [ValidateSet("opus", "sonnet", "haiku", "")]
    [string]$Model = "",
    [ValidateSet("specify", "clarify", "plan", "tasks", "checklist", "analyze", "implement", "")]
    [string]$Phase = "",
    [ValidateSet("implementation", "testing", "review", "planning", "analysis", "other", "")]
    [string]$Category = "",
    [string]$FeatureBranch = "",

    # Parallel execution parameters
    [switch]$IsParallel,
    [string]$ParallelGroupId = "",
    [int]$GroupSize = 1,

    # Trends/Compare/Export parameters
    [int]$Days = 7,
    [string]$Session1 = "",
    [string]$Session2 = "",
    [ValidateSet("CSV", "JSON", "")]
    [string]$Format = "",
    [switch]$IncludeArchives
)

$MetricsDir = Join-Path $PSScriptRoot "..\..\metrics"
$MetricsFile = Join-Path $MetricsDir "agent-metrics.json"
$SettingsFile = Join-Path $MetricsDir "settings.json"

function Get-MetricsSettings {
    <#
    .SYNOPSIS
        Loads metrics settings from settings.json with fallback to defaults

    .DESCRIPTION
        Reads the settings.json file from the metrics directory.
        If the file doesn't exist or is invalid, returns default settings.

    .OUTPUTS
        PSCustomObject with retention, costWeights, and display settings
    #>

    $defaults = @{
        schemaVersion = "1.0.0"
        retention = @{
            archiveDays = 7
            autoCleanupEnabled = $true
        }
        costWeights = @{
            haiku = 1.0
            sonnet = 3.0
            opus = 5.0
        }
        display = @{
            colorOutput = $true
            showCostEstimates = $true
            showParallelMetrics = $true
        }
    }

    if (Test-Path $SettingsFile) {
        try {
            $settings = Get-Content $SettingsFile -Raw -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop

            # Merge with defaults to ensure all required properties exist
            $merged = @{
                schemaVersion = if ($settings.schemaVersion) { $settings.schemaVersion } else { $defaults.schemaVersion }
                retention = @{
                    archiveDays = if ($null -ne $settings.retention.archiveDays) { $settings.retention.archiveDays } else { $defaults.retention.archiveDays }
                    autoCleanupEnabled = if ($null -ne $settings.retention.autoCleanupEnabled) { $settings.retention.autoCleanupEnabled } else { $defaults.retention.autoCleanupEnabled }
                }
                costWeights = @{
                    haiku = if ($null -ne $settings.costWeights.haiku) { $settings.costWeights.haiku } else { $defaults.costWeights.haiku }
                    sonnet = if ($null -ne $settings.costWeights.sonnet) { $settings.costWeights.sonnet } else { $defaults.costWeights.sonnet }
                    opus = if ($null -ne $settings.costWeights.opus) { $settings.costWeights.opus } else { $defaults.costWeights.opus }
                }
                display = @{
                    colorOutput = if ($null -ne $settings.display.colorOutput) { $settings.display.colorOutput } else { $defaults.display.colorOutput }
                    showCostEstimates = if ($null -ne $settings.display.showCostEstimates) { $settings.display.showCostEstimates } else { $defaults.display.showCostEstimates }
                    showParallelMetrics = if ($null -ne $settings.display.showParallelMetrics) { $settings.display.showParallelMetrics } else { $defaults.display.showParallelMetrics }
                }
            }

            return [PSCustomObject]$merged
        }
        catch {
            Write-Warning "Failed to load settings from $SettingsFile. Using defaults. Error: $_"
            return [PSCustomObject]$defaults
        }
    }

    return [PSCustomObject]$defaults
}

function Test-SchemaVersion {
    <#
    .SYNOPSIS
        Validates if a session uses the current v2.0.0 schema

    .DESCRIPTION
        Checks the schemaVersion field of a session object.
        Returns $true for v2.0.0+, $false for legacy or missing schemas.
        Used by Trends/Compare actions to skip legacy data per FR-018.

    .PARAMETER Session
        The session object to validate

    .OUTPUTS
        Boolean - $true if schema is v2.0.0 or higher, $false otherwise
    #>
    param(
        [Parameter(Mandatory=$false)]
        $Session = $null
    )

    if (-not $Session) { return $false }

    $version = $Session.schemaVersion
    if (-not $version) { return $false }

    # Parse version - expecting "major.minor.patch" format
    try {
        $parts = $version -split '\.'
        if ($parts.Count -lt 2) { return $false }

        $major = [int]$parts[0]
        $minor = [int]$parts[1]

        # v2.0.0 or higher is current schema
        return ($major -ge 2)
    }
    catch {
        return $false
    }
}

function Update-ModelAggregate {
    <#
    .SYNOPSIS
        Updates per-model aggregate statistics

    .DESCRIPTION
        Adds or updates model statistics in the session's models collection.
        Calculates cost-weighted tokens using weights from settings.

    .PARAMETER Session
        The session object containing the models aggregate

    .PARAMETER Model
        The model name (opus/sonnet/haiku)

    .PARAMETER Tokens
        Number of tokens used in this invocation

    .PARAMETER Settings
        Settings object containing cost weights
    #>
    param(
        [Parameter(Mandatory=$true)]
        $Session,
        [Parameter(Mandatory=$true)]
        [string]$Model,
        [int]$Tokens = 0,
        $Settings = $null
    )

    if (-not $Model) { $Model = "sonnet" }

    # Get cost weight from settings or use defaults
    $costWeight = switch ($Model.ToLower()) {
        "opus" { if ($Settings) { $Settings.costWeights.opus } else { 5.0 } }
        "sonnet" { if ($Settings) { $Settings.costWeights.sonnet } else { 3.0 } }
        "haiku" { if ($Settings) { $Settings.costWeights.haiku } else { 1.0 } }
        default { 3.0 }  # Default to sonnet weight
    }

    # Initialize model entry if not exists
    if (-not $Session.models.PSObject.Properties[$Model]) {
        $Session.models | Add-Member -NotePropertyName $Model -NotePropertyValue @{
            count = 0
            totalTokens = 0
            costWeight = $costWeight
            weightedTokens = 0
            costPercent = 0
        }
    }

    $modelData = $Session.models.$Model
    $modelData.count++
    $modelData.totalTokens += $Tokens
    $modelData.weightedTokens = $modelData.totalTokens * $costWeight

    # Recalculate cost percentages for all models
    $totalWeighted = 0
    foreach ($m in $Session.models.PSObject.Properties) {
        $totalWeighted += $m.Value.weightedTokens
    }

    if ($totalWeighted -gt 0) {
        foreach ($m in $Session.models.PSObject.Properties) {
            $m.Value.costPercent = [math]::Round(($m.Value.weightedTokens / $totalWeighted) * 100, 1)
        }
    }
}

function Update-CategoryAggregate {
    <#
    .SYNOPSIS
        Updates per-category aggregate statistics

    .DESCRIPTION
        Adds or updates category statistics in the session's categories collection.

    .PARAMETER Session
        The session object containing the categories aggregate

    .PARAMETER Category
        The category name (implementation/testing/review/planning/analysis/other)

    .PARAMETER Tokens
        Number of tokens used in this invocation

    .PARAMETER DurationMs
        Duration in milliseconds
    #>
    param(
        [Parameter(Mandatory=$true)]
        $Session,
        [string]$Category = "other",
        [int]$Tokens = 0,
        [int]$DurationMs = 0
    )

    if (-not $Category) { $Category = "other" }

    # Initialize category entry if not exists
    if (-not $Session.categories.PSObject.Properties[$Category]) {
        $Session.categories | Add-Member -NotePropertyName $Category -NotePropertyValue @{
            count = 0
            totalTokens = 0
            avgTokens = 0
            totalDurationMs = 0
            avgDurationMs = 0
        }
    }

    $catData = $Session.categories.$Category
    $catData.count++
    $catData.totalTokens += $Tokens
    $catData.totalDurationMs += $DurationMs
    $catData.avgTokens = [math]::Round($catData.totalTokens / $catData.count, 0)
    $catData.avgDurationMs = [math]::Round($catData.totalDurationMs / $catData.count, 0)
}

function Update-ParallelGroupAggregate {
    <#
    .SYNOPSIS
        Updates parallel group aggregate statistics

    .DESCRIPTION
        Adds or updates parallel execution group metrics in the session.
        Tracks concurrent execution timing and efficiency.

    .PARAMETER Session
        The session object containing the parallelGroups aggregate

    .PARAMETER ParallelGroupId
        Unique identifier for the parallel group

    .PARAMETER TaskId
        The task ID being added to the group

    .PARAMETER Phase
        The Spec-Kit phase

    .PARAMETER DurationMs
        Duration in milliseconds

    .PARAMETER StartTime
        When the invocation started

    .PARAMETER EndTime
        When the invocation ended

    .PARAMETER GroupSize
        Expected number of concurrent agents in this group
    #>
    param(
        [Parameter(Mandatory=$true)]
        $Session,
        [Parameter(Mandatory=$true)]
        [string]$ParallelGroupId,
        [string]$TaskId = "",
        [string]$Phase = "",
        [int]$DurationMs = 0,
        [string]$StartTime = "",
        [string]$EndTime = "",
        [int]$GroupSize = 1
    )

    if (-not $StartTime) { $StartTime = (Get-Date).AddMilliseconds(-$DurationMs).ToString("o") }
    if (-not $EndTime) { $EndTime = (Get-Date).ToString("o") }

    # Initialize parallel group entry if not exists
    if (-not $Session.parallelGroups.PSObject.Properties[$ParallelGroupId]) {
        $Session.parallelGroups | Add-Member -NotePropertyName $ParallelGroupId -NotePropertyValue @{
            groupId = $ParallelGroupId
            phase = $Phase
            declaredParallel = $true
            groupStartTime = $StartTime
            groupEndTime = $EndTime
            groupDurationMs = 0
            invocationCount = 0
            taskIds = @()
            concurrencyMetrics = @{
                maxConcurrent = $GroupSize
                avgConcurrent = 0
                sequentialEquivalentMs = 0
                actualDurationMs = 0
                timeReduction = 0
                efficiency = 0
            }
        }
    }

    $groupData = $Session.parallelGroups.$ParallelGroupId
    $groupData.invocationCount++
    if ($TaskId) {
        $groupData.taskIds += $TaskId
    }

    # Update timing
    $groupStart = [DateTime]::Parse($groupData.groupStartTime)
    $invStart = [DateTime]::Parse($StartTime)
    $invEnd = [DateTime]::Parse($EndTime)
    $groupEnd = [DateTime]::Parse($groupData.groupEndTime)

    if ($invStart -lt $groupStart) { $groupData.groupStartTime = $StartTime }
    if ($invEnd -gt $groupEnd) { $groupData.groupEndTime = $EndTime }

    # Update metrics
    $groupData.concurrencyMetrics.sequentialEquivalentMs += $DurationMs
    $groupStart = [DateTime]::Parse($groupData.groupStartTime)
    $groupEnd = [DateTime]::Parse($groupData.groupEndTime)
    $groupData.groupDurationMs = ($groupEnd - $groupStart).TotalMilliseconds
    $groupData.concurrencyMetrics.actualDurationMs = $groupData.groupDurationMs

    # Calculate efficiency
    if ($groupData.groupDurationMs -gt 0) {
        $seqMs = $groupData.concurrencyMetrics.sequentialEquivalentMs
        $actMs = $groupData.groupDurationMs
        $groupData.concurrencyMetrics.timeReduction = [math]::Round((1 - ($actMs / $seqMs)) * 100, 1)
        $groupData.concurrencyMetrics.avgConcurrent = [math]::Round($seqMs / $actMs, 1)
        $groupData.concurrencyMetrics.efficiency = [math]::Round(($groupData.concurrencyMetrics.avgConcurrent / $groupData.concurrencyMetrics.maxConcurrent) * 100, 1)
    }
}

function Initialize-Metrics {
    param(
        [string]$Phase,
        [string]$Branch = ""
    )

    if (-not (Test-Path $MetricsDir)) {
        New-Item -ItemType Directory -Path $MetricsDir -Force | Out-Null
    }

    # Archive any existing incomplete session before creating new one (FR-016)
    if (Test-Path $MetricsFile) {
        $existingSession = Get-Content $MetricsFile -Raw | ConvertFrom-Json
        # Check if session is incomplete (either no completionStatus or not "complete")
        $isIncomplete = $true
        if ($existingSession.PSObject.Properties["completionStatus"]) {
            $isIncomplete = $existingSession.completionStatus -ne "complete"
        }
        if ($existingSession -and $isIncomplete) {
            # Mark as incomplete and archive (add property if missing for legacy sessions)
            if (-not $existingSession.PSObject.Properties["completionStatus"]) {
                $existingSession | Add-Member -NotePropertyName "completionStatus" -NotePropertyValue "incomplete"
            } else {
                $existingSession.completionStatus = "incomplete"
            }
            if (-not $existingSession.endTime) {
                $existingSession.endTime = (Get-Date).ToString("o")
            }

            $archiveDir = Join-Path $MetricsDir "archive"
            if (-not (Test-Path $archiveDir)) {
                New-Item -ItemType Directory -Path $archiveDir -Force | Out-Null
            }

            $timestamp = (Get-Date).ToString("yyyyMMdd-HHmmss")
            $archivePath = Join-Path $archiveDir "agent-metrics-$timestamp.json"
            $existingSession | ConvertTo-Json -Depth 10 | Set-Content $archivePath -Encoding UTF8
            Write-Host "Archived incomplete previous session: $archivePath" -ForegroundColor Yellow
        }
    }

    # Determine feature branch from git if not provided
    if (-not $Branch) {
        try {
            $Branch = git rev-parse --abbrev-ref HEAD 2>$null
        }
        catch {
            $Branch = "unknown"
        }
    }

    # Generate session ID
    $sessionId = [guid]::NewGuid().ToString()

    # Create v2.0.0 schema session
    $metrics = @{
        schemaVersion = "2.0.0"
        phase = $Phase
        featureBranch = $Branch
        startTime = (Get-Date).ToString("o")
        endTime = $null
        completionStatus = "active"
        sessionId = $sessionId
        agents = @{}
        invocations = @()
        models = @{}
        categories = @{}
        parallelGroups = @{}
        totals = @{
            totalInvocations = 0
            totalTokens = 0
            totalSuccesses = 0
            totalFailures = 0
            totalTimeouts = 0
            totalDurationMs = 0
            parallelInvocations = 0
            sequentialInvocations = 0
            avgTokensPerInvocation = 0
            avgDurationMs = 0
            overallSuccessRate = 0
        }
    }

    $metrics | ConvertTo-Json -Depth 10 | Set-Content $MetricsFile -Encoding UTF8
    Write-Host "Metrics initialized for phase: $Phase (schema v2.0.0)" -ForegroundColor Green
    if ($Branch) {
        Write-Host "Feature branch: $Branch" -ForegroundColor Gray
    }
}

function Record-AgentMetric {
    param(
        [string]$Agent,
        [string]$Task,
        [string]$CompletionStatus,
        [int]$Tokens,
        [int]$Duration,
        [string]$Desc,
        [string]$ModelName = "",
        [string]$CategoryName = "",
        [bool]$Parallel = $false,
        [string]$GroupId = "",
        [int]$GrpSize = 1
    )

    if (-not (Test-Path $MetricsFile)) {
        Write-Error "Metrics not initialized. Run with -Action Init first."
        return
    }

    $metrics = Get-Content $MetricsFile -Raw | ConvertFrom-Json
    $settings = Get-MetricsSettings

    # Get phase from session
    $phaseName = $metrics.phase

    # Default model to sonnet if not specified (FR-010)
    if (-not $ModelName) { $ModelName = "sonnet" }

    # Default category to other if not specified (FR-010)
    if (-not $CategoryName) { $CategoryName = "other" }

    # Calculate invocation times
    $invEndTime = (Get-Date).ToString("o")
    $invStartTime = (Get-Date).AddMilliseconds(-$Duration).ToString("o")

    # Initialize agent entry if not exists
    if (-not $metrics.agents.PSObject.Properties[$Agent]) {
        $metrics.agents | Add-Member -NotePropertyName $Agent -NotePropertyValue ([PSCustomObject]@{
            count = 0
            successes = 0
            failures = 0
            timeouts = 0
            totalTokens = 0
            totalDurationMs = 0
            minDurationMs = [int]::MaxValue
            maxDurationMs = 0
            avgTokens = 0
            avgDurationMs = 0
            models = [PSCustomObject]@{}
        })
    }

    $agentMetrics = $metrics.agents.$Agent
    $agentMetrics.count++
    $agentMetrics.totalTokens += $Tokens
    $agentMetrics.totalDurationMs += $Duration

    if ($Duration -lt $agentMetrics.minDurationMs) { $agentMetrics.minDurationMs = $Duration }
    if ($Duration -gt $agentMetrics.maxDurationMs) { $agentMetrics.maxDurationMs = $Duration }

    switch ($CompletionStatus) {
        "completed" { $agentMetrics.successes++; $metrics.totals.totalSuccesses++ }
        "failed" { $agentMetrics.failures++; $metrics.totals.totalFailures++ }
        "timeout" { $agentMetrics.timeouts++; $metrics.totals.totalTimeouts++ }
    }

    # Update per-agent model tracking
    if (-not $agentMetrics.PSObject.Properties["models"]) {
        $agentMetrics | Add-Member -NotePropertyName "models" -NotePropertyValue ([PSCustomObject]@{}) -Force
    }
    if (-not $agentMetrics.models.PSObject.Properties[$ModelName]) {
        $agentMetrics.models | Add-Member -NotePropertyName $ModelName -NotePropertyValue ([PSCustomObject]@{
            count = 0
            tokens = 0
        })
    }
    $agentMetrics.models.$ModelName.count++
    $agentMetrics.models.$ModelName.tokens += $Tokens

    # Calculate averages
    $agentMetrics.avgTokens = [math]::Round($agentMetrics.totalTokens / $agentMetrics.count, 0)
    $agentMetrics.avgDurationMs = [math]::Round($agentMetrics.totalDurationMs / $agentMetrics.count, 0)

    # Update session-level aggregates
    Update-ModelAggregate -Session $metrics -Model $ModelName -Tokens $Tokens -Settings $settings
    Update-CategoryAggregate -Session $metrics -Category $CategoryName -Tokens $Tokens -DurationMs $Duration

    # Update parallel group if applicable
    if ($Parallel -and $GroupId) {
        Update-ParallelGroupAggregate -Session $metrics -ParallelGroupId $GroupId -TaskId $Task `
            -Phase $phaseName -DurationMs $Duration -StartTime $invStartTime -EndTime $invEndTime `
            -GroupSize $GrpSize
        $metrics.totals.parallelInvocations++
    } else {
        $metrics.totals.sequentialInvocations++
    }

    # Update totals
    $metrics.totals.totalInvocations++
    $metrics.totals.totalTokens += $Tokens
    $metrics.totals.totalDurationMs += $Duration

    # Calculate overall averages
    if ($metrics.totals.totalInvocations -gt 0) {
        $metrics.totals.avgTokensPerInvocation = [math]::Round($metrics.totals.totalTokens / $metrics.totals.totalInvocations, 0)
        $metrics.totals.avgDurationMs = [math]::Round($metrics.totals.totalDurationMs / $metrics.totals.totalInvocations, 0)
        $metrics.totals.overallSuccessRate = [math]::Round(($metrics.totals.totalSuccesses / $metrics.totals.totalInvocations) * 100, 1)
    }

    # Record individual invocation with v2.0.0 fields
    $invocation = @{
        timestamp = $invEndTime
        agent = $Agent
        model = $ModelName
        taskId = $Task
        status = $CompletionStatus
        tokens = $Tokens
        durationMs = $Duration
        description = $Desc
        phase = $phaseName
        category = $CategoryName
        invocationStartTime = $invStartTime
        invocationEndTime = $invEndTime
        isParallel = $Parallel
        parallelGroupId = if ($Parallel) { $GroupId } else { $null }
        groupSize = if ($Parallel) { $GrpSize } else { 1 }
    }
    $metrics.invocations += $invocation

    $metrics | ConvertTo-Json -Depth 10 | Set-Content $MetricsFile -Encoding UTF8
    Write-Host "Recorded: $Agent/$ModelName ($CompletionStatus) - ${Tokens} tokens, $([math]::Round($Duration/1000, 1))s" -ForegroundColor Cyan
}

function Format-Duration {
    param([int]$Ms)
    if ($Ms -eq [int]::MaxValue) { return "N/A" }
    $seconds = [math]::Round($Ms / 1000, 1)
    if ($seconds -lt 60) { return "${seconds}s" }
    $minutes = [math]::Floor($seconds / 60)
    $secs = [math]::Round($seconds % 60, 0)
    return "${minutes}m ${secs}s"
}

function Get-SuccessRate {
    param($AgentData)
    if ($AgentData.count -eq 0) { return "N/A" }
    $rate = [math]::Round(($AgentData.successes / $AgentData.count) * 100, 1)
    return "$rate%"
}

function Generate-Report {
    if (-not (Test-Path $MetricsFile)) {
        Write-Error "No metrics file found. Nothing to report."
        return
    }

    $metrics = Get-Content $MetricsFile -Raw | ConvertFrom-Json
    $metrics.endTime = (Get-Date).ToString("o")
    $metrics.completionStatus = "complete"

    # Calculate phase duration
    $startTime = [DateTime]::Parse($metrics.startTime)
    $endTime = [DateTime]::Parse($metrics.endTime)
    $phaseDuration = $endTime - $startTime

    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════════════════════" -ForegroundColor Magenta
    Write-Host "                         AGENT METRICS REPORT                                  " -ForegroundColor Magenta
    Write-Host "═══════════════════════════════════════════════════════════════════════════════" -ForegroundColor Magenta
    Write-Host ""
    Write-Host "Phase: $($metrics.phase)" -ForegroundColor White
    Write-Host "Duration: $($phaseDuration.ToString('hh\:mm\:ss'))" -ForegroundColor White
    Write-Host "Period: $($startTime.ToString('yyyy-MM-dd HH:mm')) to $($endTime.ToString('HH:mm'))" -ForegroundColor Gray
    Write-Host ""

    # Summary Stats
    Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
    Write-Host "                              SUMMARY                                          " -ForegroundColor Yellow
    Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray

    $totals = $metrics.totals
    $overallSuccessRate = if ($totals.totalInvocations -gt 0) {
        [math]::Round(($totals.totalSuccesses / $totals.totalInvocations) * 100, 1)
    } else { 0 }

    Write-Host ("  Total Agent Invocations:  {0}" -f $totals.totalInvocations)
    Write-Host ("  Total Tokens Used:        {0:N0}" -f $totals.totalTokens)
    Write-Host ("  Overall Success Rate:     {0}%" -f $overallSuccessRate) -ForegroundColor $(if ($overallSuccessRate -ge 90) { "Green" } elseif ($overallSuccessRate -ge 70) { "Yellow" } else { "Red" })
    Write-Host ("  Successes / Failures:     {0} / {1}" -f $totals.totalSuccesses, ($totals.totalFailures + $totals.totalTimeouts))
    Write-Host ""

    # MODEL DISTRIBUTION (FR-003)
    if ($metrics.models -and $metrics.models.PSObject.Properties.Count -gt 0) {
        Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
        Write-Host "                         MODEL DISTRIBUTION                                    " -ForegroundColor Yellow
        Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
        Write-Host ""

        # Table header
        $modelHeader = "{0,-12} {1,12} {2,14} {3,14} {4,10}" -f "Model", "Invocations", "Tokens", "Cost Weight", "Cost %"
        Write-Host "  $modelHeader" -ForegroundColor Cyan
        Write-Host "  $("-" * 66)" -ForegroundColor DarkGray

        # Sort models by cost percentage (highest first)
        $sortedModels = $metrics.models.PSObject.Properties | Sort-Object { $_.Value.costPercent } -Descending

        foreach ($modelProp in $sortedModels) {
            $name = $modelProp.Name
            $data = $modelProp.Value

            $modelRow = "{0,-12} {1,12} {2,14:N0} {3,14} {4,10}" -f `
                $name, `
                $data.count, `
                $data.totalTokens, `
                "$($data.costWeight)x", `
                "$($data.costPercent)%"

            Write-Host "  $modelRow" -ForegroundColor White
        }
        Write-Host ""
    }

    # CATEGORY BREAKDOWN (FR-007)
    if ($metrics.categories -and $metrics.categories.PSObject.Properties.Count -gt 0) {
        Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
        Write-Host "                         CATEGORY BREAKDOWN                                    " -ForegroundColor Yellow
        Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
        Write-Host ""

        # Table header
        $catHeader = "{0,-16} {1,8} {2,14} {3,12} {4,10}" -f "Category", "Count", "Total Tokens", "Avg Tokens", "% of Total"
        Write-Host "  $catHeader" -ForegroundColor Cyan
        Write-Host "  $("-" * 64)" -ForegroundColor DarkGray

        # Calculate total tokens for percentage
        $totalCategoryTokens = 0
        foreach ($cat in $metrics.categories.PSObject.Properties) {
            $totalCategoryTokens += $cat.Value.totalTokens
        }

        # Sort categories by token usage (highest first)
        $sortedCategories = $metrics.categories.PSObject.Properties | Sort-Object { $_.Value.totalTokens } -Descending

        foreach ($catProp in $sortedCategories) {
            $name = $catProp.Name
            $data = $catProp.Value

            $pctOfTotal = if ($totalCategoryTokens -gt 0) {
                [math]::Round(($data.totalTokens / $totalCategoryTokens) * 100, 1)
            } else { 0 }

            $catRow = "{0,-16} {1,8} {2,14:N0} {3,12:N0} {4,10}" -f `
                $name, `
                $data.count, `
                $data.totalTokens, `
                $data.avgTokens, `
                "$pctOfTotal%"

            Write-Host "  $catRow" -ForegroundColor White
        }
        Write-Host ""
    }

    # PARALLELIZATION METRICS (FR-009)
    if ($totals.parallelInvocations -gt 0 -or ($metrics.parallelGroups -and $metrics.parallelGroups.PSObject.Properties.Count -gt 0)) {
        Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
        Write-Host "                      PARALLELIZATION METRICS                                  " -ForegroundColor Yellow
        Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
        Write-Host ""

        $parallelPct = if ($totals.totalInvocations -gt 0) {
            [math]::Round(($totals.parallelInvocations / $totals.totalInvocations) * 100, 1)
        } else { 0 }
        $sequentialPct = 100 - $parallelPct

        Write-Host ("  Total Invocations:        {0}" -f $totals.totalInvocations)
        Write-Host ("  Parallel Declarations:    {0} ({1}%)" -f $totals.parallelInvocations, $parallelPct)
        Write-Host ("  Sequential:               {0} ({1}%)" -f $totals.sequentialInvocations, $sequentialPct)

        if ($metrics.parallelGroups -and $metrics.parallelGroups.PSObject.Properties.Count -gt 0) {
            $groupCount = $metrics.parallelGroups.PSObject.Properties.Count
            $maxConcurrent = 0
            $totalEfficiency = 0
            $totalTimeReduction = 0

            foreach ($grp in $metrics.parallelGroups.PSObject.Properties) {
                if ($grp.Value.concurrencyMetrics.maxConcurrent -gt $maxConcurrent) {
                    $maxConcurrent = $grp.Value.concurrencyMetrics.maxConcurrent
                }
                $totalEfficiency += $grp.Value.concurrencyMetrics.efficiency
                $totalTimeReduction += $grp.Value.concurrencyMetrics.timeReduction
            }
            $avgEfficiency = [math]::Round($totalEfficiency / $groupCount, 1)
            $avgTimeReduction = [math]::Round($totalTimeReduction / $groupCount, 1)

            Write-Host ""
            Write-Host ("  Parallel Groups:          {0}" -f $groupCount)
            Write-Host ("  Max Concurrent Observed:  {0} agents" -f $maxConcurrent)

            $effColor = if ($avgEfficiency -ge 80) { "Green" } elseif ($avgEfficiency -ge 50) { "Yellow" } else { "Red" }
            $effLabel = if ($avgEfficiency -ge 80) { "excellent" } elseif ($avgEfficiency -ge 50) { "good" } else { "needs improvement" }
            Write-Host ("  Efficiency:               {0}% ({1})" -f $avgEfficiency, $effLabel) -ForegroundColor $effColor
            Write-Host ("  Time Reduction:           {0}% vs sequential baseline" -f $avgTimeReduction)
        }
        Write-Host ""
    }

    # Agent Performance Table
    Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
    Write-Host "                         AGENT PERFORMANCE                                     " -ForegroundColor Yellow
    Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
    Write-Host ""

    # Table header
    $header = "{0,-25} {1,6} {2,8} {3,10} {4,12} {5,10} {6,10} {7,10}" -f "Agent", "Count", "Success", "Fail/TO", "Avg Tokens", "Avg Time", "Min Time", "Max Time"
    Write-Host $header -ForegroundColor Cyan
    Write-Host ("-" * 95) -ForegroundColor DarkGray

    # Sort agents by count (most used first)
    $sortedAgents = $metrics.agents.PSObject.Properties | Sort-Object { $_.Value.count } -Descending

    foreach ($agentProp in $sortedAgents) {
        $name = $agentProp.Name
        $data = $agentProp.Value

        $successRate = Get-SuccessRate $data
        $failCount = $data.failures + $data.timeouts

        $row = "{0,-25} {1,6} {2,8} {3,10} {4,12:N0} {5,10} {6,10} {7,10}" -f `
            $name, `
            $data.count, `
            $successRate, `
            $failCount, `
            $data.avgTokens, `
            (Format-Duration $data.avgDurationMs), `
            (Format-Duration $data.minDurationMs), `
            (Format-Duration $data.maxDurationMs)

        $color = if ($data.failures -gt 0 -or $data.timeouts -gt 0) { "Yellow" } else { "White" }
        Write-Host $row -ForegroundColor $color
    }

    Write-Host ""

    # Efficiency Indicators
    Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
    Write-Host "                        EFFICIENCY INDICATORS                                  " -ForegroundColor Yellow
    Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
    Write-Host ""

    # Most used agent
    $mostUsed = $sortedAgents | Select-Object -First 1
    if ($mostUsed) {
        Write-Host "  Most Used Agent:          $($mostUsed.Name) ($($mostUsed.Value.count) invocations)" -ForegroundColor Green
    }

    # Most token-intensive agent
    $mostTokens = $sortedAgents | Sort-Object { $_.Value.avgTokens } -Descending | Select-Object -First 1
    if ($mostTokens) {
        Write-Host "  Highest Token Usage:      $($mostTokens.Name) ($($mostTokens.Value.avgTokens.ToString('N0')) avg tokens)" -ForegroundColor Yellow
    }

    # Fastest agent (by avg time)
    $fastest = $sortedAgents | Where-Object { $_.Value.avgDurationMs -gt 0 } | Sort-Object { $_.Value.avgDurationMs } | Select-Object -First 1
    if ($fastest) {
        Write-Host "  Fastest Agent (avg):      $($fastest.Name) ($(Format-Duration $fastest.Value.avgDurationMs))" -ForegroundColor Cyan
    }

    # Agents with failures
    $failingAgents = $sortedAgents | Where-Object { $_.Value.failures -gt 0 -or $_.Value.timeouts -gt 0 }
    if ($failingAgents) {
        Write-Host ""
        Write-Host "  ⚠ Agents with Failures:" -ForegroundColor Red
        foreach ($agent in $failingAgents) {
            Write-Host "    - $($agent.Name): $($agent.Value.failures) failures, $($agent.Value.timeouts) timeouts" -ForegroundColor Red
        }
    }

    Write-Host ""

    # Token Cost Estimate (optional - based on typical API pricing)
    $estimatedCost = [math]::Round($totals.totalTokens * 0.000003, 2)  # Rough estimate
    $tokenCount = $totals.totalTokens.ToString('N0')
    $costLine = "  Estimated Token Cost:     ~`${0} ({1} tokens)" -f $estimatedCost, $tokenCount
    Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
    Write-Host $costLine -ForegroundColor Gray
    Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
    Write-Host ""

    # Save updated metrics with end time
    $metrics | ConvertTo-Json -Depth 10 | Set-Content $MetricsFile -Encoding UTF8

    # Also output JSON report
    $jsonReport = Join-Path $MetricsDir "agent-metrics-report.json"
    $metrics | ConvertTo-Json -Depth 10 | Set-Content $jsonReport -Encoding UTF8
    Write-Host "Full report saved to: $jsonReport" -ForegroundColor Gray
    Write-Host ""
}

function Generate-TrendsReport {
    <#
    .SYNOPSIS
        Generates a trend analysis report from archived sessions

    .DESCRIPTION
        Loads archived sessions, filters by date range and feature branch,
        calculates trends for tokens, success rate, and duration,
        and generates insights for concerning patterns.

    .PARAMETER Days
        Number of days to analyze (default: 30)

    .PARAMETER Branch
        Filter by specific feature branch (optional)
    #>
    param(
        [int]$Days = 30,
        [string]$Branch = ""
    )

    $archiveDir = Join-Path $MetricsDir "archive"

    # Check if archive directory exists
    if (-not (Test-Path $archiveDir)) {
        Write-Host "No archived sessions found. Run -Action Reset to archive sessions first." -ForegroundColor Yellow
        return
    }

    # Load all archive files
    $archiveFiles = Get-ChildItem $archiveDir -Filter "agent-metrics-*.json" -ErrorAction SilentlyContinue
    if (-not $archiveFiles -or $archiveFiles.Count -eq 0) {
        Write-Host "No archived sessions found." -ForegroundColor Yellow
        return
    }

    # Load and parse all sessions
    $allSessions = @()
    foreach ($file in $archiveFiles) {
        try {
            $session = Get-Content $file.FullName -Raw | ConvertFrom-Json
            $allSessions += $session
        }
        catch {
            # Skip invalid files
        }
    }

    # Filter out legacy schema (< 2.0.0) per FR-018
    $validSessions = @()
    foreach ($s in $allSessions) {
        if (Test-SchemaVersion -Session $s) {
            $validSessions += $s
        }
    }

    if ($validSessions.Count -eq 0) {
        Write-Host "No v2.0.0+ sessions available for trend analysis. Legacy archives are excluded." -ForegroundColor Yellow
        return
    }

    # Filter by date range
    $cutoffDate = (Get-Date).AddDays(-$Days)
    $filteredSessions = @()
    foreach ($s in $validSessions) {
        try {
            $sessionDate = [DateTime]::Parse($s.startTime)
            if ($sessionDate -ge $cutoffDate) {
                $filteredSessions += $s
            }
        }
        catch {
            # Skip sessions with invalid dates
        }
    }

    # Filter by feature branch if specified
    if ($Branch) {
        $filteredSessions = @($filteredSessions | Where-Object { $_.featureBranch -eq $Branch })
    }

    if ($filteredSessions.Count -eq 0) {
        Write-Host "No sessions found matching the specified criteria." -ForegroundColor Yellow
        return
    }

    # Sort sessions by start time (wrap in @() to preserve array for single elements)
    $sortedSessions = @($filteredSessions | Sort-Object { [DateTime]::Parse($_.startTime) })

    # Calculate period
    $periodStart = [DateTime]::Parse($sortedSessions[0].startTime)
    $periodEnd = [DateTime]::Parse($sortedSessions[-1].startTime)
    $branchLabel = if ($Branch) { $Branch } else { "all branches" }

    # --- Display Report Header ---
    Write-Host ""
    Write-Host "=======================================================================" -ForegroundColor Magenta
    Write-Host "                         TREND ANALYSIS REPORT                          " -ForegroundColor Magenta
    Write-Host "=======================================================================" -ForegroundColor Magenta
    Write-Host ""
    Write-Host "Period: $($periodStart.ToString('yyyy-MM-dd')) to $($periodEnd.ToString('yyyy-MM-dd')) ($Days days)" -ForegroundColor White
    Write-Host "Sessions Analyzed: $($sortedSessions.Count)" -ForegroundColor White
    Write-Host "Branch: $branchLabel" -ForegroundColor Gray
    Write-Host ""

    # --- TOKEN USAGE TREND ---
    Write-Host "-----------------------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host "                          TOKEN USAGE TREND                              " -ForegroundColor Yellow
    Write-Host "-----------------------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host ""

    $tokenValues = @()
    $tokenDates = @()
    foreach ($s in $sortedSessions) {
        $tokenValues += $s.totals.totalTokens
        $tokenDates += [DateTime]::Parse($s.startTime).ToString("yyyy-MM-dd")
    }

    # Display token table
    $tokenHeader = "{0,-12} {1,-25} {2,12} {3,12}" -f "Date", "Session", "Tokens", "Trend"
    Write-Host "  $tokenHeader" -ForegroundColor Cyan
    Write-Host "  $("-" * 65)" -ForegroundColor DarkGray

    for ($i = 0; $i -lt $sortedSessions.Count; $i++) {
        $s = $sortedSessions[$i]
        $date = [DateTime]::Parse($s.startTime).ToString("yyyy-MM-dd")
        $sessionLabel = "$($s.phase) ($($s.featureBranch))"
        if ($sessionLabel.Length -gt 24) { $sessionLabel = $sessionLabel.Substring(0, 21) + "..." }
        $tokens = $s.totals.totalTokens

        if ($i -eq 0) {
            $trendLabel = "baseline"
        }
        else {
            $prev = $sortedSessions[$i - 1].totals.totalTokens
            if ($prev -gt 0) {
                $changePct = [math]::Round((($tokens - $prev) / $prev) * 100, 1)
                $arrow = if ($changePct -ge 0) { [char]0x2191 } else { [char]0x2193 }
                $trendLabel = "$arrow $changePct%"
            }
            else {
                $trendLabel = "N/A"
            }
        }

        $row = "{0,-12} {1,-25} {2,12:N0} {3,12}" -f $date, $sessionLabel, $tokens, $trendLabel
        Write-Host "  $row" -ForegroundColor White
    }

    # Overall token trend
    $firstTokens = $sortedSessions[0].totals.totalTokens
    $lastTokens = $sortedSessions[-1].totals.totalTokens
    $overallTokenChange = if ($firstTokens -gt 0) {
        [math]::Round((($lastTokens - $firstTokens) / $firstTokens) * 100, 1)
    } else { 0 }
    $tokenTrendDirection = if ($overallTokenChange -gt 0) { "increasing" } elseif ($overallTokenChange -lt 0) { "decreasing" } else { "stable" }

    Write-Host ""
    $changeSign = if ($overallTokenChange -ge 0) { "+" } else { "" }
    Write-Host "  Overall: ${changeSign}${overallTokenChange}% ($tokenTrendDirection)" -ForegroundColor $(if ($overallTokenChange -gt 20) { "Red" } elseif ($overallTokenChange -gt 0) { "Yellow" } else { "Green" })
    Write-Host ""

    # --- SUCCESS RATE TREND ---
    Write-Host "-----------------------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host "                          SUCCESS RATE TREND                             " -ForegroundColor Yellow
    Write-Host "-----------------------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host ""

    $successHeader = "{0,-12} {1,-25} {2,14} {3,12}" -f "Date", "Session", "Success Rate", "Trend"
    Write-Host "  $successHeader" -ForegroundColor Cyan
    Write-Host "  $("-" * 65)" -ForegroundColor DarkGray

    for ($i = 0; $i -lt $sortedSessions.Count; $i++) {
        $s = $sortedSessions[$i]
        $date = [DateTime]::Parse($s.startTime).ToString("yyyy-MM-dd")
        $sessionLabel = "$($s.phase) ($($s.featureBranch))"
        if ($sessionLabel.Length -gt 24) { $sessionLabel = $sessionLabel.Substring(0, 21) + "..." }
        $successRate = $s.totals.overallSuccessRate

        if ($i -eq 0) {
            $trendLabel = "baseline"
        }
        else {
            $prev = $sortedSessions[$i - 1].totals.overallSuccessRate
            if ($successRate -gt $prev) {
                $trendLabel = [string]([char]0x2191) + " improving"
            }
            elseif ($successRate -lt $prev) {
                $trendLabel = [string]([char]0x2193) + " declining"
            }
            else {
                $trendLabel = [string]([char]0x2192) + " stable"
            }
        }

        $row = "{0,-12} {1,-25} {2,14} {3,12}" -f $date, $sessionLabel, "$($successRate)%", $trendLabel
        Write-Host "  $row" -ForegroundColor White
    }

    $firstSuccess = $sortedSessions[0].totals.overallSuccessRate
    $lastSuccess = $sortedSessions[-1].totals.overallSuccessRate
    $successTrend = if ($lastSuccess -gt $firstSuccess) { "improving" } elseif ($lastSuccess -lt $firstSuccess) { "declining" } else { "stable" }

    Write-Host ""
    Write-Host "  Overall: Success rate $successTrend ($firstSuccess% -> $lastSuccess%)" -ForegroundColor $(if ($successTrend -eq "improving") { "Green" } elseif ($successTrend -eq "declining") { "Red" } else { "Yellow" })
    Write-Host ""

    # --- AVERAGE DURATION TREND ---
    Write-Host "-----------------------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host "                        AVG DURATION TREND                               " -ForegroundColor Yellow
    Write-Host "-----------------------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host ""

    $durationHeader = "{0,-12} {1,-25} {2,14} {3,12}" -f "Date", "Session", "Avg Duration", "Trend"
    Write-Host "  $durationHeader" -ForegroundColor Cyan
    Write-Host "  $("-" * 65)" -ForegroundColor DarkGray

    for ($i = 0; $i -lt $sortedSessions.Count; $i++) {
        $s = $sortedSessions[$i]
        $date = [DateTime]::Parse($s.startTime).ToString("yyyy-MM-dd")
        $sessionLabel = "$($s.phase) ($($s.featureBranch))"
        if ($sessionLabel.Length -gt 24) { $sessionLabel = $sessionLabel.Substring(0, 21) + "..." }
        $avgDuration = $s.totals.avgDurationMs

        if ($i -eq 0) {
            $trendLabel = "baseline"
        }
        else {
            $prev = $sortedSessions[$i - 1].totals.avgDurationMs
            if ($prev -gt 0) {
                $changePct = [math]::Round((($avgDuration - $prev) / $prev) * 100, 1)
                if ($changePct -lt 0) {
                    $trendLabel = [string]([char]0x2193) + " faster"
                }
                elseif ($changePct -gt 0) {
                    $trendLabel = [string]([char]0x2191) + " slower"
                }
                else {
                    $trendLabel = [string]([char]0x2192) + " stable"
                }
            }
            else {
                $trendLabel = "N/A"
            }
        }

        $row = "{0,-12} {1,-25} {2,14} {3,12}" -f $date, $sessionLabel, (Format-Duration $avgDuration), $trendLabel
        Write-Host "  $row" -ForegroundColor White
    }

    $firstDuration = $sortedSessions[0].totals.avgDurationMs
    $lastDuration = $sortedSessions[-1].totals.avgDurationMs
    $durationChange = if ($firstDuration -gt 0) {
        [math]::Round((($lastDuration - $firstDuration) / $firstDuration) * 100, 1)
    } else { 0 }
    $durationTrend = if ($durationChange -lt 0) { "improving" } elseif ($durationChange -gt 0) { "slower" } else { "stable" }

    Write-Host ""
    Write-Host "  Overall: Duration $durationTrend (${durationChange}% change)" -ForegroundColor $(if ($durationTrend -eq "improving") { "Green" } elseif ($durationTrend -eq "slower") { "Red" } else { "Yellow" })
    Write-Host ""

    # --- INSIGHTS ---
    Write-Host "-----------------------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host "                              INSIGHTS                                   " -ForegroundColor Yellow
    Write-Host "-----------------------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host ""

    $hasInsights = $false

    # Warning: Token usage increased >20%
    if ($overallTokenChange -gt 20) {
        Write-Host "  WARNING: Token usage increased ${overallTokenChange}% - consider prompt optimization" -ForegroundColor Red
        $hasInsights = $true
    }

    # Positive: Success rate improved
    if ($lastSuccess -gt $firstSuccess) {
        $successImprovement = [math]::Round($lastSuccess - $firstSuccess, 1)
        Write-Host "  POSITIVE: Success rate improved by ${successImprovement}% ($firstSuccess% -> $lastSuccess%)" -ForegroundColor Green
        $hasInsights = $true
    }

    # Warning: Success rate declined
    if ($lastSuccess -lt $firstSuccess) {
        $successDecline = [math]::Round($firstSuccess - $lastSuccess, 1)
        Write-Host "  WARNING: Success rate declined by ${successDecline}% ($firstSuccess% -> $lastSuccess%)" -ForegroundColor Red
        $hasInsights = $true
    }

    # Positive: Duration improved
    if ($durationChange -lt -10) {
        Write-Host "  POSITIVE: Average duration decreased by $([math]::Abs($durationChange))%" -ForegroundColor Green
        $hasInsights = $true
    }

    # Warning: Duration increased significantly
    if ($durationChange -gt 20) {
        Write-Host "  WARNING: Average duration increased by ${durationChange}% - investigate bottlenecks" -ForegroundColor Red
        $hasInsights = $true
    }

    # Info: Limited data
    if ($sortedSessions.Count -lt 3) {
        Write-Host "  INFO: Limited data for trend analysis ($($sortedSessions.Count) sessions)" -ForegroundColor Gray
        $hasInsights = $true
    }

    if (-not $hasInsights) {
        Write-Host "  No concerning trends detected." -ForegroundColor Green
    }

    Write-Host ""
}

function Reset-Metrics {
    if (Test-Path $MetricsFile) {
        # Archive current metrics
        $archiveDir = Join-Path $MetricsDir "archive"
        if (-not (Test-Path $archiveDir)) {
            New-Item -ItemType Directory -Path $archiveDir -Force | Out-Null
        }

        $timestamp = (Get-Date).ToString("yyyyMMdd-HHmmss")
        $archivePath = Join-Path $archiveDir "agent-metrics-$timestamp.json"
        Copy-Item $MetricsFile $archivePath
        Remove-Item $MetricsFile

        Write-Host "Metrics archived to: $archivePath" -ForegroundColor Yellow

        # Auto-cleanup old archives per retention settings (FR-020)
        $settings = Get-MetricsSettings
        if ($settings.retention.autoCleanupEnabled) {
            $retentionDays = $settings.retention.archiveDays
            $cutoffDate = (Get-Date).AddDays(-$retentionDays)

            $archiveFiles = Get-ChildItem $archiveDir -Filter "agent-metrics-*.json" -ErrorAction SilentlyContinue
            $deletedCount = 0

            foreach ($file in $archiveFiles) {
                if ($file.LastWriteTime -lt $cutoffDate) {
                    Remove-Item $file.FullName -Force
                    $deletedCount++
                }
            }

            if ($deletedCount -gt 0) {
                Write-Host "Cleaned up $deletedCount archive(s) older than $retentionDays days." -ForegroundColor Gray
            }
        }

        Write-Host "Metrics reset for next phase." -ForegroundColor Green
    } else {
        Write-Host "No metrics to reset." -ForegroundColor Gray
    }
}

# Execute action - only run when not being dot-sourced for testing
# When dot-sourced, $MyInvocation.InvocationName will be '.' or '&'
$isDotSourced = $MyInvocation.InvocationName -eq '.' -or $MyInvocation.InvocationName -eq '&'
if (-not $isDotSourced -and $Action) {
    switch ($Action) {
        "Init" { Initialize-Metrics -Phase $PhaseName -Branch $FeatureBranch }
        "Record" {
            Record-AgentMetric -Agent $AgentName -Task $TaskId -CompletionStatus $Status `
                -Tokens $TokensUsed -Duration $DurationMs -Desc $Description `
                -ModelName $Model -CategoryName $Category `
                -Parallel $IsParallel.IsPresent -GroupId $ParallelGroupId -GrpSize $GroupSize
        }
        "Report" { Generate-Report }
        "Reset" { Reset-Metrics }
        "Trends" {
            Generate-TrendsReport -Days $Days -Branch $FeatureBranch
        }
        "Compare" {
            # Placeholder for Compare action (US4)
            Write-Host "Compare action not yet implemented (US4)" -ForegroundColor Yellow
        }
        "Cumulative" {
            # Placeholder for Cumulative action (US5)
            Write-Host "Cumulative action not yet implemented (US5)" -ForegroundColor Yellow
        }
        "Export" {
            # Placeholder for Export action (US5)
            Write-Host "Export action not yet implemented (US5)" -ForegroundColor Yellow
        }
    }
}
