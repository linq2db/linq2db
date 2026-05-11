# KB selection grammar

Shared filter / action grammar for `/kb-issues` and any future skill that browses KB items. Defines how a user picks one or many items from a list and what actions they can apply.

## Selection input

The user types a selection string after the skill prints a numbered result list. Recognized forms:

### Exact IDs

- Single ID: `DI-0042`
- Comma-separated IDs (whitespace optional): `DI-0042, DI-0099, DI-0123`
- Range: `DI-0040..DI-0045` — inclusive both ends.

### Group shorthand

- `all` — every item in the current result list (not the full KB; the filter from a prior step is implicit).
- `all <facet>:<value>` — filter the current result list by one facet.
- `all <facet>:<value> <facet>:<value>` — multi-facet filter (AND).

Recognized facets:

| Facet | Values |
|---|---|
| `severity` | `high`, `med`, `low` |
| `category` | one of the categories in [`kb-issue-categories.md`](kb-issue-categories.md) |
| `source` | `code`, `git`, `gh`, `cross` |
| `area` | any area code from [`kb-areas.md`](kb-areas.md) |
| `status` | `open`, `triaged`, `accepted`, `wontfix`, `duplicate-of`, `fixed`, `dismissed` |
| `confidence` | `high`, `medium`, `low` |
| `file` | full or partial path; matches if `files[]` contains a path with this substring |
| `gh` | `yes` (has a `gh_issue`) or `no` |

### Random sampling

- `random N` — N random items from the current result list.
- `random N <facet>:<value>` — N random items matching the filter.

### Numeric position

- `1`, `1,3,5`, `1-5` — positions within the most recent result list (1-indexed).

## Action menu

After a selection, the skill offers the following actions. Single-key shortcuts on the left; type the letter (or letters comma-separated for multiple actions on different selected items, but the same action runs against all selected items per invocation).

| Key | Action | Notes |
|---|---|---|
| `d` | **Detail** — open `detected-issues/items/<id>.md` and print the full body | Read-only. |
| `g` | **Create GH issue** — delegate to `/create-issue` with body pre-filled from the item | Sets status → `accepted`, populates `gh_issue` on success. Skips items already with `gh_issue`. |
| `f` | **Drive /fix-issue** — only valid when `gh_issue` is set; hands off to `/fix-issue <gh_issue>` | One item at a time; if multiple selected, asks the user to pick. |
| `w` | **Mark wontfix** — prompts for a one-line reason; sets status → `wontfix` and appends reason to MD body | Reason is required (no empty wontfix). |
| `u` | **Mark duplicate** — prompts for the canonical `DI-NNNN` or `gh#NNNN`; sets status → `duplicate-of:<x>` | Validates that the canonical exists. |
| `x` | **Dismiss as false-positive** — prompts for reason; sets status → `dismissed` | Reason is required. |
| `r` | **Re-triage** — sets status → `triaged` (no other change) | Useful for marking that the user has seen the item without committing to an action. |
| `s` | **Skip** — exits the action menu, returns to the result list | |
| `q` | **Quit** — exit the skill | |

Bulk actions: when multiple items are selected, the same action runs against each. The skill asks for confirmation before any write that touches more than 5 items at once (`Apply <action> to <N> items? [y/N]`).

## Examples

```
> /kb-issues all severity:high status:open
[lists 7 results numbered 1..7]

> 1,3
[selects DI-0042 and DI-0099]

> g
[delegates each to /create-issue]
```

```
> /kb-issues all area:PROV-ORACLE
[lists 14 results]

> all category:legacy-pattern
[narrows to 4]

> 1
> d
[prints DI-0017 detail]

> w
Reason: Provider-specific quirk we accept; documented in conventions/legacy-patterns.md.
[status flipped to wontfix]
```

```
> /kb-issues random 5 status:open
[5 random open items]

> all
> r
[all 5 marked triaged — quick "I've seen these" sweep]
```

## Implementation note

Skills implementing this grammar parse the selection string into:

```
{
  ids: ["DI-0042", "DI-0099"],          # explicit IDs after expansion (ranges, positions, random)
  filters: {                             # only present for `all` or `random N` forms
    severity: "high",
    status: "open"
  }
}
```

then resolve `ids[]` against the most recent result list. The filter dict is applied left-to-right with AND semantics. The skill enforces facet legality (rejects `severity:foo` with a one-line error and re-prompts).
