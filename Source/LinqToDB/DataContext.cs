using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Infrastructure;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;
using LinqToDB.Metrics;

namespace LinqToDB
{
	/// <summary>
	/// Implements abstraction over non-persistent database connection that could be released after query or transaction execution.
	/// </summary>
	[PublicAPI]
	public partial class DataContext : IDataContext, IInfrastructure<IServiceProvider>
	{
		bool _disposed;

		#region .ctors

		/// <summary>
		/// Creates data context using default database configuration.
		/// <see cref="DataConnection.DefaultConfiguration"/> for more details.
		/// </summary>
		public DataContext() : this(DataConnection.DefaultDataOptions)
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
			: this(configurationString == null
				? DataConnection.DefaultDataOptions
				: DataConnection.ConnectionOptionsByConfigurationString.GetOrAdd(configurationString, _ => new(new(ConfigurationString : configurationString))))
		{
		}

		/// <summary>
		/// Creates data context using specific data provider implementation and connection string.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation.</param>
		/// <param name="connectionString">Database connection string.</param>
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataContext(new DataOptions().UseConnectionString(dataProvider, connectionString))"), EditorBrowsable(EditorBrowsableState.Never)]
		public DataContext(IDataProvider dataProvider, string connectionString)
			: this(new DataOptions()
				.UseConnectionString(
					dataProvider     ?? throw new ArgumentNullException(nameof(dataProvider)),
					connectionString ?? throw new ArgumentNullException(nameof(connectionString))))
		{
		}

		/// <summary>
		/// Creates data context using specified database provider and connection string.
		/// </summary>
		/// <param name="providerName">Name of database provider to use with this connection. <see cref="ProviderName"/> class for list of providers.</param>
		/// <param name="connectionString">Database connection string to use for connection with database.</param>
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataContext(new DataOptions().UseConnectionString(providerName, connectionString))"), EditorBrowsable(EditorBrowsableState.Never)]
		public DataContext( string providerName, string connectionString)
			: this(new DataOptions()
				.UseConnectionString(
					providerName     ?? throw new ArgumentNullException(nameof(providerName)),
					connectionString ?? throw new ArgumentNullException(nameof(connectionString))))
		{
		}

		/// <summary>
		/// Creates database context object that uses a <see cref="DataOptions"/> to configure the connection.
		/// </summary>
		/// <param name="options">Options, setup ahead of time.</param>
		public DataContext(DataOptions options)
		{
			Options       = options;
			MappingSchema = default!;
			DataProvider  = default!;
			Options.Apply(this);
		}

		#endregion

		/// <summary>
		/// Current DataContext options
		/// </summary>
		public DataOptions   Options             { get; private set; }
		/// <summary>
		/// Gets initial value for database connection configuration name.
		/// </summary>
		public string?       ConfigurationString { get; private set; }

		public void AddMappingSchema(MappingSchema mappingSchema)
		{
			MappingSchema    = MappingSchema.CombineSchemas(mappingSchema, MappingSchema);
			_configurationID = null;
		}

		void IDataContext.SetMappingSchema(MappingSchema mappingSchema)
		{
			MappingSchema    = mappingSchema;
			_configurationID = null;
		}

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
		public string        ContextName         => DataProvider.Name;

		int  _msID;
		int? _configurationID;
		/// <summary>
		/// Gets context configuration ID.
		/// </summary>
		int IConfigurationID.ConfigurationID
		{
			// can we just delegate it to underlying DataConnection?
			get
			{
				AssertDisposed();

				if (_configurationID == null || _msID != ((IConfigurationID)MappingSchema).ConfigurationID)
				{
					using var idBuilder = new IdentifierBuilder();
					_configurationID = idBuilder
						.Add(_msID = ((IConfigurationID)MappingSchema).ConfigurationID)
						.Add(ConfigurationString)
						.Add(Options)
						.Add(GetType())
						.CreateID();
				}

				return _configurationID.Value;
			}
		}

