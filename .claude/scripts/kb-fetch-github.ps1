#!/usr/bin/env pwsh
<#
KB GitHub fetcher — paginated fetch of issues / PRs / discussions / milestones
for linq2db/linq2db, with cursor support.

One permission rule:
    Bash(pwsh -NoProfile -File .claude/scripts/kb-fetch-github.ps1 *)

Input (stdin, JSON):
  {
    "source":  "issues" | "prs" | "discussions" | "milestones",
    "since":   "<ISO-timestamp>",   // for issues/prs/discussions, optional
    "owner":   "linq2db",
    "repo":    "linq2db",
    "perPage": 100,
    "maxPages": 100                 // cap to avoid runaway; default 200
  }

Output (stdout, JSON):
  issues / prs:
    {
      "source": "issues",
      "items": [
        {
          "id": 5414, "title": "...", "state": "open|closed",
          "labels": ["bug", "provider:oracle"],
          "user": "octocat",
          "created_at": "...", "updated_at": "...", "closed_at": "...",
          "body_excerpt": "<first 500 chars>",
          "url": "https://...",
          "is_pr": false,
          // PR-only:
          "merged_at": "...", "head_ref": "...", "base_ref": "...", "draft": false
        },
        ...
      ],
      "next_cursor": "<ISO of last item>",
      "fetched": 314
    }

  milestones:
    { "source": "milestones",
      "open":   [{title, due_on, description}, ...],
      "closed": [...] }

  discussions:
    { "source": "discussions",
      "items": [{id, title, category, state, labels[], user, updated_at, body_excerpt, url}, ...],
      "next_cursor": "<ISO>", "fetched": N }

  Errors:
    { "ok": false, "status": "rate-limited" | "auth-failed" | "error",
      "reset_at": "<ISO>"?, "error": "<message>" }
#>

$global:ScriptBaseName = 'kb-fetch-github'
. "$PSScriptRoot/_shared.ps1"

$m = Read-StdinJson
if (-not $m.source) { Exit-WithError 'source required' }
$source = [string]$m.source
$owner  = if ($m.owner) { [string]$m.owner } else { 'linq2db' }
$repo   = if ($m.repo)  { [string]$m.repo }  else { 'linq2db' }
$perPage = if ((Test-IsInteger $m.perPage) -and [long]$m.perPage -gt 0) { [int]$m.perPage } else { 100 }
$maxPages = if ((Test-IsInteger $m.maxPages) -and [long]$m.maxPages -gt 0) { [int]$m.maxPages } else { 200 }
$since = if ($m.since) { [string]$m.since } else { $null }

function Truncate-Excerpt {
    param([string]$Text, [int]$N = 500)
    if (-not $Text) { return '' }
    $t = $Text -replace "`r`n", "`n"
    if ($t.Length -le $N) { return $t }
    return $t.Substring(0, $N) + '…'
}

function Fetch-IssuesOrPrs {
    param([bool]$PrsOnly)
    $items = @()
    $page = 1
    $endpoint = "repos/$owner/$repo/issues"
    while ($page -le $maxPages) {
        $args = @('api', '-X', 'GET', $endpoint,
                  '-f', "state=all",
                  '-f', "sort=updated",
                  '-f', "direction=asc",
                  '-f', "per_page=$perPage",
                  '-f', "page=$page")
        if ($since) { $args += @('-f', "since=$since") }
        $r = Invoke-Gh -ArgumentList $args
        if (-not $r.ok) {
            $err = ($r.stderr + $r.stdout).Trim()
            if ($err -match 'rate limit|API rate') {
                return [pscustomobject]@{ ok = $false; status = 'rate-limited'; error = $err }
            }
            return [pscustomobject]@{ ok = $false; status = 'error'; error = $err }
        }
        $batch = @()
        try { $batch = @($r.stdout | ConvertFrom-Json -Depth 100) } catch {
            return [pscustomobject]@{ ok = $false; status = 'error'; error = "parse: $($_.Exception.Message)" }
        }
        if ($batch.Count -eq 0) { break }
        foreach ($it in $batch) {
            $isPr = ($null -ne $it.pull_request)
            if ($PrsOnly -and -not $isPr) { continue }
            if ((-not $PrsOnly) -and $isPr) { continue }
            $entry = [ordered]@{
                id           = $it.number
                title        = $it.title
                state        = $it.state
                labels       = @($it.labels | ForEach-Object { $_.name })
                user         = $it.user.login
                created_at   = $it.created_at
                updated_at   = $it.updated_at
                closed_at    = $it.closed_at
                body_excerpt = (Truncate-Excerpt -Text ([string]$it.body) -N 500)
                url          = $it.html_url
                is_pr        = $isPr
            }
            if ($isPr) {
                $entry['merged_at'] = $it.pull_request.merged_at
                $entry['draft']     = $false
            }
            $items += [pscustomobject]$entry
        }
        if ($batch.Count -lt $perPage) { break }
        $page++
    }

    if ($PrsOnly -and $items.Count -gt 0) {
        # Fetch additional PR-specific fields for items above (head_ref/base_ref/draft).
        # /issues endpoint doesn't return those — issue an extra /pulls/{n} call per PR.
        # To keep cost bounded, only enrich up to 200 most-recent PRs in this batch.
        $enrichSlice = $items | Sort-Object updated_at -Descending | Select-Object -First 200
        $enriched = @{}
        foreach ($pr in $enrichSlice) {
            $r = Invoke-Gh -ArgumentList @('api', "repos/$owner/$repo/pulls/$($pr.id)")
            if ($r.ok) {
                try {
                    $detail = $r.stdout | ConvertFrom-Json -Depth 100
                    $enriched[[string]$pr.id] = [pscustomobject]@{
                        head_ref = $detail.head.ref
                        base_ref = $detail.base.ref
                        draft    = [bool]$detail.draft
                        merged_at = $detail.merged_at
                    }
                } catch { }
            }
        }
        for ($i = 0; $i -lt $items.Count; $i++) {
            $key = [string]$items[$i].id
            if ($enriched.ContainsKey($key)) {
                $items[$i] | Add-Member -NotePropertyName 'head_ref' -NotePropertyValue $enriched[$key].head_ref -Force
                $items[$i] | Add-Member -NotePropertyName 'base_ref' -NotePropertyValue $enriched[$key].base_ref -Force
                $items[$i] | Add-Member -NotePropertyName 'draft'    -NotePropertyValue $enriched[$key].draft    -Force
                $items[$i] | Add-Member -NotePropertyName 'merged_at' -NotePropertyValue $enriched[$key].merged_at -Force
            }
        }
    }

    $next = $since
    if ($items.Count -gt 0) {
        $latest = ($items | Sort-Object updated_at -Descending | Select-Object -First 1).updated_at
        if ($latest) { $next = $latest }
    }
    return [pscustomobject]@{
        ok = $true
        source = if ($PrsOnly) { 'prs' } else { 'issues' }
        items = $items
        next_cursor = $next
        fetched = $items.Count
    }
}

