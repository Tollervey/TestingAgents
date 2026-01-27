#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Copies Spec-Kit framework files to a new or existing project.

.DESCRIPTION
    Scaffolds a new project with the Spec-Kit framework by copying Tier 1 (framework)
    and Tier 2 (core agents/skills) files from the source repository to a destination
    directory. Preserves directory structure, creates placeholder directories, and
    optionally includes domain-specific agent/skill modules.

    Domain modules (e.g., umbraco, breezsdk) are auto-discovered from filename prefixes
    in .claude/agents/ and .claude/skills/. By default they are excluded; use
    -IncludeDomains to selectively include them.

.PARAMETER Destination
    Target project directory. Created if it does not exist. Must differ from source.

.PARAMETER Source
    Source repository root containing the Spec-Kit framework.
    Defaults to the repository root relative to this script.

.PARAMETER Preview
    Dry-run mode. Lists all planned operations grouped by tier without making
    any filesystem changes.

.PARAMETER Force
    Overwrite existing files at the destination. Without this flag, existing files
    are skipped and a warning is reported. Files at the destination not in the
    manifest are never deleted.

.PARAMETER IncludeDomains
    Domain modules to include alongside the core framework (e.g., "umbraco", "breezsdk").
    Unknown domains produce a warning and are skipped.

.PARAMETER PassThru
    Returns a PSCustomObject (CopyResult) for pipeline consumption instead of
    only writing to the console.

.EXAMPLE
    .\Copy-SpecKit.ps1 -Destination C:\Projects\MyNewProject
    Copies all framework and core agent files to the destination.

.EXAMPLE
    .\Copy-SpecKit.ps1 -Destination C:\Projects\MyNewProject -Preview
    Shows what would be copied without making changes.

.EXAMPLE
    .\Copy-SpecKit.ps1 -Destination C:\Projects\MyProject -IncludeDomains umbraco
    Copies framework files plus Umbraco domain agents and skills.

.EXAMPLE
    .\Copy-SpecKit.ps1 -Destination C:\Projects\Existing -Force
    Overwrites any existing framework files at the destination.

.EXAMPLE
    $result = .\Copy-SpecKit.ps1 -Destination C:\Projects\New -PassThru
    $result.FilesCopied
    Returns a CopyResult object for pipeline use.
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Position = 0)]
    [string]$Destination,

    [Parameter()]
    [string]$Source,

    [Parameter()]
    [switch]$Preview,

    [Parameter()]
    [switch]$Force,

    [Parameter()]
    [string[]]$IncludeDomains,

    [Parameter()]
    [switch]$PassThru
)

$ErrorActionPreference = 'Stop'

# Resolve default source to repo root (script is at .specify/scripts/powershell/)
if (-not $Source) {
    $Source = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
}

# --- Core agent/skill names (used for domain detection) ---
$script:CoreAgentNames = @(
    'backend-developer'
    'code-reviewer'
    'database-architect'
    'frontend-developer'
    'security-auditor'
    'solution-architect'
    'test-engineer'
)

$script:CoreSkillNames = @(
    'external-plugins'
    'implementation-execution'
)

# --- Placeholder directories to create empty at destination ---
$script:PlaceholderDirectories = @(
    '.specify/memory'
    '.specify/metrics'
    '.specify/plans'
    'specs'
)

#region Internal Functions

