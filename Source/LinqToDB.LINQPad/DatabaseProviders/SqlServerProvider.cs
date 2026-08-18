using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;

using LinqToDB.Data;

using Microsoft.Data.SqlClient;

#if NETFRAMEWORK
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Microsoft.SqlServer.Types;
#else
using LINQPad.Extensibility.DataContext;

using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SqlServer;
#endif

namespace LinqToDB.LINQPad;

internal sealed class SqlServerProvider : DatabaseProviderBase
{
#if !NETFRAMEWORK
	private const string? Troubleshoot = null;
#else
	// SqlClient loads assembly by strong name and if it is present in GAC - there is no way to override it
	// in any way from running application and user need to use binding redirect policy config
	private const string? Troubleshoot = @"
If you have errors displaying types from Microsoft.SqlServer.Types assembly, add following binding redirect policy (CTRL+SHIFT+O):

<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
      <dependentAssembly>
        <assemblyIdentity name=""Microsoft.SqlServer.Types"" publicKeyToken=""89845dcd8080cc91"" culture=""neutral"" />
        <bindingRedirect oldVersion=""10.0.0.0-17.0.0.0"" newVersion=""17.0.0.0"" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>
";
#endif

	private static readonly IReadOnlyList<ProviderInfo> _providers =
	[
		new (ProviderName.SqlServer    , "Detect Dialect Automatically", true, Troubleshoot: Troubleshoot),
		new (ProviderName.SqlServer2005, "SQL Server 2005 Dialect"           , Troubleshoot: Troubleshoot),
		new (ProviderName.SqlServer2008, "SQL Server 2008 Dialect"           , Troubleshoot: Troubleshoot),
		new (ProviderName.SqlServer2012, "SQL Server 2012 Dialect"           , Troubleshoot: Troubleshoot),
		new (ProviderName.SqlServer2014, "SQL Server 2014 Dialect"           , Troubleshoot: Troubleshoot),
		new (ProviderName.SqlServer2016, "SQL Server 2016 Dialect"           , Troubleshoot: Troubleshoot),
		new (ProviderName.SqlServer2017, "SQL Server 2017 Dialect"           , Troubleshoot: Troubleshoot),
		new (ProviderName.SqlServer2019, "SQL Server 2019 Dialect"           , Troubleshoot: Troubleshoot),
		new (ProviderName.SqlServer2022, "SQL Server 2022 Dialect"           , Troubleshoot: Troubleshoot),
		new (ProviderName.SqlServer2025, "SQL Server 2025 Dialect"           , Troubleshoot: Troubleshoot),
	];

	public SqlServerProvider()
		: base(ProviderName.SqlServer, "Microsoft SQL Server", _providers)
	{
	}

#if NETFRAMEWORK
	private static readonly IReadOnlyList<Assembly> _additionalAssemblies = [typeof(SqlHierarchyId).Assembly];
#else
	private static IReadOnlyList<Assembly>? _additionalAssemblies;
#endif

	public override void ClearAllPools(string providerName)
	{
		SqlConnection.ClearAllPools();
	}

	public override IReadOnlyCollection<Assembly> GetAdditionalReferences(string providerName)
	{
#if NETFRAMEWORK
		return _additionalAssemblies;
#else
		// spatial types package is provisioned only for SQL Server connections, so the assembly cannot be
		// referenced from a static field - that would load it for a connection to any other database too
		if (_additionalAssemblies == null)
		{
			try
			{
				_additionalAssemblies = [DataContextDriver.LoadAssemblySafely("Microsoft.SqlServer.Types.dll")];
			}
			catch (Exception ex)
			{
				Notification.Error(ex, "Failed to load Microsoft.SqlServer.Types assembly.", "SQL Server Provider");
				_additionalAssemblies = [];
			}
		}

		return _additionalAssemblies;
#endif
	}

#if !NETFRAMEWORK
	public override IEnumerable<(string Id, string Version)> GetNuGetPackages(string providerName)
	{
		yield return ("Microsoft.Data.SqlClient", NuGetPackageVersions.Microsoft_Data_SqlClient);

		// Microsoft's spatial types need SqlServerSpatial*.dll, shipped for Windows only, so other systems get
		// the managed reimplementation instead. Both provide the Microsoft.SqlServer.Types assembly, and neither
		// is referenced at compile time by this build, so linq2db and LINQPad load whichever one is provisioned.
		yield return Platform.IsWindows
			? ("Microsoft.SqlServer.Types"        , NuGetPackageVersions.Microsoft_SqlServer_Types)
			: ("dotMorten.Microsoft.SqlServer.Types", NuGetPackageVersions.dotMorten_Microsoft_SqlServer_Types);
	}
#endif

	public override DateTime? GetLastSchemaUpdate(ConnectionSettings settings)
	{
		using var db = new LINQPadDataConnection(settings);
		return db.Query<DateTime?>("SELECT MAX(modify_date) FROM sys.objects").FirstOrDefault();
	}

	public override DbProviderFactory GetProviderFactory(string providerName)
	{
		return SqlClientFactory.Instance;
	}

#if !NETFRAMEWORK
	public override IDataProvider GetDataProvider(string providerName, string connectionString)
	{
		// provider detector fails to detect Microsoft.Data.SqlClient
		// kinda regression in linq2db v5
		return providerName switch
		{
			ProviderName.SqlServer2005 => SqlServerTools.GetDataProvider(SqlServerVersion.v2005, DataProvider.SqlServer.SqlServerProvider.MicrosoftDataSqlClient, connectionString),
			ProviderName.SqlServer2008 => SqlServerTools.GetDataProvider(SqlServerVersion.v2008, DataProvider.SqlServer.SqlServerProvider.MicrosoftDataSqlClient, connectionString),
			ProviderName.SqlServer2012 => SqlServerTools.GetDataProvider(SqlServerVersion.v2012, DataProvider.SqlServer.SqlServerProvider.MicrosoftDataSqlClient, connectionString),
			ProviderName.SqlServer2014 => SqlServerTools.GetDataProvider(SqlServerVersion.v2014, DataProvider.SqlServer.SqlServerProvider.MicrosoftDataSqlClient, connectionString),
			ProviderName.SqlServer2016 => SqlServerTools.GetDataProvider(SqlServerVersion.v2016, DataProvider.SqlServer.SqlServerProvider.MicrosoftDataSqlClient, connectionString),
			ProviderName.SqlServer2017 => SqlServerTools.GetDataProvider(SqlServerVersion.v2017, DataProvider.SqlServer.SqlServerProvider.MicrosoftDataSqlClient, connectionString),
			ProviderName.SqlServer2019 => SqlServerTools.GetDataProvider(SqlServerVersion.v2019, DataProvider.SqlServer.SqlServerProvider.MicrosoftDataSqlClient, connectionString),
			ProviderName.SqlServer2022 => SqlServerTools.GetDataProvider(SqlServerVersion.v2022, DataProvider.SqlServer.SqlServerProvider.MicrosoftDataSqlClient, connectionString),
			ProviderName.SqlServer2025 => SqlServerTools.GetDataProvider(SqlServerVersion.v2025, DataProvider.SqlServer.SqlServerProvider.MicrosoftDataSqlClient, connectionString),
			ProviderName.SqlServer     => SqlServerTools.GetDataProvider(SqlServerVersion.AutoDetect, DataProvider.SqlServer.SqlServerProvider.MicrosoftDataSqlClient, connectionString),
			_                          => base.GetDataProvider(providerName, connectionString),
		};
	}
#endif
}