function Fetch-Milestones {
    $open = @()
    $closed = @()
    foreach ($state in @('open', 'closed')) {
        $page = 1
        while ($page -le $maxPages) {
            $r = Invoke-Gh -ArgumentList @('api', '-X', 'GET', "repos/$owner/$repo/milestones",
                '-f', "state=$state", '-f', "per_page=$perPage", '-f', "page=$page")
            if (-not $r.ok) {
                return [pscustomobject]@{ ok = $false; status = 'error'; error = ($r.stderr + $r.stdout).Trim() }
            }
            $batch = @()
            try { $batch = @($r.stdout | ConvertFrom-Json -Depth 100) } catch { break }
            if ($batch.Count -eq 0) { break }
            foreach ($ms in $batch) {
                $entry = [pscustomobject]@{
                    title       = $ms.title
                    due_on      = $ms.due_on
                    closed_at   = $ms.closed_at
                    description = (Truncate-Excerpt -Text ([string]$ms.description) -N 1000)
                    open_issues = $ms.open_issues
                    closed_issues = $ms.closed_issues
                    url         = $ms.html_url
                }
                if ($state -eq 'open') { $open += $entry } else { $closed += $entry }
            }
            if ($batch.Count -lt $perPage) { break }
            $page++
        }
    }
    return [pscustomobject]@{ ok = $true; source = 'milestones'; open = $open; closed = $closed }
}

function Fetch-Discussions {
    # GraphQL — discussions aren't on REST.
    $items = @()
    $cursor = $null
    $pages = 0
    while ($pages -lt $maxPages) {
        $afterClause = if ($cursor) { ", after: `"$cursor`"" } else { '' }
        $query = @"
{
  repository(owner: "$owner", name: "$repo") {
    discussions(first: $perPage$afterClause, orderBy: {field: UPDATED_AT, direction: ASC}) {
      pageInfo { hasNextPage endCursor }
      nodes {
        number
        title
        category { name }
        labels(first: 20) { nodes { name } }
        author { login }
        bodyText
        updatedAt createdAt
        url
        closed
        answerChosenAt
      }
    }
  }
}
"@
        $r = Invoke-Gh -ArgumentList @('api', 'graphql', '-f', "query=$query")
        if (-not $r.ok) {
            $err = ($r.stderr + $r.stdout).Trim()
            if ($err -match 'rate limit|API rate') {
                return [pscustomobject]@{ ok = $false; status = 'rate-limited'; error = $err }
            }
            return [pscustomobject]@{ ok = $false; status = 'error'; error = $err }
        }
        $data = $null
        try { $data = $r.stdout | ConvertFrom-Json -Depth 100 } catch {
            return [pscustomobject]@{ ok = $false; status = 'error'; error = "parse: $($_.Exception.Message)" }
        }
        $disc = $data.data.repository.discussions
        foreach ($n in $disc.nodes) {
            if ($since -and $n.updatedAt -lt $since) { continue }
            $items += [pscustomobject]@{
                id           = $n.number
                title        = $n.title
                category     = $n.category.name
                state        = if ($n.closed) { 'closed' } else { 'open' }
                labels       = @($n.labels.nodes | ForEach-Object { $_.name })
                user         = $n.author.login
                created_at   = $n.createdAt
                updated_at   = $n.updatedAt
                body_excerpt = (Truncate-Excerpt -Text ([string]$n.bodyText) -N 500)
                url          = $n.url
                answered     = ($null -ne $n.answerChosenAt)
            }
        }
        if (-not $disc.pageInfo.hasNextPage) { break }
        $cursor = $disc.pageInfo.endCursor
        $pages++
    }
    $next = $since
    if ($items.Count -gt 0) {
        $latest = ($items | Sort-Object updated_at -Descending | Select-Object -First 1).updated_at
        if ($latest) { $next = $latest }
    }
    return [pscustomobject]@{
        ok = $true
        source = 'discussions'
        items = $items
        next_cursor = $next
        fetched = $items.Count
    }
}

$result = switch ($source) {
    'issues'       { Fetch-IssuesOrPrs -PrsOnly:$false }
    'prs'          { Fetch-IssuesOrPrs -PrsOnly:$true }
    'milestones'   { Fetch-Milestones }
    'discussions'  { Fetch-Discussions }
    default        { Exit-WithError "unknown source: $source" }
}

Write-JsonOutput -InputObject $result
