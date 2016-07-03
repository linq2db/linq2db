using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.DB2
{
	using System.Configuration;
	using System.Linq;
	using System.Linq.Expressions;

	using Data;

	[PublicAPI]
	public static class DB2Tools
	{
		static readonly DB2DataProvider _db2DataProviderzOS = new DB2DataProvider(ProviderName.DB2zOS, DB2Version.zOS);
		static readonly DB2DataProvider _db2DataProviderLUW = new DB2DataProvider(ProviderName.DB2LUW, DB2Version.LUW);

		public static bool AutoDetectProvider { get; set; }

		static DB2Tools()
		{
			AutoDetectProvider = true;

			DataConnection.AddDataProvider(ProviderName.DB2, _db2DataProviderLUW);
			DataConnection.AddDataProvider(_db2DataProviderLUW);
			DataConnection.AddDataProvider(_db2DataProviderzOS);

			DataConnection.AddProviderDetector(ProviderDetector);
		}

		static IDataProvider ProviderDetector(ConnectionStringSettings css)
		{
			if (DataConnection.IsMachineConfig(css))
				return null;

			switch (css.ProviderName)
			{
				case ""             :
				case null           :

					if (css.Name == "DB2")
						goto case "DB2";
					break;

				case "DB2"          :
				case "IBM.Data.DB2" :

					if (css.Name.Contains("LUW") || css.Name.Contains("z/OS") || css.Name.Contains("zOS"))
						break;

					if (AutoDetectProvider)
					{
						try
						{
							var connectionType = Type.GetType("IBM.Data.DB2.DB2Connection, IBM.Data.DB2", true);
							var serverTypeProp = connectionType
								.GetProperties (BindingFlags.NonPublic | BindingFlags.Instance)
								.FirstOrDefault(p => p.Name == "eServerType");

							if (serverTypeProp != null)
							{
								var connectionCreator = DynamicDataProviderBase.CreateConnectionExpression(connectionType).Compile();

								using (var conn = connectionCreator(css.ConnectionString))
								{
									conn.Open();

									var serverType = Expression.Lambda<Func<object>>(
										Expression.Convert(
											Expression.MakeMemberAccess(Expression.Constant(conn), serverTypeProp),
											typeof(object)))
										.Compile()();

									var iszOS = serverType.ToString() == "DB2_390";

									return iszOS ? _db2DataProviderzOS : _db2DataProviderLUW;
								}
							}
						}
						catch (Exception)
						{
						}
					}

					break;
			}

			return null;
		}

		public static IDataProvider GetDataProvider(DB2Version version = DB2Version.LUW)
		{
			switch (version)
			{
				case DB2Version.zOS : return _db2DataProviderzOS;
			}

			return _db2DataProviderLUW;
		}

		public static void ResolveDB2(string path)
		{
			new AssemblyResolver(path, "IBM.Data.DB2");
		}

		public static void ResolveDB2(Assembly assembly)
		{
			new AssemblyResolver(assembly, "IBM.Data.DB2");
		}

		#region OnInitialized

		private static  bool                  _isInitialized;
		static readonly object                _syncAfterInitialized    = new object();
		private static  ConcurrentBag<Action> _afterInitializedActions = new ConcurrentBag<Action>();

		internal static void Initialized()
		{
			if (!_isInitialized)
			{
				lock (_syncAfterInitialized)
				{
					if (!_isInitialized)
					{
						_isInitialized = true;

						foreach (var action in _afterInitializedActions)
							action();
						_afterInitializedActions = null;
					}
				}
			}
		}

		public static void AfterInitialized(Action action)
		{
			if (_isInitialized)
			{
				action();
			}
			else
			{
				lock (_syncAfterInitialized)
				{
					if (_isInitialized)
					{
						action();
					}
					else
					{
						_afterInitializedActions.Add(action);
					}
				}
			}
		}

		#endregion

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, DB2Version version = DB2Version.LUW)
		{
			switch (version)
			{
				case DB2Version.zOS : return new DataConnection(_db2DataProviderzOS, connectionString);
			}

			return new DataConnection(_db2DataProviderLUW, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection, DB2Version version = DB2Version.LUW)
		{
			switch (version)
			{
				case DB2Version.zOS : return new DataConnection(_db2DataProviderzOS, connection);
			}

			return new DataConnection(_db2DataProviderLUW, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction, DB2Version version = DB2Version.LUW)
		{
			switch (version)
			{
				case DB2Version.zOS : return new DataConnection(_db2DataProviderzOS, transaction);
			}

			return new DataConnection(_db2DataProviderLUW, transaction);
		}

		#endregion

		#region BulkCopy

		private static BulkCopyType _defaultBulkCopyType = BulkCopyType.MultipleRows;
		public  static BulkCopyType  DefaultBulkCopyType
		{
			get { return _defaultBulkCopyType;  }
			set { _defaultBulkCopyType = value; }
		}

		public static BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection             dataConnection,
			IEnumerable<T>             source,
			int                        maxBatchSize       = 1000,
			Action<BulkCopyRowsCopied> rowsCopiedCallback = null)
		{
			return dataConnection.BulkCopy(
				new BulkCopyOptions
				{
					BulkCopyType       = BulkCopyType.MultipleRows,
					MaxBatchSize       = maxBatchSize,
					RowsCopiedCallback = rowsCopiedCallback,
				}, source);
		}

		public static BulkCopyRowsCopied ProviderSpecificBulkCopy<T>(
			DataConnection             dataConnection,
			IEnumerable<T>             source,
			int?                       bulkCopyTimeout    = null,
			bool                       keepIdentity       = false,
			int                        notifyAfter        = 0,
			Action<BulkCopyRowsCopied> rowsCopiedCallback = null)
		{
			return dataConnection.BulkCopy(
				new BulkCopyOptions
				{
					BulkCopyType       = BulkCopyType.ProviderSpecific,
					BulkCopyTimeout    = bulkCopyTimeout,
					KeepIdentity       = keepIdentity,
					NotifyAfter        = notifyAfter,
					RowsCopiedCallback = rowsCopiedCallback,
				}, source);
		}

		#endregion
	}
}