function Test-SpecKitSource {
    <#
    .SYNOPSIS
        Validates that the source path contains a valid Spec-Kit installation.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$SourcePath,

        [Parameter(Mandatory)]
        [string]$DestinationPath
    )

    Write-Verbose "Validating source path: $SourcePath"

    $errors = @()

    if (-not (Test-Path $SourcePath)) {
        Write-Verbose "Validation result: Invalid - Source path does not exist"
        return [PSCustomObject]@{
            Valid   = $false
            Errors  = @("Source path does not exist: $SourcePath")
        }
    }

    # Check required structure
    $requiredItems = @(
        @{ Path = '.claude';   Type = 'Directory'; Label = '.claude/ directory' }
        @{ Path = '.specify';  Type = 'Directory'; Label = '.specify/ directory' }
        @{ Path = 'CLAUDE.md'; Type = 'File';      Label = 'CLAUDE.md file' }
    )

    $missing = @()
    foreach ($item in $requiredItems) {
        $fullPath = Join-Path $SourcePath $item.Path
        $exists = if ($item.Type -eq 'Directory') {
            Test-Path $fullPath -PathType Container
        } else {
            Test-Path $fullPath -PathType Leaf
        }
        Write-Verbose "Checking required item: $($item.Label) - $(if ($exists) { 'Found' } else { 'Missing' })"
        if (-not $exists) {
            $missing += $item.Label
        }
    }

    if ($missing.Count -gt 0) {
        $errors += "Source path does not contain required Spec-Kit structure. Missing: $($missing -join ', ')"
    }

    # Same source/dest check
    $resolvedSource = [System.IO.Path]::GetFullPath($SourcePath)
    $resolvedDest   = [System.IO.Path]::GetFullPath($DestinationPath)
    if ($resolvedSource -eq $resolvedDest) {
        $errors += "Source and destination paths are the same: $resolvedSource"
    }

    $isValid = ($errors.Count -eq 0)
    Write-Verbose "Validation result: $(if ($isValid) { 'Valid' } else { 'Invalid' })"

    return [PSCustomObject]@{
        Valid  = $isValid
        Errors = $errors
    }
}

function Build-FileManifest {
    <#
    .SYNOPSIS
        Builds the file manifest of all files eligible for copying, classified by tier.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$SourcePath,

        [Parameter()]
        [string[]]$IncludeDomains
    )

    Write-Verbose "Building file manifest from: $SourcePath"

    $manifest = @()

    # Tier 1: Framework files (always included)
    $tier1Patterns = @(
        @{ Glob = '.claude/commands/*.md';            Description = 'Spec-Kit commands' }
        @{ Glob = '.claude/settings.json';            Description = 'Claude Code configuration' }
        @{ Glob = '.claude/hooks.json';               Description = 'Automation hooks' }
        @{ Glob = '.claude/QUICK-REFERENCE.md';       Description = 'Quick reference guide' }
        @{ Glob = '.specify/templates/*.md';           Description = 'Document templates' }
        @{ Glob = '.specify/scripts/powershell/*.ps1'; Description = 'Automation scripts' }
        @{ Glob = 'CLAUDE.md';                         Description = 'Core governance document' }
    )

    foreach ($pattern in $tier1Patterns) {
        $fullPattern = Join-Path $SourcePath $pattern.Glob
        $files = @(Get-ChildItem -Path $fullPattern -File -ErrorAction SilentlyContinue)
        Write-Verbose "Scanning tier 1 pattern '$($pattern.Glob)': Found $($files.Count) files"
        foreach ($file in $files) {
            $relativePath = $file.FullName.Substring($SourcePath.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
            $manifest += [PSCustomObject]@{
                RelativePath = $relativePath
                Tier         = 'Framework'
                Domain       = $null
                Included     = $true
                Reason       = 'Tier 1: Framework file (always copied)'
            }
        }
    }

    # Tier 2: Core agents (always included) and domain agents (conditional)
    $agentFiles = @(Get-ChildItem -Path (Join-Path $SourcePath '.claude/agents/*.md') -File -ErrorAction SilentlyContinue)
    Write-Verbose "Processing agents: Found $($agentFiles.Count) agent files"
    foreach ($agentFile in $agentFiles) {
        $baseName = [System.IO.Path]::GetFileNameWithoutExtension($agentFile.Name)
        $relativePath = $agentFile.FullName.Substring($SourcePath.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)

        if ($baseName -in $script:CoreAgentNames) {
            $manifest += [PSCustomObject]@{
                RelativePath = $relativePath
                Tier         = 'CoreAgent'
                Domain       = $null
                Included     = $true
                Reason       = 'Tier 2: Core agent (always copied)'
            }
        } else {
            # Domain agent - extract prefix
            $domainName = ($baseName -split '-', 2)[0]
            $included = $IncludeDomains -and ($domainName -in $IncludeDomains)
            $reason = if ($included) {
                "Tier 3: Domain agent included via -IncludeDomains"
            } else {
                "Tier 3: Domain agent excluded (use -IncludeDomains $domainName to include)"
            }
            $manifest += [PSCustomObject]@{
                RelativePath = $relativePath
                Tier         = 'DomainAgent'
                Domain       = $domainName
                Included     = $included
                Reason       = $reason
            }
        }
    }

    # Tier 2: Core skills (always included) and domain skills (conditional)
    $skillFiles = @(Get-ChildItem -Path (Join-Path $SourcePath '.claude/skills/*.md') -File -ErrorAction SilentlyContinue)
    Write-Verbose "Processing skills: Found $($skillFiles.Count) skill files"
    foreach ($skillFile in $skillFiles) {
        $baseName = [System.IO.Path]::GetFileNameWithoutExtension($skillFile.Name)
        $relativePath = $skillFile.FullName.Substring($SourcePath.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)

        if ($baseName -in $script:CoreSkillNames) {
            $manifest += [PSCustomObject]@{
                RelativePath = $relativePath
                Tier         = 'CoreSkill'
                Domain       = $null
                Included     = $true
                Reason       = 'Tier 2: Core skill (always copied)'
            }
        } else {
            $domainName = ($baseName -split '-', 2)[0]
            $included = $IncludeDomains -and ($domainName -in $IncludeDomains)
            $reason = if ($included) {
                "Tier 3: Domain skill included via -IncludeDomains"
            } else {
                "Tier 3: Domain skill excluded (use -IncludeDomains $domainName to include)"
            }
            $manifest += [PSCustomObject]@{
                RelativePath = $relativePath
                Tier         = 'DomainSkill'
                Domain       = $domainName
                Included     = $included
                Reason       = $reason
            }
        }
    }

    Write-Verbose "Total manifest entries: $($manifest.Count)"
    return $manifest
}

