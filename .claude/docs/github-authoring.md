## GitHub content authoring — rules and mechanics

Full reference for editing, posting, and replying to content on `linq2db/linq2db`. The summary in [`agent-rules.md`](agent-rules.md) → *GitHub content authoring* keeps one-line triggers; this doc is what you load when one of those triggers fires.

### Never edit content authored by others

Never edit, PATCH, or overwrite GitHub content authored by a user other than the current `gh`-authenticated user. This covers:

- issue bodies
- PR bodies
- issue-comment bodies
- review-comment bodies
- commit messages
- CHANGELOG entries attributed to others (only amend your own lines)

To respond to or add to someone else's content, post a new comment / reply / review — don't modify the original. Retractions and corrections happen in a reply on the same thread, not by overwriting the thing you're retracting.

Metadata changes — closing/reopening, labels, milestones, assignees — are **not** content edits and remain allowed under their usual confirmation rules (commits need explicit user ask, pushes need explicit user ask, etc.).

### Retraction mechanics

Also applies to your **own** submitted review/comment — overwriting erases public history.

- **Check `state` + `submitted_at` before any `PUT` / `PATCH`** (`gh api repos/<o>/<r>/pulls/<n>/reviews/<id> --jq '{state, submitted_at}'`). A submitted review (`submitted_at` populated, `state` ∈ {APPROVED, CHANGES_REQUESTED, COMMENTED}) must not be edited for **substantive** changes — retract those via reply; a truly `PENDING` (`submitted_at: null`) is still editable in place for anything. The mechanical-edit exception below carves out non-substantive bookkeeping edits on submitted reviews.
- **Line / file review comments:** reply via `POST /repos/{o}/{r}/pulls/{n}/comments/{comment_id}/replies` (or GraphQL `addPullRequestReviewComment` with `inReplyTo`). Body starts with `Retraction:` or `Correction:` and states the correct reading in one line.
- **Review body (top-level):** post a new review or PR issue comment that references the prior; never `PUT` the original.
- Exception: typo / broken-link / formatting-only fixes that don't change meaning are OK to edit in place.
- Exception: **mechanical state-tracking edits** on your own submitted reviews — flipping `[ ]` → `[x]` on a finding's checkbox after verifying the fix landed, updating a per-finding status tag, etc. The `/verify-review` skill PUTs `/repos/{o}/{r}/pulls/{n}/reviews/{review_id}` exactly for this purpose; see [`github-review-api.md`](github-review-api.md) → *Edit a review body (after submission)*. The original review prose stays intact — only the bookkeeping markers move. Don't extend this exception to substantive corrections (those still go via reply per the rule above).

### Verify after every manual PATCH / PUT

After any manual `gh api PATCH` / `PUT` on a comment or review body, re-fetch and verify. The API's success response only confirms the request was accepted — it doesn't confirm the body you intended was actually stored. Two known traps:

- `gh api -f body=@<file>` does **not** read the file; it stores the literal string `@<file>` as the body. Same trap as `gh … --body @-`. Use `--input <json-file>` with a properly-escaped wrapper instead — build it via pwsh (`@{body=Get-Content -Raw <md>} | ConvertTo-Json -Compress | Set-Content <json>`), then `gh api --method PATCH ... --input <json>`.
- Stdin encoding via Bash pipes can mangle non-ASCII (em-dash → `ΓÇö` etc.) on Windows.

After every manual PATCH/PUT, run `gh api repos/<o>/<r>/issues/comments/<id> --jq '.body[:200]'` (or equivalent) and confirm the prefix matches what you intended. Skill-driven posts via `post-pr-review.ps1` already do this byte-compare via `verify: true`; manual calls don't, so verify by hand.

### Transient API outages — don't retry-loop

When a `gh api` call returns HTTP 422 with body `{"errors":["An internal error occurred, please try again."]}`, treat it as a transient GitHub-side outage on the specific endpoint. Report once with the in-flight context (manifest path, payload, what was about to be posted), preserve any scratch artefacts under `.build/.claude/`, and wait for explicit user direction.

- **Don't auto-retry on a timer.** Insistent retries waste user attention and burn rate budget without changing anything — the same 422 has been observed repeating for ~30 minutes against the same endpoint.
- **Don't poll `githubstatus.com`.** The public status page only surfaces *broad* incidents — partial-feature outages (specific endpoints flaky for a window) don't show as red components. The 422 wording from the API itself is a more reliable signal that the endpoint is temporarily broken than the dashboard.
- If the user later says "retry" or "try again", attempt once and report. If it fails again with the same signature, surface it once and stop — don't enter a retry loop.

Surfaced 2026-05-06 during PR #5467 review posting against `POST /repos/{o}/{r}/pulls/{n}/reviews`.

### Wording discipline

Issue bodies, PR bodies, review comments, and replies are terse and fact-dense — a record of what changed and why, not a place for framing, apologies, or summaries of what the diff already shows.

**Cut:** restating the diff in prose; apologetic framing ("sorry for the churn", "I wasn't sure"); puffed adjectives ("comprehensive", "robust", "clean", "thorough", "elegant", "proper" — replace with the concrete fact or drop); anticipatory reassurance ("I made sure not to break anything"); meta-narrative about the process ("I originally tried X then switched to Y" belongs in a commit message at most).

**Keep:** what changed (bullets, imperative); why it changed (constraint / upstream / linked issue, with a link); non-obvious trade-offs the reviewer must notice (new public type, deferred test-plan item, baselines refresh); `Fixes #<n>` / `Closes #<n>` for auto-closing.

Review comments: lead with `**<Severity> · <ID>**`, state the finding, state the fix — no "I noticed that…" / "this might be worth looking at…", the severity label already says "I think this matters". Retraction / correction replies: state what was wrong, the correct reading, one link to evidence — no apologies (the retraction is the apology). Your own prior posts authored by the current `gh` user are editable without this guardrail applying.

The first-party provider-behavior verification rule (XML docs / source comments / agent-prose claims about how a provider translates a member) lives in [`agent-rules.md`](agent-rules.md) → *Agent Guardrails*. It applies to code-authoring content too, not just GitHub posts.
