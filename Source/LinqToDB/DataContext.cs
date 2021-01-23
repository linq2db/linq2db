using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB
{
	using Data;
	using DataProvider;
	using Linq;
	using Async;
	using Mapping;
	using SqlProvider;

	/// <summary>
	/// Implements abstraction over non-persistent database connection that could be released after query or transaction execution.
	/// </summary>
	[PublicAPI]
	public class DataContext : IDataContext, IEntityServices
	{
		private bool _disposed;

		/// <summary>
		/// Creates data context using default database configuration.
		/// <see cref="DataConnection.DefaultConfiguration"/> for more details.
		/// </summary>
		public DataContext() : this(DataConnection.DefaultConfiguration)
		{
		}

		/// <summary>
		/// Creates data context using specific database configuration.
		/// </summary>
		/// <param name="configurationString">Connection configuration name.
		/// In case of <c>null</c> value, context will use default configuration.
		/// <see cref="DataConnection.DefaultConfiguration"/> for more details.
		/// </param>
		public DataContext(string? configurationString)
		{
			ConfigurationString = configurationString ?? DataConnection.DefaultConfiguration;
			DataProvider        = DataConnection.GetDataProvider(ConfigurationString!);
			ContextID           = DataProvider.Name;
			MappingSchema       = DataProvider.MappingSchema;
		}

		/// <summary>
		/// Creates data context using specific data provider implementation and connection string.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation.</param>
		/// <param name="connectionString">Database connection string.</param>
		public DataContext(IDataProvider dataProvider, string connectionString)
		{
			DataProvider     = dataProvider     ?? throw new ArgumentNullException(nameof(dataProvider));
			ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
			ContextID        = DataProvider.Name;
			MappingSchema    = DataProvider.MappingSchema;
		}

		/// <summary>
		/// Creates data context using specified database provider and connection string.
		/// </summary>
		/// <param name="providerName">Name of database provider to use with this connection. <see cref="ProviderName"/> class for list of providers.</param>
		/// <param name="connectionString">Database connection string to use for connection with database.</param>
		public DataContext( string providerName, string connectionString)
		{
			if (providerName     == null) throw new ArgumentNullException(nameof(providerName));
			if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));
			var dataProvider = DataConnection.GetDataProvider(providerName, connectionString);
			DataProvider     = dataProvider ?? throw new LinqToDBException($"DataProvider '{providerName}' not found.");
			ContextID        = DataProvider.Name;
			ConnectionString = connectionString;
			MappingSchema    = DataProvider.MappingSchema;
		}

		/// <summary>
		/// Gets initial value for database connection configuration name.
		/// </summary>
		public string?       ConfigurationString { get; private set; }
		/// <summary>
		/// Gets initial value for database connection string.
		/// </summary>
		public string?       ConnectionString    { get; private set; }
		/// <summary>
		/// Gets database provider implementation.
		/// </summary>
		public IDataProvider DataProvider        { get; private set; }
		/// <summary>
		/// Gets or sets context identifier. Uses provider's name by default.
		/// </summary>
		public string        ContextID           { get; set; }
		/// <summary>
		/// Gets or sets mapping schema. Uses provider's mapping schema by default.
		/// </summary>
		public MappingSchema MappingSchema       { get; set; }
		/// <summary>
		/// Gets or sets option to force inline parameter values as literals into command text. If parameter inlining not supported
		/// for specific value type, it will be used as parameter.
		/// </summary>
		public bool          InlineParameters    { get; set; }
		/// <summary>
		/// Contains text of last command, sent to database using current context.
		/// </summary>
		public string?       LastQuery           { get; set; }

		/// <summary>
		/// Gets or sets trace handler, used for data connection instance.
		/// </summary>
		public Action<TraceInfo>? OnTraceConnection { get; set; }

		private bool _keepConnectionAlive;
		/// <summary>
		/// Gets or sets option to dispose underlying connection after use.
		/// Default value: <c>false</c>.
		/// </summary>
		public  bool  KeepConnectionAlive
		{
			get => _keepConnectionAlive;
			set
			{
				_keepConnectionAlive = value;

				if (value == false)
					ReleaseQuery();
			}
		}

		private bool? _isMarsEnabled;
		/// <summary>
		/// Gets or sets status of Multiple Active Result Sets (MARS) feature. This feature available only for
		/// SQL Azure and SQL Server 2005+.
		/// </summary>
		public bool   IsMarsEnabled
		{
			get
			{
				if (_isMarsEnabled == null)
				{
					if (_dataConnection == null)
						return false;
					_isMarsEnabled = _dataConnection.IsMarsEnabled;
				}

				return _isMarsEnabled.Value;
			}
			set => _isMarsEnabled = value;
		}

		private List<string>? _queryHints;
		/// <summary>
		/// Gets list of query hints (writable collection), that will be used for all queries, executed through current context.
		/// </summary>
		public List<string>  QueryHints
		{
			get
			{
				if (_dataConnection != null)
					return _dataConnection.QueryHints;

				return _queryHints ??= new List<string>();
			}
		}

		private List<string>? _nextQueryHints;
		/// <summary>
		/// Gets list of query hints (writable collection), that will be used only for next query, executed through current context.
		/// </summary>
		public List<string>  NextQueryHints
		{
			get
			{
				if (_dataConnection != null)
					return _dataConnection.NextQueryHints;

				return _nextQueryHints ??= new List<string>();
			}
		}

		/// <summary>
		/// Gets or sets flag to close context after query execution or leave it open.
		/// </summary>
		public bool CloseAfterUse { get; set; }

		/// <summary>
		/// Counts number of locks, put on underlying connection. Connection will not be released while counter is not zero.
		/// </summary>
		internal int LockDbManagerCounter;


		private int? _commandTimeout;

		/// <summary>
		/// Gets or sets command execution timeout in seconds.
		/// Negative timeout value means that default timeout will be used.
		/// 0 timeout value corresponds to infinite timeout.
		/// By default timeout is not set and default value for current provider used.
		/// </summary>
		public  int   CommandTimeout
		{
			get => _commandTimeout ?? -1;
			set
			{
				if (value < 0)
				{
					_commandTimeout = null;
					if (_dataConnection != null)
						_dataConnection.CommandTimeout = -1;
				}
				else
				{
					_commandTimeout = value;
					if (_dataConnection != null)
						_dataConnection.CommandTimeout = value;
				}
			}
		}

		/// <summary>
		/// Underlying active database connection.
		/// </summary>
		DataConnection? _dataConnection;

		/// <summary>
		/// Creates instance of <see cref="DataConnection"/> class, used by context internally.
		/// </summary>
		/// <returns>New <see cref="DataConnection"/> instance.</returns>
		protected virtual DataConnection CreateDataConnection()
		{
			return ConnectionString != null
				? new DataConnection(DataProvider, ConnectionString)
				: new DataConnection(ConfigurationString);
		}

		/// <summary>
		/// Returns associated database connection <see cref="DataConnection"/> or create new connection, if connection
		/// doesn't exists.
		/// </summary>
		/// <returns>Data connection.</returns>
		internal DataConnection GetDataConnection()
		{
			AssertDisposed();

			if (_dataConnection == null)
			{
				_dataConnection = CreateDataConnection();

				if (_commandTimeout != null)
					_dataConnection.CommandTimeout = CommandTimeout;

				if (_queryHints != null && _queryHints.Count > 0)
				{
					_dataConnection.QueryHints.AddRange(_queryHints);
					_queryHints = null;
				}

				if (_nextQueryHints != null && _nextQueryHints.Count > 0)
				{
					_dataConnection.NextQueryHints.AddRange(_nextQueryHints);
					_nextQueryHints = null;
				}

				if (OnTraceConnection != null)
					_dataConnection.OnTraceConnection = OnTraceConnection;

				if (MappingSchema != null && MappingSchema != _dataConnection.MappingSchema)
					_dataConnection.AddMappingSchema(MappingSchema);
			}

			return _dataConnection;
		}

		private void AssertDisposed()
		{
			if (_disposed)
				throw new ObjectDisposedException(GetType().FullName);
		}

		/// <summary>
		/// For active underlying connection, updates information about last executed query <see cref="LastQuery"/> and
		/// releases connection, if it is not locked (<see cref="LockDbManagerCounter"/>)
		/// and <see cref="KeepConnectionAlive"/> is <c>false</c>.
		/// </summary>
		internal void ReleaseQuery()
		{
			if (_dataConnection != null)
			{
				LastQuery = _dataConnection.LastQuery;

				if (LockDbManagerCounter == 0 && KeepConnectionAlive == false)
				{
					if (_dataConnection.QueryHints.    Count > 0) QueryHints.    AddRange(_queryHints!);
					if (_dataConnection.NextQueryHints.Count > 0) NextQueryHints.AddRange(_nextQueryHints!);

					_dataConnection.Dispose();
					_dataConnection = null;
				}
			}
		}

		Func<ISqlBuilder>   IDataContext.CreateSqlProvider     => () => DataProvider.CreateSqlBuilder(MappingSchema);
		Func<ISqlOptimizer> IDataContext.GetSqlOptimizer       => DataProvider.GetSqlOptimizer;
		Type                IDataContext.DataReaderType        => DataProvider.DataReaderType;
		SqlProviderFlags    IDataContext.SqlProviderFlags      => DataProvider.SqlProviderFlags;
		TableOptions        IDataContext.SupportedTableOptions => DataProvider.SupportedTableOptions;

		Expression IDataContext.GetReaderExpression(IDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			return DataProvider.GetReaderExpression(reader, idx, readerExpression, toType);
		}

		bool? IDataContext.IsDBNullAllowed(IDataReader reader, int idx)
		{
			return DataProvider.IsDBNullAllowed(reader, idx);
		}

		/// <summary>
		/// Noop constructor for context cloning.
		/// </summary>
		/// <param name="n">Unused.</param>