function Get-DomainModules {
    <#
    .SYNOPSIS
        Discovers domain modules by scanning agent and skill file prefixes.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$SourcePath
    )

    Write-Verbose "Discovering domain modules in: $SourcePath"

    $domains = @{}

    # Scan agents
    $agentFiles = @(Get-ChildItem -Path (Join-Path $SourcePath '.claude/agents/*.md') -File -ErrorAction SilentlyContinue)
    foreach ($file in $agentFiles) {
        $baseName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        if ($baseName -notin $script:CoreAgentNames) {
            $domainName = ($baseName -split '-', 2)[0]
            $relativePath = $file.FullName.Substring($SourcePath.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
            if (-not $domains.ContainsKey($domainName)) {
                Write-Verbose "Found domain module: $domainName"
                $domains[$domainName] = @{ AgentFiles = @(); SkillFiles = @() }
            }
            $domains[$domainName].AgentFiles += $relativePath
        }
    }

    # Scan skills
    $skillFiles = @(Get-ChildItem -Path (Join-Path $SourcePath '.claude/skills/*.md') -File -ErrorAction SilentlyContinue)
    foreach ($file in $skillFiles) {
        $baseName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        if ($baseName -notin $script:CoreSkillNames) {
            $domainName = ($baseName -split '-', 2)[0]
            $relativePath = $file.FullName.Substring($SourcePath.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
            if (-not $domains.ContainsKey($domainName)) {
                Write-Verbose "Found domain module: $domainName"
                $domains[$domainName] = @{ AgentFiles = @(); SkillFiles = @() }
            }
            $domains[$domainName].SkillFiles += $relativePath
        }
    }

    # Convert to DomainModule objects
    $result = @()
    foreach ($name in ($domains.Keys | Sort-Object)) {
        $d = $domains[$name]
        $result += [PSCustomObject]@{
            Name       = $name
            AgentFiles = $d.AgentFiles
            SkillFiles = $d.SkillFiles
            FileCount  = $d.AgentFiles.Count + $d.SkillFiles.Count
        }
    }

    Write-Verbose "Total domains found: $($result.Count)"
    return $result
}

function Write-Header {
    <#
    .SYNOPSIS
        Writes a formatted header block to the console.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Title
    )

    $separator = '=' * 39
    Write-Host ''
    Write-Host $separator -ForegroundColor Cyan
    Write-Host "  $Title" -ForegroundColor Cyan
    Write-Host $separator -ForegroundColor Cyan
}

