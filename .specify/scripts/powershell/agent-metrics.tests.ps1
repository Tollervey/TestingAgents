#Requires -Modules Pester

<#
.SYNOPSIS
    Pester tests for agent-metrics.ps1

.DESCRIPTION
    Test suite for the Enhanced Agent Metrics System.
    Tests all 8 actions: Init, Record, Report, Reset, Trends, Compare, Cumulative, Export

.NOTES
    Compatible with Pester 3.4.x and 5.x
    Run with: Invoke-Pester -Path .\agent-metrics.tests.ps1
#>

# Get the directory where the test script is located
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Dot-source the main script to import functions
# The script now detects when it's dot-sourced and skips action execution
. "$ScriptDir\agent-metrics.ps1"

# Helper functions defined at module scope
function New-TestMetricsDir {
    $testMetricsDir = Join-Path $TestDrive "metrics"
    if (-not (Test-Path $testMetricsDir)) {
        New-Item -ItemType Directory -Path $testMetricsDir -Force | Out-Null
    }
    return $testMetricsDir
}

function New-TestArchiveDir {
    $testMetricsDir = New-TestMetricsDir
    $archiveDir = Join-Path $testMetricsDir "archive"
    if (-not (Test-Path $archiveDir)) {
        New-Item -ItemType Directory -Path $archiveDir -Force | Out-Null
    }
    return $archiveDir
}

