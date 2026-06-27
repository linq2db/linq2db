# Window Functions — Provider Capability Matrix

Reference for the `Sql.Window.*` API (see `Sql.Window.cs`) describing which window functions and
clauses each database provider / dialect supports. Generated from the capability flags on
`WindowFunctionsMemberTranslator` and its per-provider subclasses (PR #5468).

**How gating works.** Each provider's `…WindowFunctionsMemberTranslator` overrides virtual
`Is…Supported` flags. When a feature is unsupported the translator throws a descriptive
`LinqToDBException` **at translation time** (it never sends invalid SQL to the database). A few
clauses are *emulated* rather than rejected (noted below).

Legend: **✓** native · **~** emulated · **✗** rejected at translate time (unsupported) · **—** n/a.

> §2–4 show **current behaviour** (what the API does today). Cells marked **✗†** are rejected today
> but proven (or strongly suspected) to be a *fidelity gap* — the engine supports the feature; see
> [§5 Divergences](#5-divergences--open-questions) for the per-function verified detail.
> Cells marked **✓ᵗ** / **✗ᵗ** were proven against a live instance.

---

## 1. Provider / dialect coverage

Providers with **no window-function support at all** (`IsWindowFunctionsSupported => false`):
**SQL Server 2005/2008**, **MySQL 5.7**, **Firebird 2.5**, **Sybase ASE**, **Access**, **SQL CE**.
(SQL Server 2005/2008 supports the four ranking functions natively, but linq2db always emits an
`ORDER BY` inside `OVER`, which 2005/2008 only allow for ranking functions — so aggregate windows
are rejected and the whole feature is gated off conservatively.)

Dialect splits that matter:

| Provider | Dialects with distinct capabilities |
|---|---|
| SQL Server | **≤2008** (none) · **2012–2019** · **2022/2025** (adds `IGNORE`/`RESPECT NULLS`) |
| Firebird   | **2.5** (none) · **3** · **4 / 5 / 6** (adds ROWS/RANGE frames, NTILE, PERCENT_RANK, CUME_DIST) |
| MySQL      | **5.7** (none) · **8.0** · **MariaDB 10.3+** (adds windowed PERCENTILE_* and MEDIAN) |

---

## 2. Ranking & navigation functions

| Function | SqlSrv 2012+ | PG | Oracle | MySQL 8 | MariaDB | SQLite | ClickHouse | DuckDB | DB2 | SAP HANA | Informix | FB 3 | FB 4–6 | YDB |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| ROW_NUMBER | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| RANK / DENSE_RANK | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| PERCENT_RANK | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✗ | ✓ | ✓ |
| CUME_DIST | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✗ | ✓ | ✓ | ✓ | ✓ | ✗ | ✓ | ✓ |
| NTILE | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✗ | ✓ | ✓ |
| LEAD / LAG | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| LEAD/LAG default-value arg | ✓ | ✓ | ✓ | ✓ | ✗ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✗ |
| FIRST_VALUE / LAST_VALUE | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| NTH_VALUE | ✗ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✗ | ✓ | ✓ | ✓ |

Notes: SQL Server has no `NTH_VALUE` (any version). MariaDB & YDB reject the LEAD/LAG 3-argument
(default-value) overload. ClickHouse has no `CUME_DIST`.

---

## 3. Aggregate, statistical & ordered-set functions

| Function | SqlSrv 2012+ | PG | Oracle | MySQL 8 | MariaDB | SQLite | ClickHouse | DuckDB | DB2 | SAP HANA | Informix | FB 3 | FB 4–6 | YDB |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| SUM/AVG/MIN/MAX/COUNT OVER | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `DISTINCT` in window agg | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✓ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| STDDEV_POP/SAMP, VAR_POP/SAMP | ✗† | ✓ | ✓ | ✓ | ✓ | ✗ | ✓ | ✓ | ✓ | ✗† | ✗† | ✗ | ✗ | ✗ |
| bare STDDEV / VARIANCE | ✗† | ✓ | ✓ | ✓ | ✓ | ✗ | ✗ | ✓ | ✓ | ✗† | ✗† | ✗ | ✗ | ✗ |

(Grouped statistical rows hide per-function asymmetry for SAP HANA & Informix — e.g. HANA has
`STDDEV_POP/SAMP`+`VAR_POP/SAMP` but not bare `VARIANCE`; Informix has `STDDEV*`+bare `VARIANCE` but
not `VAR_POP/SAMP`. See §5 for the exact per-function verified results.)
| COVAR_POP/SAMP, CORR | ✗ | ✓ | ✓ | ✗ | ✗ | ✗ | ✓ | ✓ | ✓ | ✗† | ✗ | ✗ | ✗ | ✗ |
| REGR_* (9 functions) | ✗ | ✓ | ✓ | ✗ | ✗ | ✗ | ✗ | ✓ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ |
| MEDIAN | ✗ | ✗ | ✓ | ✗ | ✓ | ✗ | ✗† | ✓ | ✓ | ✗† | ✗ | ✗ | ✗ | ✗ |
| RATIO_TO_REPORT | ~ | ~ | ✓ | ~ | ~ | ~ | ~ | ~ | ✓ | ~ | ~ | ~ | ~ | ~ |
| PERCENTILE_CONT/DISC — `WITHIN GROUP` (group) | ✗ | ✓ | ✓ | ✗ | ✗ | ✗ | ✗ | ✓ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ |
| PERCENTILE_CONT/DISC — windowed `OVER` | ✓ | ✗ | ✓ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗† | ✗ | ✗ | ✗ | ✗ |
| Hypothetical-set RANK/… `WITHIN GROUP` | ✗ | ✓ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| KEEP (DENSE_RANK FIRST/LAST) | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |

`RATIO_TO_REPORT` is native on Oracle/DB2 and emulated everywhere else as `x / SUM(x) OVER (…)`
(verified: the PostgreSQL baseline emits `x::Float / SUM(x) OVER (PARTITION BY …)`).
`STDDEV` is spelled `STDEV` on SQL Server / Sybase via `StdDevFunctionName`.

---

## 4. Frame & clause options

| Clause | SqlSrv 2012+ | PG | Oracle | MySQL 8 | MariaDB | SQLite | ClickHouse | DuckDB | DB2 | SAP HANA | Informix | FB 3 | FB 4–6 | YDB |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| ROWS frame | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | **✓ᵗ** | ✓ | ✗ | ✓ | ✓ |
| RANGE frame | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | **✗ᵗ** | ✓ | ✗ | ✓ | ✗ |
| GROUPS frame | ✗ | ✓ | ✗ | ✗ | ✗ | ✓ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| Frame `EXCLUDE` | ✗ | ✓ | ✗ | ✗ | ✗ | ✓ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| `FILTER (WHERE …)` | ~ | ✓ | ~ | ~ | ~ | ~ | ~ | ✓ | ~ | ~ | ~ | ~ | ~ | ~ |
| `FILTER` on ordered-set agg | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| `IGNORE/RESPECT NULLS` — LEAD/LAG | ✓²² | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✓ | ✓ | ✗ | ✓ | ✗ | ✗ | ✗ |
| `IGNORE/RESPECT NULLS` — value fns | ✓²² | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✓ | ✓ | ✗ | ✓ | ✗ | ✗ | ✓ |
| `FROM FIRST/LAST` (NthValue) | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ | ✓ | ✓ | ✗ |
| `NULLS FIRST/LAST` ordering | ~ | ✓ | ✓ | ~ | ~ | ~ | ~ | ✓ | ~ | ~ | ~ | ~ | ✓ | ~ |

`²²` = SQL Server 2022/2025 only. `FILTER` is native on PostgreSQL & DuckDB and `CASE WHEN`-emulated
elsewhere. `NULLS FIRST/LAST` is native where the provider supports it and emulated via a
`CASE WHEN expr IS NULL …` sort key elsewhere (skipped when the requested position already matches
the provider's natural NULL ordering, and for non-nullable keys).

---

## 5. Divergences & open questions

Candidate fidelity gaps where the gate may not match the real database. **⚠ verify** cells above.

### Confirmed gaps

- **SQL Server (2012+) statistical window aggregates** — `STDEV`/`STDEVP`/`VAR`/`VARP` are valid
  window aggregates on SQL Server, but `IsVarianceSupported`/`IsVarianceBareSupported` are `false`,
  so `Sql.Window.StdDev/Variance/…` throw. Enabling them needs per-name mapping (the base emits the
  standard `STDDEV_POP`/`VAR_POP`/… names SQL Server rejects), not just flipping the flags. There is
  also a dead `StdDevFunctionName => "STDEV"` override that is never reached today (raised as SUG013
  on the PR). *Net: SQL Server users cannot compute windowed variance/stddev through this API.*

### Resolved during review (PR #5468)

- **SAP HANA ROWS frame** — was gated off; verified supported against a live HANA instance and
  enabled (`✓ᵗ`). **RANGE/EXCLUDE/GROUPS** confirmed *unsupported* on HANA (`✗ᵗ` — HANA raises
  `feature not supported: Window frame specification of RANGE not allowed`).

### Verified by test-proofing (this audit, against live instances)

Each candidate was probed by flipping the gate and executing `Sql.Window.*` against the live DB
(`✓ᵗ` = DB accepted, `✗ᵗ` = DB rejected with the noted error).

**SAP HANA — real fidelity gaps (gated off, but the engine supports them):**

| Function | Result | Note |
|---|---|---|
| `STDDEV`, `STDDEV_POP`, `STDDEV_SAMP` | **✓ᵗ supported** | currently rejected by `IsVariance*Supported => false` |
| `VAR_POP`, `VAR_SAMP` | **✓ᵗ supported** | currently rejected |
| `CORR` | **✓ᵗ supported** | currently rejected by `IsCorrelationSupported => false` |
| `MEDIAN` | **✓ᵗ supported** | currently rejected by `IsMedianSupported => false` |
| `PERCENTILE_CONT/DISC` (windowed `OVER`) | **✓ᵗ supported** | currently rejected; the group-`WITHIN GROUP` projection forms hit linq2db query-shape errors (`not a GROUP BY expression` / `LATERAL`) so leave the group form off |
| bare `VARIANCE` | ✗ᵗ | `feature not supported: not supported window function` (HANA has no bare `VARIANCE`) |
| `COVAR_POP`/`COVAR_SAMP` | ✗ᵗ | `not supported window function` |
| `REGR_*` | ✗ᵗ | `not supported window function` |

**Informix — partial fidelity gap (asymmetric):**

| Function | Result |
|---|---|
| `STDDEV`, `STDDEV_POP`, `STDDEV_SAMP` | **✓ᵗ supported** (gated off) |
| bare `VARIANCE` | **✓ᵗ supported** (gated off) |
| `VAR_POP`, `VAR_SAMP` | ✗ᵗ syntax error |
| `CORR`, `COVAR_POP/SAMP`, `REGR_*` | ✗ᵗ syntax error |
| `MEDIAN` | ✗ᵗ `Not implemented yet` |

**ClickHouse & DuckDB — current gating confirmed correct:**

- ClickHouse: bare `STDDEV`/`VARIANCE` ✗ᵗ (`… does not exist` — ClickHouse only has `stddevPop/Samp`,
  `varPop/Samp`), `REGR_*` ✗ᵗ. `MEDIAN` ✗ᵗ — ClickHouse *does* have a median aggregate but spelled
  lowercase `median`; linq2db emits `MEDIAN`, which ClickHouse rejects (`Unknown aggregate function
  MEDIAN. Maybe you meant: ['median','medianDD']`). A future enablement would need lowercase emission.
- DuckDB: windowed `PERCENTILE_CONT/DISC` ✗ᵗ (`Aggregate Function … percentile_cont does not exist` —
  DuckDB uses `quantile_cont`), hypothetical-set RANK/… ✗ᵗ (`Unknown ordered aggregate`).

**Flag-granularity caveat for enabling the HANA/Informix gaps.** The support is *asymmetric* in a way
the current coarse flags can't express: `IsVarianceSupported` gates `STDDEV_POP`/`STDDEV_SAMP`/`VAR_POP`/`VAR_SAMP`
as one group, and `IsVarianceBareSupported` gates bare `STDDEV`+`VARIANCE` together — but HANA wants
`STDDEV*`+`VAR_POP/SAMP`+bare-`STDDEV` *without* bare-`VARIANCE`, and Informix wants `STDDEV*`+bare-`VARIANCE`
*without* `VAR_POP/SAMP`. Cleanly enabling these requires finer-grained flags (or per-function name
mapping), so they are documented here rather than enabled in this PR. Same coarse-flag issue blocks
the SQL Server variance gap in §5.1.

(The **GROUPS-frame ✗** on Oracle / DB2 / SQL Server / MySQL / ClickHouse and **frame-EXCLUDE ✗** on
those providers are believed correct — those engines lack the SQL:2011 `GROUPS` frame and `EXCLUDE`
clause — and were not individually re-proven here.)

---

## 6. API-completeness vs the SQL standard

Standard window grammar (SQL:2016) productions and whether the `Sql.Window` API exposes them:

| Production | Exposed? | Notes |
|---|---|---|
| Ranking (ROW_NUMBER/RANK/DENSE_RANK/PERCENT_RANK/CUME_DIST/NTILE) | ✓ | |
| Navigation (LEAD/LAG/FIRST_VALUE/LAST_VALUE/NTH_VALUE) | ✓ | with offset/default overloads |
| Aggregates as window functions | ✓ | incl. statistical/regression families |
| `PARTITION BY` / `ORDER BY` | ✓ | |
| Frame `ROWS` / `RANGE` / `GROUPS` | ✓ | `RowsBetween` / `RangeBetween` / `GroupsBetween` |
| Frame `BETWEEN … AND …`, explicit PRECEDING/FOLLOWING direction | ✓ | `ValuePreceding`/`ValueFollowing` + `…BetweenValues` shortcuts |
| Frame `EXCLUDE CURRENT ROW / GROUP / TIES` | ✓ | `ExcludeCurrentRow()` / `ExcludeGroup()` / `ExcludeTies()` |
| Frame `EXCLUDE NO OTHERS` | **✗ gap** | the default; not separately exposable. Minor — harmless to omit. |
| `FILTER (WHERE …)` | ✓ | native + CASE-WHEN emulation |
| `IGNORE / RESPECT NULLS` | ✓ | `IgnoreNulls()` / `RespectNulls()` |
| `FROM FIRST / FROM LAST` (NTH_VALUE) | ✓ | `FromFirst()` / `FromLast()` |
| Named window reuse (`WINDOW w AS (…)`) | ✓ | `DefineWindow` + `UseWindow` |
| Ordered-set `PERCENTILE_CONT/DISC WITHIN GROUP` | ✓ | group + windowed forms |
| Ordered-set **`MODE() WITHIN GROUP`** | **✗ gap** | SQL-standard ordered-set aggregate (PostgreSQL/Oracle/…); not implemented |
| Hypothetical-set RANK/DENSE_RANK/PERCENT_RANK/CUME_DIST `WITHIN GROUP` | ✓ | on `IEnumerable<T>` |
| Aggregate `DISTINCT` modifier | ✓ | `Distinct()` (gated per provider) |
| KEEP (DENSE_RANK FIRST/LAST) — Oracle extension | ✓ | `KeepFirst()` / `KeepLast()` |

**Standard productions not implemented at all:**
1. **`MODE() WITHIN GROUP (ORDER BY …)`** — the SQL-standard ordered-set mode aggregate. Supported by
   PostgreSQL, Oracle, and others; no `Sql.Window.Mode(…)` exists.
2. **`EXCLUDE NO OTHERS`** — the explicit form of the default frame-exclusion; cosmetic, not worth adding.

---

## 7. Test-proof status

- **Proven against live instances:**
  - SAP HANA ROWS frame (supported) and RANGE/EXCLUDE/GROUPS (rejected) — `WindowFunctionsTests` 307/307.
  - SAP HANA statistical / percentile / median (this audit) — see §5; `STDDEV*`/`VAR_POP/SAMP`/`CORR`/
    `MEDIAN`/windowed-`PERCENTILE` supported; bare `VARIANCE`/`COVAR`/`REGR_*` rejected.
  - Informix statistical (this audit) — `STDDEV*`+bare-`VARIANCE` supported; the rest rejected.
  - ClickHouse `MEDIAN`/bare-stats/`REGR_*` and DuckDB windowed-percentile/hypothetical-set — all
    confirmed *rejected* by the engine, so the current gating is correct.
- **Not re-proven (assumed correct):** the GROUPS-frame / frame-EXCLUDE ✗ cells on the providers that
  lack those SQL:2011 features (Oracle/DB2/SQL Server/MySQL/ClickHouse).
- **Follow-up to enable the proven gaps** (SAP HANA + Informix statistical/percentile/median, SQL Server
  variance) needs finer-grained capability flags than the current coarse `IsVariance*Supported` pair,
  plus the usual per-test `[ThrowsForProvider]` reconciliation and baseline regeneration — see §5.
