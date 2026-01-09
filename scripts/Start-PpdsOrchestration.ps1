<#
.SYNOPSIS
    Orchestrates parallel Claude Code sessions working on GitHub issues.

.DESCRIPTION
    Creates worktrees, spawns Windows Terminal tabs with Ralph Wiggum loops,
    and monitors session status files. Prompts human for guidance when sessions
    get stuck.

.PARAMETER Issues
    Array of GitHub issue numbers to work on.

.PARAMETER MaxIterations
    Maximum Ralph loop iterations per session (default: 50).

.PARAMETER PromptFile
    Path to Ralph prompt template (default: scripts/ralph-prompt-template.md).

.EXAMPLE
    .\Start-PpdsOrchestration.ps1 -Issues 123, 456, 789

.EXAMPLE
    .\Start-PpdsOrchestration.ps1 -Issues 123 -MaxIterations 20
#>
param(
    [Parameter(Mandatory)]
    [int[]]$Issues,

    [int]$MaxIterations = 50,

    [string]$PromptFile = "$PSScriptRoot\ralph-prompt-template.md"
)

$ErrorActionPreference = "Stop"
$SessionDir = Join-Path $env:USERPROFILE ".ppds/sessions"
$RepoRoot = git rev-parse --show-toplevel

# Ensure session directory exists
New-Item -ItemType Directory -Path $SessionDir -Force | Out-Null

Write-Host "=== PPDS Orchestration ===" -ForegroundColor Cyan
Write-Host "Issues: $($Issues -join ', ')"
Write-Host "Max iterations: $MaxIterations"
Write-Host ""

# Fetch issue details from GitHub
function Get-IssueDetails {
    param([int]$IssueNumber)
    $json = gh issue view $IssueNumber --json title,body,labels
    return $json | ConvertFrom-Json
}

