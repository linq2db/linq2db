# Liveness assertion for the provider containers.
#
# Sourced, not executed:  . "$(dirname "$0")/docker-liveness.sh"
#
# The readiness gates grep `docker logs` for the image's ready banner, and a container's log
# outlives the process - so once the banner is printed the gate passes even for a container that
# has since exited. Liveness therefore has to be asserted from container state, separately.
require_running() {
	local container=$1
	local state
	state=$(docker inspect -f '{{.State.Status}} exit={{.State.ExitCode}} oom={{.State.OOMKilled}}' "$container" 2>/dev/null)
	case "$state" in
		running*) return 0 ;;
	esac
	echo ">>> $container is not running: ${state:-no such container}"
	docker logs "$container" 2>&1 | tail -50
	exit 1
}
