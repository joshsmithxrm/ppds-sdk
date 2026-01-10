$env:PPDS_INTERNAL = '1'
Write-Host 'Worker session for issue #328' -ForegroundColor Cyan
Write-Host ''
claude --permission-mode bypassPermissions "Read .claude/session-prompt.md and implement issue #328. Start by understanding the issue, then implement the fix."
