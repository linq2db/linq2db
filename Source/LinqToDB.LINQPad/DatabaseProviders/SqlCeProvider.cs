using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
#if !NETFRAMEWORK
using System.IO;
#endif

namespace LinqToDB.LINQPad;

internal sealed class SqlCeProvider : DatabaseProviderBase
{
	private static readonly IReadOnlyList<ProviderInfo> _providers =
	[
		new (ProviderName.SqlCe, "Microsoft SQL Server Compact Edition")
	];

	public SqlCeProvider()
		: base(ProviderName.SqlCe, "Microsoft SQL Server Compact Edition (SQL CE)", _providers)
	{
	}

	public override DateTime? GetLastSchemaUpdate(ConnectionSettings settings)
	{
		// no information in schema
		return null;
	}

	public override string? GetProviderDownloadUrl(string? providerName)
	{
		return "https://www.microsoft.com/en-us/download/details.aspx?id=30709";
	}

	public override void ClearAllPools(string providerName)
	{
		// connection pooling not supported by provider
	}

	public override DbProviderFactory GetProviderFactory(string providerName)
	{
		var typeName = "System.Data.SqlServerCe.SqlCeProviderFactory, System.Data.SqlServerCe";
		return (DbProviderFactory)Type.GetType(typeName, false)?.GetField("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)!;
	}

#if !NETFRAMEWORK
	public override bool IsProviderPathSupported(string providerName)
	{
		return true;
	}

	public override IEnumerable<string> GetProviderAssemblyNames(string providerName)
	{
		yield return "System.Data.SqlServerCe.dll";
	}

	public override string? TryGetDefaultPath(string providerName)
	{
		var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
		if (!string.IsNullOrEmpty(programFiles))
		{
			var path = Path.Combine(programFiles, "Microsoft SQL Server Compact Edition\\v4.0\\Private\\System.Data.SqlServerCe.dll");

			if (File.Exists(path))
				return path;
		}

		return null;
	}

	private static bool _factoryRegistered;
	public override void RegisterProviderFactory(string providerName, string providerPath)
	{
		if (_factoryRegistered)
			return;

		if (!File.Exists(providerPath))
			throw new LinqToDBLinqPadException($"Cannot find SQL CE provider assembly at '{providerPath}'");

		try
		{
			var assembly = Assembly.LoadFrom(providerPath);
			DbProviderFactories.RegisterFactory("System.Data.SqlServerCe.4.0", assembly.GetType("System.Data.SqlServerCe.SqlCeProviderFactory")!);
			_factoryRegistered = true;
		}
		catch (Exception ex)
		{
			throw new LinqToDBLinqPadException($"Failed to initialize SQL CE provider factory: ({ex.GetType().Name}) {ex.Message}");
		}
	}
#endif
}