		/// <summary>
		/// Gets or sets mapping schema. Uses provider's mapping schema by default.
		/// </summary>
		public MappingSchema MappingSchema       { get; private set; }
		/// <summary>
		/// Gets or sets option to force inline parameter values as literals into command text. If parameter inlining not supported
		/// for specific value type, it will be used as parameter.
		/// </summary>
		public bool          InlineParameters    { get; set; }
		/// <summary>
		/// Contains text of last command, sent to database using current context.
		/// </summary>
		public string?       LastQuery
		{
			get;
			// TODO: Mark private in v7 and remove warning suppressions from callers
			[Obsolete("This API scheduled for removal in v7"), EditorBrowsable(EditorBrowsableState.Never)]
			set;
		}

		/// <summary>
		/// Gets or sets trace handler, used for data connection instance.
		/// </summary>
		public Action<TraceInfo>? OnTraceConnection
		{
			get;
			// TODO: Make private in v7 and remove obsoletion warning ignores from callers
			[Obsolete("This API scheduled for removal in v7. Use DataOptions's UseTracing API"), EditorBrowsable(EditorBrowsableState.Never)]
			set;
		}

		private bool _keepConnectionAlive;
		/// <summary>
		/// Gets flag indicating wether context should dispose underlying connection after use or not.
		/// Default value: <c>false</c>.
		/// </summary>
		public  bool  KeepConnectionAlive
		{
			get => _keepConnectionAlive;
			// TODO: Remove in v7
			[Obsolete("This API scheduled for removal in v7. To set KeepAlive value use SetKeepAlive or SetKeepAliveAsync methods"), EditorBrowsable(EditorBrowsableState.Never)]
			set => SetKeepConnectionAlive(value);
		}

		/// <summary>
		/// Sets connection management behavior to specify if context should dispose underlying connection after use or not.
		/// </summary>
		public void SetKeepConnectionAlive(bool keepAlive)
		{
			AssertDisposed();

			_keepConnectionAlive = keepAlive;

			if (keepAlive == false)
				ReleaseQuery();
		}

