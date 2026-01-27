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

    # Create directory
    if (-not (Test-Path $script:TestMetricsDir)) {
        New-Item -ItemType Directory -Path $script:TestMetricsDir -Force | Out-Null
    }
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
}