#nullable disable
		DataContext(int n) {}
#nullable enable

		/// <summary>
		/// Creates instance of <see cref="DataConnection"/> class, attached to same database connection/transaction.
		/// Used by <see cref="IDataContext.Clone(bool)"/> API only if <see cref="DataConnection.IsMarsEnabled"/>
		/// is <c>true</c> and there is an active connection associated with current context.
		/// <paramref name="dbConnection"/> and <paramref name="dbTransaction"/> parameters are mutually exclusive.
		/// One and only one parameter will have value - if there is active transaction, <paramref name="dbTransaction"/>
		/// parameter value provided, otherwise <paramref name="dbConnection"/> parameter has value.
		/// </summary>
		/// <param name="currentConnection"><see cref="DataConnection"/> instance, used by current context instance.</param>
		/// <param name="dbTransaction">Transaction, associated with <paramref name="currentConnection"/>.</param>
		/// <param name="dbConnection">Connection, associated with <paramref name="dbConnection"/>.</param>
		/// <returns>New <see cref="DataConnection"/> instance.</returns>
		protected virtual DataConnection CloneDataConnection(
			DataConnection       currentConnection, // not used by implementation, but could be useful in override
			IAsyncDbTransaction? dbTransaction,
			IAsyncDbConnection?  dbConnection)
		{
			// we pass both dataconnection and db connection/transaction, because connection/transaction accessors
			// are internal and it is not possible to access them in derived class. And we definitely don't want them
			// to be public.
			return dbTransaction != null
				? new DataConnection(DataProvider, dbTransaction)
				: new DataConnection(DataProvider, dbConnection!);
		}

		IDataContext IDataContext.Clone(bool forNestedQuery)
		{
			AssertDisposed();

			var dc = new DataContext(0)
			{
				ConfigurationString = ConfigurationString,
				ConnectionString    = ConnectionString,
				KeepConnectionAlive = KeepConnectionAlive,
				DataProvider        = DataProvider,
				ContextID           = ContextID,
				MappingSchema       = MappingSchema,
				InlineParameters    = InlineParameters,
			};

			if (forNestedQuery && _dataConnection != null && _dataConnection.IsMarsEnabled)
				dc._dataConnection = CloneDataConnection(
					_dataConnection,
					_dataConnection.TransactionAsync,
					_dataConnection.TransactionAsync == null ? _dataConnection.EnsureConnection() : null);


			dc.QueryHints.    AddRange(QueryHints);
			dc.NextQueryHints.AddRange(NextQueryHints);

			return dc;
		}

		/// <summary>
		/// Event, triggered before underlying connection closed on context disposal or closing.
		/// Not fired, if context doesn't have active connection (bug?).
		/// </summary>
		public event EventHandler? OnClosing;

		/// <inheritdoc />
		public Action<EntityCreatedEventArgs>? OnEntityCreated { get; set; }

		void IDisposable.Dispose()
		{
			_disposed = true;
			Close();
		}

		/// <summary>
		/// Closes underlying connection and fires <see cref="OnClosing"/> event (only if connection existed).
		/// </summary>
		void Close()
		{
			if (_dataConnection != null)
			{
				OnClosing?.Invoke(this, EventArgs.Empty);

				if (_dataConnection.QueryHints.    Count > 0) QueryHints.    AddRange(_queryHints!);
				if (_dataConnection.NextQueryHints.Count > 0) NextQueryHints.AddRange(_nextQueryHints!);

				_dataConnection.Dispose();
				_dataConnection = null;
			}
		}

		void IDataContext.Close()
		{
			Close();
		}

		/// <summary>
		/// Starts new transaction for current context with specified isolation level.
		/// If connection already has transaction, it will be rolled back.
		/// </summary>
		/// <param name="level">Transaction isolation level.</param>
		/// <returns>Database transaction object.</returns>
		public virtual DataContextTransaction BeginTransaction(IsolationLevel level)
		{
			var dct = new DataContextTransaction(this);

			dct.BeginTransaction(level);

			return dct;
		}

		/// <summary>
		/// Starts new transaction for current context with default isolation level.
		/// If connection already has transaction, it will be rolled back.
		/// </summary>
		/// <param name="autoCommitOnDispose">Not supported, see <a href="https://github.com/linq2db/linq2db/issues/104">issue</a>.</param>
		/// <returns>Database transaction object.</returns>
		public virtual DataContextTransaction BeginTransaction(bool autoCommitOnDispose = true)
		{
			var dct = new DataContextTransaction(this);

			dct.BeginTransaction();

			return dct;
		}

		/// <summary>
		/// Starts new transaction asynchronously for current context with specified isolation level.
		/// If connection already has transaction, it will be rolled back.
		/// </summary>
		/// <param name="level">Transaction isolation level.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Database transaction object.</returns>
		public virtual async Task<DataContextTransaction> BeginTransactionAsync(IsolationLevel level, CancellationToken cancellationToken = default)
		{
			var dct = new DataContextTransaction(this);

			await dct.BeginTransactionAsync(level, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return dct;
		}

		/// <summary>
		/// Starts new transaction asynchronously for current context with default isolation level.
		/// If connection already has transaction, it will be rolled back.
		/// </summary>
		/// <param name="autoCommitOnDispose">Not supported, see <a href="https://github.com/linq2db/linq2db/issues/104">issue</a>.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Database transaction object.</returns>
		public virtual async Task<DataContextTransaction> BeginTransactionAsync(bool autoCommitOnDispose = true, CancellationToken cancellationToken = default)
		{
			var dct = new DataContextTransaction(this);

			await dct.BeginTransactionAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return dct;
		}

		IQueryRunner IDataContext.GetQueryRunner(Query query, int queryNumber, Expression expression, object?[]? parameters, object?[]? preambles)
		{
			return new QueryRunner(this, ((IDataContext)GetDataConnection()).GetQueryRunner(query, queryNumber, expression, parameters, preambles));
		}

		class QueryRunner : IQueryRunner
		{
			public QueryRunner(DataContext dataContext, IQueryRunner queryRunner)
			{
				_dataContext = dataContext;
				_queryRunner = (DataConnection.QueryRunner)queryRunner;
			}

			DataContext? _dataContext;
			DataConnection.QueryRunner? _queryRunner;

			public void Dispose()
			{
				_queryRunner!.Dispose();
				_dataContext!.ReleaseQuery();
				_queryRunner = null;
				_dataContext = null;
			}

			public int ExecuteNonQuery()
			{
				return _queryRunner!.ExecuteNonQuery();
			}

			public object? ExecuteScalar()
			{
				return _queryRunner!.ExecuteScalar();
			}

			public DataReaderWrapper ExecuteReader()
			{
				return _queryRunner!.ExecuteReader();
			}

			public Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
			{
				return _queryRunner!.ExecuteScalarAsync(cancellationToken);
			}

			public Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken)
			{
				return _queryRunner!.ExecuteReaderAsync(cancellationToken);
			}

			public Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
			{
				return _queryRunner!.ExecuteNonQueryAsync(cancellationToken);
			}

			public string GetSqlText()
			{
				return _queryRunner!.GetSqlText();
			}

			public IDataContext DataContext      { get => _queryRunner!.DataContext;      set => _queryRunner!.DataContext      = value; }
			public Expression   Expression       { get => _queryRunner!.Expression;       set => _queryRunner!.Expression       = value; }
			public object?[]?   Parameters       { get => _queryRunner!.Parameters;       set => _queryRunner!.Parameters       = value; }
			public object?[]?   Preambles        { get => _queryRunner!.Preambles;        set => _queryRunner!.Preambles        = value; }
			public Expression?  MapperExpression { get => _queryRunner!.MapperExpression; set => _queryRunner!.MapperExpression = value; }
			public int          RowsCount        { get => _queryRunner!.RowsCount;        set => _queryRunner!.RowsCount        = value; }
			public int          QueryNumber      { get => _queryRunner!.QueryNumber;      set => _queryRunner!.QueryNumber      = value; }
		}
	}
}
