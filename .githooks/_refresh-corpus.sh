#!/bin/sh
# Populate and fast-forward the .claude submodule (the agent-instruction corpus) to the
# tip of github.com/linq2db/agents master.
#
# The .claude gitlink recorded in this repo is a bootstrap pointer, not a version pin:
# corpus history lives in its own repo, so the working checkout should track that repo's
# tip rather than whatever commit a linq2db branch happens to record.
#
# `git worktree add` runs `reset --hard --no-recurse-submodules` internally, so it leaves
# the new worktree's `.claude/` empty - and because `.gitmodules` sets `ignore = all`, the
# tree still reads clean. An agent starting there resolves its CLAUDE.md @-imports against
# the empty directory before any session-level hook can run, so it loads zero project rules
# with nothing signalling the absence. Hence: bootstrap here, at checkout time, not later.
#
# Never clobbers in-flight corpus work (skips when .claude has uncommitted tracked changes)
# and never fails the git operation that triggered it - post-checkout's exit status becomes
# the exit status of the checkout itself.

sm=".claude"

# --- not populated yet (fresh worktree, or a clone that never ran --init) ----
if [ ! -e "$sm/.git" ]; then
  case "$(git ls-tree HEAD -- "$sm" 2>/dev/null)" in
    160000*) ;;
    *) exit 0 ;;                       # no gitlink recorded here - nothing to bootstrap
  esac

  set -- --init
  # Borrow the primary clone's corpus objects when we can: no download, and --dissociate
  # copies them in so this checkout stays independent afterwards. git refuses a *shallow*
  # reference outright, so only pass it when the primary's corpus isn't shallow.
  common="$(git rev-parse --git-common-dir 2>/dev/null)"
  if [ -n "$common" ]; then
    ref="$(CDPATH= cd -- "$(dirname -- "$common")" 2>/dev/null && pwd)/$sm"
    if [ -e "$ref/.git" ] &&
       [ "$(git -C "$ref" rev-parse --is-shallow-repository 2>/dev/null)" = "false" ]; then
      set -- "$@" --reference "$ref" --dissociate
    fi
  fi

  if ! err="$(git submodule update "$@" -- "$sm" 2>&1)"; then
    echo "[.githooks] $sm bootstrap skipped: $err" >&2
    echo "[.githooks] run 'git submodule update --init -- $sm' by hand before working here." >&2
    exit 0
  fi
fi

# uncommitted tracked edits inside the corpus - leave them alone
if [ -n "$(git -C "$sm" status --porcelain --untracked-files=no 2>/dev/null)" ]; then
  echo "[.githooks] $sm has uncommitted changes - skipping auto-refresh." >&2
  exit 0
fi

if ! git submodule update --remote --merge -- "$sm" >/dev/null 2>&1; then
  echo "[.githooks] $sm auto-refresh skipped (offline or fetch failed)." >&2
fi
exit 0