function Write-Section {
    <#
    .SYNOPSIS
        Writes a section header to the console.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Title
    )

    Write-Host ''
    Write-Host "--- $Title ---" -ForegroundColor White
}

function Write-FileEntry {
    <#
    .SYNOPSIS
        Writes a file operation entry to the console with status symbol.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter()]
        [ValidateSet('Copied', 'Skipped', 'Overwritten', 'Preview', 'CreateDir')]
        [string]$Action = 'Copied',

        [Parameter()]
        [string]$Note
    )

    $checkmark = [char]0x2713
    $warning   = [char]0x26A0
    $arrow     = [char]0x2192

    # Normalize display path to forward slashes
    $displayPath = $Path -replace '\\', '/'

    switch ($Action) {
        'Copied' {
            Write-Host "  $checkmark $displayPath" -ForegroundColor Green
        }
        'Overwritten' {
            Write-Host "  $checkmark $displayPath (overwritten)" -ForegroundColor Yellow
        }
        'Skipped' {
            $msg = "  $warning $displayPath"
            if ($Note) { $msg += " ($Note)" }
            Write-Host $msg -ForegroundColor Yellow
        }
        'Preview' {
            Write-Host "  $arrow $displayPath" -ForegroundColor Cyan
        }
        'CreateDir' {
            Write-Host "  $checkmark $displayPath/" -ForegroundColor Green
        }
    }
}

function Invoke-SpecKitCopy {
    <#
    .SYNOPSIS
        Copies Spec-Kit framework files from source to destination based on a file manifest.
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [object[]]$Manifest,

        [Parameter(Mandatory)]
        [string]$SourcePath,

        [Parameter(Mandatory)]
        [string]$DestinationPath,

        [Parameter()]
        [switch]$Preview,

        [Parameter()]
        [switch]$Force,

        [Parameter()]
        [string[]]$RequestedDomains
    )

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    # Initialize result
    $result = [PSCustomObject]@{
        Source               = $SourcePath
        Destination          = $DestinationPath
        FilesCopied          = 0
        FilesSkipped         = 0
        FilesOverwritten     = 0
        DirectoriesCreated   = 0
        PlaceholderDirectories = 0
        Warnings             = @()
        Errors               = @()
        DomainsIncluded      = @()
        DomainsAvailable     = @()
        Duration             = [TimeSpan]::Zero
        ExitCode             = 0
        IsPreview            = [bool]$Preview
        Success              = $true
    }

    # Validate source
    $validation = Test-SpecKitSource -SourcePath $SourcePath -DestinationPath $DestinationPath
    if (-not $validation.Valid) {
        $result.ExitCode = 1
        $result.Success = $false
        $result.Errors = $validation.Errors
        $stopwatch.Stop()
        $result.Duration = $stopwatch.Elapsed
        return $result
    }

    # Build manifest if not provided
    if (-not $Manifest) {
        $Manifest = Build-FileManifest -SourcePath $SourcePath
    }

    # Populate domain info
    $domainModules = Get-DomainModules -SourcePath $SourcePath
    $result.DomainsAvailable = @($domainModules | ForEach-Object { $_.Name })
    $result.DomainsIncluded = @($Manifest | Where-Object { $_.Included -and $_.Domain } | ForEach-Object { $_.Domain } | Select-Object -Unique)

    # Check for unknown requested domains (FR-009, FR-010)
    if ($RequestedDomains) {
        foreach ($requested in $RequestedDomains) {
            if ($requested -notin $result.DomainsAvailable) {
                $availableList = $result.DomainsAvailable -join ', '
                $result.Warnings += "Domain module '$requested' not found in source. Available: $availableList"
                Write-Verbose "Unknown domain requested: $requested"
            }
        }
    }

    # Copy included files (skip in preview mode)
    $includedFiles = @($Manifest | Where-Object { $_.Included -eq $true })

    if (-not $Preview) {
        $createdDirs = @{}

        foreach ($entry in $includedFiles) {
            $srcFile = Join-Path $SourcePath $entry.RelativePath
            $destFile = Join-Path $DestinationPath $entry.RelativePath
            $destDir = Split-Path $destFile -Parent

            # Create directory if needed
            if (-not $createdDirs.ContainsKey($destDir) -and -not (Test-Path $destDir)) {
                try {
                    New-Item -ItemType Directory -Path $destDir -Force -ErrorAction Stop | Out-Null
                    $createdDirs[$destDir] = $true
                    $result.DirectoriesCreated++
                    Write-Verbose "Created directory: $destDir"
                } catch {
                    $result.Errors += "Failed to create directory: $destDir - $($_.Exception.Message)"
                    $result.Success = $false
                    continue
                }
            }

            # Check for conflict (existing file at destination)
            $conflict = Test-Path $destFile
            if ($conflict -and -not $Force) {
                # Skip existing file without -Force
                $result.FilesSkipped++
                Write-Verbose "Skipped (exists): $($entry.RelativePath)"
            } else {
                # Copy file (new or overwrite with -Force)
                try {
                    Copy-Item -Path $srcFile -Destination $destFile -Force -ErrorAction Stop
                    if ($conflict) {
                        $result.FilesOverwritten++
                        Write-Verbose "Overwritten: $($entry.RelativePath)"
                    } else {
                        $result.FilesCopied++
                        Write-Verbose "Copied: $($entry.RelativePath)"
                    }
                } catch {
                    $result.Errors += "Failed to copy $($entry.RelativePath): $($_.Exception.Message)"
                    $result.Success = $false
                }
            }
        }

        # Create placeholder directories
        foreach ($placeholder in $script:PlaceholderDirectories) {
            $placeholderPath = Join-Path $DestinationPath $placeholder
            if (-not (Test-Path $placeholderPath)) {
                try {
                    New-Item -ItemType Directory -Path $placeholderPath -Force -ErrorAction Stop | Out-Null
                    $result.PlaceholderDirectories++
                    Write-Verbose "Created placeholder directory: $placeholder"
                } catch {
                    $result.Errors += "Failed to create placeholder directory: $placeholder - $($_.Exception.Message)"
                    $result.Success = $false
                }
            } else {
                $result.PlaceholderDirectories++
            }
        }
        # Set exit code 2 for partial success (files skipped due to conflicts)
        if ($result.FilesSkipped -gt 0) {
            $result.ExitCode = 2
        }
    } else {
        Write-Verbose "Preview mode: skipping all filesystem operations"
    }

    $stopwatch.Stop()
    $result.Duration = $stopwatch.Elapsed
    return $result
}

