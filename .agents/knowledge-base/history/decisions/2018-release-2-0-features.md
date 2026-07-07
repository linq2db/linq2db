---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Release 2.0 / .NET Standard support (PR #1155)

## Context
linq2db 2.0 was a major version bump. The release consolidated multiple new features and breaking changes that had accumulated on the development branch. A primary driver was adding .NET Standard 2.0 support to enable use in .NET Core and Xamarin environments.

## Decision
The 2.0 feature batch was merged via PR #1155 from branch issue-1047-net-standard. Commit subject: Merge pull request #1155 from sdanyliv/issue-1047-net-standard; 93 files changed, 6073 insertions, 3207 deletions.

## Why
The branch name references issue 1047 which tracked .NET Standard 2.0 compatibility. The 2.0 major version signals intentional breaking changes accompanying the new TFM target.

## Consequences
- .NET Standard 2.0 target added to the core library, enabling use in .NET Core and Xamarin contexts.
- Provider abstractions updated to compile under the netstandard TFM.
- Public API adjusted for compatibility across target frameworks.
- 2.0 became the baseline for subsequent netstandard-aware development.

## Sources
- Commit `cc857c5` -- Merge pull request #1155 from sdanyliv/issue-1047-net-standard (MaceWindu, 2018-04-02)
- PR #1155
