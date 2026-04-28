# Test databases

Reference table mapping every test-provider family to its local-development database setup. Consumed by `/test` and `/fix-issue` when they need to stand up a provider before running tests.

## Reading this table

- **Provider family** — the `TestProvName.All<X>` family the test would select with `[IncludeDataSources]`.
- **Provider IDs** — the strings that appear in `UserDataProviders.json` `Providers` arrays, one per version. Multiple IDs map to one container when the container exposes both the native and managed ADO.NET paths.
- **Setup script** — Windows `.cmd` under `Data/Setup Scripts/`. Creates + starts the container (destroys any prior instance with the same name). Must be run from that directory: `cd "Data/Setup Scripts" && <script>.cmd`.
- **Container name** — the `--name` Docker assigns. Used for `docker container inspect <name>` / `docker start <name>` / `docker stop <name>`.
- **Image** — the Docker image tag the script pulls. Used for `docker image inspect <image>` to decide whether the setup script needs to run (pull costs minutes).
- **Preference rank** — which version `/test` proposes by default when the user asks for that family and hasn't picked a specific version. Lower = preferred.

## Preferred provider order (cross-family)

When a test targets "any provider" or the user hasn't picked a family, `/test` proposes providers in this order:

1. **SQLite** — no docker, runs from NuGet packages, always available.
2. **SQL Server** — prefer `SqlServer.2016` / `SqlServer.2016.MS` (typically reachable via a local non-docker SQL Server Express / Developer instance on the dev machine — zero startup cost). Fall back to docker only if the user explicitly asks for docker, or the local instance is unavailable.
3. **PostgreSQL** — prefer `PostgreSQL.18` (latest; slim image).
4. Anything else — propose only if the test specifically requires it.

## Local (non-docker) SQL Server

SQL Server 2016 and earlier are typically already installed on Windows developer machines as a local SQL Server Express / Developer instance — no docker needed and no per-session startup cost. **Prefer the local instance by default** for `SqlServer.2005`, `SqlServer.2008`, `SqlServer.2012`, `SqlServer.2014`, `SqlServer.2016`. Only fall back to docker when the user explicitly asks for docker, or the local instance isn't reachable.

SQL Server 2017+ does not have a supported local Windows installer in the typical dev setup — use docker (Linux images, listed below). The Windows-based `-win` variants exist but aren't recommended (base-image mismatch, see note under the SQL Server table).

## SQLite

No setup needed. Runs against `Microsoft.Data.Sqlite` (provider ID `SQLite.MS`) and `System.Data.SQLite` (provider ID `SQLite.Classic`) via their NuGet packages. No container, no ports.

**Provider IDs:** `SQLite.MS`, `SQLite.Classic`.

## SQL Server

| Version | Provider IDs | Local non-docker? | Setup script | Container | Image | Pref |
|---|---|---|---|---|---|---|
| 2005 | `SqlServer.2005`, `SqlServer.2005.MS` | yes (usually) | `sqlserver2005-win.cmd` | `mssql` | `linq2db/linq2db:win-mssql-2005` | local |
| 2016 | `SqlServer.2016`, `SqlServer.2016.MS` | yes (usually) | `sqlserver2016.cmd` | `sql2016` | `microsoft/mssql-server-2016-express-windows` | **local: 1** |
| 2017 | `SqlServer.2017`, `SqlServer.2017.MS` | no | `sqlserver2017.cmd` | `sql2017` | `linq2db/linq2db:mssql-2017` | docker: 3 |
| 2019 | `SqlServer.2019`, `SqlServer.2019.MS` | no | `sqlserver2019.cmd` | `sql2019` | `linq2db/linq2db:mssql-2019-fts` | docker: 2 |
| 2022 | `SqlServer.2022`, `SqlServer.2022.MS` | no | `sqlserver2022.cmd` | `sql2022` | `linq2db/linq2db:mssql-2022` | docker: 1 |
| 2025 | `SqlServer.2025`, `SqlServer.2025.MS` | no | `sqlserver2025.cmd` | `sql2025` | `linq2db/linq2db:mssql-2025` | docker: latest |
| 2022 (Win) | — | no | `sqlserver2022-win.cmd` | `sql2022` | `linq2db/linq2db:win-mssql-2022` | avoid |
| 2025 (Win) | — | no | `sqlserver2025-win.cmd` | `sql2025` | `linq2db/linq2db:win-mssql-2025` | avoid |

