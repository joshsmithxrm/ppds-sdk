$env:PPDS_INTERNAL = '1'
Write-Host 'Worker session for issue #332' -ForegroundColor Cyan
Write-Host ''
claude --permission-mode bypassPermissions "Read .claude/session-prompt.md and implement issue #332. Start by understanding the issue, then implement the fix."
