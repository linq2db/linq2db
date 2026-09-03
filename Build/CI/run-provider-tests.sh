#!/bin/bash
# Run one TFM's test suites for one provider leg, then free the binaries.
#
# This is the body of the `${{ each tfm in parameters.tfms }}` loop in
# Build/Azure/pipelines/templates/test-workflow-linux.yml, extracted so the GitHub Actions leg runs
# the same sequence. GitHub cannot express Azure's loop: a matrix leg's steps are fixed at parse
# time, and the artifact download has to be an action, so the workflow keeps one download step per
# TFM and calls this once per downloaded TFM.
#
# Ordering, and the exit behaviour, mirror the Azure steps deliberately:
#   config -> local setup -> main suite (optional) -> EF.Core suite -> remove binaries
# Azure guards every step with succeeded(), so a failed main suite skips the EF.Core suite and every
# later TFM while still publishing the .trx written so far. Here that falls out of exiting non-zero
# plus the workflow's default `if: success()` on the following steps, with the report step on
# `if: always()`.
#
# Usage:
#   run-provider-tests.sh --tfm net8.0 --flag net80 --config sqlite.core \
#                         --setup mysql.local.sh --main true --retry false
#
# Every switch takes a value, and --setup accepts an empty one, so a caller can forward matrix
# fields straight through without building the argument list conditionally. That matters on the
# GitHub side: `shell: bash` runs under `set -e`, so a `[ -n "$x" ] && args+=(…)` guard aborts the
# step when the test is false.
#
# Paths are relative to the working directory, which must be the leg's root - the one holding
# scripts/, configs/ and the downloaded <tfm>/ directory.

set -u

tfm=
flag=
config=
setup=
run_main=false
retry=false

while [ $# -gt 0 ]; do
	case "$1" in
		--tfm)    tfm=$2;      shift 2 ;;
		--flag)   flag=$2;     shift 2 ;;
		--config) config=$2;   shift 2 ;;
		--setup)  setup=$2;    shift 2 ;;
		--main)   run_main=$2; shift 2 ;;
		--retry)  retry=$2;    shift 2 ;;
		*) echo "::error::run-provider-tests: unknown argument '$1'"; exit 2 ;;
	esac
done

for required in tfm flag config; do
	if [ -z "${!required}" ]; then
		echo "::error::run-provider-tests: --$required is required"
		exit 2
	fi
done

# A typo in a boolean would otherwise read as false and silently skip the main suite - the same
# class of quiet no-op an empty test matrix produces.
for boolean in run_main retry; do
	case "${!boolean}" in
		true|false) ;;
		*) echo "::error::run-provider-tests: --${boolean/run_/} must be true or false, got '${!boolean}'"; exit 2 ;;
	esac
done

if [ ! -d "$tfm" ]; then
	echo "::error::run-provider-tests: '$tfm' does not exist - the binaries artifact was not downloaded"
	exit 2
fi

root=$(pwd)
results="$root/TestResults"
mkdir -p "$results"

# The config lands in the TFM root rather than beside the test app: TestConfiguration walks up from
# the assembly location to find UserDataProviders.json.
cp "configs/$flag/$config.json" "$tfm/UserDataProviders.json"
echo ">>> config: configs/$flag/$config.json -> $tfm/UserDataProviders.json"

# Azure removes the TFM directory whether or not the suites passed, so the next TFM's download has
# the disk. Keep that: the trap re-raises the suite's status after cleaning up.
status=0
cleanup() {
	rm -rf "$root/$tfm"
	exit $status
}
trap cleanup EXIT

if [ -n "$setup" ]; then
	echo "::group::Setup $tfm ($setup)"
	chmod +x "scripts/$setup"

	# A local setup script may export variables the test process needs - db2.provider.sh publishes
	# the clidriver's PATH and LD_LIBRARY_PATH, without which the DB2 provider cannot load its
	# native library. On Azure those arrive as a task.setvariable logging command and the agent
	# applies them to the *following* steps; here the setup and the suites are one step, so
	# $GITHUB_ENV would drop them. Point ci-setvar.sh at a private file instead and load it back.
	# (Prefix spelled out nowhere in this file on purpose - see the note in ci-setvar.sh.)
	env_file="$root/.ci-env.$tfm"
	: > "$env_file"

	# Run from the main test app's directory: db2.provider.sh derives the TFM from $PWD and drops
	# the swapped-in native library beside the assembly.
	( cd "$tfm/main/x64" && GITHUB_ENV="$env_file" "$root/scripts/$setup" )
	status=$?
	echo "::endgroup::"
	if [ $status -ne 0 ]; then
		echo "::error::run-provider-tests: local setup script '$setup' failed for $tfm"
		exit $status
	fi

	while IFS= read -r line; do
		[ -z "$line" ] && continue
		export "${line%%=*}=${line#*=}"
		echo ">>> setup exported ${line%%=*}"
	done < "$env_file"
	rm -f "$env_file"
fi

# MTP's own failed-test retry, applied to the flaky legs (Access, Oracle) only.
retry_args=()
if [ "$retry" = true ]; then
	retry_args=(--retry-failed-tests 2 --retry-failed-tests-max-tests 5)
fi

run_suite() {
	local kind=$1 dll=$2
	local args=(
		"./$tfm/$kind/x64/$dll"
		--filter "TestCategory != SkipCI"
		--settings "./$tfm/$kind/x64/.runsettings"
		--report-trx --report-trx-filename "$tfm-$kind-x64.trx"
		--results-directory "$results"
		--hangdump --hangdump-timeout 5m
	)
	# Only the main suite retries as a whole, matching retryCountOnTaskFailure: 2 on the Azure
	# step - GitHub Actions has no step-level retry, so the loop lives here. It covers the crash
	# case that MTP's in-process retry cannot: a host that dies takes its results with it.
	local attempts=1
	if [ "$retry" = true ] && [ "$kind" = main ]; then attempts=3; fi

	local i=1
	while : ; do
		echo "::group::$kind suite, $tfm (attempt $i/$attempts)"
		dotnet "${args[@]}" "${retry_args[@]}"
		local rc=$?
		echo "::endgroup::"
		if [ $rc -eq 0 ] || [ $i -ge $attempts ]; then return $rc; fi
		echo "::warning::$kind suite failed for $tfm (attempt $i/$attempts), retrying"
		i=$(( i + 1 ))
	done
}

# The main suite is skipped on non-release runs for every TFM but the newest - see the pr_main flag
# in test-workflow-linux.yml. The EF.Core suite always runs, on every enabled TFM.
if [ "$run_main" = true ]; then
	run_suite main linq2db.Tests.dll
	status=$?
	if [ $status -ne 0 ]; then exit $status; fi
fi

run_suite efcore linq2db.EntityFrameworkCore.Tests.dll
status=$?
exit $status