# Create worktrees and spawn terminals
foreach ($issue in $Issues) {
    Write-Host "Setting up issue #$issue..." -ForegroundColor Yellow

    # Get issue details
    $issueDetails = Get-IssueDetails -IssueNumber $issue
    $branchName = "issue-$issue"
    $worktreeName = "ppds-issue-$issue"
    $worktreePath = Join-Path (Split-Path $RepoRoot -Parent) $worktreeName

    # Create worktree if not exists
    if (-not (Test-Path $worktreePath)) {
        Push-Location $RepoRoot
        git worktree add $worktreePath -b $branchName 2>$null
        if ($LASTEXITCODE -ne 0) {
            # Branch might exist, try without -b
            git worktree add $worktreePath $branchName
        }
        Pop-Location
    }

    # Initialize session file
    $startedTime = (Get-Date -Format "o")
    $sessionFile = Join-Path $SessionDir "work-$issue.json"
    @{
        id         = "work-$issue"
        status     = "starting"
        issue      = "#$issue"
        branch     = $branchName
        worktree   = $worktreePath
        started    = $startedTime
        lastUpdate = $startedTime
        stuck      = $null
        guidance   = $null
        prUrl      = $null
    } | ConvertTo-Json | Set-Content $sessionFile

    # Generate prompt from template
    $prompt = Get-Content $PromptFile -Raw
    $prompt = $prompt -replace '\{ISSUE_NUMBER\}', $issue
    $prompt = $prompt -replace '\{ISSUE_TITLE\}', $issueDetails.title
    $prompt = $prompt -replace '\{ISSUE_BODY\}', ($issueDetails.body -replace '[\r\n]+', ' ')
    $prompt = $prompt -replace '\{BRANCH_NAME\}', $branchName
    $prompt = $prompt -replace '\{WORKTREE_PATH\}', $worktreePath
    $prompt = $prompt -replace '\{STARTED_ISO\}', $startedTime
    $prompt = $prompt -replace '\{RELATED_ISSUES\}', ''

    # Write prompt to temp file (avoids command line escaping issues)
    $promptTempFile = Join-Path $env:TEMP "ppds-prompt-$issue.md"
    $prompt | Set-Content $promptTempFile -Encoding UTF8

    # Spawn terminal tab with Ralph loop
    Write-Host "  Spawning terminal for #$issue..." -ForegroundColor Gray
    $claudeCmd = "claude '/ralph-loop --file `"$promptTempFile`" --max-iterations $MaxIterations --completion-promise PR_READY'"
    wt -w 0 nt -d $worktreePath --title "Issue #$issue" powershell -NoExit -Command $claudeCmd

    Start-Sleep -Milliseconds 500  # Let terminal initialize
}

Write-Host ""
Write-Host "All workers spawned. Entering monitor loop..." -ForegroundColor Green
Write-Host "Press Ctrl+C to exit orchestrator (workers continue)" -ForegroundColor Gray
Write-Host ""

# Monitor loop with human prompting
$lastStuckPrompted = @{}  # Track which stuck sessions we've prompted for

while ($true) {
    Clear-Host
    Write-Host "+=============================================================+" -ForegroundColor Cyan
    Write-Host "|           PPDS Orchestration Status                         |" -ForegroundColor Cyan
    Write-Host "|           $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')                            |" -ForegroundColor Cyan
    Write-Host "+=============================================================+" -ForegroundColor Cyan
    Write-Host ""

    # Load all session files
    $sessions = @()
    Get-ChildItem $SessionDir -Filter "work-*.json" -ErrorAction SilentlyContinue | ForEach-Object {
        try {
            $sessions += Get-Content $_.FullName -Raw | ConvertFrom-Json
        } catch {
            Write-Host "Warning: Could not read $($_.Name)" -ForegroundColor Yellow
        }
    }

    if ($sessions.Count -eq 0) {
        Write-Host "No active sessions found." -ForegroundColor Yellow
        Start-Sleep -Seconds 10
        continue
    }

    # Categorize sessions
    $working = @($sessions | Where-Object { $_.status -eq "working" })
    $stuck = @($sessions | Where-Object { $_.status -eq "stuck" })
    $ready = @($sessions | Where-Object { $_.status -eq "pr_ready" })
    $blocked = @($sessions | Where-Object { $_.status -eq "blocked" })
    $starting = @($sessions | Where-Object { $_.status -eq "starting" })

    # Summary line
    Write-Host "  [*] Working: $($working.Count)  |  [!] Stuck: $($stuck.Count)  |  [+] PR Ready: $($ready.Count)  |  [x] Blocked: $($blocked.Count)" -ForegroundColor White
    Write-Host ""

    # Detail for each session
    foreach ($s in $sessions | Sort-Object issue) {
        $icon = switch ($s.status) {
            "working"  { "[*]" }
            "stuck"    { "[!]" }
            "pr_ready" { "[+]" }
            "blocked"  { "[x]" }
            "starting" { "[>]" }
            default    { "[?]" }
        }

        $duration = ""
        if ($s.started) {
            $elapsed = (Get-Date) - [DateTime]$s.started
            $duration = " ({0:hh\:mm} elapsed)" -f $elapsed
        }

        $statusColor = switch ($s.status) {
            "working"  { "White" }
            "stuck"    { "Yellow" }
            "pr_ready" { "Green" }
            "blocked"  { "Red" }
            "starting" { "Cyan" }
            default    { "Gray" }
        }

        Write-Host "  $icon $($s.issue) [$($s.status)]$duration" -ForegroundColor $statusColor

        if ($s.status -eq "stuck" -and $s.stuck) {
            Write-Host "       Reason: $($s.stuck.reason)" -ForegroundColor Yellow
            if ($s.stuck.context) {
                Write-Host "       Context: $($s.stuck.context)" -ForegroundColor Gray
            }
            if ($s.stuck.options) {
                Write-Host "       Options: $($s.stuck.options -join ', ')" -ForegroundColor Gray
            }
        }

        if ($s.status -eq "pr_ready" -and $s.prUrl) {
            Write-Host "       PR: $($s.prUrl)" -ForegroundColor Green
        }
    }

    Write-Host ""

    # Handle stuck sessions - prompt human for guidance
    foreach ($s in $stuck) {
        $issueNum = $s.issue -replace '#', ''
        $stuckSince = if ($s.stuck.since) { [DateTime]$s.stuck.since } else { Get-Date }
        $lastPrompt = $lastStuckPrompted[$issueNum]

        # Only prompt if we haven't prompted for this stuck instance
        if (-not $lastPrompt -or $lastPrompt -lt $stuckSince) {
            Write-Host "===============================================================" -ForegroundColor Red
            Write-Host "  SESSION $($s.issue) NEEDS YOUR INPUT" -ForegroundColor Red
            Write-Host "===============================================================" -ForegroundColor Red
            Write-Host ""
            Write-Host "  Reason: $($s.stuck.reason)" -ForegroundColor Yellow
            if ($s.stuck.context) {
                Write-Host "  Context: $($s.stuck.context)" -ForegroundColor White
            }
            if ($s.stuck.options) {
                Write-Host "  Options:" -ForegroundColor White
                $i = 1
                foreach ($opt in $s.stuck.options) {
                    Write-Host "    [$i] $opt" -ForegroundColor Cyan
                    $i++
                }
            }
            Write-Host ""

            # Prompt for guidance
            $guidance = Read-Host "Enter guidance for $($s.issue) (or press Enter to skip)"

            if ($guidance) {
                # Write guidance to session file
                $sessionFile = Join-Path $SessionDir "work-$issueNum.json"
                $sessionData = Get-Content $sessionFile -Raw | ConvertFrom-Json
                $sessionData.guidance = $guidance
                $sessionData.lastUpdate = (Get-Date -Format "o")
                $sessionData | ConvertTo-Json -Depth 3 | Set-Content $sessionFile

                Write-Host "  [OK] Guidance written. Session will pick it up on next iteration." -ForegroundColor Green
            }

            $lastStuckPrompted[$issueNum] = Get-Date
            Write-Host ""
        }
    }

    # Check completion
    $activeCount = $working.Count + $stuck.Count + $blocked.Count + $starting.Count
    if ($activeCount -eq 0 -and $ready.Count -eq $sessions.Count) {
        Write-Host "===============================================================" -ForegroundColor Green
        Write-Host "  ALL SESSIONS COMPLETE!" -ForegroundColor Green
        Write-Host "===============================================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "  PRs ready for review:" -ForegroundColor White
        foreach ($s in $ready) {
            Write-Host "    - $($s.issue): $($s.prUrl)" -ForegroundColor Cyan
        }
        Write-Host ""
        break
    }

    Start-Sleep -Seconds 15
}