		/// <summary>
		/// Sets connection management behavior to specify if context should dispose underlying connection after use or not.
		/// </summary>
		public Task SetKeepConnectionAliveAsync(bool keepAlive)
		{
			AssertDisposed();

			_keepConnectionAlive = keepAlive;

			if (keepAlive == false)
				return ReleaseQueryAsync();

			return Task.CompletedTask;
		}

		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7"), EditorBrowsable(EditorBrowsableState.Never)]
		private bool? _isMarsEnabled;
		/// <summary>
		/// Gets or sets status of Multiple Active Result Sets (MARS) feature. This feature available only for
		/// SQL Azure and SQL Server 2005+.
		/// </summary>
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7"), EditorBrowsable(EditorBrowsableState.Never)]
		public bool   IsMarsEnabled
		{
			get
			{
				AssertDisposed();

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
				AssertDisposed();

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
				AssertDisposed();

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
				AssertDisposed();

				if (value < 0)
				{
					throw new ArgumentOutOfRangeException(nameof(value), "Timeout value cannot be negative. To reset command timeout use ResetCommandTimeout or ResetCommandTimeoutAsync methods instead.");
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
		/// Resets command timeout to provider or connection defaults.
		/// Note that default provider/connection timeout is not the same value as timeout value you can specify upon context configuration.
		/// </summary>
		public void ResetCommandTimeout()
		{
			// because DbConnection.CommandTimeout doesn't allow user to reset timeout, we must re-create command instead
			// some providers support in-place reset logic (at least SqlClient has ResetCommandTimeout() API), but taking into account how
			// rare this operation it doesn't make sense to add provider-specific reset operation support to IDataProvider interface
			_commandTimeout = null;

#pragma warning disable CS0618 // Type or member is obsolete
			_dataConnection?.ResetCommandTimeout();
#pragma warning restore CS0618 // Type or member is obsolete
		}

		/// <summary>
		/// Sets command timeout to default connection value.
		/// Note that default provider/connection timeout is not the same value as timeout value you can specify upon context configuration.
		/// </summary>
		public ValueTask ResetCommandTimeoutAsync()
		{
			_commandTimeout = null;

#pragma warning disable CS0618 // Type or member is obsolete
			return _dataConnection?.ResetCommandTimeoutAsync() ?? default;
#pragma warning restore CS0618 // Type or member is obsolete
		}

		/// <summary>
		/// Underlying active database connection.
		/// </summary>
#pragma warning disable CA2213 // Disposable fields should be disposed : disposed using DisposeCommand[Async] call from Dispose[Async]
		DataConnection? _dataConnection;
#pragma warning restore CA2213 // Disposable fields should be disposed

		/// <summary>
		/// Creates instance of <see cref="DataConnection"/> class, used by context internally.
		/// </summary>
		/// <returns>New <see cref="DataConnection"/> instance.</returns>
		protected virtual DataConnection CreateDataConnection(DataOptions options)
		{
			AssertDisposed();

			return new(options);
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
				_dataConnection = CreateDataConnection(Options);

				if (_commandTimeout != null)
					_dataConnection.CommandTimeout = CommandTimeout;

				if (_queryHints?.Count > 0)
				{
					_dataConnection.QueryHints.AddRange(_queryHints);
					_queryHints = null;
				}

				if (_nextQueryHints?.Count > 0)
				{
					_dataConnection.NextQueryHints.AddRange(_nextQueryHints);
					_nextQueryHints = null;
				}

				if (OnTraceConnection != null)
#pragma warning disable CS0618 // Type or member is obsolete
					_dataConnection.OnTraceConnection = OnTraceConnection;
#pragma warning restore CS0618 // Type or member is obsolete

				_dataConnection.OnRemoveInterceptor += RemoveInterceptor;
			}

			return _dataConnection;
		}

		void AssertDisposed()
		{
			if (_disposed)
				// GetType().FullName to support inherited types
				throw new ObjectDisposedException(GetType().FullName, "IDataContext is disposed, see https://github.com/linq2db/linq2db/wiki/Managing-data-connection");
		}

		/// <summary>
		/// For active underlying connection, updates information about last executed query <see cref="LastQuery"/> and
		/// releases connection, if it is not locked (<see cref="LockDbManagerCounter"/>)
		/// and <see cref="KeepConnectionAlive"/> is <c>false</c>.
		/// </summary>
		internal void ReleaseQuery()
		{
			AssertDisposed();

			if (_dataConnection != null)
			{
#pragma warning disable CS0618 // Type or member is obsolete
				LastQuery = _dataConnection.LastQuery;
#pragma warning restore CS0618 // Type or member is obsolete

				if (LockDbManagerCounter == 0 && KeepConnectionAlive == false)
				{
					if (_dataConnection.QueryHints.    Count > 0) (_queryHints     ??= new()).AddRange(_dataConnection.QueryHints);
					if (_dataConnection.NextQueryHints.Count > 0) (_nextQueryHints ??= new()).AddRange(_dataConnection.NextQueryHints);

					_dataConnection.OnRemoveInterceptor -= RemoveInterceptor;
					_dataConnection.Dispose();
					_dataConnection = null;
				}
			}
		}

		/// <summary>
		/// For active underlying connection, updates information about last executed query <see cref="LastQuery"/> and
		/// releases connection, if it is not locked (<see cref="LockDbManagerCounter"/>)
		/// and <see cref="KeepConnectionAlive"/> is <c>false</c>.
		/// </summary>
		internal async ValueTask ReleaseQueryAsync()
		{
			AssertDisposed();

			if (_dataConnection != null)
			{
#pragma warning disable CS0618 // Type or member is obsolete
				LastQuery = _dataConnection.LastQuery;
#pragma warning restore CS0618 // Type or member is obsolete

				if (LockDbManagerCounter == 0 && KeepConnectionAlive == false)
				{
					if (_dataConnection.QueryHints.    Count > 0) QueryHints.    AddRange(_queryHints!);
					if (_dataConnection.NextQueryHints.Count > 0) NextQueryHints.AddRange(_nextQueryHints!);

					_dataConnection.OnRemoveInterceptor -= RemoveInterceptor;
					await _dataConnection.DisposeAsync().ConfigureAwait(false);
					_dataConnection = null;
				}
			}
		}

		Func<ISqlBuilder>               IDataContext.CreateSqlBuilder => () => DataProvider.CreateSqlBuilder(MappingSchema, Options);
		Func<DataOptions,ISqlOptimizer> IDataContext.GetSqlOptimizer       => DataProvider.GetSqlOptimizer;
		Type                            IDataContext.DataReaderType        => DataProvider.DataReaderType;
		SqlProviderFlags                IDataContext.SqlProviderFlags      => DataProvider.SqlProviderFlags;
		TableOptions                    IDataContext.SupportedTableOptions => DataProvider.SupportedTableOptions;

		Expression IDataContext.GetReaderExpression(DbDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			AssertDisposed();

			return DataProvider.GetReaderExpression(reader, idx, readerExpression, toType);
		}

		bool? IDataContext.IsDBNullAllowed(DbDataReader reader, int idx) => DataProvider.IsDBNullAllowed(Options, reader, idx);

		public void Dispose()
		{
			Dispose(disposing: true);
		}

		/// <summary>
		/// Closes underlying connection.
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			((IDataContext)this).Close();

			_disposed = true;
		}

		public async ValueTask DisposeAsync()
		{
			await DisposeAsync(disposing: true).ConfigureAwait(false);
		}

		/// <summary>
		/// Closes underlying connection.
		/// </summary>
		protected virtual async ValueTask DisposeAsync(bool disposing)
		{
			await ((IDataContext)this).CloseAsync().ConfigureAwait(false);

			_disposed = true;
		}

		void IDataContext.Close()
		{
			if (_dataContextInterceptor != null)
				using (ActivityService.Start(ActivityID.DataContextInterceptorOnClosing))
					_dataContextInterceptor.OnClosing(new(this));

			if (_dataConnection != null)
			{
				if (_dataConnection.QueryHints.    Count > 0) (_queryHints     ??= new ()).AddRange(_dataConnection.QueryHints);
				if (_dataConnection.NextQueryHints.Count > 0) (_nextQueryHints ??= new ()).AddRange(_dataConnection.NextQueryHints);

				_dataConnection.OnRemoveInterceptor -= RemoveInterceptor;
				_dataConnection.Dispose();
				_dataConnection = null;
			}

			if (_dataContextInterceptor != null)
				using (ActivityService.Start(ActivityID.DataContextInterceptorOnClosed))
					_dataContextInterceptor.OnClosed(new (this));
		}

		async Task IDataContext.CloseAsync()
		{
			if (_dataContextInterceptor != null)
				await using (ActivityService.StartAndConfigureAwait(ActivityID.DataContextInterceptorOnClosingAsync))
					await _dataContextInterceptor.OnClosingAsync(new(this))
						.ConfigureAwait(false);

			if (_dataConnection != null)
			{
				if (_dataConnection.QueryHints.    Count > 0) (_queryHints     ??= new ()).AddRange(_dataConnection.QueryHints);
				if (_dataConnection.NextQueryHints.Count > 0) (_nextQueryHints ??= new ()).AddRange(_dataConnection.NextQueryHints);

				_dataConnection.OnRemoveInterceptor -= RemoveInterceptor;
				await _dataConnection.DisposeAsync().ConfigureAwait(false);
				_dataConnection = null;
			}

			if (_dataContextInterceptor != null)
				await using (ActivityService.StartAndConfigureAwait(ActivityID.DataContextInterceptorOnClosedAsync))
					await _dataContextInterceptor.OnClosedAsync(new (this))
						.ConfigureAwait(false);
		}

		/// <summary>
		/// Starts new transaction for current context with specified isolation level.
		/// If connection already has transaction, it will throw <see cref="InvalidOperationException"/>.
		/// </summary>
		/// <param name="level">Transaction isolation level.</param>
		/// <returns>Database transaction object.</returns>
		/// <exception cref="InvalidOperationException">Thrown when connection already has a transaction.</exception>
		public virtual DataContextTransaction BeginTransaction(IsolationLevel level)
		{
			AssertDisposed();

			var dct = new DataContextTransaction(this);

			dct.BeginTransaction(level);

			return dct;
		}

		/// <summary>
		/// Starts new transaction for current context with default isolation level.
		/// If connection already has transaction, it will throw <see cref="InvalidOperationException"/>.
		/// </summary>
		/// <returns>Database transaction object.</returns>
		/// <exception cref="InvalidOperationException">Thrown when connection already has a transaction.</exception>
		public virtual DataContextTransaction BeginTransaction()
		{
			AssertDisposed();

			var dct = new DataContextTransaction(this);

			dct.BeginTransaction();

			return dct;
		}

		/// <summary>
		/// Starts new transaction asynchronously for current context with specified isolation level.
		/// If connection already has transaction, it will throw <see cref="InvalidOperationException"/>.
		/// </summary>
		/// <param name="level">Transaction isolation level.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Database transaction object.</returns>
		/// <exception cref="InvalidOperationException">Thrown when connection already has a transaction.</exception>
		public virtual async Task<DataContextTransaction> BeginTransactionAsync(IsolationLevel level, CancellationToken cancellationToken = default)
		{
			AssertDisposed();

			var dct = new DataContextTransaction(this);

			await dct.BeginTransactionAsync(level, cancellationToken).ConfigureAwait(false);

			return dct;
		}

		/// <summary>
		/// Starts new transaction asynchronously for current context with default isolation level.
		/// If connection already has transaction, it will throw <see cref="InvalidOperationException"/>.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Database transaction object.</returns>
		/// <exception cref="InvalidOperationException">Thrown when connection already has a transaction.</exception>
		public virtual async Task<DataContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
		{
			AssertDisposed();

			var dct = new DataContextTransaction(this);

			await dct.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

			return dct;
		}

		IQueryRunner IDataContext.GetQueryRunner(Query query, IDataContext parametersContext, int queryNumber, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles)
		{
			AssertDisposed();

			return new QueryRunner(this, ((IDataContext)GetDataConnection()).GetQueryRunner(query, parametersContext, queryNumber, expressions, parameters, preambles));
		}

		sealed class QueryRunner : IQueryRunner
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

			public async ValueTask DisposeAsync()
			{
				await _queryRunner!.DisposeAsync().ConfigureAwait(false);
				await _dataContext!.ReleaseQueryAsync().ConfigureAwait(false);

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

			public IReadOnlyList<QuerySql> GetSqlText()
			{
				return _queryRunner!.GetSqlText();
			}

			public IDataContext      DataContext      => _dataContext!;
			public IQueryExpressions Expressions      => _queryRunner!.Expressions;
			public object?[]?        Parameters       => _queryRunner!.Parameters;
			public object?[]?        Preambles        => _queryRunner!.Preambles;
			public Expression?       MapperExpression { get => _queryRunner!.MapperExpression; set => _queryRunner!.MapperExpression = value; }
			public int               RowsCount        { get => _queryRunner!.RowsCount;        set => _queryRunner!.RowsCount        = value; }
			public int               QueryNumber      { get => _queryRunner!.QueryNumber;      set => _queryRunner!.QueryNumber      = value; }
		}

		internal static class ConfigurationApplier
		{
			public static void Apply(DataContext dataContext, ConnectionOptions options)
			{
				var dataProvider = options.DataProviderFactory == null ? options.DataProvider : options.DataProviderFactory(options);

				switch (
				          options.ConfigurationString,
				                           options.ConnectionString,
				                                                dataProvider,
				                                                             options.ProviderName)
				{
					case (_,               {} connectionString, {} provider, _) :
					{
						dataContext.DataProvider     = provider;
						dataContext.ConnectionString = connectionString;
						dataContext.MappingSchema    = provider.MappingSchema;

						break;
					}
					case (_,               {} connectionString, _,           {} providerName) :
					{
						dataContext.DataProvider     = DataConnection.GetDataProviderEx(providerName, connectionString);
						dataContext.ConnectionString = connectionString;
						dataContext.MappingSchema    = dataContext.DataProvider.MappingSchema;

						break;
					}
					case (_,               _,                   {} provider, _) :
					{
						dataContext.DataProvider  = provider;
						dataContext.MappingSchema = provider.MappingSchema;
						break;
					}
					case ({} configString, _,                   _,           _) :
					{
						dataContext.ConfigurationString = configString;

						var ci = DataConnection.GetConfigurationInfo(configString);

						dataContext.DataProvider     = ci.DataProvider;
						dataContext.ConnectionString = ci.ConnectionString;
						dataContext.MappingSchema    = ci.DataProvider.MappingSchema;

						break;
					}
					case (null,            _,                   _,           _)
						when DataConnection.DefaultConfiguration != null :
					{
						dataContext.ConfigurationString = DataConnection.DefaultConfiguration;

						var ci = DataConnection.GetConfigurationInfo(DataConnection.DefaultConfiguration);

						dataContext.DataProvider     = ci.DataProvider;
						dataContext.ConnectionString = ci.ConnectionString;
						dataContext.MappingSchema    = ci.DataProvider.MappingSchema;

						break;
					}
					default :
						throw new LinqToDBException("Invalid configuration. Configuration string or DataProvider is not provided.");
				}

				if (options.MappingSchema != null)
				{
					dataContext.MappingSchema = options.MappingSchema;
				}
				else if (dataContext.Options.LinqOptions.EnableContextSchemaEdit)
				{
					dataContext.MappingSchema = new (dataContext.MappingSchema);
				}
			}

			public static Action? Reapply(DataContext dataContext, ConnectionOptions options, ConnectionOptions? previousOptions)
			{
				// For ConnectionOptions we reapply only mapping schema and connection interceptor.
				// Connection string, configuration, data provider, etc. are not reapplyable.
				//
				if (options.ConfigurationString       != previousOptions?.ConfigurationString)       throw new LinqToDBException($"Option '{nameof(ConnectionOptions.ConfigurationString)} cannot be changed for context dynamically.");
				if (options.ConnectionString          != previousOptions?.ConnectionString)          throw new LinqToDBException($"Option '{nameof(ConnectionOptions.ConnectionString)} cannot be changed for context dynamically.");
				if (options.ProviderName              != previousOptions?.ProviderName)              throw new LinqToDBException($"Option '{nameof(ConnectionOptions.ProviderName)} cannot be changed for context dynamically.");
				if (options.DbConnection              != previousOptions?.DbConnection)              throw new LinqToDBException($"Option '{nameof(ConnectionOptions.DbConnection)} cannot be changed for context dynamically.");
				if (options.DbTransaction             != previousOptions?.DbTransaction)             throw new LinqToDBException($"Option '{nameof(ConnectionOptions.DbTransaction)} cannot be changed for context dynamically.");
				if (options.DisposeConnection         != previousOptions?.DisposeConnection)         throw new LinqToDBException($"Option '{nameof(ConnectionOptions.DisposeConnection)} cannot be changed for context dynamically.");
				if (options.DataProvider              != previousOptions?.DataProvider)              throw new LinqToDBException($"Option '{nameof(ConnectionOptions.DataProvider)} cannot be changed for context dynamically.");
				if (options.ConnectionFactory         != previousOptions?.ConnectionFactory)         throw new LinqToDBException($"Option '{nameof(ConnectionOptions.ConnectionFactory)} cannot be changed for context dynamically.");
				if (options.DataProviderFactory       != previousOptions?.DataProviderFactory)       throw new LinqToDBException($"Option '{nameof(ConnectionOptions.DataProviderFactory)} cannot be changed for context dynamically.");
				if (options.OnEntityDescriptorCreated != previousOptions?.OnEntityDescriptorCreated) throw new LinqToDBException($"Option '{nameof(ConnectionOptions.OnEntityDescriptorCreated)} cannot be changed for context dynamically.");

				Action? action = null;

				if (!ReferenceEquals(options.ConnectionInterceptor, previousOptions?.ConnectionInterceptor))
				{
					if (previousOptions?.ConnectionInterceptor != null)
						dataContext.RemoveInterceptor(previousOptions.ConnectionInterceptor);

					if (options.ConnectionInterceptor != null)
						dataContext.AddInterceptor(options.ConnectionInterceptor);

					action += () =>
					{
						if (options.ConnectionInterceptor != null)
							dataContext.RemoveInterceptor(options.ConnectionInterceptor);

						if (previousOptions?.ConnectionInterceptor != null)
							dataContext.AddInterceptor(previousOptions.ConnectionInterceptor);
					};
				}

				if (!ReferenceEquals(options.MappingSchema, previousOptions?.MappingSchema))
				{
					var mappingSchema = dataContext.MappingSchema;

					dataContext.MappingSchema = dataContext.DataProvider.MappingSchema;

					if (options.MappingSchema != null)
					{
						dataContext.MappingSchema = options.MappingSchema;
					}
					else if (dataContext.Options.LinqOptions.EnableContextSchemaEdit)
					{
						dataContext.MappingSchema = new (dataContext.MappingSchema);
					}

					action += () => dataContext.MappingSchema = mappingSchema;
				}

				return action;
			}

			public static void Apply(DataContext dataContext, DataContextOptions options)
			{
				dataContext._commandTimeout = options.CommandTimeout;

				if (options.Interceptors != null)
					foreach (var interceptor in options.Interceptors)
						dataContext.AddInterceptor(interceptor, false);
			}

			public static Action? Reapply(DataContext dataContext, DataContextOptions options, DataContextOptions? previousOptions)
			{
				Action? action = null;

				if (options.CommandTimeout != previousOptions?.CommandTimeout)
				{
					var commandTimeout = dataContext._commandTimeout;

					if (options.CommandTimeout != null)
						dataContext.CommandTimeout = options.CommandTimeout.Value;
					else
						dataContext.ResetCommandTimeout();

					action += () =>
					{
						if (commandTimeout != null)
							dataContext.CommandTimeout = commandTimeout.Value;
						else
							dataContext.ResetCommandTimeout();
					};
				}

				if (!ReferenceEquals(options.Interceptors, previousOptions?.Interceptors))
				{
					if (previousOptions?.Interceptors != null)
						foreach (var interceptor in previousOptions.Interceptors)
							dataContext.RemoveInterceptor(interceptor);

					if (options.Interceptors != null)
						foreach (var interceptor in options.Interceptors)
							dataContext.AddInterceptor(interceptor);

					action += () =>
					{
						if (options.Interceptors != null)
							foreach (var interceptor in options.Interceptors)
								dataContext.RemoveInterceptor(interceptor);

						if (previousOptions?.Interceptors != null)
							foreach (var interceptor in previousOptions.Interceptors)
								dataContext.AddInterceptor(interceptor);
					};
				}

				return action;
			}
		}

		/// <summary>
		/// Gets service provider, used for data connection instance.
		/// </summary>
		IServiceProvider IInfrastructure<IServiceProvider>.Instance => ((IInfrastructure<IServiceProvider>)DataProvider).Instance;

		/// <inheritdoc cref="IDataContext.UseOptions"/>
		public IDisposable? UseOptions(Func<DataOptions,DataOptions> optionsSetter)
		{
			var prevOptions = Options;
			var newOptions  = optionsSetter(Options) ?? throw new ArgumentNullException(nameof(optionsSetter));

			if (((IConfigurationID)prevOptions).ConfigurationID == ((IConfigurationID)newOptions).ConfigurationID)
				return null;

			var configurationID = _configurationID;

			Options          = newOptions;
			_configurationID = null;

			var action = Options.Reapply(this, prevOptions);

			action += () =>
			{
				Options          = prevOptions;

#if DEBUG
				_configurationID = null;
#else
				_configurationID = configurationID;
#endif
			};

			return new DisposableAction(action);
		}

		/// <inheritdoc cref="IDataContext.UseMappingSchema"/>
		public IDisposable? UseMappingSchema(MappingSchema mappingSchema)
		{
			var oldSchema       = MappingSchema;
			var configurationID = _configurationID;

			AddMappingSchema(mappingSchema);

			return new DisposableAction(() =>
			{
				((IDataContext)this).SetMappingSchema(oldSchema);
				_configurationID = configurationID;
			});
		}
	}
}
