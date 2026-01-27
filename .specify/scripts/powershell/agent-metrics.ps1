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
    [Parameter(Mandatory=$true)]
    [ValidateSet("Init", "Record", "Report", "Reset")]
    [string]$Action,

    [string]$AgentName,
    [string]$TaskId,
    [ValidateSet("completed", "failed", "timeout", "")]
    [string]$Status,
    [int]$TokensUsed = 0,
    [int]$DurationMs = 0,
    [string]$PhaseName = "Unknown",
    [string]$Description = ""
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

function Initialize-Metrics {
    param([string]$Phase)

    if (-not (Test-Path $MetricsDir)) {
        New-Item -ItemType Directory -Path $MetricsDir -Force | Out-Null
    }

    $metrics = @{
        phase = $Phase
        startTime = (Get-Date).ToString("o")
        endTime = $null
        agents = @{}
        invocations = @()
        totals = @{
            totalInvocations = 0
            totalTokens = 0
            totalSuccesses = 0
            totalFailures = 0
            totalTimeouts = 0
        }
    }

    $metrics | ConvertTo-Json -Depth 10 | Set-Content $MetricsFile -Encoding UTF8
    Write-Host "Metrics initialized for phase: $Phase" -ForegroundColor Green
}

function Record-AgentMetric {
    param(
        [string]$Agent,
        [string]$Task,
        [string]$CompletionStatus,
        [int]$Tokens,
        [int]$Duration,
        [string]$Desc
    )

    if (-not (Test-Path $MetricsFile)) {
        Write-Error "Metrics not initialized. Run with -Action Init first."
        return
    }

    $metrics = Get-Content $MetricsFile -Raw | ConvertFrom-Json

    # Initialize agent entry if not exists
    if (-not $metrics.agents.PSObject.Properties[$Agent]) {
        $metrics.agents | Add-Member -NotePropertyName $Agent -NotePropertyValue @{
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
        }
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

    # Calculate averages
    $agentMetrics.avgTokens = [math]::Round($agentMetrics.totalTokens / $agentMetrics.count, 0)
    $agentMetrics.avgDurationMs = [math]::Round($agentMetrics.totalDurationMs / $agentMetrics.count, 0)

    # Update totals
    $metrics.totals.totalInvocations++
    $metrics.totals.totalTokens += $Tokens

    # Record individual invocation
    $invocation = @{
        timestamp = (Get-Date).ToString("o")
        agent = $Agent
        taskId = $Task
        status = $CompletionStatus
        tokens = $Tokens
        durationMs = $Duration
        description = $Desc
    }
    $metrics.invocations += $invocation

    $metrics | ConvertTo-Json -Depth 10 | Set-Content $MetricsFile -Encoding UTF8
    Write-Host "Recorded: $Agent ($CompletionStatus) - ${Tokens} tokens, $([math]::Round($Duration/1000, 1))s" -ForegroundColor Cyan
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
        Write-Host "Metrics reset for next phase." -ForegroundColor Green
    } else {
        Write-Host "No metrics to reset." -ForegroundColor Gray
    }
}

# Execute action
switch ($Action) {
    "Init" { Initialize-Metrics -Phase $PhaseName }
    "Record" {
        Record-AgentMetric -Agent $AgentName -Task $TaskId -CompletionStatus $Status -Tokens $TokensUsed -Duration $DurationMs -Desc $Description
    }
    "Report" { Generate-Report }
    "Reset" { Reset-Metrics }
}
