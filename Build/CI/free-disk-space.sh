#!/bin/bash
# Free disk space on a hosted Linux agent so the large-DB legs (Oracle, SAP HANA) fit.
#
# Extracted from the inline block in test-workflow-linux.yml so the GitHub Actions legs run the same
# thing. Azure and GitHub use the same ubuntu image (actions/runner-images), so every path below is
# valid on both - the original derived from
# https://github.com/apache/flink/blob/master/tools/azure-pipelines/free_disk_space.sh
#
# Each step logs what it actually freed (avail before -> after) rather than trusting the ~sizes below,
# which were measured on Azure build 22224 and will drift as the image changes.
#
# Enabled (each frees >= 1 GB): ~23.6 GB total, taking avail from ~15 GB to ~39 GB.
# Left out on purpose:
#   apt clean      ~0.1 GB, below the threshold
#   remove dotnet  ~2-3 GB, but breaks every leg - the system dotnet is used before the SDK install

avail()  { df --output=avail -m / | tail -1 | tr -d ' '; }
report() { local a=$(avail); echo ">>> $2: freed $(( a - $1 )) MB (avail now $a MB)"; }

b=$(avail); sudo apt-get remove -y '^llvm-.*';        report $b "remove llvm (~1.4Gb)"
b=$(avail); sudo rm -rf /usr/local/lib/android;       report $b "remove android (~10Gb)"
b=$(avail); sudo rm -rf /usr/local/.ghcup/;           report $b "remove ghcup (~3.7Gb)"
b=$(avail); sudo rm -rf /opt/hostedtoolcache/CodeQL;  report $b "remove CodeQL (~1.7Gb, unused)"
b=$(avail); sudo rm -rf /usr/share/swift;             report $b "remove swift (~3.4Gb, unused)"
b=$(avail); sudo rm -rf /usr/lib/jvm;                 report $b "remove jvm (~1.4Gb, unused)"
# Safe here because it runs before any DB container exists. It prints its own "Total reclaimed space"
# line as well as the avail delta.
b=$(avail); sudo docker image prune -af;              report $b "docker image prune (~1.9Gb)"

df -h /

# No `set -e`: freeing space is best-effort. A path that has moved in a newer image should log a smaller
# delta, not fail the leg before a single test runs.
exit 0
