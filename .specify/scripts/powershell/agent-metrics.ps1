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
    [string]$OutputPath = "",
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

    # Token Cost Estimate (display-only, based on ~$3/1M tokens blended average)
    $estimatedCost = [math]::Round($totals.totalTokens * 0.000003, 2)
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

function Resolve-SessionFile {
    <#
    .SYNOPSIS
        Resolves a session identifier to an archive file path

    .DESCRIPTION
        Accepts: full filename, timestamp (YYYYMMDD-HHMMSS), or date-only (YYYYMMDD).
        For date-only, picks the latest matching file.

    .PARAMETER SessionId
        The session identifier string

    .OUTPUTS
        Full path to the archive file, or $null if not found
    #>
    param(
        [Parameter(Mandatory=$true)]
        [string]$SessionId
    )

    $archiveDir = Join-Path $MetricsDir "archive"
    if (-not (Test-Path $archiveDir)) { return $null }

    # Try full filename first
    $fullPath = Join-Path $archiveDir $SessionId
    if (Test-Path $fullPath) { return $fullPath }

    # Try as timestamp pattern (YYYYMMDD-HHMMSS)
    $timestampPath = Join-Path $archiveDir "agent-metrics-$SessionId.json"
    if (Test-Path $timestampPath) { return $timestampPath }

    # Try as date-only pattern (YYYYMMDD) - pick latest match
    $datePattern = "agent-metrics-$SessionId-*.json"
    $matches = Get-ChildItem $archiveDir -Filter $datePattern -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending
    if ($matches -and $matches.Count -gt 0) {
        return $matches[0].FullName
    }

    return $null
}

