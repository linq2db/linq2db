# Set an environment variable for the rest of the CI job, on whichever CI is running.
#
# Sourced, not executed:  . "$(dirname "$0")/ci-setvar.sh"
#
# Azure DevOps and GitHub Actions both scope a step's environment to that step and offer their own
# channel for exporting a variable onwards - `##vso[task.setvariable]` and $GITHUB_ENV. A plain
# `export` reaches neither, so the provider setup scripts have to use the host's channel.
#
# GitHub is detected by $GITHUB_ENV being set rather than by $GITHUB_ACTIONS, which matters: a
# caller that needs the variables in its *own* step - not a later one - can point GITHUB_ENV at a
# file of its own and read them back. run-provider-tests.sh does exactly that, because it runs a
# leg's local setup script and its test suites in a single step, so $GITHUB_ENV proper would drop
# the variables on the floor. Keying off GITHUB_ACTIONS would make that impossible to express.
ci_setvar() {
	if [ $# -ne 2 ]; then
		echo "ci_setvar: expected <name> <value>, got $# argument(s)" >&2
		return 2
	fi
	if [ -n "${GITHUB_ENV:-}" ]; then
		# Single-line form. None of the values here contain a newline; the heredoc form would be
		# needed if one ever did.
		echo "$1=$2" >> "$GITHUB_ENV"
	else
		echo "##vso[task.setvariable variable=$1;]$2"
	fi
}
