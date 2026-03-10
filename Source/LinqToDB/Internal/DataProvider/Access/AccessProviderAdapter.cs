using System;
using System.Data;
using System.Data.Common;
using System.Threading;

using LinqToDB.DataProvider.Access;

using OdbcType = LinqToDB.Internal.DataProvider.OdbcProviderAdapter.OdbcType;
using OleDbType = LinqToDB.Internal.DataProvider.OleDbProviderAdapter.OleDbType;

namespace LinqToDB.Internal.DataProvider.Access
{
	public sealed class AccessProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly Lock _oledbSyncRoot = new ();
		private static readonly Lock _odbcSyncRoot  = new ();

		private static AccessProviderAdapter? _oledbProvider;
		private static AccessProviderAdapter? _odbcProvider;

		private AccessProviderAdapter(OleDbProviderAdapter adapter)
		{
			ConnectionType     = adapter.ConnectionType;
			DataReaderType     = adapter.DataReaderType;
			ParameterType      = adapter.ParameterType;
			CommandType        = adapter.CommandType;
			TransactionType    = adapter.TransactionType;
			_connectionFactory = adapter.CreateConnection;

			SetOleDbDbType      = adapter.SetDbType;
			GetOleDbDbType      = adapter.GetDbType;
			GetOleDbSchemaTable = adapter.GetOleDbSchemaTable;
		}

		private AccessProviderAdapter(OdbcProviderAdapter adapter)
		{
			ConnectionType     = adapter.ConnectionType;
			DataReaderType     = adapter.DataReaderType;
			ParameterType      = adapter.ParameterType;
			CommandType        = adapter.CommandType;
			TransactionType    = adapter.TransactionType;
			_connectionFactory = adapter.CreateConnection;

			SetOdbcDbType = adapter.SetDbType;
			GetOdbcDbType = adapter.GetDbType;
		}

		#region IDynamicProviderAdapter

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		readonly Func<string, DbConnection> _connectionFactory;
		public DbConnection CreateConnection(string connectionString) => _connectionFactory(connectionString);

		#endregion

		public Action<DbParameter, OleDbType>? SetOleDbDbType { get; }
		public Func<DbParameter, OleDbType>?   GetOleDbDbType { get; }

		public Action<DbParameter, OdbcType>? SetOdbcDbType   { get; }
		public Func<DbParameter, OdbcType>?   GetOdbcDbType   { get; }

		public Func<DbConnection, Guid, object[]?, DataTable>? GetOleDbSchemaTable { get; }

		internal static AccessProviderAdapter GetInstance(AccessProvider provider)
		{
			return provider switch
			{
				AccessProvider.ODBC  => GetOdbcAdapter(),
				AccessProvider.OleDb => GetOledbAdapter(),
				_ => throw new InvalidOperationException($"Unsupported provider type: {provider}"),
			};

			static AccessProviderAdapter GetOdbcAdapter()
			{
				if (_odbcProvider == null)
				{
					lock (_odbcSyncRoot)
						_odbcProvider ??= new AccessProviderAdapter(OdbcProviderAdapter.GetInstance());
				}

				return _odbcProvider;
			}

			static AccessProviderAdapter GetOledbAdapter()
			{
				if (_oledbProvider == null)
				{
					lock (_oledbSyncRoot)
						_oledbProvider ??= new AccessProviderAdapter(OleDbProviderAdapter.GetInstance());
				}

				return _oledbProvider;
			}
		}
	}
}
