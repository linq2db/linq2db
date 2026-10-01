#!/bin/sh
# linq2db-cli credential helper, protocol 1, over pass (the standard unix password manager).
# Generated as a starter by "dotnet linq2db credentials helper init --backend pass"; edit freely.
# Secrets travel only on stdin/stdout. See the linq2db-cli credential helper protocol.
set -eu
umask 077
# sed and sort are run with LC_ALL=C (byte semantics).

fail() {
	printf '%s\n' "$1" >&2
	exit 1
}

if [ "${LINQ2DB_CREDENTIAL_INTERACTIVE-1}" = 0 ]; then
	PASSWORD_STORE_GPG_OPTS="${PASSWORD_STORE_GPG_OPTS-} --pinentry-mode=error"
	export PASSWORD_STORE_GPG_OPTS
fi

store_dir=${PASSWORD_STORE_DIR:-$HOME/.password-store}
prefix=linq2db-cli

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

# Decrypts one entry into $entry_text (the assignment carries pass's exit status; a pipeline would not).
show() {
	entry_text=$(pass show -- "$1") || fail "pass show failed for $1"
}

case ${1-} in
	get)
		[ -f "$store_dir/$prefix/$target.gpg" ] || exit 0
		show "$prefix/$target"
		secret=$(printf '%s\n' "$entry_text" | LC_ALL=C sed -n '1p')
		user=$(printf '%s\n' "$entry_text" | LC_ALL=C sed -n 's/^user: //p')
		printf 'username=%s\npassword=%s\n' "$user" "$secret"
		;;
	store)
		printf '%s\nuser: %s\n' "$password" "$username" | pass insert -m -f -- "$prefix/$target" >/dev/null \
			|| fail 'pass insert failed'
		;;
	erase)
		if [ ! -f "$store_dir/$prefix/$target.gpg" ]; then
			printf 'removed=false\n'
			exit 0
		fi
		pass rm -f -- "$prefix/$target" >/dev/null || fail 'pass rm failed'
		# "pass rm" can exit 0 without removing the file (measured: read-only store directory).
		[ ! -e "$store_dir/$prefix/$target.gpg" ] || fail 'pass rm did not remove the entry'
		printf 'removed=true\n'
		;;
	list)
		[ -d "$store_dir/$prefix" ] || exit 0
		entries=$(find "$store_dir/$prefix" -type f -name '*.gpg') || fail 'cannot list the password store'
		printf '%s\n' "$entries" | LC_ALL=C sort | while IFS= read -r file; do
			[ -n "$file" ] || continue
			entry=${file#"$store_dir/"}
			entry=${entry%.gpg}
			show "$entry"
			user=$(printf '%s\n' "$entry_text" | LC_ALL=C sed -n 's/^user: //p')
			printf 'target=%s\nusername=%s\n\n' "${entry#"$prefix/"}" "$user"
		done
		;;
	*)
		printf 'unsupported=verb\n'
		;;
esac