function New-TestSession {
    param(
        [string]$Phase = "implement",
        [string]$FeatureBranch = "test-branch",
        [string]$Status = "active",
        [array]$Invocations = @()
    )

    return @{
        schemaVersion = "2.0.0"
        phase = $Phase
        featureBranch = $FeatureBranch
        startTime = (Get-Date).ToString("o")
        endTime = $null
        completionStatus = $Status
        sessionId = [guid]::NewGuid().ToString()
        invocations = $Invocations
        agents = @{}
        models = [PSCustomObject]@{}
        categories = [PSCustomObject]@{}
        parallelGroups = [PSCustomObject]@{}
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
}

function New-TestInvocation {
    param(
        [string]$Agent = "backend-developer",
        [string]$Model = "sonnet",
        [string]$TaskId = "test-001",
        [string]$Status = "completed",
        [int]$Tokens = 30000,
        [int]$DurationMs = 60000,
        [string]$Phase = "implement",
        [string]$Category = "implementation",
        [bool]$IsParallel = $false,
        [string]$ParallelGroupId = $null,
        [int]$GroupSize = 1
    )

    return @{
        timestamp = (Get-Date).ToString("o")
        agent = $Agent
        model = $Model
        taskId = $TaskId
        status = $Status
        tokens = $Tokens
        durationMs = $DurationMs
        description = "Test invocation for $Agent"
        phase = $Phase
        category = $Category
        invocationStartTime = (Get-Date).AddSeconds(-$DurationMs/1000).ToString("o")
        invocationEndTime = (Get-Date).ToString("o")
        isParallel = $IsParallel
        parallelGroupId = $ParallelGroupId
        groupSize = $GroupSize
    }
}

function New-TestSettings {
    return @{
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
}

# Test setup helper - call at start of each test that needs file isolation
function Setup-TestEnvironment {
    $script:TestMetricsDir = Join-Path $TestDrive "metrics"
    $script:TestMetricsFile = Join-Path $script:TestMetricsDir "agent-metrics.json"
    $script:TestSettingsFile = Join-Path $script:TestMetricsDir "settings.json"
    $script:TestArchiveDir = Join-Path $script:TestMetricsDir "archive"

    # Save original values
    $script:OriginalMetricsDir = $MetricsDir
    $script:OriginalMetricsFile = $MetricsFile
    $script:OriginalSettingsFile = $SettingsFile

    # Override module variables
    $script:MetricsDir = $script:TestMetricsDir
    $script:MetricsFile = $script:TestMetricsFile
    $script:SettingsFile = $script:TestSettingsFile

    # Create clean directory (remove any leftovers from prior tests)
    if (Test-Path $script:TestMetricsDir) {
        Remove-Item $script:TestMetricsDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $script:TestMetricsDir -Force | Out-Null
}

function Teardown-TestEnvironment {
    $script:MetricsDir = $script:OriginalMetricsDir
    $script:MetricsFile = $script:OriginalMetricsFile
    $script:SettingsFile = $script:OriginalSettingsFile
}

Describe 'agent-metrics.ps1' {

    Context 'Schema Validation' {
        # T005: Schema version validation function
        It 'validates v2.0.0 schema as current' {
            $session = New-TestSession
            $session.schemaVersion = "2.0.0"
            $result = Test-SchemaVersion -Session $session
            $result | Should Be $true
        }

        It 'validates v2.1.0 schema as current' {
            $session = New-TestSession
            $session.schemaVersion = "2.1.0"
            $result = Test-SchemaVersion -Session $session
            $result | Should Be $true
        }

        It 'rejects v1.x.x schema as legacy' {
            $session = @{ schemaVersion = "1.0.0"; phase = "implement" }
            $result = Test-SchemaVersion -Session $session
            $result | Should Be $false
        }

        It 'rejects v1.5.0 schema as legacy' {
            $session = @{ schemaVersion = "1.5.0"; phase = "implement" }
            $result = Test-SchemaVersion -Session $session
            $result | Should Be $false
        }

        It 'handles missing schemaVersion gracefully' {
            $session = @{ phase = "implement" }
            $result = Test-SchemaVersion -Session $session
            $result | Should Be $false
        }

        It 'handles null session gracefully' {
            $result = Test-SchemaVersion -Session $null
            $result | Should Be $false
        }

        It 'handles empty session gracefully' {
            $result = Test-SchemaVersion -Session @{}
            $result | Should Be $false
        }
    }

    Context 'Init Action' {
        # T008 [US1]: Init action creates session with v2.0.0 schema
        It 'creates metrics directory if missing' {
            Setup-TestEnvironment
            try {
                if (Test-Path $script:TestMetricsDir) { Remove-Item $script:TestMetricsDir -Recurse -Force }
                Initialize-Metrics -Phase "implement"
                $script:TestMetricsDir | Should Exist
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'initializes session with v2.0.0 schema' {
            Setup-TestEnvironment
            try {
                Initialize-Metrics -Phase "implement"
                $script:TestMetricsFile | Should Exist
                $session = Get-Content $script:TestMetricsFile -Raw | ConvertFrom-Json
                $session.schemaVersion | Should Be "2.0.0"
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'creates session with models aggregate object' {
            Setup-TestEnvironment
            try {
                Initialize-Metrics -Phase "implement"
                $session = Get-Content $script:TestMetricsFile -Raw | ConvertFrom-Json
                $session.models | Should Not BeNullOrEmpty
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'creates session with categories aggregate object' {
            Setup-TestEnvironment
            try {
                Initialize-Metrics -Phase "implement"
                $session = Get-Content $script:TestMetricsFile -Raw | ConvertFrom-Json
                $session.categories | Should Not BeNullOrEmpty
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'captures phase parameter in session' {
            Setup-TestEnvironment
            try {
                Initialize-Metrics -Phase "plan"
                $session = Get-Content $script:TestMetricsFile -Raw | ConvertFrom-Json
                $session.phase | Should Be "plan"
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'captures featureBranch parameter in session' {
            Setup-TestEnvironment
            try {
                Initialize-Metrics -Phase "implement" -Branch "my-feature-branch"
                $session = Get-Content $script:TestMetricsFile -Raw | ConvertFrom-Json
                $session.featureBranch | Should Be "my-feature-branch"
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'sets completionStatus to active' {
            Setup-TestEnvironment
            try {
                Initialize-Metrics -Phase "implement"
                $session = Get-Content $script:TestMetricsFile -Raw | ConvertFrom-Json
                $session.completionStatus | Should Be "active"
            } finally {
                Teardown-TestEnvironment
            }
        }

        # T022 [US2]: Init archives incomplete previous session
        It 'archives incomplete previous session' {
            Setup-TestEnvironment
            try {
                # Create an existing incomplete session
                $existingSession = New-TestSession -Phase "plan" -Status "active"
                $existingSession | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8

                # Initialize new session
                Initialize-Metrics -Phase "implement"

                # Verify archive was created
                $archiveDir = Join-Path $script:TestMetricsDir "archive"
                $archiveDir | Should Exist
                $archiveFiles = Get-ChildItem $archiveDir -Filter "agent-metrics-*.json"
                $archiveFiles.Count | Should BeGreaterThan 0
            } finally {
                Teardown-TestEnvironment
            }
        }
    }

    Context 'Record Action' {
        # T009 [US1]: Record action captures model parameter
        It 'adds invocation with model identifier' {
            Setup-TestEnvironment
            try {
                # Create session
                $session = New-TestSession -Phase "implement" -FeatureBranch "test-branch"
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8
                # Create settings
                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                # Record invocation
                Record-AgentMetric -Agent "backend-developer" -Task "test-001" -CompletionStatus "completed" `
                    -Tokens 30000 -Duration 60000 -Desc "Test task" -ModelName "sonnet"

                # Verify
                $result = Get-Content $script:TestMetricsFile -Raw | ConvertFrom-Json
                $result.invocations.Count | Should Be 1
                $result.invocations[0].model | Should Be "sonnet"
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'records opus model correctly' {
            Setup-TestEnvironment
            try {
                $session = New-TestSession -Phase "implement"
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8
                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                Record-AgentMetric -Agent "solution-architect" -Task "test-002" -CompletionStatus "completed" `
                    -Tokens 50000 -Duration 120000 -Desc "Architecture task" -ModelName "opus"

                $result = Get-Content $script:TestMetricsFile -Raw | ConvertFrom-Json
                $result.invocations[0].model | Should Be "opus"
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'records haiku model correctly' {
            Setup-TestEnvironment
            try {
                $session = New-TestSession -Phase "implement"
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8
                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                Record-AgentMetric -Agent "code-reviewer" -Task "test-003" -CompletionStatus "completed" `
                    -Tokens 10000 -Duration 30000 -Desc "Review task" -ModelName "haiku"

                $result = Get-Content $script:TestMetricsFile -Raw | ConvertFrom-Json
                $result.invocations[0].model | Should Be "haiku"
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'defaults to sonnet model when not specified' {
            Setup-TestEnvironment
            try {
                $session = New-TestSession -Phase "implement"
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8
                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                Record-AgentMetric -Agent "backend-developer" -Task "test-004" -CompletionStatus "completed" `
                    -Tokens 25000 -Duration 45000 -Desc "Default model task"

                $result = Get-Content $script:TestMetricsFile -Raw | ConvertFrom-Json
                $result.invocations[0].model | Should Be "sonnet"
            } finally {
                Teardown-TestEnvironment
            }
        }

        # T010 [US1]: Record action updates per-model aggregates
        It 'updates per-model aggregates' {
            Setup-TestEnvironment
            try {
                $session = New-TestSession -Phase "implement"
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8
                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                # Record invocations with different models
                Record-AgentMetric -Agent "backend-developer" -Task "test-001" -CompletionStatus "completed" `
                    -Tokens 30000 -Duration 60000 -ModelName "sonnet"
                Record-AgentMetric -Agent "solution-architect" -Task "test-002" -CompletionStatus "completed" `
                    -Tokens 50000 -Duration 120000 -ModelName "opus"
                Record-AgentMetric -Agent "code-reviewer" -Task "test-003" -CompletionStatus "completed" `
                    -Tokens 10000 -Duration 30000 -ModelName "haiku"

                $result = Get-Content $script:TestMetricsFile -Raw | ConvertFrom-Json
                $result.models.sonnet.count | Should Be 1
                $result.models.sonnet.totalTokens | Should Be 30000
                $result.models.opus.count | Should Be 1
                $result.models.opus.totalTokens | Should Be 50000
                $result.models.haiku.count | Should Be 1
                $result.models.haiku.totalTokens | Should Be 10000
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'accumulates tokens for same model across invocations' {
            Setup-TestEnvironment
            try {
                $session = New-TestSession -Phase "implement"
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8
                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                Record-AgentMetric -Agent "backend-developer" -Task "test-001" -CompletionStatus "completed" `
                    -Tokens 30000 -Duration 60000 -ModelName "sonnet"
                Record-AgentMetric -Agent "test-engineer" -Task "test-002" -CompletionStatus "completed" `
                    -Tokens 25000 -Duration 50000 -ModelName "sonnet"

                $result = Get-Content $script:TestMetricsFile -Raw | ConvertFrom-Json
                $result.models.sonnet.count | Should Be 2
                $result.models.sonnet.totalTokens | Should Be 55000
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'calculates weighted tokens using cost weights' {
            Setup-TestEnvironment
            try {
                $session = New-TestSession -Phase "implement"
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8
                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                Record-AgentMetric -Agent "backend-developer" -Task "test-001" -CompletionStatus "completed" `
                    -Tokens 30000 -Duration 60000 -ModelName "sonnet"

                $result = Get-Content $script:TestMetricsFile -Raw | ConvertFrom-Json
                # sonnet weight is 3.0, so 30000 * 3 = 90000
                $result.models.sonnet.weightedTokens | Should Be 90000
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'calculates cost percentage across all models' {
            Setup-TestEnvironment
            try {
                $session = New-TestSession -Phase "implement"
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8
                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                # Record with calculated values:
                # sonnet: 10000 * 3 = 30000 weighted
                # opus: 10000 * 5 = 50000 weighted
                # haiku: 20000 * 1 = 20000 weighted
                # Total: 100000 weighted
                Record-AgentMetric -Agent "backend-developer" -Task "test-001" -CompletionStatus "completed" `
                    -Tokens 10000 -Duration 60000 -ModelName "sonnet"
                Record-AgentMetric -Agent "solution-architect" -Task "test-002" -CompletionStatus "completed" `
                    -Tokens 10000 -Duration 120000 -ModelName "opus"
                Record-AgentMetric -Agent "code-reviewer" -Task "test-003" -CompletionStatus "completed" `
                    -Tokens 20000 -Duration 30000 -ModelName "haiku"

                $result = Get-Content $script:TestMetricsFile -Raw | ConvertFrom-Json
                $result.models.sonnet.costPercent | Should Be 30.0
                $result.models.opus.costPercent | Should Be 50.0
                $result.models.haiku.costPercent | Should Be 20.0
            } finally {
                Teardown-TestEnvironment
            }
        }

        # T019 [US2]: Record action captures phase name from session
        It 'captures phase name from active session' {
            Setup-TestEnvironment
            try {
                $session = New-TestSession -Phase "implement"
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8
                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                Record-AgentMetric -Agent "backend-developer" -Task "test-001" -CompletionStatus "completed" `
                    -Tokens 30000 -Duration 60000 -ModelName "sonnet"

                $result = Get-Content $script:TestMetricsFile -Raw | ConvertFrom-Json
                $result.invocations[0].phase | Should Be "implement"
            } finally {
                Teardown-TestEnvironment
            }
        }

        # T020 [US2]: Record action captures category parameter
        It 'captures category parameter' {
            Setup-TestEnvironment
            try {
                $session = New-TestSession -Phase "implement"
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8
                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                Record-AgentMetric -Agent "backend-developer" -Task "test-001" -CompletionStatus "completed" `
                    -Tokens 30000 -Duration 60000 -ModelName "sonnet" -CategoryName "implementation"

                $result = Get-Content $script:TestMetricsFile -Raw | ConvertFrom-Json
                $result.invocations[0].category | Should Be "implementation"
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'defaults to other category when not specified' {
            Setup-TestEnvironment
            try {
                $session = New-TestSession -Phase "implement"
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8
                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                Record-AgentMetric -Agent "backend-developer" -Task "test-001" -CompletionStatus "completed" `
                    -Tokens 30000 -Duration 60000 -ModelName "sonnet"

                $result = Get-Content $script:TestMetricsFile -Raw | ConvertFrom-Json
                $result.invocations[0].category | Should Be "other"
            } finally {
                Teardown-TestEnvironment
            }
        }

        # T045 [US4]: Record action captures parallel execution fields
        It 'captures parallel execution fields' {
            Setup-TestEnvironment
            try {
                $session = New-TestSession -Phase "implement"
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8
                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                Record-AgentMetric -Agent "backend-developer" -Task "test-001" -CompletionStatus "completed" `
                    -Tokens 30000 -Duration 60000 -ModelName "sonnet" `
                    -Parallel $true -GroupId "pg-test-001" -GrpSize 3

                $result = Get-Content $script:TestMetricsFile -Raw | ConvertFrom-Json
                $result.invocations[0].isParallel | Should Be $true
                $result.invocations[0].parallelGroupId | Should Be "pg-test-001"
                $result.invocations[0].groupSize | Should Be 3
            } finally {
                Teardown-TestEnvironment
            }
        }

        # T046 [US4]: Record action updates parallel group metrics
        It 'updates parallel group metrics' {
            Setup-TestEnvironment
            try {
                $session = New-TestSession -Phase "implement"
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8
                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                Record-AgentMetric -Agent "backend-developer" -Task "test-001" -CompletionStatus "completed" `
                    -Tokens 30000 -Duration 60000 -ModelName "sonnet" `
                    -Parallel $true -GroupId "pg-test-001" -GrpSize 3

                $result = Get-Content $script:TestMetricsFile -Raw | ConvertFrom-Json
                $result.parallelGroups."pg-test-001" | Should Not BeNullOrEmpty
                $result.parallelGroups."pg-test-001".invocationCount | Should Be 1
                $result.totals.parallelInvocations | Should Be 1
            } finally {
                Teardown-TestEnvironment
            }
        }
    }

    Context 'Report Action' {
        # T011 [US1]: Report action calculates cost distribution by model
        It 'calculates cost distribution by model (1:3:5 weights)' {
            Setup-TestEnvironment
            try {
                # Create session with pre-populated model data
                $session = New-TestSession -Phase "implement"
                $session.invocations = @(
                    (New-TestInvocation -Agent "backend-developer" -Model "sonnet" -Tokens 10000),
                    (New-TestInvocation -Agent "solution-architect" -Model "opus" -Tokens 10000),
                    (New-TestInvocation -Agent "code-reviewer" -Model "haiku" -Tokens 20000)
                )
                $session.models = [PSCustomObject]@{
                    sonnet = @{ count = 1; totalTokens = 10000; costWeight = 3.0; weightedTokens = 30000; costPercent = 30.0 }
                    opus = @{ count = 1; totalTokens = 10000; costWeight = 5.0; weightedTokens = 50000; costPercent = 50.0 }
                    haiku = @{ count = 1; totalTokens = 20000; costWeight = 1.0; weightedTokens = 20000; costPercent = 20.0 }
                }
                $session.totals.totalInvocations = 3
                $session.totals.totalTokens = 40000
                $session.totals.totalSuccesses = 3
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8
                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                # Run report
                Generate-Report | Out-Null

                # Verify cost percentages in saved file
                $result = Get-Content $script:TestMetricsFile -Raw | ConvertFrom-Json
                $result.models.sonnet.costPercent | Should Be 30.0
                $result.models.opus.costPercent | Should Be 50.0
                $result.models.haiku.costPercent | Should Be 20.0
            } finally {
                Teardown-TestEnvironment
            }
        }

        # T012 [US1]: Report action displays model distribution section
        It 'displays model distribution section' {
            Setup-TestEnvironment
            try {
                $session = New-TestSession -Phase "implement"
                $session.invocations = @(
                    (New-TestInvocation -Agent "backend-developer" -Model "sonnet" -Tokens 30000)
                )
                $session.models = [PSCustomObject]@{
                    sonnet = @{ count = 1; totalTokens = 30000; costWeight = 3.0; weightedTokens = 90000; costPercent = 100.0 }
                }
                $session.totals.totalInvocations = 1
                $session.totals.totalTokens = 30000
                $session.totals.totalSuccesses = 1
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8
                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                # Capture output
                $output = Generate-Report 6>&1 | Out-String

                # Verify MODEL DISTRIBUTION appears
                $output | Should Match "MODEL DISTRIBUTION"
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'displays model with count, tokens, weight, and cost percent' {
            Setup-TestEnvironment
            try {
                $session = New-TestSession -Phase "implement"
                $session.invocations = @(
                    (New-TestInvocation -Agent "backend-developer" -Model "sonnet" -Tokens 30000),
                    (New-TestInvocation -Agent "code-reviewer" -Model "haiku" -Tokens 15000)
                )
                $session.models = [PSCustomObject]@{
                    sonnet = @{ count = 1; totalTokens = 30000; costWeight = 3.0; weightedTokens = 90000; costPercent = 85.7 }
                    haiku = @{ count = 1; totalTokens = 15000; costWeight = 1.0; weightedTokens = 15000; costPercent = 14.3 }
                }
                $session.totals.totalInvocations = 2
                $session.totals.totalTokens = 45000
                $session.totals.totalSuccesses = 2
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8
                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                $output = Generate-Report 6>&1 | Out-String

                $output | Should Match "sonnet"
                $output | Should Match "haiku"
            } finally {
                Teardown-TestEnvironment
            }
        }

        # T021 [US2]: Report action displays category breakdown section
        It 'displays category breakdown section' {
            Setup-TestEnvironment
            try {
                $session = New-TestSession -Phase "implement"
                $session.invocations = @(
                    (New-TestInvocation -Agent "backend-developer" -Model "sonnet" -Category "implementation" -Tokens 30000)
                )
                $session.categories = [PSCustomObject]@{
                    implementation = @{ count = 1; totalTokens = 30000; avgTokens = 30000; totalDurationMs = 60000; avgDurationMs = 60000 }
                }
                $session.models = [PSCustomObject]@{
                    sonnet = @{ count = 1; totalTokens = 30000; costWeight = 3.0; weightedTokens = 90000; costPercent = 100.0 }
                }
                $session.totals.totalInvocations = 1
                $session.totals.totalTokens = 30000
                $session.totals.totalSuccesses = 1
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8
                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                $output = Generate-Report 6>&1 | Out-String

                $output | Should Match "CATEGORY BREAKDOWN"
            } finally {
                Teardown-TestEnvironment
            }
        }

        # T047 [US4]: Report action displays parallelization metrics section
        It 'displays parallelization metrics section' {
            Setup-TestEnvironment
            try {
                $session = New-TestSession -Phase "implement"
                $session.invocations = @(
                    (New-TestInvocation -Agent "backend-developer" -Model "sonnet" -Tokens 30000 -IsParallel $true -ParallelGroupId "pg-001" -GroupSize 2)
                )
                $session.models = [PSCustomObject]@{
                    sonnet = @{ count = 1; totalTokens = 30000; costWeight = 3.0; weightedTokens = 90000; costPercent = 100.0 }
                }
                $session.parallelGroups = [PSCustomObject]@{
                    "pg-001" = @{
                        groupId = "pg-001"
                        invocationCount = 1
                        concurrencyMetrics = @{ maxConcurrent = 2; efficiency = 50.0; timeReduction = 25.0 }
                    }
                }
                $session.totals.totalInvocations = 1
                $session.totals.totalTokens = 30000
                $session.totals.totalSuccesses = 1
                $session.totals.parallelInvocations = 1
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8
                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                $output = Generate-Report 6>&1 | Out-String

                $output | Should Match "PARALLELIZATION|Parallel"
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'sets completionStatus to complete on report' {
            Setup-TestEnvironment
            try {
                $session = New-TestSession -Phase "implement"
                $session.invocations = @((New-TestInvocation -Agent "backend-developer" -Model "sonnet" -Tokens 30000))
                $session.models = [PSCustomObject]@{
                    sonnet = @{ count = 1; totalTokens = 30000; costWeight = 3.0; weightedTokens = 90000; costPercent = 100.0 }
                }
                $session.totals.totalInvocations = 1
                $session.totals.totalSuccesses = 1
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8
                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                Generate-Report | Out-Null

                $result = Get-Content $script:TestMetricsFile -Raw | ConvertFrom-Json
                $result.completionStatus | Should Be "complete"
            } finally {
                Teardown-TestEnvironment
            }
        }
    }

    Context 'Reset Action' {
        It 'archives current metrics with timestamp' {
            Setup-TestEnvironment
            try {
                # Create a session
                $session = New-TestSession -Phase "implement"
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8
                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                Reset-Metrics

                $archiveFiles = Get-ChildItem $script:TestArchiveDir -Filter "agent-metrics-*.json" -ErrorAction SilentlyContinue
                $archiveFiles.Count | Should BeGreaterThan 0
                $archiveFiles[0].Name | Should Match "agent-metrics-\d{8}-\d{6}\.json"
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'purges archives older than retention period' {
            Setup-TestEnvironment
            try {
                # Create archive directory with old files
                if (-not (Test-Path $script:TestArchiveDir)) {
                    New-Item -ItemType Directory -Path $script:TestArchiveDir -Force | Out-Null
                }

                # Create old archive (10 days ago)
                $oldDate = (Get-Date).AddDays(-10)
                $oldTimestamp = $oldDate.ToString("yyyyMMdd-HHmmss")
                $oldArchive = Join-Path $script:TestArchiveDir "agent-metrics-$oldTimestamp.json"
                $oldSession = New-TestSession -Phase "old-session"
                $oldSession | ConvertTo-Json -Depth 10 | Set-Content $oldArchive -Encoding UTF8
                (Get-Item $oldArchive).LastWriteTime = $oldDate

                # Create recent archive (2 days ago)
                $recentDate = (Get-Date).AddDays(-2)
                $recentTimestamp = $recentDate.ToString("yyyyMMdd-HHmmss")
                $recentArchive = Join-Path $script:TestArchiveDir "agent-metrics-$recentTimestamp.json"
                $recentSession = New-TestSession -Phase "recent-session"
                $recentSession | ConvertTo-Json -Depth 10 | Set-Content $recentArchive -Encoding UTF8
                (Get-Item $recentArchive).LastWriteTime = $recentDate

                # Create current session
                $session = New-TestSession -Phase "implement"
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8

                # Enable auto-cleanup
                $settings = New-TestSettings
                $settings.retention.archiveDays = 7
                $settings.retention.autoCleanupEnabled = $true
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                Reset-Metrics

                # Old archive should be deleted, recent should remain
                Test-Path $oldArchive | Should Be $false
                Test-Path $recentArchive | Should Be $true
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'clears active metrics file' {
            Setup-TestEnvironment
            try {
                $session = New-TestSession -Phase "implement"
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8
                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                Reset-Metrics

                Test-Path $script:TestMetricsFile | Should Be $false
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'skips cleanup when autoCleanupEnabled is false' {
            Setup-TestEnvironment
            try {
                # Create old archive
                if (-not (Test-Path $script:TestArchiveDir)) {
                    New-Item -ItemType Directory -Path $script:TestArchiveDir -Force | Out-Null
                }
                $oldDate = (Get-Date).AddDays(-10)
                $oldTimestamp = $oldDate.ToString("yyyyMMdd-HHmmss")
                $oldArchive = Join-Path $script:TestArchiveDir "agent-metrics-$oldTimestamp.json"
                $oldSession = New-TestSession -Phase "old-session"
                $oldSession | ConvertTo-Json -Depth 10 | Set-Content $oldArchive -Encoding UTF8
                (Get-Item $oldArchive).LastWriteTime = $oldDate

                # Create current session
                $session = New-TestSession -Phase "implement"
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8

                # Disable auto-cleanup
                $settings = New-TestSettings
                $settings.retention.autoCleanupEnabled = $false
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                Reset-Metrics

                # Old archive should still exist
                Test-Path $oldArchive | Should Be $true
            } finally {
                Teardown-TestEnvironment
            }
        }
    }

    Context 'Settings Loading' {
        It 'loads settings from settings.json when present' {
            Setup-TestEnvironment
            try {
                $settings = @{
                    schemaVersion = "1.0.0"
                    retention = @{ archiveDays = 14; autoCleanupEnabled = $false }
                    costWeights = @{ haiku = 2.0; sonnet = 4.0; opus = 6.0 }
                    display = @{ colorOutput = $false; showCostEstimates = $false; showParallelMetrics = $false }
                }
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                $result = Get-MetricsSettings

                $result.retention.archiveDays | Should Be 14
                $result.costWeights.sonnet | Should Be 4.0
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'returns default settings when file is missing' {
            Setup-TestEnvironment
            try {
                if (Test-Path $script:TestSettingsFile) { Remove-Item $script:TestSettingsFile -Force }

                $result = Get-MetricsSettings

                $result.retention.archiveDays | Should Be 7
                $result.costWeights.sonnet | Should Be 3.0
                $result.costWeights.opus | Should Be 5.0
                $result.costWeights.haiku | Should Be 1.0
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'returns default settings when file is invalid' {
            Setup-TestEnvironment
            try {
                "this is not valid json" | Set-Content $script:TestSettingsFile -Encoding UTF8

                $result = Get-MetricsSettings

                $result.retention.archiveDays | Should Be 7
            } finally {
                Teardown-TestEnvironment
            }
        }
    }

    Context 'Trends Action' {
        # Helper to create archived session files
        function New-TestArchiveSession {
            param(
                [string]$Phase = "implement",
                [string]$FeatureBranch = "test-branch",
                [string]$SchemaVersion = "2.0.0",
                [int]$TotalTokens = 100000,
                [int]$TotalInvocations = 5,
                [int]$TotalSuccesses = 5,
                [int]$TotalFailures = 0,
                [int]$TotalDurationMs = 300000,
                [string]$StartTime = "",
                [string]$EndTime = ""
            )

            if (-not $StartTime) { $StartTime = (Get-Date).ToString("o") }
            if (-not $EndTime) { $EndTime = (Get-Date).AddMinutes(30).ToString("o") }

            return @{
                schemaVersion = $SchemaVersion
                phase = $Phase
                featureBranch = $FeatureBranch
                startTime = $StartTime
                endTime = $EndTime
                completionStatus = "complete"
                sessionId = [guid]::NewGuid().ToString()
                invocations = @()
                agents = @{}
                models = [PSCustomObject]@{}
                categories = [PSCustomObject]@{}
                parallelGroups = [PSCustomObject]@{}
                totals = @{
                    totalInvocations = $TotalInvocations
                    totalTokens = $TotalTokens
                    totalSuccesses = $TotalSuccesses
                    totalFailures = $TotalFailures
                    totalTimeouts = 0
                    totalDurationMs = $TotalDurationMs
                    parallelInvocations = 0
                    sequentialInvocations = $TotalInvocations
                    avgTokensPerInvocation = [math]::Round($TotalTokens / [math]::Max($TotalInvocations, 1), 0)
                    avgDurationMs = [math]::Round($TotalDurationMs / [math]::Max($TotalInvocations, 1), 0)
                    overallSuccessRate = if ($TotalInvocations -gt 0) { [math]::Round(($TotalSuccesses / $TotalInvocations) * 100, 1) } else { 0 }
                }
            }
        }

        # T034 [US3]: Trends action loads archives from archive directory
        It 'loads archives from archive directory' {
            Setup-TestEnvironment
            try {
                # Create archive directory with sessions
                $archiveDir = Join-Path $script:TestMetricsDir "archive"
                New-Item -ItemType Directory -Path $archiveDir -Force | Out-Null

                $session1 = New-TestArchiveSession -Phase "implement" -TotalTokens 100000 `
                    -StartTime (Get-Date).AddDays(-3).ToString("o")
                $session1 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260124-100000.json") -Encoding UTF8

                $session2 = New-TestArchiveSession -Phase "plan" -TotalTokens 50000 `
                    -StartTime (Get-Date).AddDays(-1).ToString("o")
                $session2 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260126-100000.json") -Encoding UTF8

                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                # Run Trends action and capture output
                $output = Generate-TrendsReport -Days 30 6>&1 | Out-String

                # Should include data from both sessions
                $output | Should Match "Sessions Analyzed:\s+2"
            } finally {
                Teardown-TestEnvironment
            }
        }

        # T035 [US3]: Trends action filters by date range and feature branch
        It 'filters by date range and feature branch' {
            Setup-TestEnvironment
            try {
                $archiveDir = Join-Path $script:TestMetricsDir "archive"
                New-Item -ItemType Directory -Path $archiveDir -Force | Out-Null

                # Session within range, matching branch
                $session1 = New-TestArchiveSession -Phase "implement" -FeatureBranch "feature-a" -TotalTokens 100000 `
                    -StartTime (Get-Date).AddDays(-2).ToString("o")
                $session1 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260125-100000.json") -Encoding UTF8

                # Session within range, different branch
                $session2 = New-TestArchiveSession -Phase "plan" -FeatureBranch "feature-b" -TotalTokens 50000 `
                    -StartTime (Get-Date).AddDays(-1).ToString("o")
                $session2 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260126-100000.json") -Encoding UTF8

                # Session outside date range (40 days ago)
                $session3 = New-TestArchiveSession -Phase "implement" -FeatureBranch "feature-a" -TotalTokens 80000 `
                    -StartTime (Get-Date).AddDays(-40).ToString("o")
                $session3 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20251218-100000.json") -Encoding UTF8
                (Get-Item (Join-Path $archiveDir "agent-metrics-20251218-100000.json")).LastWriteTime = (Get-Date).AddDays(-40)

                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                # Filter by branch and 7-day range
                $output = Generate-TrendsReport -Days 7 -Branch "feature-a" 6>&1 | Out-String

                # Should only include session1 (matching branch + date range)
                $output | Should Match "Sessions Analyzed:\s+1"
            } finally {
                Teardown-TestEnvironment
            }
        }

        # T036 [US3]: Trends action calculates token usage trend
        It 'calculates token usage trend' {
            Setup-TestEnvironment
            try {
                $archiveDir = Join-Path $script:TestMetricsDir "archive"
                New-Item -ItemType Directory -Path $archiveDir -Force | Out-Null

                # Create sessions with increasing token usage
                $session1 = New-TestArchiveSession -Phase "implement" -TotalTokens 100000 `
                    -StartTime (Get-Date).AddDays(-3).ToString("o")
                $session1 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260124-100000.json") -Encoding UTF8

                $session2 = New-TestArchiveSession -Phase "implement" -TotalTokens 150000 `
                    -StartTime (Get-Date).AddDays(-2).ToString("o")
                $session2 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260125-100000.json") -Encoding UTF8

                $session3 = New-TestArchiveSession -Phase "implement" -TotalTokens 200000 `
                    -StartTime (Get-Date).AddDays(-1).ToString("o")
                $session3 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260126-100000.json") -Encoding UTF8

                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                $output = Generate-TrendsReport -Days 30 6>&1 | Out-String

                # Should show TOKEN USAGE TREND section and increasing trend
                $output | Should Match "TOKEN USAGE"
                $output | Should Match "increasing|100.0%|\+100"
            } finally {
                Teardown-TestEnvironment
            }
        }

        # T037 [US3]: Trends action calculates success rate trend
        It 'calculates success rate trend' {
            Setup-TestEnvironment
            try {
                $archiveDir = Join-Path $script:TestMetricsDir "archive"
                New-Item -ItemType Directory -Path $archiveDir -Force | Out-Null

                # Create sessions with improving success rate
                $session1 = New-TestArchiveSession -Phase "implement" -TotalInvocations 10 -TotalSuccesses 8 -TotalFailures 2 `
                    -StartTime (Get-Date).AddDays(-3).ToString("o")
                $session1 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260124-100000.json") -Encoding UTF8

                $session2 = New-TestArchiveSession -Phase "implement" -TotalInvocations 10 -TotalSuccesses 9 -TotalFailures 1 `
                    -StartTime (Get-Date).AddDays(-2).ToString("o")
                $session2 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260125-100000.json") -Encoding UTF8

                $session3 = New-TestArchiveSession -Phase "implement" -TotalInvocations 10 -TotalSuccesses 10 -TotalFailures 0 `
                    -StartTime (Get-Date).AddDays(-1).ToString("o")
                $session3 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260126-100000.json") -Encoding UTF8

                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                $output = Generate-TrendsReport -Days 30 6>&1 | Out-String

                # Should show SUCCESS RATE section and improving trend
                $output | Should Match "SUCCESS RATE"
                $output | Should Match "improving|80.*100"
            } finally {
                Teardown-TestEnvironment
            }
        }

        # T038 [US3]: Trends action generates insights for concerning trends
        It 'generates insights for concerning trends' {
            Setup-TestEnvironment
            try {
                $archiveDir = Join-Path $script:TestMetricsDir "archive"
                New-Item -ItemType Directory -Path $archiveDir -Force | Out-Null

                # Create sessions with large token increase (>20% threshold for warning)
                $session1 = New-TestArchiveSession -Phase "implement" -TotalTokens 100000 `
                    -StartTime (Get-Date).AddDays(-3).ToString("o")
                $session1 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260124-100000.json") -Encoding UTF8

                $session2 = New-TestArchiveSession -Phase "implement" -TotalTokens 200000 `
                    -StartTime (Get-Date).AddDays(-1).ToString("o")
                $session2 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260126-100000.json") -Encoding UTF8

                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                $output = Generate-TrendsReport -Days 30 6>&1 | Out-String

                # Should show INSIGHTS section with warning about token increase
                $output | Should Match "INSIGHTS"
                $output | Should Match "WARNING|warning|Token usage increased"
            } finally {
                Teardown-TestEnvironment
            }
        }

        # T039 [US3]: Trends action ignores legacy schema archives
        It 'ignores legacy schema archives' {
            Setup-TestEnvironment
            try {
                $archiveDir = Join-Path $script:TestMetricsDir "archive"
                New-Item -ItemType Directory -Path $archiveDir -Force | Out-Null

                # Legacy session (v1.0.0)
                $legacySession = New-TestArchiveSession -Phase "implement" -SchemaVersion "1.0.0" -TotalTokens 50000 `
                    -StartTime (Get-Date).AddDays(-3).ToString("o")
                $legacySession | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260124-100000.json") -Encoding UTF8

                # v2.0.0 sessions
                $modernSession1 = New-TestArchiveSession -Phase "implement" -TotalTokens 100000 `
                    -StartTime (Get-Date).AddDays(-2).ToString("o")
                $modernSession1 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260125-100000.json") -Encoding UTF8

                $modernSession2 = New-TestArchiveSession -Phase "plan" -TotalTokens 80000 `
                    -StartTime (Get-Date).AddDays(-1).ToString("o")
                $modernSession2 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260126-100000.json") -Encoding UTF8

                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                $output = Generate-TrendsReport -Days 30 6>&1 | Out-String

                # Should only count the 2 v2.0.0 sessions, not the legacy one
                $output | Should Match "Sessions Analyzed:\s+2"
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'handles no archives found' {
            Setup-TestEnvironment
            try {
                # No archive directory
                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                $output = Generate-TrendsReport -Days 30 6>&1 | Out-String

                $output | Should Match "No archived sessions found"
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'handles all legacy archives gracefully' {
            Setup-TestEnvironment
            try {
                $archiveDir = Join-Path $script:TestMetricsDir "archive"
                New-Item -ItemType Directory -Path $archiveDir -Force | Out-Null

                # Only legacy sessions
                $legacySession = New-TestArchiveSession -SchemaVersion "1.0.0" -TotalTokens 50000 `
                    -StartTime (Get-Date).AddDays(-2).ToString("o")
                $legacySession | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260125-100000.json") -Encoding UTF8

                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                $output = Generate-TrendsReport -Days 30 6>&1 | Out-String

                $output | Should Match "No v2.0.0\+ sessions|no.*sessions available"
            } finally {
                Teardown-TestEnvironment
            }
        }
    }

    Context 'Compare Action' {
        function New-TestArchiveSession {
            param(
                [string]$Phase = "implement",
                [string]$FeatureBranch = "test-branch",
                [string]$SchemaVersion = "2.0.0",
                [int]$TotalTokens = 100000,
                [int]$TotalInvocations = 5,
                [int]$TotalSuccesses = 5,
                [int]$TotalFailures = 0,
                [int]$TotalDurationMs = 300000,
                [string]$StartTime = "",
                [string]$EndTime = ""
            )

            if (-not $StartTime) { $StartTime = (Get-Date).ToString("o") }
            if (-not $EndTime) { $EndTime = (Get-Date).AddMinutes(30).ToString("o") }

            return @{
                schemaVersion = $SchemaVersion
                phase = $Phase
                featureBranch = $FeatureBranch
                startTime = $StartTime
                endTime = $EndTime
                completionStatus = "complete"
                sessionId = [guid]::NewGuid().ToString()
                invocations = @()
                agents = @{}
                models = [PSCustomObject]@{}
                categories = [PSCustomObject]@{}
                parallelGroups = [PSCustomObject]@{}
                totals = @{
                    totalInvocations = $TotalInvocations
                    totalTokens = $TotalTokens
                    totalSuccesses = $TotalSuccesses
                    totalFailures = $TotalFailures
                    totalTimeouts = 0
                    totalDurationMs = $TotalDurationMs
                    parallelInvocations = 0
                    sequentialInvocations = $TotalInvocations
                    avgTokensPerInvocation = [math]::Round($TotalTokens / [math]::Max($TotalInvocations, 1), 0)
                    avgDurationMs = [math]::Round($TotalDurationMs / [math]::Max($TotalInvocations, 1), 0)
                    overallSuccessRate = if ($TotalInvocations -gt 0) { [math]::Round(($TotalSuccesses / $TotalInvocations) * 100, 1) } else { 0 }
                }
            }
        }

        # T048 [US4]: Compare action loads two sessions from archive
        It 'loads two sessions from archive by filename' {
            Setup-TestEnvironment
            try {
                $archiveDir = Join-Path $script:TestMetricsDir "archive"
                New-Item -ItemType Directory -Path $archiveDir -Force | Out-Null

                $session1 = New-TestArchiveSession -Phase "implement" -FeatureBranch "feature-a" -TotalTokens 100000 `
                    -StartTime (Get-Date).AddDays(-3).ToString("o")
                $session1 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260124-100000.json") -Encoding UTF8

                $session2 = New-TestArchiveSession -Phase "implement" -FeatureBranch "feature-a" -TotalTokens 200000 `
                    -StartTime (Get-Date).AddDays(-1).ToString("o")
                $session2 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260126-100000.json") -Encoding UTF8

                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                $output = Compare-Sessions -Sess1 "agent-metrics-20260124-100000.json" -Sess2 "agent-metrics-20260126-100000.json" 6>&1 | Out-String

                # Should display SESSION COMPARISON header and both sessions
                $output | Should Match "SESSION COMPARISON"
                $output | Should Match "20260124"
                $output | Should Match "20260126"
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'loads sessions by timestamp pattern' {
            Setup-TestEnvironment
            try {
                $archiveDir = Join-Path $script:TestMetricsDir "archive"
                New-Item -ItemType Directory -Path $archiveDir -Force | Out-Null

                $session1 = New-TestArchiveSession -Phase "implement" -TotalTokens 100000 `
                    -StartTime (Get-Date).AddDays(-3).ToString("o")
                $session1 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260124-100000.json") -Encoding UTF8

                $session2 = New-TestArchiveSession -Phase "implement" -TotalTokens 200000 `
                    -StartTime (Get-Date).AddDays(-1).ToString("o")
                $session2 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260126-100000.json") -Encoding UTF8

                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                $output = Compare-Sessions -Sess1 "20260124-100000" -Sess2 "20260126-100000" 6>&1 | Out-String

                $output | Should Match "SESSION COMPARISON"
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'loads sessions by date-only pattern (picks latest for that date)' {
            Setup-TestEnvironment
            try {
                $archiveDir = Join-Path $script:TestMetricsDir "archive"
                New-Item -ItemType Directory -Path $archiveDir -Force | Out-Null

                $session1 = New-TestArchiveSession -Phase "implement" -TotalTokens 100000 `
                    -StartTime (Get-Date).AddDays(-3).ToString("o")
                $session1 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260124-100000.json") -Encoding UTF8

                $session2 = New-TestArchiveSession -Phase "implement" -TotalTokens 200000 `
                    -StartTime (Get-Date).AddDays(-1).ToString("o")
                $session2 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260126-100000.json") -Encoding UTF8

                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                $output = Compare-Sessions -Sess1 "20260124" -Sess2 "20260126" 6>&1 | Out-String

                $output | Should Match "SESSION COMPARISON"
            } finally {
                Teardown-TestEnvironment
            }
        }

        # T049 [US4]: Compare action calculates deltas between sessions
        It 'calculates deltas between sessions' {
            Setup-TestEnvironment
            try {
                $archiveDir = Join-Path $script:TestMetricsDir "archive"
                New-Item -ItemType Directory -Path $archiveDir -Force | Out-Null

                $session1 = New-TestArchiveSession -Phase "implement" -FeatureBranch "feature-a" `
                    -TotalTokens 100000 -TotalInvocations 8 -TotalSuccesses 7 -TotalFailures 1 -TotalDurationMs 480000 `
                    -StartTime (Get-Date).AddDays(-3).ToString("o")
                $session1 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260124-100000.json") -Encoding UTF8

                $session2 = New-TestArchiveSession -Phase "implement" -FeatureBranch "feature-a" `
                    -TotalTokens 150000 -TotalInvocations 12 -TotalSuccesses 11 -TotalFailures 1 -TotalDurationMs 600000 `
                    -StartTime (Get-Date).AddDays(-1).ToString("o")
                $session2 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260126-100000.json") -Encoding UTF8

                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                $output = Compare-Sessions -Sess1 "agent-metrics-20260124-100000.json" -Sess2 "agent-metrics-20260126-100000.json" 6>&1 | Out-String

                # Should show SUMMARY COMPARISON with deltas
                $output | Should Match "SUMMARY COMPARISON"
                # Should show invocation count change (+4)
                $output | Should Match "\+4"
                # Should show token change (+50,000 or +50000)
                $output | Should Match "\+50"
            } finally {
                Teardown-TestEnvironment
            }
        }

        # T050 [US4]: Compare action rejects legacy schema sessions
        It 'rejects legacy schema sessions' {
            Setup-TestEnvironment
            try {
                $archiveDir = Join-Path $script:TestMetricsDir "archive"
                New-Item -ItemType Directory -Path $archiveDir -Force | Out-Null

                # Legacy session (v1.0.0)
                $legacySession = New-TestArchiveSession -Phase "implement" -SchemaVersion "1.0.0" -TotalTokens 100000 `
                    -StartTime (Get-Date).AddDays(-3).ToString("o")
                $legacySession | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260124-100000.json") -Encoding UTF8

                # v2.0.0 session
                $modernSession = New-TestArchiveSession -Phase "implement" -TotalTokens 200000 `
                    -StartTime (Get-Date).AddDays(-1).ToString("o")
                $modernSession | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260126-100000.json") -Encoding UTF8

                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                $output = Compare-Sessions -Sess1 "agent-metrics-20260124-100000.json" -Sess2 "agent-metrics-20260126-100000.json" 6>&1 | Out-String

                # Should report legacy format error
                $output | Should Match "legacy|pre-2.0.0|Legacy"
            } finally {
                Teardown-TestEnvironment
            }
        }

        It 'reports error when session file not found' {
            Setup-TestEnvironment
            try {
                $archiveDir = Join-Path $script:TestMetricsDir "archive"
                New-Item -ItemType Directory -Path $archiveDir -Force | Out-Null

                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                $output = Compare-Sessions -Sess1 "nonexistent-session.json" -Sess2 "also-nonexistent.json" 6>&1 | Out-String

                $output | Should Match "not found|Not found|No.*found"
            } finally {
                Teardown-TestEnvironment
            }
        }
    }

    Context 'Export Action' {
        function New-TestArchiveSession {
            param(
                [string]$Phase = "implement",
                [string]$FeatureBranch = "test-branch",
                [string]$SchemaVersion = "2.0.0",
                [int]$TotalTokens = 100000,
                [int]$TotalInvocations = 5,
                [int]$TotalSuccesses = 5,
                [int]$TotalFailures = 0,
                [int]$TotalDurationMs = 300000,
                [string]$StartTime = "",
                [string]$EndTime = ""
            )

            if (-not $StartTime) { $StartTime = (Get-Date).ToString("o") }
            if (-not $EndTime) { $EndTime = (Get-Date).AddMinutes(30).ToString("o") }

            return @{
                schemaVersion = $SchemaVersion
                phase = $Phase
                featureBranch = $FeatureBranch
                startTime = $StartTime
                endTime = $EndTime
                completionStatus = "complete"
                sessionId = [guid]::NewGuid().ToString()
                invocations = @()
                agents = @{}
                models = [PSCustomObject]@{}
                categories = [PSCustomObject]@{}
                parallelGroups = [PSCustomObject]@{}
                totals = @{
                    totalInvocations = $TotalInvocations
                    totalTokens = $TotalTokens
                    totalSuccesses = $TotalSuccesses
                    totalFailures = $TotalFailures
                    totalTimeouts = 0
                    totalDurationMs = $TotalDurationMs
                    parallelInvocations = 0
                    sequentialInvocations = $TotalInvocations
                    avgTokensPerInvocation = [math]::Round($TotalTokens / [math]::Max($TotalInvocations, 1), 0)
                    avgDurationMs = [math]::Round($TotalDurationMs / [math]::Max($TotalInvocations, 1), 0)
                    overallSuccessRate = if ($TotalInvocations -gt 0) { [math]::Round(($TotalSuccesses / $TotalInvocations) * 100, 1) } else { 0 }
                }
            }
        }

        # T058 [US5]: Export generates valid CSV with correct columns
        It 'generates valid CSV with correct columns' {
            Setup-TestEnvironment
            try {
                # Create session with invocations
                $session = New-TestSession -Phase "implement" -FeatureBranch "test-branch"
                $inv1 = @{
                    timestamp = (Get-Date).ToString("o")
                    agent = "backend-developer"
                    model = "sonnet"
                    taskId = "t-001"
                    status = "completed"
                    tokens = 30000
                    durationMs = 60000
                    description = "Test task"
                    phase = "implement"
                    category = "implementation"
                    invocationStartTime = (Get-Date).AddSeconds(-60).ToString("o")
                    invocationEndTime = (Get-Date).ToString("o")
                    isParallel = $false
                    parallelGroupId = $null
                    groupSize = 1
                }
                $session.invocations = @($inv1)
                $session.models = [PSCustomObject]@{
                    sonnet = @{ count = 1; totalTokens = 30000; costWeight = 3.0; weightedTokens = 90000; costPercent = 100.0 }
                }
                $session.totals.totalInvocations = 1
                $session.totals.totalTokens = 30000
                $session.totals.totalSuccesses = 1
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8

                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                $csvPath = Join-Path $script:TestMetricsDir "test-export.csv"

                # Call Export-Metrics
                Export-Metrics -Format "CSV" -OutputPath $csvPath

                # Verify file exists
                $csvPath | Should Exist

                # Verify header
                $content = Get-Content $csvPath
                $content[0] | Should Match "Timestamp,Phase,FeatureBranch,Agent,Model,TaskId,Category,Status,Tokens,DurationMs,DurationSec,IsParallel,ParallelGroupId,Description"

                # Verify at least 1 data row
                $content.Count | Should BeGreaterThan 1
            } finally {
                Teardown-TestEnvironment
            }
        }

        # T059 [US5]: Export generates valid JSON format
        It 'generates valid JSON format' {
            Setup-TestEnvironment
            try {
                # Create session with invocations
                $session = New-TestSession -Phase "implement" -FeatureBranch "test-branch"
                $inv1 = @{
                    timestamp = (Get-Date).ToString("o")
                    agent = "backend-developer"
                    model = "sonnet"
                    taskId = "t-002"
                    status = "completed"
                    tokens = 25000
                    durationMs = 50000
                    description = "JSON export test"
                    phase = "implement"
                    category = "implementation"
                    invocationStartTime = (Get-Date).AddSeconds(-50).ToString("o")
                    invocationEndTime = (Get-Date).ToString("o")
                    isParallel = $false
                    parallelGroupId = $null
                    groupSize = 1
                }
                $session.invocations = @($inv1)
                $session.models = [PSCustomObject]@{
                    sonnet = @{ count = 1; totalTokens = 25000; costWeight = 3.0; weightedTokens = 75000; costPercent = 100.0 }
                }
                $session.totals.totalInvocations = 1
                $session.totals.totalTokens = 25000
                $session.totals.totalSuccesses = 1
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8

                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                $jsonPath = Join-Path $script:TestMetricsDir "test-export.json"

                # Call Export-Metrics
                Export-Metrics -Format "JSON" -OutputPath $jsonPath

                # Verify file exists
                $jsonPath | Should Exist

                # Verify valid JSON
                $content = Get-Content $jsonPath -Raw | ConvertFrom-Json
                $content | Should Not BeNullOrEmpty
                $content -is [array] | Should Be $true
            } finally {
                Teardown-TestEnvironment
            }
        }

        # T060 [US5]: Export includes archives when -IncludeArchives specified
        It 'includes archives when -IncludeArchives specified' {
            Setup-TestEnvironment
            try {
                # Create active session with 1 invocation
                $session = New-TestSession -Phase "implement" -FeatureBranch "test-branch"
                $inv1 = @{
                    timestamp = (Get-Date).ToString("o")
                    agent = "backend-developer"
                    model = "sonnet"
                    taskId = "t-active-001"
                    status = "completed"
                    tokens = 30000
                    durationMs = 60000
                    description = "Active task"
                    phase = "implement"
                    category = "implementation"
                    invocationStartTime = (Get-Date).AddSeconds(-60).ToString("o")
                    invocationEndTime = (Get-Date).ToString("o")
                    isParallel = $false
                    parallelGroupId = $null
                    groupSize = 1
                }
                $session.invocations = @($inv1)
                $session.models = [PSCustomObject]@{
                    sonnet = @{ count = 1; totalTokens = 30000; costWeight = 3.0; weightedTokens = 90000; costPercent = 100.0 }
                }
                $session.totals.totalInvocations = 1
                $session.totals.totalTokens = 30000
                $session.totals.totalSuccesses = 1
                $session | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8

                # Create archive directory with 1 archived session containing 2 invocations
                $archiveDir = Join-Path $script:TestMetricsDir "archive"
                New-Item -ItemType Directory -Path $archiveDir -Force | Out-Null

                $archivedSession = New-TestArchiveSession -Phase "plan" -FeatureBranch "test-branch" -TotalTokens 50000 -TotalInvocations 2
                $inv2 = @{
                    timestamp = (Get-Date).AddDays(-1).ToString("o")
                    agent = "solution-architect"
                    model = "opus"
                    taskId = "t-archived-001"
                    status = "completed"
                    tokens = 25000
                    durationMs = 120000
                    description = "Archived task 1"
                    phase = "plan"
                    category = "architecture"
                    invocationStartTime = (Get-Date).AddDays(-1).AddSeconds(-120).ToString("o")
                    invocationEndTime = (Get-Date).AddDays(-1).ToString("o")
                    isParallel = $false
                    parallelGroupId = $null
                    groupSize = 1
                }
                $inv3 = @{
                    timestamp = (Get-Date).AddDays(-1).AddHours(1).ToString("o")
                    agent = "database-architect"
                    model = "sonnet"
                    taskId = "t-archived-002"
                    status = "completed"
                    tokens = 25000
                    durationMs = 90000
                    description = "Archived task 2"
                    phase = "plan"
                    category = "database"
                    invocationStartTime = (Get-Date).AddDays(-1).AddHours(1).AddSeconds(-90).ToString("o")
                    invocationEndTime = (Get-Date).AddDays(-1).AddHours(1).ToString("o")
                    isParallel = $false
                    parallelGroupId = $null
                    groupSize = 1
                }
                $archivedSession.invocations = @($inv2, $inv3)
                $archivedSession | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260125-100000.json") -Encoding UTF8

                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                $csvPath = Join-Path $script:TestMetricsDir "test-export-with-archives.csv"

                # Call Export-Metrics with -IncludeArchives
                Export-Metrics -Format "CSV" -OutputPath $csvPath -IncludeArchives

                # Verify file has header + 3 data lines (1 from active + 2 from archive)
                $content = Get-Content $csvPath
                $content.Count | Should Be 4
            } finally {
                Teardown-TestEnvironment
            }
        }
    }

    Context 'Cumulative Action' {
        function New-TestArchiveSession {
            param(
                [string]$Phase = "implement",
                [string]$FeatureBranch = "test-branch",
                [string]$SchemaVersion = "2.0.0",
                [int]$TotalTokens = 100000,
                [int]$TotalInvocations = 5,
                [int]$TotalSuccesses = 5,
                [int]$TotalFailures = 0,
                [int]$TotalDurationMs = 300000,
                [string]$StartTime = "",
                [string]$EndTime = ""
            )

            if (-not $StartTime) { $StartTime = (Get-Date).ToString("o") }
            if (-not $EndTime) { $EndTime = (Get-Date).AddMinutes(30).ToString("o") }

            return @{
                schemaVersion = $SchemaVersion
                phase = $Phase
                featureBranch = $FeatureBranch
                startTime = $StartTime
                endTime = $EndTime
                completionStatus = "complete"
                sessionId = [guid]::NewGuid().ToString()
                invocations = @()
                agents = @{}
                models = [PSCustomObject]@{}
                categories = [PSCustomObject]@{}
                parallelGroups = [PSCustomObject]@{}
                totals = @{
                    totalInvocations = $TotalInvocations
                    totalTokens = $TotalTokens
                    totalSuccesses = $TotalSuccesses
                    totalFailures = $TotalFailures
                    totalTimeouts = 0
                    totalDurationMs = $TotalDurationMs
                    parallelInvocations = 0
                    sequentialInvocations = $TotalInvocations
                    avgTokensPerInvocation = [math]::Round($TotalTokens / [math]::Max($TotalInvocations, 1), 0)
                    avgDurationMs = [math]::Round($TotalDurationMs / [math]::Max($TotalInvocations, 1), 0)
                    overallSuccessRate = if ($TotalInvocations -gt 0) { [math]::Round(($TotalSuccesses / $TotalInvocations) * 100, 1) } else { 0 }
                }
            }
        }

        # T061 [US5]: Cumulative aggregates all sessions for feature branch
        It 'aggregates all sessions for feature branch' {
            Setup-TestEnvironment
            try {
                # Create archive with 2 v2.0.0 sessions for "feature-x" branch
                $archiveDir = Join-Path $script:TestMetricsDir "archive"
                New-Item -ItemType Directory -Path $archiveDir -Force | Out-Null

                # Session 1: plan phase (50000 tokens, 3 invocations)
                $session1 = New-TestArchiveSession -Phase "plan" -FeatureBranch "feature-x" `
                    -TotalTokens 50000 -TotalInvocations 3 -TotalSuccesses 3 `
                    -StartTime (Get-Date).AddDays(-3).ToString("o")
                $session1 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260124-100000.json") -Encoding UTF8

                # Session 2: implement phase (100000 tokens, 5 invocations)
                $session2 = New-TestArchiveSession -Phase "implement" -FeatureBranch "feature-x" `
                    -TotalTokens 100000 -TotalInvocations 5 -TotalSuccesses 5 `
                    -StartTime (Get-Date).AddDays(-2).ToString("o")
                $session2 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260125-100000.json") -Encoding UTF8

                # Active session for same branch (30000 tokens, 2 invocations)
                $activeSession = New-TestSession -Phase "implement" -FeatureBranch "feature-x"
                $activeSession.totals.totalInvocations = 2
                $activeSession.totals.totalTokens = 30000
                $activeSession.totals.totalSuccesses = 2
                $activeSession | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8

                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                # Call Generate-CumulativeReport
                $output = Generate-CumulativeReport -Branch "feature-x" 6>&1 | Out-String

                # Verify output
                $output | Should Match "CUMULATIVE FEATURE REPORT"
                $output | Should Match "10"  # Total invocations (3+5+2)
                $output | Should Match "180.*000|180000"  # Total tokens (50000+100000+30000)
            } finally {
                Teardown-TestEnvironment
            }
        }

        # T062 [US5]: Cumulative displays phase breakdown
        It 'displays phase breakdown' {
            Setup-TestEnvironment
            try {
                # Create archive with 2 sessions for "feature-x" branch (different phases)
                $archiveDir = Join-Path $script:TestMetricsDir "archive"
                New-Item -ItemType Directory -Path $archiveDir -Force | Out-Null

                # Session 1: plan phase
                $session1 = New-TestArchiveSession -Phase "plan" -FeatureBranch "feature-x" `
                    -TotalTokens 50000 -TotalInvocations 3 -TotalSuccesses 3 `
                    -StartTime (Get-Date).AddDays(-3).ToString("o")
                $session1 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260124-100000.json") -Encoding UTF8

                # Session 2: implement phase
                $session2 = New-TestArchiveSession -Phase "implement" -FeatureBranch "feature-x" `
                    -TotalTokens 100000 -TotalInvocations 5 -TotalSuccesses 5 `
                    -StartTime (Get-Date).AddDays(-2).ToString("o")
                $session2 | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $archiveDir "agent-metrics-20260125-100000.json") -Encoding UTF8

                # Active session (implement phase)
                $activeSession = New-TestSession -Phase "implement" -FeatureBranch "feature-x"
                $activeSession.totals.totalInvocations = 2
                $activeSession.totals.totalTokens = 30000
                $activeSession.totals.totalSuccesses = 2
                $activeSession | ConvertTo-Json -Depth 10 | Set-Content $script:TestMetricsFile -Encoding UTF8

                $settings = New-TestSettings
                $settings | ConvertTo-Json -Depth 10 | Set-Content $script:TestSettingsFile -Encoding UTF8

                # Call Generate-CumulativeReport
                $output = Generate-CumulativeReport -Branch "feature-x" 6>&1 | Out-String

                # Verify phase breakdown section
                $output | Should Match "PHASE BREAKDOWN"
                $output | Should Match "plan"
                $output | Should Match "implement"
            } finally {
                Teardown-TestEnvironment
            }
        }
    }
}
