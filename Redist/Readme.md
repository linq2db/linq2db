Third-party binaries required for testing. Most are **not** redistributed; two of them are — see below.

- `Devart` : DevArt Oracle provider express edition binaries. Use of those binaries should be enabled in `linq2db.Providers.props`. Test-only.
- `dotMorten.Microsoft.SqlServer.Types` : signed build of [dotMorten.Microsoft.SqlServer.Types](https://www.nuget.org/packages/dotMorten.Microsoft.SqlServer.Types) v1.5.0. Test-only.
- `IBM` : `IDS` and `OCI` versions of `IBM.Data.Informix` .NET Framework provider for tests. Test-only.
- `Oracle` : Assembly for `Oracle.DataAccess.dll` ODP.NET Unmanaged provider (.NET Framework). Test-only.
- `SapHana` : SAP HANA .NET Framework, .NET Core and ODBC providers for T4 testing. **`v4.5/Sap.Data.Hana.v4.5.dll` is redistributed** — `NuGet/NuGet.csproj` copies it into the T4 tools folder and it is packed into `linq2db.SapHana` and `linq2db.t4models`.
- `SqlCe` : SQL CE 4.0 Runtimes. **Redistributed** — `System.Data.SqlServerCe.dll` and the `amd64/` natives (which include `Microsoft.VC90.CRT`) are packed into `linq2db.SqlCe` and `linq2db.t4models`.
- `SqlServerTypes` : `SqlServerTypes` 14.0 for .NET Framework testing. Test-only.

Both redistributed components are inventoried in [`../Build/licenses/components.json`](../Build/licenses/README.md), which carries the terms they ship under.

- **SqlCe** — settled. These files are byte-identical to the contents of the [`Microsoft.SqlServer.Compact`](https://www.nuget.org/packages/Microsoft.SqlServer.Compact) 4.0.8876.1 package, whose EULA grants Distributable Code rights; the hand-copy here simply left the licence behind. Sourcing them from the package instead of this folder, and deleting this folder, is tracked in [#5861](https://github.com/linq2db/linq2db/issues/5861).
- **SapHana** — `redistribution: unresolved`. `Sap.Data.Hana.v4.5.dll` arrived from a HANA client installation with no accompanying terms. SAP's own nuget packages for the same provider carry the SAP Developer License Agreement 3.2, whose §2(a) says the software may not be made available to any third party — so whether it may be packed at all needs a decision. Migrating the .NET-side binaries here to SAP's packages, and that decision, are tracked in [#5862](https://github.com/linq2db/linq2db/issues/5862). See also [#5731](https://github.com/linq2db/linq2db/issues/5731).