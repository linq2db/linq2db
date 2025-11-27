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

using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Types;

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
}