**Picking a version.**
- **Default** → `SqlServer.2016` via the local non-docker instance (no startup cost; already reachable on typical dev machines). Applies to 2005 / 2008 / 2012 / 2014 / 2016.
- **If docker is explicitly requested, or the test depends on a 2017+ feature** → pick the oldest docker version that covers the feature. `sql2022` (latest stable) and `sql2025` (latest) are the usual docker choices; `sql2019` for FTS coverage.
- **`-win` images are avoid-by-default.** Linux images are slimmer, faster to pull, and cover the same SQL surface for everything we test. Only use `-win` when the user explicitly requests it.

## PostgreSQL

Postgres images are slim (~150MB) and fast to start, so running multiple dialect versions in parallel is cheap. The recommended default set covers every SQL dialect linq2db emits, with latest on top.

| Version | Provider IDs | Dialect-anchor? | Setup script | Container | Image | Pref |
|---|---|---|---|---|---|---|
| 9.2 | `PostgreSQL.9.2` | yes | `pgsql92.cmd` | `pgsql92` | `postgres:9.2` | **default** |
| 9.3 | `PostgreSQL.9.3` | yes | `pgsql93.cmd` | `pgsql93` | `postgres:9.3` | **default** |
| 9.5 | `PostgreSQL.9.5` | yes | `pgsql95.cmd` | `pgsql95` | `postgres:9.5` | **default** |
| 10 | — | no | `pgsql10.cmd` | `pgsql10` | `postgres:10` | on demand |
| 11 | — | no | `pgsql11.cmd` | `pgsql11` | `postgres:11` | on demand |
| 12 | — | no | `pgsql12.cmd` | `pgsql12` | `postgres:12` | on demand |
| 13 | `PostgreSQL.13` | yes | `pgsql13.cmd` | `pgsql13` | `postgres:13` | **default** |
| 14 | — | no | `pgsql14.cmd` | `pgsql14` | `postgres:14` | on demand |
| 15 | `PostgreSQL.15` | yes | `pgsql15.cmd` | `pgsql15` | `postgres:15` | **default** |
| 16 | — | no | `pgsql16.cmd` | `pgsql16` | `postgres:16` | on demand |
| 17 | — | no | `pgsql17.cmd` | `pgsql17` | `postgres:17` | on demand |
| 18 | `PostgreSQL.18` | yes | `pgsql18.cmd` | `pgsql18` | `postgres:18` | **default (latest)** |

**Picking a version.**
- **"Default" rows above** are the dialect anchors — each introduces a new PostgreSQL SQL dialect we target, so running one per row gives full dialect coverage.
- **Single-version pick** (smallest useful coverage) → `PostgreSQL.18` — latest, slim, most surface.
- **"On demand" rows** exist as containers for ad-hoc reproduction (e.g. a version-specific bug) and aren't wired into `UserDataProviders.json.template`. Use only when the user asks.

Test provider IDs referenced from code are in `Source/LinqToDB/ProviderName.cs`.

## MySQL / MariaDB

| Flavor | Provider IDs | Setup script | Container | Image |
|---|---|---|---|---|
| MySQL latest | `MySql.8.0.MySql.Data`, `MySql.8.0.MySqlConnector` | `mysql.cmd` | `mysql` | `mysql:latest` |
| MySQL 5.7 | `MySql.5.7.MySql.Data`, `MySql.5.7.MySqlConnector` | `mysql57.cmd` | `mysql57` | `mysql:5.7` |
| MariaDB | `MariaDB.11` (plus connector variants) | `mariadb.cmd` | `mariadb` | `mariadb:latest` |

## Oracle

| Version | Provider IDs | Setup script | Container | Image | Pref |
|---|---|---|---|---|---|
| 11 | `Oracle.11.Native`, `Oracle.11.Managed`, `Oracle.11.Devart` | `oracle11.cmd` | `oracle11` | `datagrip/oracle:11.2` | **default (slim)** |
| 12 | `Oracle.12.Native`, `Oracle.12.Managed`, `Oracle.12.Devart` | `oracle12.cmd` | `oracle12` | `datagrip/oracle:12.2.0.1-se2-directio` | **default (lowest dialect)** |
| 18 | `Oracle.18.Native`, `Oracle.18.Managed`, `Oracle.18.Devart` | `oracle18.cmd` | `oracle18` | `container-registry.oracle.com/database/express:18.4.0-xe` | on demand |
| 19 | `Oracle.19.Native`, `Oracle.19.Managed`, `Oracle.19.Devart` | `oracle19.cmd` | `oracle19` | `oracledb19c/oracle.19.3.0-ee:oracle19.3.0-ee` | on demand |
| 21 | `Oracle.21.Native`, `Oracle.21.Managed`, `Oracle.21.Devart` | `oracle21.cmd` | `oracle21` | `container-registry.oracle.com/database/express:21.3.0-xe` | on demand |
| 23 | `Oracle.23.Native`, `Oracle.23.Managed`, `Oracle.23.Devart` | `oracle23.cmd` | `oracle23` | `container-registry.oracle.com/database/free:23.2.0.0` | on demand |

