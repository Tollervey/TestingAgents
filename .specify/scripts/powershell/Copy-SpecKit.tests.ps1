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
    }

    Context 'US1: Placeholder directory creation' {
        # T011: .specify/memory/, .specify/metrics/, .specify/plans/, specs/ created empty
    }

    Context 'US1: Error handling' {
        # T012: Source not found, missing structure exit codes
    }

    #endregion

    #region Phase 4: User Story 2 - Preview / Dry Run

    Context 'US2: Preview mode' {
        # T017: No filesystem changes, output lists files by tier
    }

    #endregion

    #region Phase 5: User Story 3 - Include Domain Modules

    Context 'US3: Domain inclusion' {
        # T020: -IncludeDomains copies domain agents, unknown domain warns
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
