using System;
using System.Data.Common;
using System.IO;
using System.Reflection;

namespace LinqToDB.DataProvider.MySql
{
	using Common;
	using Configuration;
	using Data;
	using Extensions;

	public static partial class MySqlTools
	{
		static readonly Lazy<IDataProvider> _mySqlDataProvider          = DataConnection.CreateDataProvider<MySqlDataProviderMySqlOfficial>();
		static readonly Lazy<IDataProvider> _mySqlConnectorDataProvider = DataConnection.CreateDataProvider<MySqlDataProviderMySqlConnector>();

		internal static IDataProvider? ProviderDetector(ConnectionOptions options)
		{
			// ensure ClickHouse configuration over mysql protocol is not detected as mysql
			if (options.ProviderName?.ContainsEx("ClickHouse") == true || options.ConfigurationString?.ContainsEx("ClickHouse") == true)
				return null;

			switch (options.ProviderName)
			{
				case ProviderName.MySqlOfficial                :
				case MySqlProviderAdapter.MySqlDataAssemblyName: return _mySqlDataProvider.Value;
				case ProviderName.MariaDB                      :
				case ProviderName.MySqlConnector               : return _mySqlConnectorDataProvider.Value;

				case ""                         :
				case null                       :
					if (options.ConfigurationString?.ContainsEx("MySql") == true)
						goto case ProviderName.MySql;
					break;
				case MySqlProviderAdapter.MySqlDataClientNamespace:
				case ProviderName.MySql                           :
					if (options.ConfigurationString?.ContainsEx(MySqlProviderAdapter.MySqlConnectorAssemblyName) == true)
						return _mySqlConnectorDataProvider.Value;

					if (options.ConfigurationString?.ContainsEx(MySqlProviderAdapter.MySqlDataAssemblyName) == true)
						return _mySqlDataProvider.Value;

					return GetDataProvider();
				case var providerName when providerName.ContainsEx("MySql"):
					if (providerName.ContainsEx(MySqlProviderAdapter.MySqlConnectorAssemblyName))
						return _mySqlConnectorDataProvider.Value;

					if (providerName.ContainsEx(MySqlProviderAdapter.MySqlDataAssemblyName))
						return _mySqlDataProvider.Value;

					goto case ProviderName.MySql;
			}

			return null;
		}

		public static IDataProvider GetDataProvider(string? providerName = null)
		{
			return providerName switch
			{
				ProviderName.MySqlOfficial  => _mySqlDataProvider.Value,
				ProviderName.MySqlConnector => _mySqlConnectorDataProvider.Value,
				_                           =>
					DetectedProviderName == ProviderName.MySqlOfficial
					? _mySqlDataProvider.Value
					: _mySqlConnectorDataProvider.Value,
			};
		}

		private static string? _detectedProviderName;
		public  static string  DetectedProviderName =>
			_detectedProviderName ??= DetectProviderName();

		static string DetectProviderName()
		{
			try
			{
				var path = typeof(MySqlTools).Assembly.GetPath();

				if (!File.Exists(Path.Combine(path, $"{MySqlProviderAdapter.MySqlDataAssemblyName}.dll")))
					if (File.Exists(Path.Combine(path, $"{MySqlProviderAdapter.MySqlConnectorAssemblyName}.dll")))
						return ProviderName.MySqlConnector;
			}
			catch (Exception)
			{
			}

			return ProviderName.MySqlOfficial;
		}

		public static void ResolveMySql(string path, string? assemblyName)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));
			new AssemblyResolver(
				path,
				assemblyName
					?? (DetectedProviderName == ProviderName.MySqlOfficial
						? MySqlProviderAdapter.MySqlDataAssemblyName
						: MySqlProviderAdapter.MySqlConnectorAssemblyName));
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

		[Obsolete("Use MySqlOptions.Default.BulkCopyType instead.")]
		public static BulkCopyType DefaultBulkCopyType
		{
			get => MySqlOptions.Default.BulkCopyType;
			set => MySqlOptions.Default = MySqlOptions.Default with { BulkCopyType = value };
		}

		#endregion
	}
}