function Compare-Sessions {
    <#
    .SYNOPSIS
        Compares two archived sessions side-by-side (FR-011)

    .DESCRIPTION
        Loads both archive files, validates schema version,
        calculates deltas for all metrics, and displays comparison.

    .PARAMETER Sess1
        First session identifier (filename, timestamp, or date)

    .PARAMETER Sess2
        Second session identifier (filename, timestamp, or date)
    #>
    param(
        [Parameter(Mandatory=$true)]
        [string]$Sess1,
        [Parameter(Mandatory=$true)]
        [string]$Sess2
    )

    # Resolve session files
    $file1 = Resolve-SessionFile -SessionId $Sess1
    $file2 = Resolve-SessionFile -SessionId $Sess2

    if (-not $file1) {
        Write-Host "Session not found: $Sess1" -ForegroundColor Red
        return
    }
    if (-not $file2) {
        Write-Host "Session not found: $Sess2" -ForegroundColor Red
        return
    }

    # Load sessions
    $session1 = Get-Content $file1 -Raw | ConvertFrom-Json
    $session2 = Get-Content $file2 -Raw | ConvertFrom-Json

    # Validate schema versions
    if (-not (Test-SchemaVersion -Session $session1)) {
        $name1 = Split-Path $file1 -Leaf
        Write-Host "Session $name1 is legacy format (pre-2.0.0). Compare requires v2.0.0+ sessions." -ForegroundColor Red
        return
    }
    if (-not (Test-SchemaVersion -Session $session2)) {
        $name2 = Split-Path $file2 -Leaf
        Write-Host "Session $name2 is legacy format (pre-2.0.0). Compare requires v2.0.0+ sessions." -ForegroundColor Red
        return
    }

    $name1 = Split-Path $file1 -Leaf
    $name2 = Split-Path $file2 -Leaf

    # Extract totals
    $t1 = $session1.totals
    $t2 = $session2.totals

    # --- Header ---
    Write-Host ""
    Write-Host "=======================================================================" -ForegroundColor Magenta
    Write-Host "                         SESSION COMPARISON                              " -ForegroundColor Magenta
    Write-Host "=======================================================================" -ForegroundColor Magenta
    Write-Host ""
    Write-Host "Session 1: $name1" -ForegroundColor White
    Write-Host "  Phase: $($session1.phase) | Branch: $($session1.featureBranch) | Date: $([DateTime]::Parse($session1.startTime).ToString('yyyy-MM-dd'))" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Session 2: $name2" -ForegroundColor White
    Write-Host "  Phase: $($session2.phase) | Branch: $($session2.featureBranch) | Date: $([DateTime]::Parse($session2.startTime).ToString('yyyy-MM-dd'))" -ForegroundColor Gray
    Write-Host ""

    # --- SUMMARY COMPARISON ---
    Write-Host "-----------------------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host "                          SUMMARY COMPARISON                             " -ForegroundColor Yellow
    Write-Host "-----------------------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host ""

    $compHeader = "{0,-22} {1,14} {2,14} {3,18}" -f "Metric", "Session 1", "Session 2", "Delta"
    Write-Host "  $compHeader" -ForegroundColor Cyan
    Write-Host "  $("-" * 70)" -ForegroundColor DarkGray

    # Invocations
    $invDelta = $t2.totalInvocations - $t1.totalInvocations
    $invPct = if ($t1.totalInvocations -gt 0) { [math]::Round(($invDelta / $t1.totalInvocations) * 100, 1) } else { 0 }
    $invSign = if ($invDelta -ge 0) { "+" } else { "" }
    $invRow = "{0,-22} {1,14} {2,14} {3,18}" -f "Invocations", $t1.totalInvocations, $t2.totalInvocations, "${invSign}${invDelta} (${invSign}${invPct}%)"
    Write-Host "  $invRow" -ForegroundColor White

    # Tokens
    $tokDelta = $t2.totalTokens - $t1.totalTokens
    $tokPct = if ($t1.totalTokens -gt 0) { [math]::Round(($tokDelta / $t1.totalTokens) * 100, 1) } else { 0 }
    $tokSign = if ($tokDelta -ge 0) { "+" } else { "" }
    $tokRow = "{0,-22} {1,14:N0} {2,14:N0} {3,18}" -f "Total Tokens", $t1.totalTokens, $t2.totalTokens, "${tokSign}$($tokDelta.ToString('N0')) (${tokSign}${tokPct}%)"
    Write-Host "  $tokRow" -ForegroundColor White

    # Success Rate
    $sr1 = $t1.overallSuccessRate
    $sr2 = $t2.overallSuccessRate
    $srDelta = [math]::Round($sr2 - $sr1, 1)
    $srSign = if ($srDelta -ge 0) { "+" } else { "" }
    $srArrow = if ($srDelta -gt 0) { [string]([char]0x2191) } elseif ($srDelta -lt 0) { [string]([char]0x2193) } else { [string]([char]0x2192) }
    $srRow = "{0,-22} {1,14} {2,14} {3,18}" -f "Success Rate", "${sr1}%", "${sr2}%", "${srSign}${srDelta}% $srArrow"
    Write-Host "  $srRow" -ForegroundColor White

    # Avg Duration
    $ad1 = $t1.avgDurationMs
    $ad2 = $t2.avgDurationMs
    $adDelta = $ad2 - $ad1
    $adPct = if ($ad1 -gt 0) { [math]::Round(($adDelta / $ad1) * 100, 1) } else { 0 }
    $adSign = if ($adDelta -ge 0) { "+" } else { "" }
    $adArrow = if ($adDelta -lt 0) { [string]([char]0x2193) } elseif ($adDelta -gt 0) { [string]([char]0x2191) } else { [string]([char]0x2192) }
    $adRow = "{0,-22} {1,14} {2,14} {3,18}" -f "Avg Duration", (Format-Duration $ad1), (Format-Duration $ad2), "${adSign}$(Format-Duration ([math]::Abs($adDelta))) $adArrow"
    Write-Host "  $adRow" -ForegroundColor White

    Write-Host ""

    # --- MODEL COMPARISON ---
    $hasModels1 = $session1.models -and $session1.models.PSObject.Properties.Count -gt 0
    $hasModels2 = $session2.models -and $session2.models.PSObject.Properties.Count -gt 0
    if ($hasModels1 -or $hasModels2) {
        Write-Host "-----------------------------------------------------------------------" -ForegroundColor DarkGray
        Write-Host "                          MODEL COMPARISON                               " -ForegroundColor Yellow
        Write-Host "-----------------------------------------------------------------------" -ForegroundColor DarkGray
        Write-Host ""

        $mdlHeader = "{0,-10} {1,8} {2,8} {3,8} {4,8} {5,14}" -f "Model", "S1 Cnt", "S1 Cost%", "S2 Cnt", "S2 Cost%", "Change"
        Write-Host "  $mdlHeader" -ForegroundColor Cyan
        Write-Host "  $("-" * 60)" -ForegroundColor DarkGray

        # Collect all model names from both sessions
        $allModels = @()
        if ($hasModels1) { $allModels += $session1.models.PSObject.Properties.Name }
        if ($hasModels2) { $allModels += $session2.models.PSObject.Properties.Name }
        $allModels = $allModels | Sort-Object -Unique

        foreach ($modelName in $allModels) {
            $m1Count = 0; $m1Cost = 0
            $m2Count = 0; $m2Cost = 0
            if ($hasModels1 -and $session1.models.PSObject.Properties[$modelName]) {
                $m1Count = $session1.models.$modelName.count
                $m1Cost = $session1.models.$modelName.costPercent
            }
            if ($hasModels2 -and $session2.models.PSObject.Properties[$modelName]) {
                $m2Count = $session2.models.$modelName.count
                $m2Cost = $session2.models.$modelName.costPercent
            }

            $changeLabel = if ($m2Cost -gt $m1Cost) { [string]([char]0x2191) + " more usage" }
                elseif ($m2Cost -lt $m1Cost) { [string]([char]0x2193) + " less usage" }
                else { [string]([char]0x2192) + " stable" }

            $mdlRow = "{0,-10} {1,8} {2,8} {3,8} {4,8} {5,14}" -f `
                $modelName, $m1Count, "$($m1Cost)%", $m2Count, "$($m2Cost)%", $changeLabel
            Write-Host "  $mdlRow" -ForegroundColor White
        }
        Write-Host ""
    }

    # --- INSIGHTS ---
    Write-Host "-----------------------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host "                              INSIGHTS                                   " -ForegroundColor Yellow
    Write-Host "-----------------------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host ""

    if ($srDelta -gt 0) {
        Write-Host "  $([char]0x2191) Success rate improved by ${srDelta}%" -ForegroundColor Green
    } elseif ($srDelta -lt 0) {
        Write-Host "  $([char]0x2193) Success rate declined by $([math]::Abs($srDelta))%" -ForegroundColor Red
    }

    if ($tokPct -gt 20) {
        Write-Host "  $([char]0x2193) Token usage increased ${tokPct}% - monitor for trends" -ForegroundColor Yellow
    } elseif ($tokPct -lt -10) {
        Write-Host "  $([char]0x2191) Token usage decreased $([math]::Abs($tokPct))% - efficiency improving" -ForegroundColor Green
    }

    if ($adPct -lt -10) {
        Write-Host "  $([char]0x2191) Average duration decreased - agents running faster" -ForegroundColor Green
    } elseif ($adPct -gt 20) {
        Write-Host "  $([char]0x2193) Average duration increased ${adPct}% - investigate bottlenecks" -ForegroundColor Yellow
    }

    Write-Host ""
}

function Export-Metrics {
    <#
    .SYNOPSIS
        Exports metrics to CSV or JSON format for external analysis (FR-006)

    .PARAMETER Format
        Export format: "CSV" or "JSON"

    .PARAMETER OutputPath
        Output file path (auto-generated if not specified)

    .PARAMETER IncludeArchives
        Include all archived sessions

    .PARAMETER Branch
        Filter archives by feature branch
    #>
    param(
        [string]$Format = "CSV",
        [string]$OutputPath = "",
        [switch]$IncludeArchives,
        [string]$Branch = ""
    )

    $allInvocations = @()
    $featureBranch = ""

    # Load active session invocations
    if (Test-Path $MetricsFile) {
        $activeSession = Get-Content $MetricsFile -Raw | ConvertFrom-Json
        $featureBranch = $activeSession.featureBranch
        if ($activeSession.invocations -and $activeSession.invocations.Count -gt 0) {
            foreach ($inv in $activeSession.invocations) {
                # Attach featureBranch from session
                if (-not $inv.PSObject.Properties["featureBranch"]) {
                    $inv | Add-Member -NotePropertyName "featureBranch" -NotePropertyValue $activeSession.featureBranch -Force
                }
                $allInvocations += $inv
            }
        }
    }

    # Load archives if requested
    if ($IncludeArchives) {
        $archiveDir = Join-Path $MetricsDir "archive"
        if (Test-Path $archiveDir) {
            $archiveFiles = Get-ChildItem $archiveDir -Filter "agent-metrics-*.json" -ErrorAction SilentlyContinue
            foreach ($file in $archiveFiles) {
                try {
                    $session = Get-Content $file.FullName -Raw | ConvertFrom-Json
                    if (-not (Test-SchemaVersion -Session $session)) { continue }
                    if ($Branch -and $session.featureBranch -ne $Branch) { continue }
                    if ($session.invocations -and $session.invocations.Count -gt 0) {
                        foreach ($inv in $session.invocations) {
                            if (-not $inv.PSObject.Properties["featureBranch"]) {
                                $inv | Add-Member -NotePropertyName "featureBranch" -NotePropertyValue $session.featureBranch -Force
                            }
                            $allInvocations += $inv
                        }
                    }
                }
                catch {
                    # Skip invalid files
                }
            }
        }
    }

    if ($allInvocations.Count -eq 0) {
        Write-Host "No metrics found to export." -ForegroundColor Yellow
        return
    }

    # Auto-generate output path if not specified
    if (-not $OutputPath) {
        $timestamp = (Get-Date).ToString("yyyyMMdd-HHmmss")
        $suffix = if ($IncludeArchives) { "-all" } else { "" }
        $ext = if ($Format -eq "JSON") { "json" } else { "csv" }
        $OutputPath = Join-Path $MetricsDir "metrics-export$suffix-$timestamp.$ext"
    }

    if ($Format -eq "CSV") {
        $lines = @()
        $lines += "Timestamp,Phase,FeatureBranch,Agent,Model,TaskId,Category,Status,Tokens,DurationMs,DurationSec,IsParallel,ParallelGroupId,Description"

        foreach ($inv in $allInvocations) {
            $durationSec = [math]::Round($inv.durationMs / 1000, 1)
            $isParallel = if ($inv.isParallel) { "TRUE" } else { "FALSE" }
            $pgId = if ($inv.parallelGroupId) { $inv.parallelGroupId } else { "" }
            $desc = if ($inv.description) { "`"$($inv.description -replace '"', '""')`"" } else { "" }
            $fb = if ($inv.featureBranch) { $inv.featureBranch } else { $featureBranch }

            $lines += "$($inv.timestamp),$($inv.phase),$fb,$($inv.agent),$($inv.model),$($inv.taskId),$($inv.category),$($inv.status),$($inv.tokens),$($inv.durationMs),$durationSec,$isParallel,$pgId,$desc"
        }

        $lines | Set-Content $OutputPath -Encoding UTF8
    }
    elseif ($Format -eq "JSON") {
        $exportArray = @()
        foreach ($inv in $allInvocations) {
            $exportArray += @{
                Timestamp = $inv.timestamp
                Phase = $inv.phase
                FeatureBranch = if ($inv.featureBranch) { $inv.featureBranch } else { $featureBranch }
                Agent = $inv.agent
                Model = $inv.model
                TaskId = $inv.taskId
                Category = $inv.category
                Status = $inv.status
                Tokens = $inv.tokens
                DurationMs = $inv.durationMs
                DurationSec = [math]::Round($inv.durationMs / 1000, 1)
                IsParallel = [bool]$inv.isParallel
                ParallelGroupId = $inv.parallelGroupId
                Description = $inv.description
            }
        }

        # Ensure array wrapper even for single item (ConvertTo-Json unwraps single-element arrays)
        $jsonContent = $exportArray | ConvertTo-Json -Depth 5
        if ($exportArray.Count -eq 1) {
            $jsonContent = "[$jsonContent]"
        }
        $jsonContent | Set-Content $OutputPath -Encoding UTF8
    }

    Write-Host "Exported $($allInvocations.Count) invocations to: $(Split-Path $OutputPath -Leaf)" -ForegroundColor Green
}

