<#
release-notes-audit.ps1 — release-notes coverage audit. Builds the
milestone-item pair list (issue ↔ closing-PR), fetches the wiki
release-notes drafts, and computes which items are covered by an
explicit `#N` mention.

Action:

  audit       Inputs:  -Milestone <ver>
                       [-Repo <owner/repo>]                  (default: linq2db/linq2db)
                       [-Version <ver>]                      (defaults to -Milestone for plan-file naming)
              Output:  {
                ok, milestone, notes: { url, length, present },
                items[]: { issueNumber, prNumber, title, labels[], inNotes, matched, suggestion },
                counts: { total, covered, gaps }
              }

Pairing rules:
  - Each merged PR is paired with every issue it closes (PR's
    `closingIssuesReferences` GraphQL field + body regex
    `(Fixes|Closes|Resolves)\s*#<n>`).
  - Issues not paired with a merged PR become standalone rows.
  - PRs not paired with any milestone issue become standalone rows.

Coverage rule: a row is covered iff the wiki text contains `#<issueNumber>`
or `#<prNumber>` as a whole token (regex `(?<![\w])#<n>(?![\d])`).

Conventions: `.claude/docs/script-authoring.md`.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('audit')]
    [string]$Action,

    [Parameter(Mandatory)][string]$Milestone,
    [string]$Repo = 'linq2db/linq2db',
    [string]$Version
)

$global:ScriptBaseName = 'release-notes-audit'
. (Join-Path $PSScriptRoot '_shared.ps1')

if (-not $Version) { $Version = $Milestone }

# -- paths -------------------------------------------------------------------

