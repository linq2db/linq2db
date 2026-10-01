#!/bin/sh
# linq2db-cli credential helper, protocol 1, over secret-tool (libsecret-tools).
# Generated as a starter by "dotnet linq2db credentials helper init --backend secret-tool"; edit freely.
# Secrets travel only on stdin/stdout. See the linq2db-cli credential helper protocol.
set -eu
umask 077
# grep/sed/awk are run with LC_ALL=C (byte semantics). secret-tool itself is run with LC_ALL=C.UTF-8: GLib refuses
# non-ASCII arguments when the locale is C/POSIX (e.g. an MCP host started with an empty environment).

fail() {
	printf '%s\n' "$1" >&2
	exit 1
}

protocol=''
target=''
username=''
password=''
while IFS= read -r line || [ -n "$line" ]; do
	key=${line%%=*}
	value=${line#*=}
	case $key in
		protocol) protocol=$value ;;
		target)   target=$value ;;
		username) username=$value ;;
		password) password=$value ;;
		*) ;;
	esac
done

if [ "$protocol" != 1 ]; then
	printf 'unsupported=protocol\n'
	exit 0
fi

st() {
	LC_ALL=C.UTF-8 secret-tool "$@"
}

# Searches our items. "secret-tool search --all" prints an item header ("[/n]") and "secret = ..." on stdout and
# the attributes on stderr; a locked item has a header but no secret line. Our secrets never contain a line
# break, so every unlocked item has exactly one secret line. Sets $found_attributes and $found_items; fails when
# locked.
search() {
	result=$(st search --all service linq2db-cli "$@" 2>&1) || fail 'secret-tool search failed'
	headers=$(printf '%s\n' "$result" | LC_ALL=C grep -c '^\[/' || true)
	secrets=$(printf '%s\n' "$result" | LC_ALL=C grep -c '^secret = ' || true)
	[ "$headers" = "$secrets" ] || fail 'the keyring is locked'
	found_items=$headers
	found_attributes=$(printf '%s\n' "$result" | LC_ALL=C grep '^attribute\.' || true)
}

case ${1-} in
	get)
		search target "$target"
		[ -n "$found_attributes" ] || exit 0
		[ "$found_items" = 1 ] || fail "more than one keyring item for $target"
		user=$(printf '%s\n' "$found_attributes" | LC_ALL=C sed -n 's/^attribute\.user = //p')
		secret=$(st lookup service linq2db-cli target "$target") || fail 'secret-tool lookup failed'
		printf 'username=%s\npassword=%s\n' "$user" "$secret"
		;;
	store)
		# libsecret replaces an item only when all its attributes match, so a new user name would add a second
		# item. Store first (nothing is lost if that fails), then remove the items stored under other user names.
		search target "$target"
		old_users=$(printf '%s\n' "$found_attributes" | LC_ALL=C sed -n 's/^attribute\.user = //p')
		printf '%s' "$password" | st store --label="linq2db-cli $target" service linq2db-cli target "$target" user "$username" \
			|| fail 'secret-tool store failed'
		printf '%s\n' "$old_users" | while IFS= read -r old; do
			[ -n "$old" ] && [ "$old" != "$username" ] || continue
			st clear service linq2db-cli target "$target" user "$old" || fail 'secret-tool clear of the previous item failed'
		done
		;;
	erase)
		search target "$target"
		if [ -z "$found_attributes" ]; then
			printf 'removed=false\n'
			exit 0
		fi
		[ "$found_items" = 1 ] || fail "more than one keyring item for $target"
		st clear service linq2db-cli target "$target" || fail 'secret-tool clear failed'
		printf 'removed=true\n'
		;;
	list)
		search
		# Each item prints its attribute lines together; every item of ours has exactly one target and one user.
		printf '%s\n' "$found_attributes" | LC_ALL=C awk '
			/^attribute\.target = / { t = substr($0, 20); ht = 1 }
			/^attribute\.user = /   { u = substr($0, 18); hu = 1 }
			ht && hu { printf "target=%s\nusername=%s\n\n", t, u; ht = 0; hu = 0 }'
		;;
	*)
		printf 'unsupported=verb\n'
		;;
esac
