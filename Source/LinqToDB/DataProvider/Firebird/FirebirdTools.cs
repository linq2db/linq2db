using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Firebird
{
	using Data;

	[PublicAPI]
	public static class FirebirdTools
	{
		static readonly FirebirdDataProvider _firebirdDataProvider = new FirebirdDataProvider();

		static FirebirdTools()
		{
			DataConnection.AddDataProvider(_firebirdDataProvider);
		}

		public static IDataProvider GetDataProvider()
		{
			return _firebirdDataProvider;
		}

		public static void ResolveFirebird([NotNull] string path)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));
			new AssemblyResolver(path, "FirebirdSql.Data.FirebirdClient");
		}

		public static void ResolveFirebird([NotNull] Assembly assembly)
		{
			if (assembly == null) throw new ArgumentNullException(nameof(assembly));
			new AssemblyResolver(assembly, "FirebirdSql.Data.FirebirdClient");
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(_firebirdDataProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(_firebirdDataProvider, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(_firebirdDataProvider, transaction);
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

		#region ClearAllPools

		static Action _clearAllPools;

		public static void ClearAllPools()
		{
			if (_clearAllPools == null)
				_clearAllPools =
					GeneratedExpressions.Firebird_ClearAllPools(_firebirdDataProvider.GetConnectionType()).Compile();
//					Expression.Lambda<Action>(
//						Expression.Call(
//							_firebirdDataProvider.GetConnectionType(),
//							"ClearAllPools",
//							new Type[0]))
//						.Compile();
			_clearAllPools();
		}

		#endregion
	}
}
