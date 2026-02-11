<#
.SYNOPSIS
    PPDS devcontainer management script.
.EXAMPLE
    .\scripts\devcontainer.ps1 up                       # build + start + ready to go
    .\scripts\devcontainer.ps1 shell                    # bash shell (prompts for worktree)
    .\scripts\devcontainer.ps1 shell query-engine-v3    # bash shell in worktree
    .\scripts\devcontainer.ps1 claude                   # claude code (prompts for worktree)
    .\scripts\devcontainer.ps1 claude query-engine-v3   # claude code in worktree
    .\scripts\devcontainer.ps1 down                     # stop container
    .\scripts\devcontainer.ps1 reset                    # nuke everything, full clean rebuild
#>

param(
    [Parameter(Position = 0)]
    [ValidateSet('up', 'shell', 'claude', 'down', 'status', 'reset', 'help')]
    [string]$Command = 'help',

    [Parameter(Position = 1)]
    [string]$Target
)

$ErrorActionPreference = 'Stop'
$WorkspaceFolder = Split-Path -Parent $PSScriptRoot
$ImageName = 'ppds-devcontainer'
$VolumeName = 'ppds-claude-plugins'

function Write-Step($msg) { Write-Host "  $msg" -ForegroundColor Cyan }
function Write-Ok($msg)   { Write-Host "  $msg" -ForegroundColor Green }
function Write-Err($msg)  { Write-Host "  $msg" -ForegroundColor Red }

function Get-ContainerId {
    docker ps -aq --filter "label=devcontainer.local_folder=$WorkspaceFolder" 2>$null
}

function Stop-Container {
    $ids = Get-ContainerId
    if ($ids) {
        Write-Step 'Stopping container...'
        $ids | ForEach-Object { docker rm -f $_ } | Out-Null
    }
}

function Ensure-ContainerRunning {
    $id = docker ps -q --filter "label=devcontainer.local_folder=$WorkspaceFolder"
    if (-not $id) {
        Write-Step 'Container not running, starting it...'
        devcontainer up --workspace-folder $WorkspaceFolder | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Err 'Failed to start container.'
            exit 1
        }
    }
}

function Get-Worktrees {
    $wtDir = Join-Path $WorkspaceFolder '.worktrees'
    if (Test-Path $wtDir) {
        Get-ChildItem -Directory $wtDir | Select-Object -ExpandProperty Name
    }
}

function Select-WorkingDirectory {
    param([string]$Target)

    $worktrees = @(Get-Worktrees)

    # No worktrees — use repo root
    if ($worktrees.Count -eq 0) { return $null }

    # Target specified directly — validate and use it
    if ($Target) {
        if ($Target -eq 'main' -or $Target -eq 'root') { return $null }
        if ($worktrees -contains $Target) { return ".worktrees/$Target" }
        Write-Err "Worktree '$Target' not found. Available: $($worktrees -join ', ')"
        exit 1
    }

    # Prompt user to pick
    Write-Host ''
    Write-Host '  Where do you want to work?' -ForegroundColor Yellow
    Write-Host ''
    Write-Host "  [0] main repo ($(git -C $WorkspaceFolder branch --show-current))" -ForegroundColor White
    for ($i = 0; $i -lt $worktrees.Count; $i++) {
        $branch = git -C (Join-Path $WorkspaceFolder ".worktrees/$($worktrees[$i])") branch --show-current 2>$null
        if (-not $branch) { $branch = $worktrees[$i] }
        Write-Host "  [$($i + 1)] $($worktrees[$i]) ($branch)" -ForegroundColor White
    }
    Write-Host ''
    $choice = Read-Host '  Select'

    if ($choice -eq '0' -or $choice -eq '') { return $null }

    $idx = [int]$choice - 1
    if ($idx -ge 0 -and $idx -lt $worktrees.Count) {
        return ".worktrees/$($worktrees[$idx])"
    }

    Write-Err "Invalid selection."
    exit 1
}

