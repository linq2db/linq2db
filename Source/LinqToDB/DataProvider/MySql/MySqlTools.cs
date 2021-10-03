using System;
using System.Data.Common;
using System.Reflection;

namespace LinqToDB.DataProvider.MySql
{
	using Configuration;
	using Data;

	public static class MySqlTools
	{
		private static readonly Lazy<IDataProvider> _dataProvider = new (() =>
		{
			var provider = new MySqlDataProvider(ProviderName.MySql);

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			if (css.IsGlobal)
				return null;

			switch (css.ProviderName)
			{
				// two names for v3 configurations backward compatibility (old provider names for MySql.Data-based providers)
				case "MySql.Data"                                   :
				case "MySql.Official"                               :
				case MySqlProviderAdapter.OldMySqlConnectorNamespace:
				case MySqlProviderAdapter.MySqlConnectorAssemblyName:
				case ProviderName.MySql                             : return _dataProvider.Value;

				case ""                         :
				case null                       :
					if (css.Name.Contains("MySql"))
						return _dataProvider.Value;
					break;
			}

			return null;
		}

		public static IDataProvider GetDataProvider(string? providerName = null) => _dataProvider.Value;

		public static void ResolveMySql(string path)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));
			new AssemblyResolver(path, MySqlProviderAdapter.MySqlConnectorAssemblyName);
		}

		public static void ResolveMySql(Assembly assembly)
		{
			if (assembly == null) throw new ArgumentNullException(nameof(assembly));
			new AssemblyResolver(assembly, assembly.FullName!);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connectionString);
		}

		public static DataConnection CreateDataConnection(DbConnection connection, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connection);
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), transaction);
		}

		#endregion

		#region BulkCopy

		public  static BulkCopyType  DefaultBulkCopyType { get; set; } = BulkCopyType.MultipleRows;

		#endregion
	}
}
