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
> but proven (or strongly suspected) to be a *fidelity gap* — the engine supports the feature.
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
| STDDEV_POP/SAMP, VAR_POP/SAMP | ✓ | ✓ | ✓ | ✓ | ✓ | ✗ | ✓ | ✓ | ✓ | ✓ | ✓ⁱ | ✗ | ✗ | ✗ |
| bare STDDEV / VARIANCE | ✓ | ✓ | ✓ | ✓ | ✓ | ✗ | ✗ | ✓ | ✓ | ✓ | ✓ | ✗ | ✗ | ✗ |
| COVAR_POP/SAMP, CORR | ✗ | ✓ | ✓ | ✗ | ✗ | ✗ | ✓ | ✓ | ✓ | ✓ʰ | ✗ | ✗ | ✗ | ✗ |
| REGR_* (9 functions) | ✗ | ✓ | ✓ | ✗ | ✗ | ✗ | ✗ | ✓ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ |
| MEDIAN | ✗ | ✗ | ✓ | ✗ | ✓ | ✗ | ✗† | ✓ | ✓ | ✓ | ✗ | ✗ | ✗ | ✗ |
| RATIO_TO_REPORT | ~ | ~ | ✓ | ~ | ~ | ~ | ~ | ~ | ✓ | ~ | ~ | ~ | ~ | ~ |
| PERCENTILE_CONT/DISC — `WITHIN GROUP` (group) | ✗ | ✓ | ✓ | ✗ | ✗ | ✗ | ✗ | ✓ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ |
| PERCENTILE_CONT/DISC — windowed `OVER` | ✓ | ✗ | ✓ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| Hypothetical-set RANK/… `WITHIN GROUP` | ✗ | ✓ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| KEEP (DENSE_RANK FIRST/LAST) | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |

`RATIO_TO_REPORT` is native on Oracle/DB2 and emulated everywhere else as `x / SUM(x) OVER (…)`
(verified: the PostgreSQL baseline emits `x::Float / SUM(x) OVER (PARTITION BY …)`).
`STDDEV` is spelled `STDEV` on SQL Server via `StdDevFunctionName`; bare `VARIANCE` is spelled `VAR` on
SQL Server and SAP HANA via `VarianceFunctionName`.

The grouped statistical rows hide per-function asymmetry, marked above:
- **ⁱ Informix** supports `STDDEV_POP/SAMP` and bare `STDDEV`/`VARIANCE`, but **not** `VAR_POP`/`VAR_SAMP`
  (`IsStdDevSupported`/`IsVarianceBareSupported` on, `IsVarianceSupported` off).
- **ʰ SAP HANA** supports `CORR` but **not** `COVAR_POP`/`COVAR_SAMP` (`IsCovarianceSupported` off).


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
| Ordered-set **`MODE() WITHIN GROUP`** | **✗ — considered for future** | Non-standard provider extension (PostgreSQL `mode()`, Oracle `STATS_MODE`); not implemented, **considered for a future release** (see below) |
| Hypothetical-set RANK/DENSE_RANK/PERCENT_RANK/CUME_DIST `WITHIN GROUP` | ✓ | on `IEnumerable<T>` |
| Aggregate `DISTINCT` modifier | ✓ | `Distinct()` (gated per provider) |
| KEEP (DENSE_RANK FIRST/LAST) — Oracle extension | ✓ | `KeepFirst()` / `KeepLast()` |

**Not implemented:**
1. **`MODE() WITHIN GROUP (ORDER BY …)`** — a non-standard ordered-set mode aggregate (not part of the
   SQL standard; offered as a provider extension — PostgreSQL `mode()`, Oracle `STATS_MODE`, and others
   with diverging syntax). No `Sql.Window.Mode(…)` exists today; **considered for a future release**.
2. **`EXCLUDE NO OTHERS`** — the explicit form of the default frame-exclusion (this one *is* SQL-standard);
   cosmetic, not worth adding.
