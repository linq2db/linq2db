using System;
using System.Data.Common;
using System.Reflection;

using JetBrains.Annotations;

using LinqToDB.Data;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.DataProvider.DuckDB;

namespace LinqToDB.DataProvider.DuckDB
{
	[PublicAPI]
	public static class DuckDBTools
	{
		enum Fake { }

		static readonly Lazy<IDataProvider> _duckDBDataProvider = ProviderDetectorBase<Fake>.CreateDataProvider<DuckDBDataProvider>();

		internal static IDataProvider? ProviderDetector(ConnectionOptions options)
		{
			static bool HasDuckDB(string? s) =>
				s?.Contains("DuckDB", StringComparison.OrdinalIgnoreCase) == true;

			if (HasDuckDB(options.ProviderName) || HasDuckDB(options.ConfigurationString))
				return _duckDBDataProvider.Value;

			return null;
		}

		public static IDataProvider GetDataProvider() => _duckDBDataProvider.Value;

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(new DataOptions()
				.UseConnectionString(_duckDBDataProvider.Value, connectionString));
		}

		public static DataConnection CreateDataConnection(DbConnection connection)
		{
			return new DataConnection(new DataOptions()
				.UseConnection(_duckDBDataProvider.Value, connection));
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction)
		{
			return new DataConnection(new DataOptions()
				.UseTransaction(_duckDBDataProvider.Value, transaction));
		}

		#endregion

		public static void ResolveDuckDB(string path)
		{
			_ = new AssemblyResolver(path, DuckDBProviderAdapter.AssemblyName);
		}

		public static void ResolveDuckDB(Assembly assembly)
		{
			_ = new AssemblyResolver(assembly, assembly.FullName!);
		}
	}
}