#endregion Internal Functions

function Write-CopySummary {
    <#
    .SYNOPSIS
        Writes the copy operation summary to the console with rich formatting.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$CopyResult,

        [Parameter()]
        [object[]]$Manifest
    )

    # Summary header
    Write-Header -Title 'Summary'

    Write-Host "  Files copied:        $($CopyResult.FilesCopied)" -ForegroundColor White
    if ($CopyResult.FilesSkipped -gt 0) {
        Write-Host "  Files skipped:       $($CopyResult.FilesSkipped)" -ForegroundColor Yellow
    }
    if ($CopyResult.FilesOverwritten -gt 0) {
        Write-Host "  Files overwritten:   $($CopyResult.FilesOverwritten)" -ForegroundColor Yellow
    }
    Write-Host "  Directories created: $($CopyResult.DirectoriesCreated)" -ForegroundColor White
    Write-Host "  Placeholders created:$($CopyResult.PlaceholderDirectories)" -ForegroundColor White
    if ($CopyResult.Warnings.Count -gt 0) {
        Write-Host "  Warnings:            $($CopyResult.Warnings.Count)" -ForegroundColor Yellow
    }
}

function Write-NextSteps {
    <#
    .SYNOPSIS
        Writes the post-copy next steps checklist.
    #>
    [CmdletBinding()]
    param()

    $separator = '-' * 39
    Write-Host ''
    Write-Host $separator -ForegroundColor White
    Write-Host '  Next Steps' -ForegroundColor White
    Write-Host $separator -ForegroundColor White
    Write-Host '  1. Create CLAUDE.project.md with your project''s technology conventions' -ForegroundColor White
    Write-Host '  2. Run /speckit.constitution to establish project governance' -ForegroundColor White
    Write-Host '  3. Review .claude/settings.json and adjust for your project' -ForegroundColor White
    Write-Host '  4. Create .claude/settings.local.json for local environment overrides' -ForegroundColor White
    Write-Host '  5. Review .claude/hooks.json and adjust build/test commands' -ForegroundColor White
    Write-Host '  6. Start with /speckit.specify to define your first feature' -ForegroundColor White
    Write-Host ''
}

