# Set an environment variable for the rest of the CI job, on whichever CI is running.
# Sourced, not executed:  . "$(dirname "$0")/ci-setvar.sh"
#
# `export` reaches neither host's channel (Azure's task.setvariable command, GitHub's $GITHUB_ENV).
# Detection is on $GITHUB_ENV, not $GITHUB_ACTIONS, so a caller needing the variables in its own
# step can point GITHUB_ENV at a private file and read them back - run-provider-tests.sh does.
# Don't spell the Azure command prefix in prose here: the agent matches it anywhere in step output,
# so a caller echoing this file (bash -v) executes the comment.
ci_setvar() {
	if [ $# -ne 2 ]; then
		echo "ci_setvar: expected <name> <value>, got $# argument(s)" >&2
		return 2
	fi
	if [ -n "${GITHUB_ENV:-}" ]; then
		echo "$1=$2" >> "$GITHUB_ENV"
	else
		echo "##vso[task.setvariable variable=$1;]$2"
	fi
}
