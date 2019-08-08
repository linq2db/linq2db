using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.MySql
{
	using Common;
	using Configuration;
	using Data;
	using Extensions;

	public static class MySqlTools
	{
		public static string AssemblyName;

		static readonly MySqlDataProvider _mySqlDataProvider  	            = new MySqlDataProvider(ProviderName.MySqlOfficial);
		static readonly MySqlDataProvider _mySqlConnectorDataProvider       = new MySqlDataProvider(ProviderName.MySqlConnector);

		static MySqlTools()
		{
			AssemblyName = DetectedProviderName == ProviderName.MySqlConnector ? "MySqlConnector" : "MySql.Data";

			DataConnection.AddDataProvider(ProviderName.MySql, DetectedProvider);
			DataConnection.AddDataProvider(_mySqlDataProvider);
			DataConnection.AddDataProvider(_mySqlConnectorDataProvider);

			DataConnection.AddProviderDetector(ProviderDetector);
		}

		static IDataProvider ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			if (css.IsGlobal)
				return null;

			switch (css.ProviderName)
			{
				case ""                                          :
				case null                                        :
					if (css.Name.Contains("MySql"))
						goto case "MySql";
					break;
				case "MySql.Data"                                : return _mySqlDataProvider;
				case "MySqlConnector"                            : return _mySqlConnectorDataProvider;
				case "MySql"                                     :
				case var provider when provider.Contains("MySql"):

					if (css.Name.Contains("MySqlConnector"))
						return _mySqlConnectorDataProvider;

					if (css.Name.Contains("MySql"))
						return _mySqlDataProvider;

					return DetectedProvider;
			}

			return null;
		}

		public static IDataProvider GetDataProvider()
		{
			return DetectedProvider;
		}

		static string _detectedProviderName;

		public static string  DetectedProviderName =>
			_detectedProviderName ?? (_detectedProviderName = DetectProviderName());

		static MySqlDataProvider DetectedProvider =>
			DetectedProviderName != ProviderName.MySqlConnector ? _mySqlDataProvider : _mySqlConnectorDataProvider;

		static string DetectProviderName()
		{
			try
			{
				var path = typeof(MySqlTools).Assembly.GetPath();

				if (!File.Exists(Path.Combine(path, "MySql.Data.dll")))
					if (File.Exists(Path.Combine(path, "MySqlConnector.dll")))
						return ProviderName.MySqlConnector;
			}
			catch (Exception)
			{
			}

			return ProviderName.MySqlOfficial;
		}

		public static void ResolveMySql([NotNull] string path)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));
			new AssemblyResolver(path, AssemblyName);
		}

		public static void ResolveMySql([NotNull] Assembly assembly)
		{
			if (assembly == null) throw new ArgumentNullException(nameof(assembly));
			new AssemblyResolver(assembly, AssemblyName);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(DetectedProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(
				connection.GetType().Assembly.FullName.Contains("MySqlConnector") ? _mySqlConnectorDataProvider : _mySqlDataProvider,
				connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(
				transaction.GetType().Assembly.FullName.Contains("MySqlConnector") ? _mySqlConnectorDataProvider : _mySqlDataProvider,
				transaction);
		}

		#endregion

		#region BulkCopy

		public  static BulkCopyType  DefaultBulkCopyType { get; set; } = BulkCopyType.MultipleRows;

		public static BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection             dataConnection,
			IEnumerable<T>             source,
			int                        maxBatchSize       = 1000,
			Action<BulkCopyRowsCopied> rowsCopiedCallback = null)
			where T : class
		{
			return dataConnection.BulkCopy(
				new BulkCopyOptions
				{
					BulkCopyType       = BulkCopyType.MultipleRows,
					MaxBatchSize       = maxBatchSize,
					RowsCopiedCallback = rowsCopiedCallback,
				}, source);
		}

		#endregion
	}
}
