# syntax=docker/dockerfile:1
# Build image for Anthropic OSS Scanner (https://github.com/anthropics/oss-scanner).
# The scanner builds this with the repository root as the build context, then removes network access:
# everything the build and the tests need is restored here.
FROM mcr.microsoft.com/dotnet/sdk:10.0

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true \
    NUGET_XMLDOC_MODE=skip \
    MSBUILDDISABLENODEREUSE=1

RUN apt-get update && apt-get install -y --no-install-recommends \
      bash ca-certificates git sqlite3 \
    && rm -rf /var/lib/apt/lists/*

# scanner contract: the checkout lives inside the image, at /src
COPY . /src
WORKDIR /src

# The offline test run uses the suite's default SQLite provider and its Northwind database.
# TestConfiguration finds this file by walking up from the test assembly.
RUN cat > /src/UserDataProviders.json <<'JSON'
{
	"NET100": {
		"Providers": [
			"TestNoopProvider",
			"SQLite.MS",
			"Northwind.SQLite.MS"
		]
	}
}
JSON

# linq2db-oss-build: restore-free rebuild of the library and the main test suite (net10.0 only).
# linq2db-oss-test [filter]: run the main suite against the SQLite providers, optionally narrowed by an
# MTP --filter expression. CreateDatabase always runs first because it creates the test databases.
RUN <<'SETUP'
set -eu
cat > /usr/local/bin/linq2db-oss-build <<'SCRIPT'
#!/usr/bin/env bash
set -euo pipefail
cd /src
dotnet build Tests/Linq/Tests.csproj -c Testing --no-restore --disable-build-servers -m:2 -v:minimal "$@"
SCRIPT
cat > /usr/local/bin/linq2db-oss-test <<'SCRIPT'
#!/usr/bin/env bash
set -euo pipefail
cd /src/.build/bin/Tests/Testing/net10.0
if [ $# -gt 0 ]; then
	filter="FullyQualifiedName~CreateData.CreateDatabase|$1"
	shift
else
	filter="TestCategory != SkipCI"
fi
exec dotnet linq2db.Tests.dll --settings /src/.runsettings --filter "$filter" --test-progress "$@"
SCRIPT
chmod +x /usr/local/bin/linq2db-oss-build /usr/local/bin/linq2db-oss-test
SETUP

RUN dotnet restore Tests/Linq/Tests.csproj -p:Configuration=Testing \
    && linq2db-oss-build

# A cheap smoke run so a broken image is visible in the build log; failures do not fail the build.
RUN linq2db-oss-test "FullyQualifiedName~Tests.Linq.WhereTests" \
    || echo "WARNING: smoke tests reported failures"

CMD ["bash"]
