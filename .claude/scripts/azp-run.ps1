<#
Post `/azp run <pipeline>` (or `/azp list`) as a PR comment to trigger
Azure Pipelines.

Why this script exists
----------------------
Posting `/azp run test-all` directly via `gh pr comment --body "/azp run test-all"`
is a Windows Git Bash trap: MSYS rewrites the leading `/` into
`C:/Program Files/Git/...` before `gh` sees the value, and the comment lands
on GitHub silently corrupted (no error, just a mangled body that does not
trigger CI). See `.claude/docs/agent-rules.md` -> `Windows Git Bash gotchas`.

This script forwards the body via stdin (`--body-file -`), which is not
subject to MSYS path conversion. The slash literal is internal to the script,
never crossing the bash -> exe boundary as a CLI argument, so the failure
mode cannot fire.

Invoke directly via the PowerShell tool (preferred), NOT wrapped in Bash:

    .claude\scripts\azp-run.ps1 -Pr 5467
    .claude\scripts\azp-run.ps1 -Pr 5467 -Pipeline test-sqlite
    .claude\scripts\azp-run.ps1 -Pr 5467 -Pipeline list

`-Pipeline list` posts `/azp list` (every pipeline registered on the repo);
any other value posts `/azp run <value>`.

Output: prints the new comment URL on stdout. Non-zero exit on `gh` failure.
#>

param(
    [Parameter(Mandatory)][int]$Pr,
    [string]$Pipeline = 'test-all',
    [string]$Repo = 'linq2db/linq2db'
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$body = if ($Pipeline -eq 'list') { '/azp list' } else { "/azp run $Pipeline" }

$body | gh pr comment $Pr --repo $Repo --body-file -
if ($LASTEXITCODE -ne 0) {
    [Console]::Error.WriteLine("azp-run: gh pr comment failed with exit $LASTEXITCODE")
    exit $LASTEXITCODE
}
