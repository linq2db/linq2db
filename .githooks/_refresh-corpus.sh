#!/bin/sh
# Fast-forward the .claude submodule (the agent-instruction corpus) to the tip of
# github.com/linq2db/agents master.
#
# The .claude gitlink recorded in this repo is a bootstrap pointer, not a version pin:
# corpus history lives in its own repo, so the working checkout should track that repo's
# tip rather than whatever commit a linq2db branch happens to record.
#
# Never clobbers in-flight corpus work (skips when .claude has uncommitted tracked changes)
# and never fails the git operation that triggered it - post-checkout's exit status becomes
# the exit status of the checkout itself.

sm=".claude"

# submodule not populated yet (fresh clone / fresh worktree) - nothing to refresh
[ -e "$sm/.git" ] || exit 0

# uncommitted tracked edits inside the corpus - leave them alone
if [ -n "$(git -C "$sm" status --porcelain --untracked-files=no 2>/dev/null)" ]; then
  echo "[.githooks] $sm has uncommitted changes - skipping auto-refresh." >&2
  exit 0
fi

if ! git submodule update --remote --merge -- "$sm" >/dev/null 2>&1; then
  echo "[.githooks] $sm auto-refresh skipped (offline or fetch failed)." >&2
fi
exit 0
