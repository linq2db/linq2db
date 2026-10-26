Third-party binaries required for testing. Most are **not** redistributed; one of them is — see below.

- `Devart` : DevArt Oracle provider express edition binaries. Use of those binaries should be enabled in `linq2db.Providers.props`. Test-only.
- `dotMorten.Microsoft.SqlServer.Types` : signed build of [dotMorten.Microsoft.SqlServer.Types](https://www.nuget.org/packages/dotMorten.Microsoft.SqlServer.Types) v1.5.0. Test-only.
- `IBM` : `IDS` and `OCI` versions of `IBM.Data.Informix` .NET Framework provider for tests. Test-only.
- `Oracle` : Assembly for `Oracle.DataAccess.dll` ODP.NET Unmanaged provider (.NET Framework). Test-only.
- `SapHana` : SAP HANA .NET Framework provider (`v4.5/`) and its matching `libadonetHDB` natives for the net462 tests, plus `hdbsql.exe` for database setup. Test-only. The net8+ tests take the client from SAP's `Sap.Data.Hana.Net.v8.0` / `v10.0` packages; no package carries the .NET Framework one.
- `SqlCe` : SQL CE 4.0 Runtimes. **Redistributed** — `System.Data.SqlServerCe.dll` and the `amd64/` natives (which include `Microsoft.VC90.CRT`) are packed into `linq2db.SqlCe` and `linq2db.t4models`.
- `SqlServerTypes` : `SqlServerTypes` 14.0 for .NET Framework testing. Test-only.

The redistributed component is inventoried in [`../Build/licenses/components.json`](../Build/licenses/README.md), which carries the terms it ships under.

- **SqlCe** — settled. These files are byte-identical to the contents of the [`Microsoft.SqlServer.Compact`](https://www.nuget.org/packages/Microsoft.SqlServer.Compact) 4.0.8876.1 package, whose EULA grants Distributable Code rights; the hand-copy here simply left the licence behind. Sourcing them from the package instead of this folder, and deleting this folder, is tracked in [#5861](https://github.com/linq2db/linq2db/issues/5861).
- **SapHana** — no longer redistributed ([#5862](https://github.com/linq2db/linq2db/issues/5862)). `Sap.Data.Hana.v4.5.dll` arrived from a HANA client installation with no accompanying terms, and SAP's own packages for the same provider carry the SAP Developer License Agreement 3.2, whose §2(a) says the software may not be made available to any third party. The T4 packages now rely on the user's HANA client installation.