function Generate-CumulativeReport {
    <#
    .SYNOPSIS
        Generates cross-phase cumulative report for a feature branch (FR-017)

    .PARAMETER Branch
        Feature branch to aggregate
    #>
    param(
        [string]$Branch = ""
    )

    # Detect branch from git if not provided
    if (-not $Branch) {
        try {
            $Branch = git rev-parse --abbrev-ref HEAD 2>$null
        }
        catch {
            Write-Host "Specify -FeatureBranch parameter" -ForegroundColor Red
            return
        }
    }

    if (-not $Branch) {
        Write-Host "Specify -FeatureBranch parameter" -ForegroundColor Red
        return
    }

    # Collect all matching sessions
    $allSessions = @()

    # Load archives
    $archiveDir = Join-Path $MetricsDir "archive"
    if (Test-Path $archiveDir) {
        $archiveFiles = Get-ChildItem $archiveDir -Filter "agent-metrics-*.json" -ErrorAction SilentlyContinue
        foreach ($file in $archiveFiles) {
            try {
                $session = Get-Content $file.FullName -Raw | ConvertFrom-Json
                if ((Test-SchemaVersion -Session $session) -and $session.featureBranch -eq $Branch) {
                    $allSessions += $session
                }
            }
            catch { }
        }
    }

    # Include active session if matching
    if (Test-Path $MetricsFile) {
        $activeSession = Get-Content $MetricsFile -Raw | ConvertFrom-Json
        if ($activeSession.featureBranch -eq $Branch) {
            $allSessions += $activeSession
        }
    }

    if ($allSessions.Count -eq 0) {
        Write-Host "No sessions found for branch: $Branch" -ForegroundColor Red
        return
    }

    # Sort by start time
    $allSessions = @($allSessions | Sort-Object { [DateTime]::Parse($_.startTime) })

    # Calculate period
    $periodStart = [DateTime]::Parse($allSessions[0].startTime)
    $periodEnd = [DateTime]::Parse($allSessions[-1].startTime)

    # Aggregate totals
    $totalInvocations = 0
    $totalTokens = 0
    $totalSuccesses = 0
    $totalFailures = 0
    $totalDurationMs = 0

    # Phase breakdown
    $phaseData = @{}

    foreach ($s in $allSessions) {
        $totalInvocations += $s.totals.totalInvocations
        $totalTokens += $s.totals.totalTokens
        $totalSuccesses += $s.totals.totalSuccesses
        $totalFailures += $s.totals.totalFailures
        $totalDurationMs += $s.totals.totalDurationMs

        $phase = $s.phase
        if (-not $phaseData.ContainsKey($phase)) {
            $phaseData[$phase] = @{
                sessions = 0
                invocations = 0
                tokens = 0
                durationMs = 0
                successes = 0
                failures = 0
            }
        }
        $phaseData[$phase].sessions++
        $phaseData[$phase].invocations += $s.totals.totalInvocations
        $phaseData[$phase].tokens += $s.totals.totalTokens
        $phaseData[$phase].durationMs += $s.totals.totalDurationMs
        $phaseData[$phase].successes += $s.totals.totalSuccesses
        $phaseData[$phase].failures += $s.totals.totalFailures
    }

    $overallSuccessRate = if ($totalInvocations -gt 0) {
        [math]::Round(($totalSuccesses / $totalInvocations) * 100, 1)
    } else { 0 }

    # --- Display Report ---
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════════════════════" -ForegroundColor Magenta
    Write-Host "                    CUMULATIVE FEATURE REPORT                                  " -ForegroundColor Magenta
    Write-Host "═══════════════════════════════════════════════════════════════════════════════" -ForegroundColor Magenta
    Write-Host ""
    Write-Host "Feature Branch: $Branch" -ForegroundColor White
    Write-Host "Period: $($periodStart.ToString('yyyy-MM-dd')) to $($periodEnd.ToString('yyyy-MM-dd'))" -ForegroundColor White
    Write-Host "Sessions: $($allSessions.Count)" -ForegroundColor White
    Write-Host ""

    # PHASE BREAKDOWN
    Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
    Write-Host "                              PHASE BREAKDOWN                                  " -ForegroundColor Yellow
    Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
    Write-Host ""

    $phaseHeader = "{0,-14} {1,10} {2,14} {3,14} {4,12} {5,10}" -f "Phase", "Sessions", "Invocations", "Tokens", "Duration", "Success"
    Write-Host "  $phaseHeader" -ForegroundColor Cyan
    Write-Host "  $("-" * 78)" -ForegroundColor DarkGray

    foreach ($phase in ($phaseData.Keys | Sort-Object)) {
        $pd = $phaseData[$phase]
        $successRate = if ($pd.invocations -gt 0) {
            [math]::Round(($pd.successes / $pd.invocations) * 100, 0)
        } else { 0 }

        $phaseRow = "{0,-14} {1,10} {2,14} {3,14:N0} {4,12} {5,10}" -f `
            $phase, $pd.sessions, $pd.invocations, $pd.tokens, `
            (Format-Duration $pd.durationMs), "$($successRate)%"
        Write-Host "  $phaseRow" -ForegroundColor White
    }

    Write-Host "  $("-" * 78)" -ForegroundColor DarkGray
    $totalRow = "{0,-14} {1,10} {2,14} {3,14:N0} {4,12} {5,10}" -f `
        "TOTAL", $allSessions.Count, $totalInvocations, $totalTokens, `
        (Format-Duration $totalDurationMs), "$($overallSuccessRate)%"
    Write-Host "  $totalRow" -ForegroundColor White
    Write-Host ""

    # CUMULATIVE MODEL DISTRIBUTION
    $allModels = @{}
    $settings = Get-MetricsSettings
    foreach ($s in $allSessions) {
        if ($s.models -and $s.models.PSObject.Properties.Count -gt 0) {
            foreach ($mp in $s.models.PSObject.Properties) {
                $mn = $mp.Name
                if (-not $allModels.ContainsKey($mn)) {
                    $costWeight = switch ($mn.ToLower()) {
                        "opus" { $settings.costWeights.opus }
                        "sonnet" { $settings.costWeights.sonnet }
                        "haiku" { $settings.costWeights.haiku }
                        default { 3.0 }
                    }
                    $allModels[$mn] = @{ count = 0; totalTokens = 0; costWeight = $costWeight }
                }
                $allModels[$mn].count += $mp.Value.count
                $allModels[$mn].totalTokens += $mp.Value.totalTokens
            }
        }
    }

    if ($allModels.Count -gt 0) {
        # Calculate weighted totals
        $totalWeighted = 0
        foreach ($m in $allModels.Values) {
            $m.weightedTokens = $m.totalTokens * $m.costWeight
            $totalWeighted += $m.weightedTokens
        }
        foreach ($m in $allModels.Values) {
            $m.costPercent = if ($totalWeighted -gt 0) { [math]::Round(($m.weightedTokens / $totalWeighted) * 100, 1) } else { 0 }
        }

        Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
        Write-Host "                     CUMULATIVE MODEL DISTRIBUTION                             " -ForegroundColor Yellow
        Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
        Write-Host ""

        $mdlHeader = "{0,-12} {1,14} {2,14} {3,14} {4,10}" -f "Model", "Total Tokens", "Invocations", "Cost Weight", "Cost %"
        Write-Host "  $mdlHeader" -ForegroundColor Cyan
        Write-Host "  $("-" * 68)" -ForegroundColor DarkGray

        foreach ($mn in ($allModels.Keys | Sort-Object { $allModels[$_].costPercent } -Descending)) {
            $md = $allModels[$mn]
            $mdlRow = "{0,-12} {1,14:N0} {2,14} {3,14} {4,10}" -f `
                $mn, $md.totalTokens, $md.count, "$($md.costWeight)x", "$($md.costPercent)%"
            Write-Host "  $mdlRow" -ForegroundColor White
        }
        Write-Host ""
    }

    # CUMULATIVE CATEGORY BREAKDOWN
    $allCategories = @{}
    foreach ($s in $allSessions) {
        if ($s.categories -and $s.categories.PSObject.Properties.Count -gt 0) {
            foreach ($cp in $s.categories.PSObject.Properties) {
                $cn = $cp.Name
                if (-not $allCategories.ContainsKey($cn)) {
                    $allCategories[$cn] = @{ count = 0; totalTokens = 0 }
                }
                $allCategories[$cn].count += $cp.Value.count
                $allCategories[$cn].totalTokens += $cp.Value.totalTokens
            }
        }
    }

    if ($allCategories.Count -gt 0) {
        Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
        Write-Host "                     CUMULATIVE CATEGORY BREAKDOWN                             " -ForegroundColor Yellow
        Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
        Write-Host ""

        $catHeader = "{0,-16} {1,14} {2,14} {3,10}" -f "Category", "Invocations", "Tokens", "% of Total"
        Write-Host "  $catHeader" -ForegroundColor Cyan
        Write-Host "  $("-" * 58)" -ForegroundColor DarkGray

        foreach ($cn in ($allCategories.Keys | Sort-Object { $allCategories[$_].totalTokens } -Descending)) {
            $cd = $allCategories[$cn]
            $pct = if ($totalTokens -gt 0) { [math]::Round(($cd.totalTokens / $totalTokens) * 100, 1) } else { 0 }
            $catRow = "{0,-16} {1,14} {2,14:N0} {3,10}" -f $cn, $cd.count, $cd.totalTokens, "$($pct)%"
            Write-Host "  $catRow" -ForegroundColor White
        }
        Write-Host ""
    }

    # FEATURE SUMMARY
    Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
    Write-Host "                         FEATURE SUMMARY                                       " -ForegroundColor Yellow
    Write-Host "───────────────────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host ("  Total Development Time:       {0}" -f (Format-Duration $totalDurationMs))
    Write-Host ("  Total Agent Invocations:      {0}" -f $totalInvocations)
    Write-Host ("  Total Tokens Consumed:        {0:N0}" -f $totalTokens)
    Write-Host ("  Overall Success Rate:         {0}%" -f $overallSuccessRate)
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
            Compare-Sessions -Sess1 $Session1 -Sess2 $Session2
        }
        "Cumulative" {
            Generate-CumulativeReport -Branch $FeatureBranch
        }
        "Export" {
            Export-Metrics -Format $Format -OutputPath $OutputPath -IncludeArchives:$IncludeArchives.IsPresent -Branch $FeatureBranch
        }
    }
}
