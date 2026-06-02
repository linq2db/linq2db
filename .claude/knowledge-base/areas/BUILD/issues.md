---
area: BUILD
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-06-01
last_verified_sha: 2e67bafc9bfc8ae8ba573b93bde8671d9920c95d
---

# BUILD -- GitHub themes

## Open themes

- **Analyzer rule resolution** -- deferred Meziantou.Analyzer rules from 6.3.0 prep require systematic cleanup and rule enable/disable decisions; ongoing consolidation of analyzer configuration across Debug/Release/Testing builds. Sample: #5573.
- **Native DB client loading in CI** -- Linux/macOS DB2/Informix native client library loading instability in CI requires explicit resolver configuration and startup-sequence hardening. Sample: #5563.

## Resolved themes

- **Release preparation and versioning** -- coordinated dependency updates, PublicAPI baselines, analyzer-rule enforcement, and version bumps across patch/minor releases (6.3.0 prep, 6.4.0 bump). Sample: #5533, #5536.
- **Build failure quick-fix cycles** -- intermittent transient master breakage requiring rapid cross-platform stabilization (baselines reset on retry, native-lib resolver fallbacks). Sample: #5491, #5537, #5572.
- **Build-time analyzer overhead** -- IDE/ReportAnalyzer/XML-doc generation gated to Release builds to improve Debug/Testing iteration time. Sample: #5516.

## Active discussions

None currently open.

## Stats

- Open issues: 0
- Closed issues: 0
- Open PRs: 2
- Total PRs: 9
- Discussions: 0
- Last fetched: 2026-06-01

<details><summary>Coverage</summary>

- Index entries scanned: 9 (0 issues + 9 PRs + 0 discussions)
- Themes extracted: 2 open, 3 resolved
</details>
