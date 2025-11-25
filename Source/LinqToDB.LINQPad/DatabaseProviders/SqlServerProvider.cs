using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;

#if NETFRAMEWORK
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#endif
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SqlServer;

using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Types;

namespace LinqToDB.LINQPad;

internal sealed class SqlServerProvider : DatabaseProviderBase
{
#if NETFRAMEWORK
	static SqlServerProvider()
	{
		var oldCurrent = Directory.GetCurrentDirectory();
		var newCurrent = Path.GetDirectoryName(_additionalAssemblies[0].Location);

		if (oldCurrent != newCurrent)
			Directory.SetCurrentDirectory(newCurrent);

		// This will trigger GLNativeMethods .cctor, which loads native runtime for spatial types from relative path
		// We need to reset current directory before it otherwise it fails to find runtime dll as LINQPad 5 default directory is LINQPad directory, not driver's dir
		var type = _additionalAssemblies[0].GetType("Microsoft.SqlServer.Types.GLNativeMethods");
		RuntimeHelpers.RunClassConstructor(type.TypeHandle);

		if (oldCurrent != newCurrent)
			Directory.SetCurrentDirectory(oldCurrent);
	}

	[DllImport("kernel32", SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	private static extern bool FreeLibrary(IntPtr hModule);

	public override void Unload()
	{
		// must unload unmanaged dlls to allow LINQPad 5 to delete old driver instance without error
		// as SQL Server types lib doesn't implement cleanup for unloadable domains...
		foreach (ProcessModule mod in Process.GetCurrentProcess().Modules)
			if (string.Equals(mod.ModuleName, "SqlServerSpatial170.dll", StringComparison.OrdinalIgnoreCase))
				FreeLibrary(mod.BaseAddress);
	}
#endif

	private static readonly IReadOnlyList<ProviderInfo> _providers =
	[
		new (ProviderName.SqlServer    , "Detect Dialect Automatically", true),
		new (ProviderName.SqlServer2005, "SQL Server 2005 Dialect"           ),
		new (ProviderName.SqlServer2008, "SQL Server 2008 Dialect"           ),
		new (ProviderName.SqlServer2012, "SQL Server 2012 Dialect"           ),
		new (ProviderName.SqlServer2014, "SQL Server 2014 Dialect"           ),
		new (ProviderName.SqlServer2016, "SQL Server 2016 Dialect"           ),
		new (ProviderName.SqlServer2017, "SQL Server 2017 Dialect"           ),
		new (ProviderName.SqlServer2019, "SQL Server 2019 Dialect"           ),
		new (ProviderName.SqlServer2022, "SQL Server 2022 Dialect"           ),
	];

	public SqlServerProvider()
		: base(ProviderName.SqlServer, "Microsoft SQL Server", _providers)
	{
	}

	private static readonly IReadOnlyList<Assembly> _additionalAssemblies = [typeof(SqlHierarchyId).Assembly];

	public override void ClearAllPools(string providerName)
	{
		SqlConnection.ClearAllPools();
	}

	public override IReadOnlyCollection<Assembly> GetAdditionalReferences(string providerName)
	{
		return _additionalAssemblies;
	}

	public override DateTime? GetLastSchemaUpdate(ConnectionSettings settings)
	{
		using var db = new LINQPadDataConnection(settings);
		return db.Query<DateTime?>("SELECT MAX(modify_date) FROM sys.objects").FirstOrDefault();
	}

	public override DbProviderFactory GetProviderFactory(string providerName)
	{
		return SqlClientFactory.Instance;
	}

	public override IDataProvider GetDataProvider(string providerName, string connectionString)
	{
		// provider detector fails to detect Microsoft.Data.SqlClient
		// kinda regression in linq2db v5
		return providerName switch
		{
			ProviderName.SqlServer2005 => SqlServerTools.GetDataProvider(SqlServerVersion.v2005     , DataProvider.SqlServer.SqlServerProvider.MicrosoftDataSqlClient, connectionString),
			ProviderName.SqlServer2008 => SqlServerTools.GetDataProvider(SqlServerVersion.v2008     , DataProvider.SqlServer.SqlServerProvider.MicrosoftDataSqlClient, connectionString),
			ProviderName.SqlServer2012 => SqlServerTools.GetDataProvider(SqlServerVersion.v2012     , DataProvider.SqlServer.SqlServerProvider.MicrosoftDataSqlClient, connectionString),
			ProviderName.SqlServer2014 => SqlServerTools.GetDataProvider(SqlServerVersion.v2014     , DataProvider.SqlServer.SqlServerProvider.MicrosoftDataSqlClient, connectionString),
			ProviderName.SqlServer2016 => SqlServerTools.GetDataProvider(SqlServerVersion.v2016     , DataProvider.SqlServer.SqlServerProvider.MicrosoftDataSqlClient, connectionString),
			ProviderName.SqlServer2017 => SqlServerTools.GetDataProvider(SqlServerVersion.v2017     , DataProvider.SqlServer.SqlServerProvider.MicrosoftDataSqlClient, connectionString),
			ProviderName.SqlServer2019 => SqlServerTools.GetDataProvider(SqlServerVersion.v2019     , DataProvider.SqlServer.SqlServerProvider.MicrosoftDataSqlClient, connectionString),
			ProviderName.SqlServer2022 => SqlServerTools.GetDataProvider(SqlServerVersion.v2022     , DataProvider.SqlServer.SqlServerProvider.MicrosoftDataSqlClient, connectionString),
			ProviderName.SqlServer     => SqlServerTools.GetDataProvider(SqlServerVersion.AutoDetect, DataProvider.SqlServer.SqlServerProvider.MicrosoftDataSqlClient, connectionString),
			_                          => base.GetDataProvider(providerName, connectionString)
		};
	}
}