function Get-WorkDir {
    $dir = Join-Path (Get-Location) '.build/.claude'
    if (-not (Test-Path -LiteralPath $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    return $dir
}

function Get-PlanFilePath {
    param([string]$Ver)
    return (Join-Path (Get-WorkDir) "release-$Ver-notes-plan.json")
}

# -- milestone resolution ----------------------------------------------------

function Get-Milestone {
    param([string]$Title, [string]$Repo)
    $r = Invoke-GhJson -ArgumentList @(
        'api', "repos/$Repo/milestones?state=all&per_page=100", '--paginate'
    )
    if (-not $r.ok) { Exit-WithError "milestone lookup failed: $($r.error)" }
    foreach ($m in $r.data) {
        if ($m.title -eq $Title) { return $m }
    }
    return $null
}

# -- listings ----------------------------------------------------------------

# Merged PRs on the milestone. We use `gh pr list` (GraphQL-backed) so we get
# the `closingIssuesReferences` field natively. Per-PR pagination is built-in.
function Get-MergedMilestonePRs {
    param([string]$Repo, [string]$Title)
    # gh pr list supports --search syntax for milestone filtering.
    # `is:merged` ensures we get only merged PRs (not closed-but-not-merged).
    $r = Invoke-GhJson -ArgumentList @(
        'pr','list','--repo',$Repo,
        '--state','closed',
        '--search', "is:pr is:merged milestone:`"$Title`"",
        '--limit','500',
        '--json','number,title,body,labels,mergedAt,author,url,closingIssuesReferences'
    )
    if (-not $r.ok) { Exit-WithError "merged PR list failed: $($r.error)" }
    return @($r.data)
}

# Closed issues on the milestone (not PRs).
function Get-ClosedMilestoneIssues {
    param([string]$Repo, [string]$Title)
    $r = Invoke-GhJson -ArgumentList @(
        'issue','list','--repo',$Repo,
        '--state','closed',
        '--search', "is:issue milestone:`"$Title`"",
        '--limit','500',
        '--json','number,title,labels,closedAt,author,url'
    )
    if (-not $r.ok) { Exit-WithError "closed issue list failed: $($r.error)" }
    return @($r.data)
}

# -- pairing -----------------------------------------------------------------

# Extract issue references from a PR body via Fixes/Closes/Resolves #N
# patterns. Returns @(int...). Case-insensitive, single or multi-word verb.
$script:IssueRefRx = '(?im)\b(?:fix(?:es|ed)?|close[ds]?|resolve[ds]?)\s+#(?<n>\d+)\b'

function Get-IssueRefsFromBody {
    param([string]$Body)
    if (-not $Body) { return @() }
    $refs = [System.Collections.Generic.HashSet[int]]::new()
    foreach ($m in [regex]::Matches($Body, $script:IssueRefRx)) {
        [void]$refs.Add([int]$m.Groups['n'].Value)
    }
    return @($refs)
}

# Build pair list. Returns an array of @{issueNumber; prNumber; title; labels; meta...}
function Build-PairList {
    param($MergedPRs, $ClosedIssues)
    # Build issue lookup by number for fast intersection.
    $issuesByNumber = @{}
    foreach ($i in @($ClosedIssues)) {
        $issuesByNumber[[int]$i.number] = $i
    }
    $issuesPaired = [System.Collections.Generic.HashSet[int]]::new()
    $rows = New-Object 'System.Collections.Generic.List[object]'

    foreach ($pr in @($MergedPRs)) {
        # Collect linked issue numbers from both signals:
        $refs = [System.Collections.Generic.HashSet[int]]::new()
        foreach ($ci in @($pr.closingIssuesReferences)) {
            if ($ci.number) { [void]$refs.Add([int]$ci.number) }
        }
        foreach ($n in (Get-IssueRefsFromBody -Body $pr.body)) {
            [void]$refs.Add($n)
        }
        # Filter to issues that are *in this milestone*.
        $milestoneLinked = @($refs | Where-Object { $issuesByNumber.ContainsKey($_) })

        if ($milestoneLinked.Count -gt 0) {
            foreach ($issueNum in $milestoneLinked) {
                $issue = $issuesByNumber[$issueNum]
                $rows.Add([ordered]@{
                    issueNumber = [int]$issueNum
                    prNumber    = [int]$pr.number
                    title       = $pr.title
                    issueTitle  = $issue.title
                    labels      = @(@($pr.labels) + @($issue.labels) | ForEach-Object { $_.name } | Sort-Object -Unique)
                    url         = $pr.url
                    issueUrl    = $issue.url
                    kind        = 'pair'
                }) | Out-Null
                [void]$issuesPaired.Add([int]$issueNum)
            }
        } else {
            $rows.Add([ordered]@{
                issueNumber = $null
                prNumber    = [int]$pr.number
                title       = $pr.title
                issueTitle  = $null
                labels      = @($pr.labels | ForEach-Object { $_.name })
                url         = $pr.url
                issueUrl    = $null
                kind        = 'pr-only'
            }) | Out-Null
        }
    }

    # Standalone issues — closed-without-PR.
    foreach ($i in @($ClosedIssues)) {
        if ($issuesPaired.Contains([int]$i.number)) { continue }
        $rows.Add([ordered]@{
            issueNumber = [int]$i.number
            prNumber    = $null
            title       = $i.title
            issueTitle  = $i.title
            labels      = @($i.labels | ForEach-Object { $_.name })
            url         = $i.url
            issueUrl    = $i.url
            kind        = 'issue-only'
        }) | Out-Null
    }
    return $rows.ToArray()
}

# -- wiki fetch --------------------------------------------------------------

function Get-WikiText {
    param([string]$Repo, [string]$Page)
    # raw.githubusercontent.com/wiki/<owner>/<repo>/<page>.md is the documented
    # URL for accessing wiki content as raw markdown. The GitHub REST API
    # does not expose wiki content under /repos/.../contents/.
    $url = "https://raw.githubusercontent.com/wiki/$Repo/$Page.md"
    try {
        $r = Invoke-WebRequest -Uri $url -TimeoutSec 30 -UseBasicParsing
        return [pscustomobject]@{ ok = $true; url = $url; text = $r.Content; status = $r.StatusCode }
    } catch {
        $status = $null
        if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode }
        return [pscustomobject]@{ ok = $false; url = $url; text = ''; status = $status; error = $_.Exception.Message }
    }
}

# -- coverage ----------------------------------------------------------------

# Whole-token #N match — avoids matching #1234 against #12345 or partial
# anchor ids. Negative lookahead on digits, negative lookbehind on word chars.
function Test-MentionsRef {
    param([string]$Text, [int]$N)
    if (-not $Text -or -not $N) { return $false }
    # Construct the regex per-call (cheap, only ~total-row count of calls).
    return [regex]::IsMatch($Text, "(?<![\w])#$N(?!\d)")
}

# -- audit (main) ------------------------------------------------------------

function Do-Audit {
    $m = Get-Milestone -Title $Milestone -Repo $Repo
    if (-not $m) { Exit-WithError "milestone '$Milestone' not found in $Repo" }

    $mergedPRs   = Get-MergedMilestonePRs   -Repo $Repo -Title $Milestone
    $closedIss   = Get-ClosedMilestoneIssues -Repo $Repo -Title $Milestone
    $rows        = Build-PairList -MergedPRs $mergedPRs -ClosedIssues $closedIss

    # Fetch wiki pages and concatenate text. Use both the landing page (which
    # may have the per-release header inline) and the per-version page.
    $landing   = Get-WikiText -Repo $Repo -Page 'Releases-and-Roadmap'
    $perVer    = Get-WikiText -Repo $Repo -Page "Release-Notes-$Version"

    $combinedText = ''
    if ($landing.ok) { $combinedText += "`n" + $landing.text }
    if ($perVer.ok)  { $combinedText += "`n" + $perVer.text }

    $items = New-Object 'System.Collections.Generic.List[object]'
    $covered = 0
    foreach ($row in $rows) {
        $matched = $null
        $hit = $false
        if ($row.issueNumber -and (Test-MentionsRef -Text $combinedText -N $row.issueNumber)) {
            $matched = "#$($row.issueNumber)"
            $hit = $true
        } elseif ($row.prNumber -and (Test-MentionsRef -Text $combinedText -N $row.prNumber)) {
            $matched = "#$($row.prNumber)"
            $hit = $true
        }
        $suggestion = $null
        if (-not $hit) {
            $what = if ($row.issueNumber) { "issue #$($row.issueNumber)" }
                    elseif ($row.prNumber) { "PR #$($row.prNumber)" }
                    else { 'item' }
            $suggestion = "mention $what"
        }
        if ($hit) { $covered++ }
        $items.Add([ordered]@{
            issueNumber = $row.issueNumber
            prNumber    = $row.prNumber
            title       = $row.title
            issueTitle  = $row.issueTitle
            kind        = $row.kind
            labels      = @($row.labels)
            url         = $row.url
            issueUrl    = $row.issueUrl
            inNotes     = $hit
            matched     = $matched
            suggestion  = $suggestion
        }) | Out-Null
    }

    $planPath = Get-PlanFilePath -Ver $Version
    $payload = [ordered]@{
        ok        = $true
        action    = 'audit'
        planFile  = $planPath
        milestone = @{
            title         = $m.title
            number        = $m.number
            state         = $m.state
            open_issues   = $m.open_issues
            closed_issues = $m.closed_issues
            html_url      = $m.html_url
        }
        notes = @{
            landing = @{ url = $landing.url; present = $landing.ok; length = $landing.text.Length; status = $landing.status }
            perVer  = @{ url = $perVer.url;  present = $perVer.ok;  length = $perVer.text.Length;  status = $perVer.status }
        }
        items  = $items.ToArray()
        counts = @{
            total   = $items.Count
            covered = $covered
            gaps    = $items.Count - $covered
        }
    }
    [System.IO.File]::WriteAllText(
        $planPath,
        ($payload | ConvertTo-Json -Depth 100),
        [System.Text.UTF8Encoding]::new($false)
    )
    Write-JsonOutput $payload
}

# -- dispatch ----------------------------------------------------------------

switch ($Action) {
    'audit' { Do-Audit }
    default { Exit-WithError "unknown action: $Action" }
}
