using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Firebird
{
	using Data;
	using LinqToDB.Configuration;

	[PublicAPI]
	public static class FirebirdTools
	{
		private static readonly Lazy<IDataProvider> _firebirdDataProvider = new Lazy<IDataProvider>(() =>
		{
			var provider = new FirebirdDataProvider();

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			if (css.ProviderName == ProviderName.Firebird
				|| css.ProviderName == "FirebirdSql.Data.FirebirdClient"
				|| css.Name.Contains("Firebird"))
			{
				return _firebirdDataProvider.Value;
			}

			return null;
		}

		public static IDataProvider GetDataProvider()
		{
			return _firebirdDataProvider.Value;
		}

		public static void ResolveFirebird(string path)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));
			new AssemblyResolver(path, FirebirdWrappers.AssemblyName);
		}

		public static void ResolveFirebird(Assembly assembly)
		{
			if (assembly == null) throw new ArgumentNullException(nameof(assembly));
			new AssemblyResolver(assembly, FirebirdWrappers.AssemblyName);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(_firebirdDataProvider.Value, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(_firebirdDataProvider.Value, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(_firebirdDataProvider.Value, transaction);
		}

		#endregion

		#region BulkCopy

		public  static BulkCopyType  DefaultBulkCopyType { get; set; } = BulkCopyType.MultipleRows;

		public static BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection              dataConnection,
			IEnumerable<T>              source,
			int                         maxBatchSize       = 1000,
			Action<BulkCopyRowsCopied>? rowsCopiedCallback = null)
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

		#region ClearAllPools

		public static void ClearAllPools()
		{
			FirebirdWrappers.Initialize();

			FirebirdWrappers.ClearAllPools();
		}

		#endregion
	}
}