#endregion Internal Functions

# --- Main entry point ---
# Guard: only run main logic when executed directly, not when dot-sourced
if ($MyInvocation.InvocationName -ne '.' -and $MyInvocation.InvocationName -ne 'Import-Module') {
    # Validate Destination parameter
    if (-not $Destination) {
        Write-Host 'ERROR: -Destination parameter is required.' -ForegroundColor Red
        exit 1
    }

    Write-Verbose "Parameter resolution: Source='$Source', Destination='$Destination', Preview=$Preview, Force=$Force"
    if ($IncludeDomains) {
        Write-Verbose "Domains to include: $($IncludeDomains -join ', ')"
    }

    # Determine mode label
    $modeLabel = if ($Preview) { 'Preview' } elseif ($Force) { 'Force Copy' } else { 'Copy' }

    # Display header
    Write-Header -Title 'Spec-Kit Framework Scaffolding'
    Write-Host "Source:      $Source" -ForegroundColor White
    Write-Host "Destination: $Destination" -ForegroundColor White
    Write-Host "Mode:        $modeLabel" -ForegroundColor White

    # Discover and display available domains
    Write-Verbose "Discovering domain modules..."
    $domainModules = Get-DomainModules -SourcePath $Source
    if ($domainModules.Count -gt 0) {
        $checkmark = [char]0x2713
        $arrow = [char]0x2192
        Write-Host ''
        Write-Host 'Available domain modules:' -ForegroundColor White
        foreach ($dm in $domainModules) {
            if ($IncludeDomains -and ($dm.Name -in $IncludeDomains)) {
                Write-Host "  $checkmark $($dm.Name) ($($dm.FileCount) files) [included]" -ForegroundColor Green
            } else {
                Write-Host "  $arrow $($dm.Name) ($($dm.FileCount) files)" -ForegroundColor Cyan
            }
        }
    }

    # Build manifest
    Write-Verbose "Building file manifest..."
    $manifest = Build-FileManifest -SourcePath $Source -IncludeDomains $IncludeDomains

    # Check if we should proceed with the operation (handles -WhatIf and -Confirm)
    # If ShouldProcess returns false, automatically enable Preview mode
    $effectivePreview = $Preview
    if (-not $Preview -and -not $PSCmdlet.ShouldProcess($Destination, 'Copy Spec-Kit framework files')) {
        Write-Verbose "ShouldProcess returned false; enabling Preview mode"
        $effectivePreview = $true
    }

    # Execute copy
    Write-Verbose "Starting copy operation (Preview=$effectivePreview)..."
    $copyResult = Invoke-SpecKitCopy -Manifest $manifest -SourcePath $Source -DestinationPath $Destination -Preview:$effectivePreview -Force:$Force -RequestedDomains $IncludeDomains
    Write-Verbose "Copy operation completed"

    # Display warnings (e.g., unknown domain names)
    if ($copyResult.Warnings.Count -gt 0) {
        $warningSymbol = [char]0x26A0
        foreach ($warn in $copyResult.Warnings) {
            Write-Host "  $warningSymbol WARNING: $warn" -ForegroundColor Yellow
        }
    }

    if (-not $copyResult.Success) {
        foreach ($err in $copyResult.Errors) {
            Write-Host "ERROR: $err" -ForegroundColor Red
        }
        exit $copyResult.ExitCode
    }

    if ($Preview) {
        # Preview mode: show planned operations grouped by tier
        $tiers = @(
            @{ Name = 'Tier 1: Framework (Copy)'; Filter = { $_.Tier -eq 'Framework' } }
            @{ Name = 'Tier 2: Core Agents (Copy)'; Filter = { $_.Tier -eq 'CoreAgent' } }
            @{ Name = 'Tier 2: Core Skills (Copy)'; Filter = { $_.Tier -eq 'CoreSkill' } }
        )

        foreach ($tier in $tiers) {
            $tierFiles = @($manifest | Where-Object $tier.Filter | Where-Object { $_.Included })
            if ($tierFiles.Count -gt 0) {
                Write-Section -Title $tier.Name
                foreach ($entry in $tierFiles) {
                    $srcDisplay = (Join-Path $Source $entry.RelativePath) -replace '\\', '/'
                    $destDisplay = (Join-Path $Destination $entry.RelativePath) -replace '\\', '/'
                    Write-FileEntry -Path $entry.RelativePath -Action 'Preview' -Note "$srcDisplay -> $destDisplay"
                }
            }
        }

        # Show included domain files
        if ($IncludeDomains) {
            foreach ($domain in $IncludeDomains) {
                $domainFiles = @($manifest | Where-Object { $_.Domain -eq $domain -and $_.Included })
                if ($domainFiles.Count -gt 0) {
                    Write-Section -Title "Domain: $domain (Copy)"
                    foreach ($entry in $domainFiles) {
                        Write-FileEntry -Path $entry.RelativePath -Action 'Preview'
                    }
                }
            }
        }

        # Show excluded domain files
        $excludedDomainFiles = @($manifest | Where-Object { -not $_.Included })
        if ($excludedDomainFiles.Count -gt 0) {
            Write-Section -Title 'Excluded (Domain - not selected)'
            foreach ($entry in $excludedDomainFiles) {
                Write-FileEntry -Path $entry.RelativePath -Action 'Skipped' -Note $entry.Reason
            }
        }

        # Show placeholder directories that would be created
        Write-Section -Title 'Placeholder Directories (CreateDir)'
        foreach ($placeholder in @('.specify/memory', '.specify/metrics', '.specify/plans', 'specs')) {
            Write-FileEntry -Path $placeholder -Action 'CreateDir'
        }

        # Summary counts for preview
        $includedCount = @($manifest | Where-Object { $_.Included }).Count
        $excludedCount = @($manifest | Where-Object { -not $_.Included }).Count
        Write-Header -Title 'Preview Summary'
        Write-Host "  Files to copy:     $includedCount" -ForegroundColor White
        Write-Host "  Files excluded:    $excludedCount" -ForegroundColor White
        Write-Host "  Directories to create: (as needed)" -ForegroundColor White
        Write-Host "  Placeholders:      4" -ForegroundColor White
        Write-Host ''
        Write-Host '  No changes were made (preview mode).' -ForegroundColor Cyan
    } else {
        # Actual copy mode: display file operations grouped by tier
        $tiers = @(
            @{ Name = 'Tier 1: Framework'; Filter = { $_.Tier -eq 'Framework' } }
            @{ Name = 'Tier 2: Core Agents'; Filter = { $_.Tier -eq 'CoreAgent' } }
            @{ Name = 'Tier 2: Core Skills'; Filter = { $_.Tier -eq 'CoreSkill' } }
        )

        foreach ($tier in $tiers) {
            $tierFiles = @($manifest | Where-Object $tier.Filter | Where-Object { $_.Included })
            if ($tierFiles.Count -gt 0) {
                Write-Section -Title $tier.Name
                foreach ($entry in $tierFiles) {
                    Write-FileEntry -Path $entry.RelativePath -Action 'Copied'
                }
            }
        }

        # Display domain files if included
        if ($IncludeDomains) {
            foreach ($domain in $IncludeDomains) {
                $domainFiles = @($manifest | Where-Object { $_.Domain -eq $domain -and $_.Included })
                if ($domainFiles.Count -gt 0) {
                    Write-Section -Title "Domain: $domain"
                    foreach ($entry in $domainFiles) {
                        Write-FileEntry -Path $entry.RelativePath -Action 'Copied'
                    }
                }
            }
        }

        # Display skipped files warning if any were skipped
        if ($copyResult.FilesSkipped -gt 0) {
            $warningSymbol = [char]0x26A0
            Write-Host ''
            Write-Host "  $warningSymbol WARNING: $($copyResult.FilesSkipped) files skipped (already exist). Use -Force to overwrite." -ForegroundColor Yellow
        }

        # Summary and next steps
        Write-CopySummary -CopyResult $copyResult -Manifest $manifest
        Write-NextSteps
    }

    # Return PassThru object if requested
    if ($PassThru) {
        $copyResult
    }

    exit $copyResult.ExitCode
}
