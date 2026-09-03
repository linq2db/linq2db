# Set an environment variable for the rest of the CI job, on whichever CI is running.
#
# Sourced, not executed:  . "$(dirname "$0")/ci-setvar.sh"
#
# Azure DevOps and GitHub Actions both scope a step's environment to that step and offer their own
# channel for exporting a variable onwards - Azure's task.setvariable logging command and GitHub's
# $GITHUB_ENV file. A plain `export` reaches neither, so a setup script has to use the host's.
#
# Note the deliberate absence of a literal logging-command prefix in this prose. The Azure agent
# matches that token anywhere in a line of step output, so writing it in a comment is enough to have
# the comment executed as a command whenever a caller echoes this file's text - which is exactly what
# `bash -v` in db2.provider.sh used to do. Only the emitting line below may spell it out.
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
