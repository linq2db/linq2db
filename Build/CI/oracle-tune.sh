# Server-side tuning for the Oracle CI containers, applied once each is ready.
#
# Sourced, not executed:  . "$(dirname "$0")/oracle-tune.sh"
#
# open_cursors defaults to 300, which the suite exhausts on the faster runners: DROP USER in
# AK107Tests.CreateUser failed with ORA-01000, the test swallowed it, and CREATE USER then reported
# ORA-01920 "user already exists" - so the visible failure named neither the cause nor the resource.
# Server-side and scoped to the CI container, like the ClickHouse settings in clickhouse.sh.
#
# Connects with OS authentication inside the container, so no password is needed here and none is
# duplicated from the docker run line.
oracle_tune() {
	local container=$1
	echo ">>> $container: raising open_cursors"
	echo "ALTER SYSTEM SET open_cursors=3000 SCOPE=BOTH;" | docker exec -i "$container" sqlplus -S / as sysdba
	# Echo the resulting value: a silently ineffective ALTER would otherwise look identical to a
	# working one until the tests exhaust cursors again.
	echo "SELECT 'open_cursors=' || value FROM v\$parameter WHERE name = 'open_cursors';" |
		docker exec -i "$container" sqlplus -S / as sysdba
}
