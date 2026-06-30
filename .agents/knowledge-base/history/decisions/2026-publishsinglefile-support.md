---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd242e6399daa081ca
---

# PublishSingleFile deployment support

## Context

linq2db provider detection used Assembly.Location to probe whether a provider DLL was deployed next to the linq2db binary. Under PublishSingleFile, Assembly.Location returns an empty string (IL3000), so provider detection broke and build failures occurred in single-file-published apps (issue #5488). Helper methods Tools.GetFileName/Tools.GetPath(Assembly) also relied on Assembly.Location and were left unused after #5488.

## Decision

Tools.IsProviderAssemblyPresent (renamed from IsAssemblyAvailable) was introduced as the canonical provider detection method (commit a94229f, Jun 11, #5489). It first tries Assembly.Load, then falls back to a file-probe for provider.dll next to the linq2db assembly -- preserving the deployed-provider-wins historical behavior while gating the file-probe on Assembly.Location != empty. Orphaned Tools.GetFileName/GetPath helpers were removed. A Tests.SingleFile console smoke-test project was added to CI gated on with_singlefile_smoke (not on with_tests, per 034c150, Jun 12, #5607).

## Why

The hybrid Load-then-file-probe approach preserves backward compatibility (a deployed-but-unloadable provider still wins, surfacing the real load error) while removing the Assembly.Location dependency. Guarding the file-probe on a non-empty Location means the method compiles and runs correctly under PublishSingleFile without IL3000 warnings in consumers.

## Consequences

- Tools.IsAssemblyAvailable removed; all call sites updated to Tools.IsProviderAssemblyPresent.
- Tools.GetFileName/Tools.GetPath(Assembly) removed.
- Tests.SingleFile smoke project added to CI.
- File: Source/LinqToDB/Tools.cs

## Sources

- Commit a94229f -- Support PublishSingleFile deployments (#5489) (Svyatoslav Danyliv, 2026-06-11)
- Commit 034c150 -- CI: run PublishSingleFile smoke test in build/default only (#5607) (MaceWindu, 2026-06-12)