**Picking a version.** Default to `oracle11` + `oracle12`:
- `oracle11` — slim image, easy startup.
- `oracle12` — lowest version whose dialect we actually emit distinctly; Oracle 12 dialect is reused for 18 / 19 / 21 / 23 since we don't yet have distinct dialect implementations for those.

Oracle 18+ images are large and add very little test value until per-version dialects land — propose them only when the user explicitly asks.

## Firebird

| Version | Provider IDs | Setup script | Container | Image | Pref |
|---|---|---|---|---|---|
| 2.5 | `Firebird.2.5` | `firebird25.cmd` | `firebird25` | `jacobalberty/firebird:2.5-sc` | **default** |
| 3 | `Firebird.3` | `firebird30.cmd` | `firebird30` | `jacobalberty/firebird:v3` | **default** |
| 4 | `Firebird.4` | `firebird40.cmd` | `firebird40` | `jacobalberty/firebird:v4` | **default** |
| 5 | `Firebird.5` | `firebird50.cmd` | `firebird50` | `jacobalberty/firebird:v5` | **default** |

**Picking a version.** Run all four by default — the images are slim and each maps to a distinct dialect we emit (2.5 / 3 / 4 / 5), so full-matrix coverage is cheap. Drop to a single version only when the user explicitly narrows the scope.

## ClickHouse

| Provider | Provider IDs | Setup script | Container | Image | Pref |
|---|---|---|---|---|---|
| ClickHouse | `ClickHouse.Client`, `ClickHouse.MySql`, `ClickHouse.Octonica` | `clickhouse.cmd` | `clickhouse` | `clickhouse/clickhouse-server:latest` | **default (all 3 test providers)** |

**Picking a version.** Single container exposes all three linq2db ClickHouse test providers (`Client` / `MySql` / `Octonica`). Run all three by default — they share one container, so the cost is the same as running one.

## YDB

| Provider | Provider IDs | Setup script | Container | Image | Pref |
|---|---|---|---|---|---|
| YDB | `YDB` | `ydb.cmd` | `ydb` | `ydbplatform/local-ydb:latest` | on demand |

## Heavy providers (ask first)

These containers either take a long time to initialize, use a lot of RAM, or pull very large images. `/test` and `/fix-issue` must **confirm with the user** before proposing or starting any of them, even when the test scope would naturally include them.

| Provider | Provider IDs | Setup script | Container | Image | Cost |
|---|---|---|---|---|---|
| DB2 | `DB2` | `db2.cmd` | `db2` | `icr.io/db2_community/db2:latest` | slow startup, ~2GB image, high RAM |
| Informix 14 | `Informix.DB2` | `informix14.cmd` | `informix14` | `icr.io/informix/informix-developer-database:latest` | slow startup, large image |
| SAP HANA 2 | `SapHana.Native`, `SapHana.Odbc` | `saphana2.cmd` | `hana2` | `saplabs/hanaexpress:latest` | very slow startup (~5–10 min), very high RAM |
| SAP ASE | `Sybase`, `Sybase.Managed` | `sybase-ase.cmd` | `sybase` | `linq2db/linq2db:ase-16.1` | slow startup |

Confirmation prompt should spell out the expected cost (startup time / memory) so the user can decide whether to skip that provider for the session.

## Docker lifecycle (for skills)

Before running tests against a non-SQLite provider, a skill should go through this sequence:

1. `docker image inspect <image>` — succeeds if the image layer is cached locally. Failure means the setup script needs to run (which pulls the image).
2. `docker container inspect <container>` — succeeds with status `running`, `exited`, or `created`; fails if the container doesn't exist.
3. Decision tree:
   - **Container missing** — ask the user whether to run `Data/Setup Scripts/<script>.cmd` (recreates from scratch). Record that the skill initiated startup.
   - **Container exited/created** — `docker start <container>`. Record that the skill initiated startup.
   - **Container running** — use as-is; do not touch.
4. After tests finish, if the skill initiated startup during this session, ask the user whether to `docker stop <container>`. Default to **leave running** — the user may want follow-up runs.

Ports are fixed per script (see `-p <host>:<guest>` above) and don't need verification — if the container is running, the port is bound.

## Keeping this doc current

When a new setup script is added to `Data/Setup Scripts/` or a container name changes, update this table and the preferred-provider rank. The source of truth for provider ID strings is `Source/LinqToDB/ProviderName.cs`; the source of truth for scripts is `Data/Setup Scripts/readme.md`. This doc is the cached join of the two — regenerate by grepping the `.cmd` files for `docker run … --name` and cross-referencing.
