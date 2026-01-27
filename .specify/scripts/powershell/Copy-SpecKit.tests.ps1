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

Describe 'Copy-SpecKit' {

    BeforeAll {
        $ScriptDir = Split-Path -Parent $PSCommandPath

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
    }

    #region Phase 2: Foundational Functions

    Context 'Test-SpecKitSource' {
        BeforeAll {
            $script:SourceRoot = New-MockSpecKitSource
            $script:DestRoot = Join-Path $TestDrive 'destination'
            New-Item -ItemType Directory -Path $script:DestRoot -Force | Out-Null
        }

        It 'returns Valid=$true for a valid source with all required items' {
            $result = Test-SpecKitSource -SourcePath $script:SourceRoot -DestinationPath $script:DestRoot
            $result.Valid | Should -BeTrue
            $result.Errors | Should -HaveCount 0
        }

        It 'returns Valid=$false when source path does not exist' {
            $result = Test-SpecKitSource -SourcePath (Join-Path $TestDrive 'nonexistent') -DestinationPath $script:DestRoot
            $result.Valid | Should -BeFalse
            $expected = "Source path does not exist: $(Join-Path $TestDrive 'nonexistent')"
            $result.Errors | Should -Contain $expected
        }

        It 'returns Valid=$false when .claude directory is missing' {
            $incomplete = Join-Path $TestDrive 'incomplete-source'
            New-Item -ItemType Directory -Path $incomplete -Force | Out-Null
            New-Item -ItemType Directory -Path (Join-Path $incomplete '.specify') -Force | Out-Null
            Set-Content -Path (Join-Path $incomplete 'CLAUDE.md') -Value '# test'

            $result = Test-SpecKitSource -SourcePath $incomplete -DestinationPath $script:DestRoot
            $result.Valid | Should -BeFalse
            $result.Errors[0] | Should -Match 'Missing.*\.claude/ directory'
        }

        It 'returns Valid=$false when .specify directory is missing' {
            $incomplete = Join-Path $TestDrive 'no-specify'
            New-Item -ItemType Directory -Path $incomplete -Force | Out-Null
            New-Item -ItemType Directory -Path (Join-Path $incomplete '.claude') -Force | Out-Null
            Set-Content -Path (Join-Path $incomplete 'CLAUDE.md') -Value '# test'

            $result = Test-SpecKitSource -SourcePath $incomplete -DestinationPath $script:DestRoot
            $result.Valid | Should -BeFalse
            $result.Errors[0] | Should -Match 'Missing.*\.specify/ directory'
        }

        It 'returns Valid=$false when CLAUDE.md is missing' {
            $incomplete = Join-Path $TestDrive 'no-claude-md'
            New-Item -ItemType Directory -Path $incomplete -Force | Out-Null
            New-Item -ItemType Directory -Path (Join-Path $incomplete '.claude') -Force | Out-Null
            New-Item -ItemType Directory -Path (Join-Path $incomplete '.specify') -Force | Out-Null

            $result = Test-SpecKitSource -SourcePath $incomplete -DestinationPath $script:DestRoot
            $result.Valid | Should -BeFalse
            $result.Errors[0] | Should -Match 'Missing.*CLAUDE\.md'
        }

        It 'returns Valid=$false when source and destination are the same path' {
            $result = Test-SpecKitSource -SourcePath $script:SourceRoot -DestinationPath $script:SourceRoot
            $result.Valid | Should -BeFalse
            $result.Errors | Should -Contain "Source and destination paths are the same: $($script:SourceRoot)"
        }
    }

    Context 'Build-FileManifest' {
        BeforeAll {
            $script:SourceRoot = New-MockSpecKitSource
            $script:Manifest = Build-FileManifest -SourcePath $script:SourceRoot
        }

        It 'returns Framework entries for Tier 1 files' {
            $framework = $script:Manifest | Where-Object { $_.Tier -eq 'Framework' }
            $framework | Should -Not -BeNullOrEmpty

            # CLAUDE.md
            $framework.RelativePath | Should -Contain 'CLAUDE.md'
            # settings.json
            ($framework | Where-Object { $_.RelativePath -match 'settings\.json' }) | Should -Not -BeNullOrEmpty
            # hooks.json
            ($framework | Where-Object { $_.RelativePath -match 'hooks\.json' }) | Should -Not -BeNullOrEmpty
            # QUICK-REFERENCE.md
            ($framework | Where-Object { $_.RelativePath -match 'QUICK-REFERENCE\.md' }) | Should -Not -BeNullOrEmpty
        }

        It 'returns Framework entries for command files' {
            $commands = $script:Manifest | Where-Object { $_.Tier -eq 'Framework' -and $_.RelativePath -match 'commands' }
            $commands.Count | Should -BeGreaterOrEqual 3
        }

        It 'returns Framework entries for template files' {
            $templates = $script:Manifest | Where-Object { $_.Tier -eq 'Framework' -and $_.RelativePath -match 'templates' }
            $templates.Count | Should -BeGreaterOrEqual 3
        }

        It 'returns Framework entries for script files' {
            $scripts = @($script:Manifest | Where-Object { $_.Tier -eq 'Framework' -and $_.RelativePath -match 'scripts.*\.ps1' })
            $scripts.Count | Should -BeGreaterOrEqual 1
        }

        It 'returns CoreAgent entries for all 7 core agents with Included=$true' {
            $coreAgents = $script:Manifest | Where-Object { $_.Tier -eq 'CoreAgent' }
            $coreAgents.Count | Should -Be 7
            $coreAgents | ForEach-Object { $_.Included | Should -BeTrue }
            $coreAgents | ForEach-Object { $_.Domain | Should -BeNullOrEmpty }
        }

        It 'returns CoreSkill entries for core skills with Included=$true' {
            $coreSkills = $script:Manifest | Where-Object { $_.Tier -eq 'CoreSkill' }
            $coreSkills.Count | Should -Be 2
            $coreSkills | ForEach-Object { $_.Included | Should -BeTrue }
        }

        It 'returns DomainAgent entries with Included=$false by default' {
            $domainAgents = $script:Manifest | Where-Object { $_.Tier -eq 'DomainAgent' }
            $domainAgents.Count | Should -Be 5  # 3 umbraco + 2 breezsdk
            $domainAgents | ForEach-Object { $_.Included | Should -BeFalse }
            $domainAgents | ForEach-Object { $_.Domain | Should -Not -BeNullOrEmpty }
        }

        It 'returns DomainSkill entry for breezsdk-knowledge with Domain=breezsdk' {
            $domainSkills = @($script:Manifest | Where-Object { $_.Tier -eq 'DomainSkill' })
            $domainSkills.Count | Should -Be 1
            $domainSkills[0].Domain | Should -Be 'breezsdk'
            $domainSkills[0].Included | Should -BeFalse
        }

        It 'includes umbraco domain entries when -IncludeDomains umbraco is specified' {
            $manifest = Build-FileManifest -SourcePath $script:SourceRoot -IncludeDomains 'umbraco'
            $umbracoAgents = $manifest | Where-Object { $_.Tier -eq 'DomainAgent' -and $_.Domain -eq 'umbraco' }
            $umbracoAgents | ForEach-Object { $_.Included | Should -BeTrue }

            # breezsdk should still be excluded
            $breezAgents = $manifest | Where-Object { $_.Domain -eq 'breezsdk' }
            $breezAgents | ForEach-Object { $_.Included | Should -BeFalse }
        }

        It 'does not include excluded files like CLAUDE.project.md or settings.local.json' {
            $script:Manifest.RelativePath | Should -Not -Contain 'CLAUDE.project.md'
            ($script:Manifest | Where-Object { $_.RelativePath -match 'settings\.local\.json' }) | Should -BeNullOrEmpty
        }
    }

    Context 'Get-DomainModules' {
        BeforeAll {
            $script:SourceRoot = New-MockSpecKitSource
            $script:Domains = @(Get-DomainModules -SourcePath $script:SourceRoot)
        }

        It 'discovers exactly 2 domain modules' {
            $script:Domains.Count | Should -Be 2
        }

        It 'returns results sorted alphabetically by Name' {
            $script:Domains[0].Name | Should -Be 'breezsdk'
            $script:Domains[1].Name | Should -Be 'umbraco'
        }

        It 'discovers umbraco module with 3 agent files and 0 skill files' {
            $umbraco = $script:Domains | Where-Object { $_.Name -eq 'umbraco' }
            $umbraco.AgentFiles.Count | Should -Be 3
            $umbraco.SkillFiles.Count | Should -Be 0
            $umbraco.FileCount | Should -Be 3
        }

        It 'discovers breezsdk module with 2 agent files and 1 skill file' {
            $breezsdk = $script:Domains | Where-Object { $_.Name -eq 'breezsdk' }
            $breezsdk.AgentFiles.Count | Should -Be 2
            $breezsdk.SkillFiles.Count | Should -Be 1
            $breezsdk.FileCount | Should -Be 3
        }

        It 'agent files contain relative path with agents directory' {
            $breezsdk = $script:Domains | Where-Object { $_.Name -eq 'breezsdk' }
            $breezsdk.AgentFiles | ForEach-Object { $_ | Should -Match 'agents' }
        }

        It 'skill files contain relative path with skills directory' {
            $breezsdk = $script:Domains | Where-Object { $_.Name -eq 'breezsdk' }
            $breezsdk.SkillFiles | ForEach-Object { $_ | Should -Match 'skills' }
        }

        It 'returns empty array when source has no domain files' {
            $coreOnly = Join-Path $TestDrive 'core-only'
            New-Item -ItemType Directory -Path (Join-Path $coreOnly '.claude/agents') -Force | Out-Null
            New-Item -ItemType Directory -Path (Join-Path $coreOnly '.claude/skills') -Force | Out-Null
            @('backend-developer.md', 'code-reviewer.md') | ForEach-Object {
                Set-Content -Path (Join-Path $coreOnly ".claude/agents/$_") -Value "# $_"
            }
            @('external-plugins.md') | ForEach-Object {
                Set-Content -Path (Join-Path $coreOnly ".claude/skills/$_") -Value "# $_"
            }

            $result = @(Get-DomainModules -SourcePath $coreOnly)
            $result.Count | Should -Be 0
        }
    }

    #endregion

    #region Phase 3: User Story 1 - Copy Framework to New Project

    Context 'US1: Core copy operation' {
        # T010: All Tier 1 files copied, Tier 2 core agents copied, domain files excluded

        BeforeAll {
            $script:SourceRoot = New-MockSpecKitSource
            $script:DestRoot = Join-Path $TestDrive 'us1-copy-dest'
            $script:Manifest = Build-FileManifest -SourcePath $script:SourceRoot
            $script:CopyResult = Invoke-SpecKitCopy -Manifest $script:Manifest -SourcePath $script:SourceRoot -DestinationPath $script:DestRoot
        }

        It 'copies all Tier 1 framework files to destination with correct relative paths' {
            $frameworkEntries = $script:Manifest | Where-Object { $_.Tier -eq 'Framework' -and $_.Included -eq $true }
            $frameworkEntries | Should -Not -BeNullOrEmpty

            foreach ($entry in $frameworkEntries) {
                $destPath = Join-Path $script:DestRoot $entry.RelativePath
                $destPath | Should -Exist -Because "Framework file $($entry.RelativePath) should be copied"
            }
        }

        It 'copies all 7 Tier 2 core agent files to destination' {
            $coreAgents = @('backend-developer.md', 'code-reviewer.md', 'database-architect.md',
                            'frontend-developer.md', 'security-auditor.md', 'solution-architect.md',
                            'test-engineer.md')

            foreach ($agent in $coreAgents) {
                $destPath = Join-Path $script:DestRoot ".claude\agents\$agent"
                $destPath | Should -Exist -Because "Core agent $agent should be copied"
            }
        }

        It 'copies all Tier 2 core skill files to destination' {
            $coreSkills = @('external-plugins.md', 'implementation-execution.md')

            foreach ($skill in $coreSkills) {
                $destPath = Join-Path $script:DestRoot ".claude\skills\$skill"
                $destPath | Should -Exist -Because "Core skill $skill should be copied"
            }
        }

        It 'does not copy domain agent files when Included=$false' {
            $domainAgents = @('umbraco-architect.md', 'umbraco-backend-developer.md', 'umbraco-frontend-developer.md',
                              'breezsdk-architect.md', 'breezsdk-developer.md')

            foreach ($agent in $domainAgents) {
                $destPath = Join-Path $script:DestRoot ".claude\agents\$agent"
                $destPath | Should -Not -Exist -Because "Domain agent $agent should be excluded by default"
            }
        }

        It 'does not copy domain skill files when Included=$false' {
            $destPath = Join-Path $script:DestRoot '.claude\skills\breezsdk-knowledge.md'
            $destPath | Should -Not -Exist -Because 'Domain skill should be excluded by default'
        }

        It 'creates destination subdirectories automatically' {
            $expectedDirs = @(
                '.claude\commands',
                '.claude\agents',
                '.claude\skills',
                '.specify\templates',
                '.specify\scripts\powershell'
            )

            foreach ($dir in $expectedDirs) {
                $destDir = Join-Path $script:DestRoot $dir
                $destDir | Should -Exist -Because "Directory $dir should be created automatically"
            }
        }

        It 'returns CopyResult object with FilesCopied count' {
            $script:CopyResult | Should -Not -BeNullOrEmpty
            $script:CopyResult.PSObject.Properties.Name | Should -Contain 'FilesCopied'
            $script:CopyResult.FilesCopied | Should -BeOfType [int]
            $script:CopyResult.FilesCopied | Should -BeGreaterThan 0
        }

        It 'returns CopyResult with FilesCopied count matching included files in manifest' {
            $includedCount = @($script:Manifest | Where-Object { $_.Included -eq $true }).Count
            $script:CopyResult.FilesCopied | Should -Be $includedCount -Because 'FilesCopied should match included manifest entries'
        }

        It 'preserves file content during copy' {
            # Test CLAUDE.md content
            $sourcePath = Join-Path $script:SourceRoot 'CLAUDE.md'
            $destPath = Join-Path $script:DestRoot 'CLAUDE.md'

            $sourceContent = Get-Content -Path $sourcePath -Raw
            $destContent = Get-Content -Path $destPath -Raw

            $destContent | Should -Be $sourceContent -Because 'File content should be preserved during copy'
        }

        It 'preserves file content for nested files' {
            # Test a command file
            $relPath = '.claude\commands\speckit.plan.md'
            $sourcePath = Join-Path $script:SourceRoot $relPath
            $destPath = Join-Path $script:DestRoot $relPath

            $sourceContent = Get-Content -Path $sourcePath -Raw
            $destContent = Get-Content -Path $destPath -Raw

            $destContent | Should -Be $sourceContent -Because 'Nested file content should be preserved'
        }

        It 'copies CLAUDE.md to destination root' {
            $destPath = Join-Path $script:DestRoot 'CLAUDE.md'
            $destPath | Should -Exist
        }

        It 'copies settings.json to .claude directory' {
            $destPath = Join-Path $script:DestRoot '.claude\settings.json'
            $destPath | Should -Exist
        }

        It 'copies hooks.json to .claude directory' {
            $destPath = Join-Path $script:DestRoot '.claude\hooks.json'
            $destPath | Should -Exist
        }

        It 'copies all command files to .claude/commands directory' {
            $commands = @('speckit.plan.md', 'speckit.specify.md', 'speckit.implement.md')

            foreach ($cmd in $commands) {
                $destPath = Join-Path $script:DestRoot ".claude\commands\$cmd"
                $destPath | Should -Exist -Because "Command file $cmd should be copied"
            }
        }

        It 'copies template files to .specify/templates directory' {
            $templates = @('spec-template.md', 'plan-template.md', 'tasks-template.md')

            foreach ($template in $templates) {
                $destPath = Join-Path $script:DestRoot ".specify\templates\$template"
                $destPath | Should -Exist -Because "Template file $template should be copied"
            }
        }

        It 'copies script files to .specify/scripts/powershell directory' {
            $destPath = Join-Path $script:DestRoot '.specify\scripts\powershell\common.ps1'
            $destPath | Should -Exist
        }
    }

    Context 'US1: Placeholder directory creation' {
        BeforeAll {
            $script:SourceRoot = New-MockSpecKitSource
            $script:DestRoot = Join-Path $TestDrive 'us1-placeholder-dest'
            $script:Manifest = Build-FileManifest -SourcePath $script:SourceRoot
            $script:CopyResult = Invoke-SpecKitCopy -Manifest $script:Manifest -SourcePath $script:SourceRoot -DestinationPath $script:DestRoot
        }

        It 'creates .specify/memory/ placeholder directory at destination' {
            $placeholderPath = Join-Path $script:DestRoot '.specify/memory'
            Test-Path $placeholderPath -PathType Container | Should -BeTrue
        }

        It 'creates .specify/metrics/ placeholder directory at destination' {
            $placeholderPath = Join-Path $script:DestRoot '.specify/metrics'
            Test-Path $placeholderPath -PathType Container | Should -BeTrue
        }

        It 'creates .specify/plans/ placeholder directory at destination' {
            $placeholderPath = Join-Path $script:DestRoot '.specify/plans'
            Test-Path $placeholderPath -PathType Container | Should -BeTrue
        }

        It 'creates specs/ placeholder directory at destination' {
            $placeholderPath = Join-Path $script:DestRoot 'specs'
            Test-Path $placeholderPath -PathType Container | Should -BeTrue
        }

        It 'creates placeholder directories that are empty (no files)' {
            $allPlaceholderFiles = @()
            foreach ($placeholder in @('.specify/memory', '.specify/metrics', '.specify/plans', 'specs')) {
                $placeholderPath = Join-Path $script:DestRoot $placeholder
                $files = @(Get-ChildItem -Path $placeholderPath -File -ErrorAction SilentlyContinue)
                $allPlaceholderFiles += $files
            }
            $allPlaceholderFiles.Count | Should -Be 0
        }

        It 'reports PlaceholderDirectories count of 4 in CopyResult' {
            $script:CopyResult.PlaceholderDirectories | Should -Be 4
        }

        It 'creates placeholder directories even when destination path does not exist initially' {
            $freshDestRoot = Join-Path $TestDrive 'brand-new-destination'
            Test-Path $freshDestRoot | Should -BeFalse

            $freshManifest = Build-FileManifest -SourcePath $script:SourceRoot
            $freshResult = Invoke-SpecKitCopy -Manifest $freshManifest -SourcePath $script:SourceRoot -DestinationPath $freshDestRoot

            $freshResult.PlaceholderDirectories | Should -Be 4
            foreach ($placeholder in @('.specify/memory', '.specify/metrics', '.specify/plans', 'specs')) {
                $placeholderPath = Join-Path $freshDestRoot $placeholder
                Test-Path $placeholderPath -PathType Container | Should -BeTrue
            }
        }
    }

    Context 'US1: Error handling' {
        # T012: Source not found, missing structure exit codes

        BeforeAll {
            $script:DestRoot = Join-Path $TestDrive 'us1-error-dest'
            New-Item -ItemType Directory -Path $script:DestRoot -Force | Out-Null
        }

        It 'Invoke-SpecKitCopy returns ExitCode=1 when source path does not exist' {
            $nonExistent = Join-Path $TestDrive 'nonexistent-source'
            $result = Invoke-SpecKitCopy -SourcePath $nonExistent -DestinationPath $script:DestRoot

            $result.ExitCode | Should -Be 1
            $result.Success | Should -BeFalse
            $result.Errors | Should -Not -BeNullOrEmpty
            $result.Errors | Should -Match 'Source path does not exist'
        }

        It 'Invoke-SpecKitCopy returns error messages for missing source' {
            $nonExistent = Join-Path $TestDrive 'missing-source'
            $result = Invoke-SpecKitCopy -SourcePath $nonExistent -DestinationPath $script:DestRoot

            $result.Errors.Count | Should -BeGreaterThan 0
            $result.Errors[0] | Should -Match "Source path does not exist: .+$([regex]::Escape('missing-source'))"
        }

        It 'Invoke-SpecKitCopy returns ExitCode=1 for missing required structure' {
            $incomplete = Join-Path $TestDrive 'incomplete-source'
            New-Item -ItemType Directory -Path $incomplete -Force | Out-Null
            # Only create .claude directory, missing .specify and CLAUDE.md
            New-Item -ItemType Directory -Path (Join-Path $incomplete '.claude') -Force | Out-Null

            $result = Invoke-SpecKitCopy -SourcePath $incomplete -DestinationPath $script:DestRoot

            $result.ExitCode | Should -Be 1
            $result.Success | Should -BeFalse
            $result.Errors | Should -Not -BeNullOrEmpty
            $result.Errors[0] | Should -Match 'Missing.*\.specify.*CLAUDE\.md'
        }

        It 'Invoke-SpecKitCopy returns detailed error listing all missing items' {
            $incomplete = Join-Path $TestDrive 'incomplete-detailed'
            New-Item -ItemType Directory -Path $incomplete -Force | Out-Null
            # Create only .claude, missing both .specify and CLAUDE.md
            New-Item -ItemType Directory -Path (Join-Path $incomplete '.claude') -Force | Out-Null

            $result = Invoke-SpecKitCopy -SourcePath $incomplete -DestinationPath $script:DestRoot

            $errorText = $result.Errors -join ' '
            $errorText | Should -Match '\.specify'
            $errorText | Should -Match 'CLAUDE\.md'
        }

        It 'Invoke-SpecKitCopy returns ExitCode=1 when source and destination are the same' {
            $samePath = Join-Path $TestDrive 'same-path'
            New-Item -ItemType Directory -Path $samePath -Force | Out-Null

            $result = Invoke-SpecKitCopy -SourcePath $samePath -DestinationPath $samePath

            $result.ExitCode | Should -Be 1
            $result.Success | Should -BeFalse
            $result.Errors | Should -Contain "Source and destination paths are the same: $samePath"
        }

        It 'No files are copied to destination when validation fails' {
            $badSource = Join-Path $TestDrive 'bad-source-no-copy'
            $dest = Join-Path $TestDrive 'should-be-empty'
            New-Item -ItemType Directory -Path $dest -Force | Out-Null

            $result = Invoke-SpecKitCopy -SourcePath $badSource -DestinationPath $dest

            $result.ExitCode | Should -Be 1
            $result.FilesCopied | Should -Be 0
            # Verify destination has no new files (only the directory itself exists)
            $files = @(Get-ChildItem -Path $dest -Recurse -File -ErrorAction SilentlyContinue)
            $files.Count | Should -Be 0
        }

        It 'Invoke-SpecKitCopy returns ExitCode=1 when .claude directory is missing' {
            $noClaude = Join-Path $TestDrive 'no-claude-dir'
            New-Item -ItemType Directory -Path $noClaude -Force | Out-Null
            New-Item -ItemType Directory -Path (Join-Path $noClaude '.specify') -Force | Out-Null
            Set-Content -Path (Join-Path $noClaude 'CLAUDE.md') -Value '# test'

            $result = Invoke-SpecKitCopy -SourcePath $noClaude -DestinationPath $script:DestRoot

            $result.ExitCode | Should -Be 1
            $result.Errors[0] | Should -Match '\.claude'
        }

        It 'Invoke-SpecKitCopy returns ExitCode=1 when .specify directory is missing' {
            $noSpecify = Join-Path $TestDrive 'no-specify-dir'
            New-Item -ItemType Directory -Path $noSpecify -Force | Out-Null
            New-Item -ItemType Directory -Path (Join-Path $noSpecify '.claude') -Force | Out-Null
            Set-Content -Path (Join-Path $noSpecify 'CLAUDE.md') -Value '# test'

            $result = Invoke-SpecKitCopy -SourcePath $noSpecify -DestinationPath $script:DestRoot

            $result.ExitCode | Should -Be 1
            $result.Errors[0] | Should -Match '\.specify'
        }

        It 'Invoke-SpecKitCopy returns ExitCode=1 when CLAUDE.md file is missing' {
            $noClaudeMd = Join-Path $TestDrive 'no-claude-md'
            New-Item -ItemType Directory -Path $noClaudeMd -Force | Out-Null
            New-Item -ItemType Directory -Path (Join-Path $noClaudeMd '.claude') -Force | Out-Null
            New-Item -ItemType Directory -Path (Join-Path $noClaudeMd '.specify') -Force | Out-Null

            $result = Invoke-SpecKitCopy -SourcePath $noClaudeMd -DestinationPath $script:DestRoot

            $result.ExitCode | Should -Be 1
            $result.Errors[0] | Should -Match 'CLAUDE\.md'
        }

        It 'Invoke-SpecKitCopy sets Success=$false when validation fails' {
            $invalid = Join-Path $TestDrive 'invalid-validation'
            $result = Invoke-SpecKitCopy -SourcePath $invalid -DestinationPath $script:DestRoot

            $result.Success | Should -BeFalse
            $result.ExitCode | Should -Be 1
        }
    }

    #endregion

    #region Phase 4: User Story 2 - Preview / Dry Run

    Context 'US2: Preview mode' {
        # T017: No filesystem changes, output lists files by tier

        BeforeAll {
            $script:SourceRoot = New-MockSpecKitSource
            $script:PreviewDest = Join-Path $TestDrive 'us2-preview-dest'
            $script:Manifest = Build-FileManifest -SourcePath $script:SourceRoot
            $script:PreviewResult = Invoke-SpecKitCopy -Manifest $script:Manifest -SourcePath $script:SourceRoot -DestinationPath $script:PreviewDest -Preview
        }

        It 'returns IsPreview=$true in CopyResult' {
            $script:PreviewResult.IsPreview | Should -BeTrue
        }

        It 'returns FilesCopied=0 in preview mode' {
            $script:PreviewResult.FilesCopied | Should -Be 0
        }

        It 'returns DirectoriesCreated=0 in preview mode' {
            $script:PreviewResult.DirectoriesCreated | Should -Be 0
        }

        It 'returns PlaceholderDirectories=0 in preview mode' {
            $script:PreviewResult.PlaceholderDirectories | Should -Be 0
        }

        It 'does not create the destination directory in preview mode' {
            Test-Path $script:PreviewDest | Should -BeFalse
        }

        It 'does not create any files at destination in preview mode' {
            $files = @(Get-ChildItem -Path $script:PreviewDest -Recurse -File -ErrorAction SilentlyContinue)
            $files.Count | Should -Be 0
        }

        It 'returns ExitCode=0 in preview mode with valid source' {
            $script:PreviewResult.ExitCode | Should -Be 0
        }

        It 'returns Success=$true in preview mode with valid source' {
            $script:PreviewResult.Success | Should -BeTrue
        }

        It 'populates DomainsAvailable in preview mode' {
            $script:PreviewResult.DomainsAvailable | Should -Not -BeNullOrEmpty
            $script:PreviewResult.DomainsAvailable | Should -Contain 'umbraco'
            $script:PreviewResult.DomainsAvailable | Should -Contain 'breezsdk'
        }

        It 'populates manifest with included file entries for preview reporting' {
            $included = @($script:Manifest | Where-Object { $_.Included -eq $true })
            $included.Count | Should -BeGreaterThan 0
        }

        It 'manifest includes tier classification for each entry' {
            $script:Manifest | ForEach-Object {
                $_.Tier | Should -Not -BeNullOrEmpty -Because "Every manifest entry should have a Tier classification"
            }
        }

        It 'manifest includes reason for each entry' {
            $script:Manifest | ForEach-Object {
                $_.Reason | Should -Not -BeNullOrEmpty -Because "Every manifest entry should have an inclusion/exclusion reason"
            }
        }

        It 'preview mode with existing destination does not modify existing files' {
            $existingDest = Join-Path $TestDrive 'us2-existing-dest'
            New-Item -ItemType Directory -Path $existingDest -Force | Out-Null
            $markerFile = Join-Path $existingDest 'existing-file.txt'
            Set-Content -Path $markerFile -Value 'original-content'

            $manifest = Build-FileManifest -SourcePath $script:SourceRoot
            Invoke-SpecKitCopy -Manifest $manifest -SourcePath $script:SourceRoot -DestinationPath $existingDest -Preview

            $content = Get-Content -Path $markerFile -Raw
            $content.Trim() | Should -Be 'original-content'
        }

        It 'preview mode returns validation errors when source is invalid' {
            $badSource = Join-Path $TestDrive 'nonexistent-preview-source'
            $result = Invoke-SpecKitCopy -SourcePath $badSource -DestinationPath (Join-Path $TestDrive 'preview-bad') -Preview

            $result.ExitCode | Should -Be 1
            $result.Success | Should -BeFalse
            $result.IsPreview | Should -BeTrue
        }
    }

    #endregion

    #region Phase 5: User Story 3 - Include Domain Modules

    Context 'US3: Domain inclusion' {
        # T020: -IncludeDomains copies domain agents, unknown domain warns

        BeforeAll {
            $script:SourceRoot = New-MockSpecKitSource
        }

        It 'copies umbraco domain agent files when -IncludeDomains umbraco is specified' {
            $dest = Join-Path $TestDrive 'us3-umbraco-dest'
            $manifest = Build-FileManifest -SourcePath $script:SourceRoot -IncludeDomains 'umbraco'
            $result = Invoke-SpecKitCopy -Manifest $manifest -SourcePath $script:SourceRoot -DestinationPath $dest

            # Umbraco agents should be copied
            @('umbraco-architect.md', 'umbraco-backend-developer.md', 'umbraco-frontend-developer.md') | ForEach-Object {
                $destPath = Join-Path $dest ".claude\agents\$_"
                $destPath | Should -Exist -Because "Umbraco agent $_ should be copied when domain is included"
            }
        }

        It 'does not copy breezsdk files when only umbraco is included' {
            $dest = Join-Path $TestDrive 'us3-umbraco-only'
            $manifest = Build-FileManifest -SourcePath $script:SourceRoot -IncludeDomains 'umbraco'
            Invoke-SpecKitCopy -Manifest $manifest -SourcePath $script:SourceRoot -DestinationPath $dest

            @('breezsdk-architect.md', 'breezsdk-developer.md') | ForEach-Object {
                $destPath = Join-Path $dest ".claude\agents\$_"
                $destPath | Should -Not -Exist -Because "Breezsdk agent $_ should be excluded when only umbraco is included"
            }
            $destPath = Join-Path $dest '.claude\skills\breezsdk-knowledge.md'
            $destPath | Should -Not -Exist -Because 'Breezsdk skill should be excluded'
        }

        It 'copies all domain files when multiple domains are included' {
            $dest = Join-Path $TestDrive 'us3-multi-domain'
            $manifest = Build-FileManifest -SourcePath $script:SourceRoot -IncludeDomains @('umbraco', 'breezsdk')
            $result = Invoke-SpecKitCopy -Manifest $manifest -SourcePath $script:SourceRoot -DestinationPath $dest

            # All umbraco agents
            @('umbraco-architect.md', 'umbraco-backend-developer.md', 'umbraco-frontend-developer.md') | ForEach-Object {
                (Join-Path $dest ".claude\agents\$_") | Should -Exist
            }
            # All breezsdk agents
            @('breezsdk-architect.md', 'breezsdk-developer.md') | ForEach-Object {
                (Join-Path $dest ".claude\agents\$_") | Should -Exist
            }
            # Breezsdk skill
            (Join-Path $dest '.claude\skills\breezsdk-knowledge.md') | Should -Exist
        }

        It 'still copies all core framework files when domains are included' {
            $dest = Join-Path $TestDrive 'us3-core-with-domains'
            $manifest = Build-FileManifest -SourcePath $script:SourceRoot -IncludeDomains 'umbraco'
            Invoke-SpecKitCopy -Manifest $manifest -SourcePath $script:SourceRoot -DestinationPath $dest

            # Core agents should still be present
            @('backend-developer.md', 'code-reviewer.md', 'solution-architect.md') | ForEach-Object {
                (Join-Path $dest ".claude\agents\$_") | Should -Exist -Because "Core agent $_ should always be copied"
            }
            # Framework file should still be present
            (Join-Path $dest 'CLAUDE.md') | Should -Exist
        }

        It 'Build-FileManifest marks domain entries as Included=true for specified domains' {
            $manifest = Build-FileManifest -SourcePath $script:SourceRoot -IncludeDomains 'umbraco'
            $umbracoEntries = $manifest | Where-Object { $_.Domain -eq 'umbraco' }
            $umbracoEntries | ForEach-Object { $_.Included | Should -BeTrue }
        }

        It 'Build-FileManifest marks unspecified domains as Included=false' {
            $manifest = Build-FileManifest -SourcePath $script:SourceRoot -IncludeDomains 'umbraco'
            $breezEntries = $manifest | Where-Object { $_.Domain -eq 'breezsdk' }
            $breezEntries | ForEach-Object { $_.Included | Should -BeFalse }
        }

        It 'CopyResult.DomainsIncluded lists domains that were actually included' {
            $dest = Join-Path $TestDrive 'us3-domains-included'
            $manifest = Build-FileManifest -SourcePath $script:SourceRoot -IncludeDomains 'umbraco'
            $result = Invoke-SpecKitCopy -Manifest $manifest -SourcePath $script:SourceRoot -DestinationPath $dest

            $result.DomainsIncluded | Should -Contain 'umbraco'
            $result.DomainsIncluded | Should -Not -Contain 'breezsdk'
        }

        It 'CopyResult.DomainsAvailable lists all discovered domains regardless of inclusion' {
            $dest = Join-Path $TestDrive 'us3-domains-available'
            $manifest = Build-FileManifest -SourcePath $script:SourceRoot -IncludeDomains 'umbraco'
            $result = Invoke-SpecKitCopy -Manifest $manifest -SourcePath $script:SourceRoot -DestinationPath $dest

            $result.DomainsAvailable | Should -Contain 'umbraco'
            $result.DomainsAvailable | Should -Contain 'breezsdk'
        }

        It 'produces a warning for unknown domain names and continues' {
            $dest = Join-Path $TestDrive 'us3-unknown-domain'
            $manifest = Build-FileManifest -SourcePath $script:SourceRoot -IncludeDomains @('umbraco', 'nonexistent')

            # Unknown domain should not appear in any manifest entry
            $nonexistentEntries = $manifest | Where-Object { $_.Domain -eq 'nonexistent' }
            $nonexistentEntries | Should -BeNullOrEmpty

            # Copy should still succeed with the valid domain
            $result = Invoke-SpecKitCopy -Manifest $manifest -SourcePath $script:SourceRoot -DestinationPath $dest `
                -RequestedDomains @('umbraco', 'nonexistent')

            $result.Success | Should -BeTrue
            $result.Warnings | Should -Not -BeNullOrEmpty
            $result.Warnings | Where-Object { $_ -match 'nonexistent' } | Should -Not -BeNullOrEmpty
        }

        It 'includes domain skill files when domain is specified via -IncludeDomains' {
            $manifest = Build-FileManifest -SourcePath $script:SourceRoot -IncludeDomains 'breezsdk'
            $breezSkills = $manifest | Where-Object { $_.Tier -eq 'DomainSkill' -and $_.Domain -eq 'breezsdk' }
            $breezSkills | ForEach-Object { $_.Included | Should -BeTrue }
        }
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