switch ($Command) {
    'up' {
        # Build + start in one command. devcontainer CLI handles caching.
        Write-Step 'Building and starting devcontainer...'
        devcontainer up --workspace-folder $WorkspaceFolder
        if ($LASTEXITCODE -eq 0) {
            Write-Ok 'Container is running.'
            Write-Host ''
            Write-Host '  Next steps:' -ForegroundColor Yellow
            Write-Host '    .\scripts\devcontainer.ps1 shell     # bash shell'
            Write-Host '    .\scripts\devcontainer.ps1 claude    # claude code'
            Write-Host ''
        }
        else { Write-Err 'Failed to start container.'; exit 1 }
    }

    'shell' {
        Ensure-ContainerRunning
        $subdir = Select-WorkingDirectory -Target $Target
        if ($subdir) {
            devcontainer exec --workspace-folder $WorkspaceFolder bash -c "cd $subdir && exec bash"
        }
        else {
            devcontainer exec --workspace-folder $WorkspaceFolder bash
        }
    }

    'claude' {
        Ensure-ContainerRunning
        $subdir = Select-WorkingDirectory -Target $Target
        if ($subdir) {
            devcontainer exec --workspace-folder $WorkspaceFolder bash -c "cd $subdir && claude --dangerously-skip-permissions"
        }
        else {
            devcontainer exec --workspace-folder $WorkspaceFolder claude --dangerously-skip-permissions
        }
    }

    'down' {
        Stop-Container
        Write-Ok 'Container stopped.'
    }

    'status' {
        $id = docker ps -q --filter "label=devcontainer.local_folder=$WorkspaceFolder"
        if ($id) {
            Write-Ok 'Container is running.'
            docker ps --filter "label=devcontainer.local_folder=$WorkspaceFolder" --format "table {{.ID}}\t{{.Status}}\t{{.Ports}}"
        }
        else {
            Write-Err 'Container is not running.'
        }
    }

    'reset' {
        Write-Step 'Nuking everything for a clean rebuild...'

        # Stop and remove container
        Stop-Container

        # Remove named volume (plugin cache)
        $vol = docker volume ls -q --filter "name=$VolumeName" 2>$null
        if ($vol) {
            Write-Step "Removing volume $VolumeName..."
            docker volume rm $VolumeName | Out-Null
        }

        # Remove the named image so it rebuilds from scratch
        $img = docker images -q $ImageName 2>$null
        if ($img) {
            Write-Step "Removing image $ImageName..."
            docker rmi $ImageName
            if ($LASTEXITCODE -ne 0) {
                Write-Err "Failed to remove image '$ImageName'. It may be in use. Aborting reset."
                exit 1
            }
        }

        # Full rebuild no cache
        Write-Step 'Rebuilding image (no cache)...'
        docker build --no-cache -t $ImageName "$WorkspaceFolder/.devcontainer/"
        if ($LASTEXITCODE -ne 0) { Write-Err 'Build failed.'; exit 1 }

        # Start fresh
        Write-Step 'Starting fresh container...'
        devcontainer up --workspace-folder $WorkspaceFolder
        if ($LASTEXITCODE -eq 0) {
            Write-Ok 'Clean rebuild complete. Container is running.'
            Write-Host ''
            Write-Host '  Next steps:' -ForegroundColor Yellow
            Write-Host '    .\scripts\devcontainer.ps1 shell     # bash shell'
            Write-Host '    .\scripts\devcontainer.ps1 claude    # claude code'
            Write-Host ''
        }
        else { Write-Err 'Failed to start container.'; exit 1 }
    }

    'help' {
        Write-Host ''
        Write-Host '  PPDS Devcontainer' -ForegroundColor Yellow
        Write-Host ''
        Write-Host '  Usage: .\scripts\devcontainer.ps1 <command> [target]' -ForegroundColor White
        Write-Host ''
        Write-Host '  Commands:' -ForegroundColor White
        Write-Host '    up                    Build + start (one command, handles everything)'
        Write-Host '    shell [worktree]      Open bash shell (prompts for worktree if any exist)'
        Write-Host '    claude [worktree]     Start Claude Code (prompts for worktree if any exist)'
        Write-Host '    down                  Stop the container'
        Write-Host '    status                Check if container is running'
        Write-Host '    reset                 Nuke container + volumes + rebuild from scratch'
        Write-Host ''
        Write-Host '  Examples:' -ForegroundColor White
        Write-Host '    .\scripts\devcontainer.ps1 claude                   # prompts for location'
        Write-Host '    .\scripts\devcontainer.ps1 claude query-engine-v3   # straight to worktree'
        Write-Host '    .\scripts\devcontainer.ps1 shell main               # shell at repo root'
        Write-Host ''
    }
}
