using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;

using JetBrains.Annotations;

namespace LinqToDB.Data
{
	using Async;
	using Common.Internal;
	using Common;
	using DataProvider;
	using Expressions;
	using Infrastructure;
	using Interceptors;
	using Mapping;
	using RetryPolicy;
	using Tools;

	/// <summary>
	/// Implements persistent database connection abstraction over different database engines.
	/// Could be initialized using connection string name or connection string,
	/// or attached to existing connection or transaction.
	/// </summary>
	[PublicAPI]
	public partial class DataConnection : IDataContext, IInfrastructure<IServiceProvider>
	{
		#region .ctor

		/// <summary>
		/// Creates database connection object that uses default connection configuration from <see cref="DefaultConfiguration"/> property.
		/// </summary>
		public DataConnection() : this(DefaultDataOptions)
		{
		}

		/// <summary>
		/// Creates database connection object that uses default connection configuration from <see cref="DefaultConfiguration"/> property.
		/// </summary>
		public DataConnection(Func<DataOptions,DataOptions> optionsSetter) : this(optionsSetter(DefaultDataOptions))
		{
		}

		/// <summary>
		/// Creates database connection object that uses provided connection configuration.
		/// </summary>
		/// <param name="configurationString">Name of database connection configuration to use with this connection.
		/// In case of <c>null</c>, configuration from <see cref="DefaultConfiguration"/> property will be used.</param>
		public DataConnection(string? configurationString)
			: this(configurationString == null
				? DefaultDataOptions
				: ConnectionOptionsByConfigurationString.GetOrAdd(configurationString, _ => new(new(configurationString))))
		{
		}

		/// <summary>
		/// Creates database connection object that uses provided connection configuration.
		/// </summary>
		/// <param name="configurationString">Name of database connection configuration to use with this connection.
		/// In case of <c>null</c>, configuration from <see cref="DefaultConfiguration"/> property will be used.</param>
		public DataConnection(string? configurationString, Func<DataOptions,DataOptions> optionsSetter)
			: this(optionsSetter(configurationString == null
				? DefaultDataOptions
				: ConnectionOptionsByConfigurationString.GetOrAdd(configurationString, _ => new(new(configurationString)))))
		{
		}

		/// <summary>
		/// Creates database connection object that uses default connection configuration from <see cref="DefaultConfiguration"/> property and provided mapping schema.
		/// </summary>
		/// <param name="mappingSchema">Mapping schema to use with this connection.</param>
		public DataConnection(MappingSchema mappingSchema) : this(DefaultDataOptions.UseMappingSchema(mappingSchema))
		{
		}

		/// <summary>
		/// Creates database connection object that uses default connection configuration from <see cref="DefaultConfiguration"/> property and provided mapping schema.
		/// </summary>
		/// <param name="mappingSchema">Mapping schema to use with this connection.</param>
		public DataConnection(MappingSchema mappingSchema, Func<DataOptions,DataOptions> optionsSetter)
			: this(optionsSetter(DefaultDataOptions.UseMappingSchema(mappingSchema)))
		{
		}

		/// <summary>
		/// Creates database connection object that uses provided connection configuration and mapping schema.
		/// </summary>
		/// <param name="configurationString">Name of database connection configuration to use with this connection.
		/// In case of null, configuration from <see cref="DefaultConfiguration"/> property will be used.</param>
		/// <param name="mappingSchema">Mapping schema to use with this connection.</param>
		public DataConnection(string? configurationString, MappingSchema mappingSchema)
			: this(DefaultDataOptions.UseConfigurationString(configurationString).UseMappingSchema(mappingSchema))
		{
		}

		/// <summary>
		/// Creates database connection object that uses provided connection configuration and mapping schema.
		/// </summary>
		/// <param name="configurationString">Name of database connection configuration to use with this connection.
		/// In case of null, configuration from <see cref="DefaultConfiguration"/> property will be used.</param>
		/// <param name="mappingSchema">Mapping schema to use with this connection.</param>
		public DataConnection(string? configurationString, MappingSchema mappingSchema, Func<DataOptions,DataOptions> optionsSetter)
			: this(optionsSetter(DefaultDataOptions.UseConfigurationString(configurationString).UseMappingSchema(mappingSchema)))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider, connection string and mapping schema.
		/// </summary>
		/// <param name="providerName">Name of database provider to use with this connection. <see cref="ProviderName"/> class for list of providers.</param>
		/// <param name="connectionString">Database connection string to use for connection with database.</param>
		/// <param name="mappingSchema">Mapping schema to use with this connection.</param>
		public DataConnection(
			string        providerName,
			string        connectionString,
			MappingSchema mappingSchema)
			: this(DefaultDataOptions.UseConnectionString(providerName, connectionString).UseMappingSchema(mappingSchema))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider, connection string and mapping schema.
		/// </summary>
		/// <param name="providerName">Name of database provider to use with this connection. <see cref="ProviderName"/> class for list of providers.</param>
		/// <param name="connectionString">Database connection string to use for connection with database.</param>
		/// <param name="mappingSchema">Mapping schema to use with this connection.</param>
		public DataConnection(
			string                        providerName,
			string                        connectionString,
			MappingSchema                 mappingSchema,
			Func<DataOptions,DataOptions> optionsSetter)
			: this(optionsSetter(DefaultDataOptions.UseConnectionString(providerName, connectionString).UseMappingSchema(mappingSchema)))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider and connection string.
		/// </summary>
		/// <param name="providerName">Name of database provider to use with this connection. <see cref="ProviderName"/> class for list of providers.</param>
		/// <param name="connectionString">Database connection string to use for connection with database.</param>
		public DataConnection(
			string providerName,
			string connectionString)
			: this(DefaultDataOptions.UseConnectionString(providerName, connectionString))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider and connection string.
		/// </summary>
		/// <param name="providerName">Name of database provider to use with this connection. <see cref="ProviderName"/> class for list of providers.</param>
		/// <param name="connectionString">Database connection string to use for connection with database.</param>
		public DataConnection(
			string                        providerName,
			string                        connectionString,
			Func<DataOptions,DataOptions> optionsSetter)
			: this(optionsSetter(DefaultDataOptions.UseConnectionString(providerName, connectionString)))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider, connection string and mapping schema.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="connectionString">Database connection string to use for connection with database.</param>
		/// <param name="mappingSchema">Mapping schema to use with this connection.</param>
		public DataConnection(
			IDataProvider dataProvider,
			string        connectionString,
			MappingSchema mappingSchema)
			: this(DefaultDataOptions.UseConnectionString(dataProvider, connectionString).UseMappingSchema(mappingSchema))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider, connection string and mapping schema.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="connectionString">Database connection string to use for connection with database.</param>
		/// <param name="mappingSchema">Mapping schema to use with this connection.</param>
		public DataConnection(
			IDataProvider                 dataProvider,
			string                        connectionString,
			MappingSchema                 mappingSchema,
			Func<DataOptions,DataOptions> optionsSetter)
			: this(optionsSetter(DefaultDataOptions.UseConnectionString(dataProvider, connectionString).UseMappingSchema(mappingSchema)))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider and connection string.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="connectionString">Database connection string to use for connection with database.</param>
		public DataConnection(
			IDataProvider dataProvider,
			string        connectionString)
			: this(DefaultDataOptions.UseConnectionString(dataProvider, connectionString))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider and connection string.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="connectionString">Database connection string to use for connection with database.</param>
		public DataConnection(
			IDataProvider                 dataProvider,
			string                        connectionString,
			Func<DataOptions,DataOptions> optionsSetter)
			: this(optionsSetter(DefaultDataOptions.UseConnectionString(dataProvider, connectionString)))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider, connection factory and mapping schema.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="connectionFactory">Database connection factory method.</param>
		/// <param name="mappingSchema">Mapping schema to use with this connection.</param>
		public DataConnection(
			IDataProvider                   dataProvider,
			Func<DataOptions, DbConnection> connectionFactory,
			MappingSchema                   mappingSchema)
			: this(DefaultDataOptions.UseConnectionFactory(dataProvider, connectionFactory).UseMappingSchema(mappingSchema))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider, connection factory and mapping schema.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="connectionFactory">Database connection factory method.</param>
		/// <param name="mappingSchema">Mapping schema to use with this connection.</param>
		public DataConnection(
			IDataProvider                   dataProvider,
			Func<DataOptions, DbConnection> connectionFactory,
			MappingSchema                   mappingSchema,
			Func<DataOptions,DataOptions>   optionsSetter)
			: this(optionsSetter(DefaultDataOptions.UseConnectionFactory(dataProvider, connectionFactory).UseMappingSchema(mappingSchema)))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider and connection factory.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="connectionFactory">Database connection factory method.</param>
		public DataConnection(
			IDataProvider                   dataProvider,
			Func<DataOptions, DbConnection> connectionFactory)
			: this(DefaultDataOptions.UseConnectionFactory(dataProvider, connectionFactory))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider and connection factory.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="connectionFactory">Database connection factory method.</param>
		public DataConnection(
			IDataProvider                   dataProvider,
			Func<DataOptions, DbConnection> connectionFactory,
			Func<DataOptions,DataOptions>   optionsSetter)
			: this(optionsSetter(DefaultDataOptions.UseConnectionFactory(dataProvider, connectionFactory)))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider, connection and mapping schema.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="connection">Existing database connection to use.</param>
		/// <param name="mappingSchema">Mapping schema to use with this connection.</param>
		public DataConnection(
			IDataProvider dataProvider,
			DbConnection  connection,
			MappingSchema mappingSchema)
			: this(DefaultDataOptions.UseConnection(dataProvider, connection).UseMappingSchema(mappingSchema))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider, connection and mapping schema.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="connection">Existing database connection to use.</param>
		/// <param name="mappingSchema">Mapping schema to use with this connection.</param>
		public DataConnection(
			IDataProvider                 dataProvider,
			DbConnection                  connection,
			MappingSchema                 mappingSchema,
			Func<DataOptions,DataOptions> optionsSetter)
			: this(optionsSetter(DefaultDataOptions.UseConnection(dataProvider, connection).UseMappingSchema(mappingSchema)))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider and connection.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="connection">Existing database connection to use.</param>
		/// <remarks>
		/// <paramref name="connection"/> would not be disposed.
		/// </remarks>
		public DataConnection(
			IDataProvider dataProvider,
			DbConnection  connection)
			: this(dataProvider, connection, false)
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider and connection.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="connection">Existing database connection to use.</param>
		/// <remarks>
		/// <paramref name="connection"/> would not be disposed.
		/// </remarks>
		public DataConnection(
			IDataProvider dataProvider,
			DbConnection  connection,
			Func<DataOptions,DataOptions> optionsSetter)
			: this(dataProvider, connection, false, optionsSetter)
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider and connection.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="connection">Existing database connection to use.</param>
		/// <param name="disposeConnection">If true <paramref name="connection"/> would be disposed on DataConnection disposing.</param>
		public DataConnection(
			IDataProvider dataProvider,
			DbConnection  connection,
			bool          disposeConnection)
			: this(DefaultDataOptions.UseConnection(dataProvider, connection, disposeConnection))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider and connection.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="connection">Existing database connection to use.</param>
		/// <param name="disposeConnection">If true <paramref name="connection"/> would be disposed on DataConnection disposing.</param>
		public DataConnection(
			IDataProvider                 dataProvider,
			DbConnection                  connection,
			bool                          disposeConnection,
			Func<DataOptions,DataOptions> optionsSetter)
			: this(optionsSetter(DefaultDataOptions.UseConnection(dataProvider, connection, disposeConnection)))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider, transaction and mapping schema.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="transaction">Existing database transaction to use.</param>
		/// <param name="mappingSchema">Mapping schema to use with this connection.</param>
		public DataConnection(
			IDataProvider dataProvider,
			DbTransaction transaction,
			MappingSchema mappingSchema)
			: this(DefaultDataOptions.UseTransaction(dataProvider, transaction).UseMappingSchema(mappingSchema))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider, transaction and mapping schema.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="transaction">Existing database transaction to use.</param>
		/// <param name="mappingSchema">Mapping schema to use with this connection.</param>
		public DataConnection(
			IDataProvider                 dataProvider,
			DbTransaction                 transaction,
			MappingSchema                 mappingSchema,
			Func<DataOptions,DataOptions> optionsSetter)
			: this(optionsSetter(DefaultDataOptions.UseTransaction(dataProvider, transaction).UseMappingSchema(mappingSchema)))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider and transaction.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="transaction">Existing database transaction to use.</param>
		public DataConnection(
			IDataProvider dataProvider,
			DbTransaction transaction)
			: this(DefaultDataOptions.UseTransaction(dataProvider, transaction))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider and transaction.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="transaction">Existing database transaction to use.</param>
		public DataConnection(
			IDataProvider                 dataProvider,
			DbTransaction                 transaction,
			Func<DataOptions,DataOptions> optionsSetter)
			: this(optionsSetter(DefaultDataOptions.UseTransaction(dataProvider, transaction)))
		{
		}

		/// <summary>
		/// Creates database connection object that uses a <see cref="DataOptions"/> to configure the connection.
		/// </summary>
		/// <param name="options">Options, setup ahead of time.</param>
#pragma warning disable CS8618
		public DataConnection(DataOptions options)
		{
#if DEBUG
			_dataConnectionID = Interlocked.Increment(ref _dataConnectionIDCounter);
			Interlocked.Increment(ref _dataConnectionCount);
#endif

			Options = options ?? throw new ArgumentNullException(nameof(options));

			options.Apply(this);

			DataProvider!.InitContext(this);
		}

#if DEBUG
		int _dataConnectionID;
		static int _dataConnectionIDCounter;
		static int _dataConnectionCount;
#endif

#pragma warning restore CS8618

		#endregion

		#region Public Properties

		/// <summary>
		/// Current DataContext options
		/// </summary>
		public DataOptions   Options             { get; private set; }

		/// <summary>
		/// Database configuration name (connection string name).
		/// </summary>
		public string?       ConfigurationString { get; private set; }

		/// <summary>
		/// Database provider implementation for specific database engine.
		/// </summary>
		public IDataProvider DataProvider        { get; internal set; }
		/// <summary>
		/// Database connection string.
		/// </summary>
		public string?       ConnectionString    { get; private set; }
		/// <summary>
		/// Retry policy for current connection.
		/// </summary>
		public IRetryPolicy? RetryPolicy         { get; set; }
		public object?       Tag                 { get; set; }

		private bool? _isMarsEnabled;
		/// <summary>
		/// Gets or sets status of Multiple Active Result Sets (MARS) feature. This feature available only for
		/// SQL Azure and SQL Server 2005+.
		/// </summary>
		public  bool   IsMarsEnabled
		{
			get
			{
				_isMarsEnabled ??= (bool)(DataProvider.GetConnectionInfo(this, "IsMarsEnabled") ?? false);

				return _isMarsEnabled.Value;
			}
			set => _isMarsEnabled = value;
		}

		/// <summary>
		/// Gets or sets default trace handler.
		/// </summary>
		public static Action<TraceInfo> DefaultOnTraceConnection { get; set; } = OnTraceInternal;

		/// <summary>
		/// Gets or sets trace handler, used for current connection instance.
		/// Configured on the connection builder using <see cref="DataOptionsExtensions.UseTracing(DataOptions,Action{TraceInfo})"/>.
		/// defaults to <see cref="WriteTraceLineConnection"/> calls.
		/// </summary>
		public Action<TraceInfo> OnTraceConnection { get; set; } = DefaultOnTraceConnection;

		/// <summary>
		/// Writes the trace out using <see cref="WriteTraceLineConnection"/>.
		/// </summary>
		static void OnTraceInternal(TraceInfo info)
		{
			using var m = ActivityService.Start(ActivityID.OnTraceInternal);

			var dc = info.DataConnection;

			switch (info.TraceInfoStep)
			{
				case TraceInfoStep.BeforeExecute:
					dc.WriteTraceLineConnection(
						$"{info.TraceInfoStep}{Environment.NewLine}{info.SqlText}",
						dc.TraceSwitchConnection.DisplayName,
						info.TraceLevel);
					break;

				case TraceInfoStep.AfterExecute:
				{
					dc.WriteTraceLineConnection(
						info.RecordsAffected != null
							? FormattableString.Invariant($"Query Execution Time ({info.TraceInfoStep}){GetTagInfo()}: {info.ExecutionTime}. Records Affected: {info.RecordsAffected}.\r\n")
							: FormattableString.Invariant($"Query Execution Time ({info.TraceInfoStep}){GetTagInfo()}: {info.ExecutionTime}\r\n"),
						dc.TraceSwitchConnection.DisplayName,
						info.TraceLevel);
					break;
				}

				case TraceInfoStep.Error:
				{
					using var sb = Pools.StringBuilder.Allocate();

					sb.Value.Append(CultureInfo.InvariantCulture, $"{info.TraceInfoStep}");

					for (var ex = info.Exception; ex != null; ex = ex.InnerException)
					{
						try
						{
							sb.Value
								.AppendLine()
								.AppendLine(CultureInfo.InvariantCulture, $"Exception: {ex.GetType()}")
								.AppendLine(CultureInfo.InvariantCulture, $"Message  : {ex.Message}")
								.AppendLine(ex.StackTrace)
								;
						}
						catch
						{
							// Sybase provider could generate exception that will throw another exception when you
							// try to access Message property due to bug in AseErrorCollection.Message property.
							// There it tries to fetch error from first element of list without checking wether
							// list contains any elements or not
							sb.Value
								.AppendLine()
								.Append(CultureInfo.InvariantCulture, $"Failed while tried to log failure of type {ex.GetType()}")
								;
						}
					}

					dc.WriteTraceLineConnection(sb.Value.ToString(), dc.TraceSwitchConnection.DisplayName, info.TraceLevel);

					break;
				}

				case TraceInfoStep.MapperCreated:
				{
					using var sb = Pools.StringBuilder.Allocate();

					sb.Value.AppendLine(info.TraceInfoStep.ToString());

					if (info.MapperExpression != null && dc.Options.LinqOptions.TraceMapperExpression)
						sb.Value.AppendLine(info.MapperExpression.GetDebugView());

					dc.WriteTraceLineConnection(sb.Value.ToString(), dc.TraceSwitchConnection.DisplayName, info.TraceLevel);

					break;
				}

				case TraceInfoStep.Completed:
				{
					using var sb = Pools.StringBuilder.Allocate();

					sb.Value.Append(CultureInfo.InvariantCulture, $"Total Execution Time ({info.TraceInfoStep}){GetTagInfo()}: {info.ExecutionTime}.");

					if (info.RecordsAffected != null)
						sb.Value.Append(CultureInfo.InvariantCulture, $" Rows Count: {info.RecordsAffected}.");

					sb.Value.AppendLine();

					dc.WriteTraceLineConnection(sb.Value.ToString(), dc.TraceSwitchConnection.DisplayName, info.TraceLevel);

					break;
				}
			}

			string GetTagInfo()
			{
				return (info.IsAsync, Client: info.DataConnection.Tag) switch
				{
					(true,  not null) => $" (async, {info.DataConnection.Tag})",
					(true,      null) =>  " (async)",
					(false, not null) => $" ({info.DataConnection.Tag})",
					(false,     null) => "",
				};
			}
		}

		static TraceSwitch _traceSwitch = new ("DataConnection",
			"DataConnection trace switch",
#if DEBUG
			"Warning"
#else
				"Off"
#endif
		);

		/// <summary>
		/// Gets or sets global data connection trace options. Used for all new connections
		/// unless <see cref="DataOptionsExtensions.UseTraceLevel"/> is called on builder.
		/// defaults to off unless library was built in debug mode.
		/// <remarks>Should only be used when <see cref="TraceSwitchConnection"/> can not be used!</remarks>
		/// </summary>
		public static TraceSwitch TraceSwitch
		{
			// used by LoggingExtensions
			get => _traceSwitch;
			set => Volatile.Write(ref _traceSwitch, value);
		}

		/// <summary>
		/// Sets tracing level for data connections.
		/// </summary>
		/// <param name="traceLevel">Connection tracing level.</param>
		/// <remarks>Use <see cref="TraceSwitchConnection"/> when possible, configured via <see cref="DataOptionsExtensions.UseTraceLevel"/>.</remarks>
		public static void TurnTraceSwitchOn(TraceLevel traceLevel = TraceLevel.Info)
		{
			TraceSwitch = new TraceSwitch("DataConnection", "DataConnection trace switch", traceLevel.ToString());
		}

		TraceSwitch? _traceSwitchConnection;

		/// <summary>
		/// gets or sets the trace switch,
		/// this is used by some methods to determine if <see cref="OnTraceConnection"/> should be called.
		/// defaults to <see cref="TraceSwitch"/>
		/// used for current connection instance.
		/// </summary>
		public TraceSwitch TraceSwitchConnection
		{
			get => _traceSwitchConnection ?? _traceSwitch;
			set => _traceSwitchConnection = value;
		}

		/// <summary>
		/// Trace function. By Default use <see cref="Debug"/> class for logging, but could be replaced to log e.g. to your log file.
		/// will be ignored if <see cref="DataOptionsExtensions.UseTraceWith"/> is called on builder
		/// <para>First parameter contains trace message.</para>
		/// <para>Second parameter contains trace message category (<see cref="Switch.DisplayName"/>).</para>
		/// <para>Third parameter contains trace level for message (<see cref="TraceLevel"/>).</para>
		/// <seealso cref="TraceSwitch"/>
		/// <remarks>Should only not use to write trace lines, only use <see cref="WriteTraceLineConnection"/>.</remarks>
		/// </summary>
		public static Action<string,string,TraceLevel> WriteTraceLine = (message, category, level) => Debug.WriteLine(message, category);

		/// <summary>
		/// Gets the delegate to write logging messages for this connection.
		/// Defaults to <see cref="WriteTraceLine"/>.
		/// Used for the current instance.
		/// </summary>
		public Action<string,string,TraceLevel> WriteTraceLineConnection { get; protected set; } = WriteTraceLine;

		#endregion

		#region Connection

		bool                             _closeConnection;
		bool                             _disposeConnection = true;
		bool                             _closeTransaction;
		IAsyncDbConnection?              _connection;
		Func<DataOptions, DbConnection>? _connectionFactory;

		// TODO: V6 remove it or replace with non-creating access + creation method if such public APIs needed
		/// <summary>
		/// Gets underlying database connection, used by current connection object.
		/// </summary>
		public DbConnection Connection => EnsureConnection().Connection;

		internal DbConnection? CurrentConnection => _connection?.Connection;

		internal IAsyncDbConnection EnsureConnection(bool connect = true)
		{
			CheckAndThrowOnDisposed();

			try
			{
				if (_connection == null)
				{
					DbConnection connection;

					if (_connectionFactory != null)
						connection = _connectionFactory(Options);
					else
						connection = DataProvider.CreateConnection(ConnectionString!);

					_connection = AsyncFactory.CreateAndSetDataContext(this, connection);

					if (RetryPolicy != null)
						_connection = new RetryingDbConnection(this, _connection, RetryPolicy);
				}
				else if (RetryPolicy != null && _connection is not RetryingDbConnection)
					_connection = new RetryingDbConnection(this, _connection, RetryPolicy);

				if (connect && _connection.State == ConnectionState.Closed)
				{
					var interceptor = ((IInterceptable<IConnectionInterceptor>)this).Interceptor;
					if (interceptor != null)
					{
						using (ActivityService.Start(ActivityID.ConnectionInterceptorConnectionOpening))
							interceptor.ConnectionOpening(new(this), _connection.Connection);
					}

					_connection.Open();
					_closeConnection = true;

					if (interceptor != null)
					{
						using (ActivityService.Start(ActivityID.ConnectionInterceptorConnectionOpened))
							interceptor.ConnectionOpened(new(this), _connection.Connection);
					}
				}
			}
			catch (Exception ex)
			{
				if (TraceSwitchConnection.TraceError)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.Error, TraceOperation.Open, false)
					{
						TraceLevel = TraceLevel.Error,
						StartTime = DateTime.UtcNow,
						Exception = ex,
					});
				}

				throw;
			}

			return _connection;
		}

		/// <summary>
		/// Closes and dispose associated underlying database transaction/connection.
		/// </summary>
		public virtual void Close()
		{
			var interceptor = ((IInterceptable<IDataContextInterceptor>)this).Interceptor;
			if (interceptor != null)
			{
				using (ActivityService.Start(ActivityID.DataContextInterceptorOnClosing))
					interceptor.OnClosing(new(this));
			}

			DisposeCommand();

			if (TransactionAsync != null && _closeTransaction)
			{
				TransactionAsync.Dispose();
				TransactionAsync = null;
			}

			if (_connection != null)
			{
				if (_disposeConnection)
				{
					_connection.Dispose();
					_connection = null;
				}
				else if (_closeConnection)
					_connection.Close();
			}

			if (interceptor != null)
			{
				using (ActivityService.Start(ActivityID.DataContextInterceptorOnClosed))
					interceptor.OnClosed(new (this));
			}
		}

		#endregion

		#region Command

#pragma warning disable CA2213 // Disposable fields should be disposed : disposed using Close[Async] call from Dispose[Async]
		private DbCommand? _command;
#pragma warning restore CA2213 // Disposable fields should be disposed

		/// <summary>
		/// Gets current command instance if it exists or <c>null</c> otherwise.
		/// </summary>
		internal DbCommand? CurrentCommand => _command;

		/// <summary>
		/// Creates if needed and returns current command instance.
		/// </summary>
		internal DbCommand GetOrCreateCommand() => _command ??= CreateCommand();

		/// <summary>
		/// Contains text of last command, sent to database using current connection.
		/// </summary>
		public string? LastQuery { get; private set; }

		internal void InitCommand(CommandType commandType, string sql, DataParameter[]? parameters, IReadOnlyCollection<string>? queryHints, bool withParameters)
		{
			if (queryHints?.Count > 0)
			{
				var sqlProvider = DataProvider.CreateSqlBuilder(MappingSchema, Options);
				sql             = sqlProvider.ApplyQueryHints(sql, queryHints);
			}

			_command = DataProvider.InitCommand(this, GetOrCreateCommand(), commandType, sql, parameters, withParameters);
		}

		internal void CommitCommandInit()
		{
			var interceptor = ((IInterceptable<ICommandInterceptor>)this).Interceptor;
			if (interceptor != null)
			{
				using (ActivityService.Start(ActivityID.CommandInterceptorCommandInitialized))
					_command = interceptor.CommandInitialized(new (this), _command!);
			}

			LastQuery = _command!.CommandText;
		}

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
					// to reset to default timeout we dispose command because as command has no reset timeout API
					_commandTimeout = null;
					// TODO: that's not good - user is not aware that he can trigger blocking operation
					// we should postpone disposal till command used (or redesign CommandTimeout to methods)
					DisposeCommand();
				}
				else
				{
					_commandTimeout = value;
					if (_command != null)
						_command.CommandTimeout = value;
				}
			}
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// </summary>
		public DbCommand CreateCommand()
		{
			var command = EnsureConnection().CreateCommand();

			if (_commandTimeout.HasValue)
				command.CommandTimeout = _commandTimeout.Value;

			if (TransactionAsync != null)
				command.Transaction = Transaction;

			return command;
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// </summary>
		public void DisposeCommand()
		{
			if (_command != null)
			{
				DataProvider.DisposeCommand(_command);
				_command = null;
			}
		}

		#region ExecuteNonQuery

		protected virtual int ExecuteNonQuery(DbCommand command)
		{
			try
			{
				if (((IInterceptable<ICommandInterceptor>)this).Interceptor is { } cInterceptor)
				{
					Option<int> result;

					using (ActivityService.Start(ActivityID.CommandInterceptorExecuteNonQuery))
						result = cInterceptor.ExecuteNonQuery(new(this), command, Option<int>.None);

					if (result.HasValue)
						return result.Value;
				}

				using (ActivityService.Start(ActivityID.CommandExecuteNonQuery)?.AddQueryInfo(this, _command!.Connection, _command))
					return command.ExecuteNonQuery();
			}
			catch (Exception ex) when (((IInterceptable<IExceptionInterceptor>)this).Interceptor is { } eInterceptor)
			{
				using (ActivityService.Start(ActivityID.ExceptionInterceptorProcessException))
					eInterceptor.ProcessException(new(this), ex);
				throw;
			}
		}

		internal int ExecuteNonQuery()
		{
			if (TraceSwitchConnection.Level == TraceLevel.Off)
				using (DataProvider.ExecuteScope(this))
					return ExecuteNonQuery(CurrentCommand!);

			var now = DateTime.UtcNow;
			var sw  = Stopwatch.StartNew();

			if (TraceSwitchConnection.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(this, TraceInfoStep.BeforeExecute, TraceOperation.ExecuteNonQuery, false)
				{
					TraceLevel     = TraceLevel.Info,
					Command        = CurrentCommand,
					StartTime      = now,
				});
			}

			try
			{
				int ret;
				using (DataProvider.ExecuteScope(this))
					ret = ExecuteNonQuery(CurrentCommand!);

				if (TraceSwitchConnection.TraceInfo)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.AfterExecute, TraceOperation.ExecuteNonQuery, false)
					{
						TraceLevel      = TraceLevel.Info,
						Command         = CurrentCommand,
						StartTime       = now,
						ExecutionTime   = sw.Elapsed,
						RecordsAffected = ret,
					});
				}

				return ret;
			}
			catch (Exception ex)
			{
				if (TraceSwitchConnection.TraceError)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.Error, TraceOperation.ExecuteNonQuery, false)
					{
						TraceLevel     = TraceLevel.Error,
						Command        = CurrentCommand,
						StartTime      = now,
						ExecutionTime  = sw.Elapsed,
						Exception      = ex,
					});
				}

				throw;
			}
		}

		internal int ExecuteNonQueryCustom(DbCommand command, Func<DbCommand, int> customExecute)
		{
			try
			{
				if (((IInterceptable<ICommandInterceptor>)this).Interceptor is { } cInterceptor)
				{
					Option<int> result;

					using (ActivityService.Start(ActivityID.CommandInterceptorExecuteNonQuery))
						result = cInterceptor.ExecuteNonQuery(new(this), command, Option<int>.None);

					if (result.HasValue)
						return result.Value;
				}

				using (ActivityService.Start(ActivityID.CommandExecuteNonQuery)?.AddQueryInfo(this, _command!.Connection, _command))
					return customExecute(command);
			}
			catch (Exception ex) when (((IInterceptable<IExceptionInterceptor>)this).Interceptor is { } eInterceptor)
			{
				using (ActivityService.Start(ActivityID.ExceptionInterceptorProcessException))
					eInterceptor.ProcessException(new(this), ex);
				throw;
			}
		}

		internal int ExecuteNonQueryCustom(Func<DbCommand, int> customExecute)
		{
			if (TraceSwitchConnection.Level == TraceLevel.Off)
				using (DataProvider.ExecuteScope(this))
					return ExecuteNonQueryCustom(CurrentCommand!, customExecute);

			var now = DateTime.UtcNow;
			var sw  = Stopwatch.StartNew();

			if (TraceSwitchConnection.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(this, TraceInfoStep.BeforeExecute, TraceOperation.ExecuteNonQuery, false)
				{
					TraceLevel = TraceLevel.Info,
					Command = CurrentCommand,
					StartTime = now,
				});
			}

			try
			{
				int ret;
				using (DataProvider.ExecuteScope(this))
					ret = ExecuteNonQueryCustom(CurrentCommand!, customExecute);

				if (TraceSwitchConnection.TraceInfo)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.AfterExecute, TraceOperation.ExecuteNonQuery, false)
					{
						TraceLevel = TraceLevel.Info,
						Command = CurrentCommand,
						StartTime = now,
						ExecutionTime = sw.Elapsed,
						RecordsAffected = ret,
					});
				}

				return ret;
			}
			catch (Exception ex)
			{
				if (TraceSwitchConnection.TraceError)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.Error, TraceOperation.ExecuteNonQuery, false)
					{
						TraceLevel = TraceLevel.Error,
						Command = CurrentCommand,
						StartTime = now,
						ExecutionTime = sw.Elapsed,
						Exception = ex,
					});
				}

				throw;
			}
		}

		#endregion

		#region ExecuteScalar

		protected virtual object? ExecuteScalar(DbCommand command)
		{
			try
			{
				if (((IInterceptable<ICommandInterceptor>)this).Interceptor is { } cInterceptor)
				{
					Option<object?> result;

					using (ActivityService.Start(ActivityID.CommandInterceptorExecuteScalar))
						result = cInterceptor.ExecuteScalar(new(this), command, Option<object?>.None);

					if (result.HasValue)
						return result.Value;
				}

				using (ActivityService.Start(ActivityID.CommandExecuteScalar)?.AddQueryInfo(this, Connection, _command))
					return command.ExecuteScalar();
			}
			catch (Exception ex) when (((IInterceptable<IExceptionInterceptor>)this).Interceptor is { } eInterceptor)
			{
				using (ActivityService.Start(ActivityID.ExceptionInterceptorProcessException))
					eInterceptor.ProcessException(new(this), ex);
				throw;
			}
		}

		object? ExecuteScalar()
		{
			if (TraceSwitchConnection.Level == TraceLevel.Off)
				using (DataProvider.ExecuteScope(this))
					return ExecuteScalar(CurrentCommand!);

			var now = DateTime.UtcNow;
			var sw  = Stopwatch.StartNew();

			if (TraceSwitchConnection.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(this, TraceInfoStep.BeforeExecute, TraceOperation.ExecuteScalar, false)
				{
					TraceLevel     = TraceLevel.Info,
					Command        = CurrentCommand,
					StartTime      = now,
				});
			}

			try
			{
				object? ret;
				using (DataProvider.ExecuteScope(this))
					ret = ExecuteScalar(CurrentCommand!);

				if (TraceSwitchConnection.TraceInfo)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.AfterExecute, TraceOperation.ExecuteScalar, false)
					{
						TraceLevel     = TraceLevel.Info,
						Command        = CurrentCommand,
						StartTime      = now,
						ExecutionTime  = sw.Elapsed,
					});
				}

				return ret;
			}
			catch (Exception ex)
			{
				if (TraceSwitchConnection.TraceError)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.Error, TraceOperation.ExecuteScalar, false)
					{
						TraceLevel     = TraceLevel.Error,
						Command        = CurrentCommand,
						StartTime      = now,
						ExecutionTime  = sw.Elapsed,
						Exception      = ex,
					});
				}

				throw;
			}
		}

		#endregion

		#region ExecuteReader

		protected virtual DataReaderWrapper ExecuteReader(CommandBehavior commandBehavior)
		{
			try
			{
				DbDataReader reader;

				if (((IInterceptable<ICommandInterceptor>)this).Interceptor is { } cInterceptor)
				{
					Option<DbDataReader> result;

					using (ActivityService.Start(ActivityID.CommandInterceptorExecuteScalar))
						result = cInterceptor.ExecuteReader(new(this), _command!, commandBehavior, Option<DbDataReader>.None);

					if (!result.HasValue)
					{
						using (ActivityService.Start(ActivityID.CommandExecuteReader)?.AddQueryInfo(this, _command!.Connection, _command))
							reader = _command!.ExecuteReader(commandBehavior);
					}
					else
					{
						reader = result.Value;
					}

					using (ActivityService.Start(ActivityID.CommandInterceptorAfterExecuteReader))
						cInterceptor.AfterExecuteReader(new(this), _command!, commandBehavior, reader);
				}
				else
				{
					using (ActivityService.Start(ActivityID.CommandExecuteReader)?.AddQueryInfo(this, _command!.Connection, _command))
						reader = _command!.ExecuteReader(commandBehavior);
				}


				var wrapper = new DataReaderWrapper(this, reader, _command!);
				_command = null;

				return wrapper;
			}
			catch (Exception ex) when (((IInterceptable<IExceptionInterceptor>)this).Interceptor is { } eInterceptor)
			{
				using (ActivityService.Start(ActivityID.ExceptionInterceptorProcessException))
					eInterceptor.ProcessException(new(this), ex);
				throw;
			}
		}

		DataReaderWrapper ExecuteReader()
		{
			return ExecuteDataReader(CommandBehavior.Default);
		}

		internal DataReaderWrapper ExecuteDataReader(CommandBehavior commandBehavior)
		{
			if (TraceSwitchConnection.Level == TraceLevel.Off)
				using (DataProvider.ExecuteScope(this))
					return ExecuteReader(GetCommandBehavior(commandBehavior));

			var now = DateTime.UtcNow;
			var sw  = Stopwatch.StartNew();

			if (TraceSwitchConnection.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(this, TraceInfoStep.BeforeExecute, TraceOperation.ExecuteReader, false)
				{
					TraceLevel     = TraceLevel.Info,
					Command        = CurrentCommand,
					StartTime      = now,
				});
			}

			try
			{
				DataReaderWrapper ret;

				using (DataProvider.ExecuteScope(this))
					ret = ExecuteReader(GetCommandBehavior(commandBehavior));

				if (TraceSwitchConnection.TraceInfo)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.AfterExecute, TraceOperation.ExecuteReader, false)
					{
						TraceLevel     = TraceLevel.Info,
						Command        = ret.Command,
						StartTime      = now,
						ExecutionTime  = sw.Elapsed,
					});
				}

				return ret;
			}
			catch (Exception ex)
			{
				if (TraceSwitchConnection.TraceError)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.Error, TraceOperation.ExecuteReader, false)
					{
						TraceLevel     = TraceLevel.Error,
						Command        = CurrentCommand,
						StartTime      = now,
						ExecutionTime  = sw.Elapsed,
						Exception      = ex,
					});
				}

				throw;
			}
		}

		#endregion

		/// <summary>
		/// Removes cached data mappers.
		/// </summary>
		public static void ClearObjectReaderCache()
		{
			CommandInfo.ClearObjectReaderCache();
		}

		#endregion

		#region Transaction

		/// <summary>
		/// Gets current transaction, associated with connection.
		/// </summary>
		public DbTransaction? Transaction => TransactionAsync?.Transaction;

		/// <summary>
		/// Async transaction wrapper over <see cref="Transaction"/>.
		/// </summary>
		internal IAsyncDbTransaction? TransactionAsync { get; private set; }

		/// <summary>
		/// Starts new transaction for current connection with default isolation level. If connection already has transaction, it will be rolled back.
		/// </summary>
		/// <returns>Database transaction object.</returns>
		public virtual DataConnectionTransaction BeginTransaction()
		{
			if (!DataProvider.TransactionsSupported)
				return new(this);

			// If transaction is open, we dispose it, it will rollback all changes.
			//
			TransactionAsync?.Dispose();

			var dataConnectionTransaction = TraceAction(
				this,
				TraceOperation.BeginTransaction,
				static _ => "BeginTransaction",
				default(object?),
				static (dataContext, _) =>
				{
			// Create new transaction object.
			//
					dataContext.TransactionAsync = dataContext.EnsureConnection().BeginTransaction();

					dataContext._closeTransaction = true;

			// If the active command exists.
					if (dataContext._command != null)
						dataContext._command.Transaction = dataContext.Transaction;

					return new DataConnectionTransaction(dataContext);
				});

			return dataConnectionTransaction;
		}

		/// <summary>
		/// Starts new transaction for current connection with specified isolation level. If connection already have transaction, it will be rolled back.
		/// </summary>
		/// <param name="isolationLevel">Transaction isolation level.</param>
		/// <returns>Database transaction object.</returns>
		public virtual DataConnectionTransaction BeginTransaction(IsolationLevel isolationLevel)
		{
			if (!DataProvider.TransactionsSupported)
				return new(this);

			// If transaction is open, we dispose it, it will rollback all changes.
			//
			TransactionAsync?.Dispose();

			var dataConnectionTransaction = TraceAction(
				this,
				TraceOperation.BeginTransaction,
				static il => $"BeginTransaction({il})",
				isolationLevel,
				static (dataConnection, isolationLevel) =>
				{
			// Create new transaction object.
			//
					dataConnection.TransactionAsync = dataConnection.EnsureConnection().BeginTransaction(isolationLevel);

					dataConnection._closeTransaction = true;

			// If the active command exists.
					if (dataConnection._command != null)
						dataConnection._command.Transaction = dataConnection.Transaction;

					return new DataConnectionTransaction(dataConnection);
				});

			return dataConnectionTransaction;
		}

		/// <summary>
		/// Commits transaction (if any), associated with connection.
		/// </summary>
		public virtual void CommitTransaction()
		{
			if (TransactionAsync != null)
			{
				TraceAction(
					this,
					TraceOperation.CommitTransaction,
					static _ => "CommitTransaction",
					default(object?),
					static (dataConnection, _) =>
					{
						dataConnection.TransactionAsync!.Commit();

						if (dataConnection._closeTransaction)
						{
							dataConnection.TransactionAsync.Dispose();
							dataConnection.TransactionAsync = null;

							if (dataConnection._command != null)
								dataConnection._command.Transaction = null;
						}

						return true;
					});
			}
		}

		/// <summary>
		/// Rollbacks transaction (if any), associated with connection.
		/// </summary>
		public virtual void RollbackTransaction()
		{
			if (TransactionAsync != null)
			{
				TraceAction(
					this,
					TraceOperation.RollbackTransaction,
					static _ => "RollbackTransaction",
					default(object?),
					static (dataConnection, _) =>
					{
						dataConnection.TransactionAsync!.Rollback();

						if (dataConnection._closeTransaction)
						{
							dataConnection.TransactionAsync.Dispose();
							dataConnection.TransactionAsync = null;

							if (dataConnection._command != null)
								dataConnection._command.Transaction = null;
						}

						return true;
					});
			}
		}

		/// <summary>
		/// Disposes transaction (if any), associated with connection.
		/// </summary>
		public virtual void DisposeTransaction()
		{
			if (TransactionAsync != null)
			{
				TraceAction(
					this,
					TraceOperation.DisposeTransaction,
					static _ => "DisposeTransaction",
					default(object?),
					static (dataConnection, _) =>
					{
						dataConnection.TransactionAsync!.Dispose();
						dataConnection.TransactionAsync = null;

						if (dataConnection._command != null)
							dataConnection._command.Transaction = null;

						return true;
					});
			}
		}

		#endregion

		protected static TResult TraceAction<TContext, TResult>(
			DataConnection                          dataConnection,
			TraceOperation                          traceOperation,
			Func<TContext, string?>?                commandText,
			TContext                                context,
			Func<DataConnection, TContext, TResult> action)
		{
			var now       = DateTime.UtcNow;
			Stopwatch? sw = null;
			var sql       = dataConnection.TraceSwitchConnection.TraceInfo ? commandText?.Invoke(context) : null;

			if (dataConnection.TraceSwitchConnection.TraceInfo)
			{
				sw = Stopwatch.StartNew();
				dataConnection.OnTraceConnection(new TraceInfo(dataConnection, TraceInfoStep.BeforeExecute, traceOperation, false)
				{
					TraceLevel  = TraceLevel.Info,
					CommandText = sql,
					StartTime   = now,
				});
			}

			try
			{
				var actionResult = action(dataConnection, context);

				if (dataConnection.TraceSwitchConnection.TraceInfo)
				{
					dataConnection.OnTraceConnection(new TraceInfo(dataConnection, TraceInfoStep.AfterExecute, traceOperation, false)
					{
						TraceLevel    = TraceLevel.Info,
						CommandText   = sql,
						StartTime     = now,
						ExecutionTime = sw?.Elapsed
					});
				}

				return actionResult;
			}
			catch (Exception ex)
			{
				if (dataConnection.TraceSwitchConnection.TraceError)
				{
					dataConnection.OnTraceConnection(new TraceInfo(dataConnection, TraceInfoStep.Error, traceOperation, false)
					{
						TraceLevel    = TraceLevel.Error,
						CommandText   = dataConnection.TraceSwitchConnection.TraceInfo ? sql : commandText?.Invoke(context),
						StartTime     = now,
						ExecutionTime = sw?.Elapsed,
						Exception     = ex,
					});
				}

				throw;
			}
		}

		#region MappingSchema

		/// <summary>
		/// Gets mapping schema, used for current connection.
		/// </summary>
		public  MappingSchema  MappingSchema { get; private set; }

		/// <summary>
		/// Gets or sets option to force inline parameter values as literals into command text. If parameter inlining not supported
		/// for specific value type, it will be used as parameter.
		/// </summary>
		public bool InlineParameters { get; set; }

		private List<string>? _queryHints;
		/// <summary>
		/// Gets list of query hints (writable collection), that will be used for all queries, executed through current connection.
		/// </summary>
		public  List<string>  QueryHints => _queryHints ??= new();

		private List<string>? _nextQueryHints;
		/// <summary>
		/// Gets list of query hints (writable collection), that will be used only for next query, executed through current connection.
		/// </summary>
		public  List<string>  NextQueryHints => _nextQueryHints ??= new();

		/// <summary>
		/// Adds additional mapping schema to current connection.
		/// </summary>
		/// <remarks><see cref="DataConnection"/> will share <see cref="Mapping.MappingSchema"/> instances that were created by combining same mapping schemas.</remarks>
		/// <param name="mappingSchema">Mapping schema.</param>
		/// <returns>Current connection object.</returns>
		public DataConnection AddMappingSchema(MappingSchema mappingSchema)
		{
			MappingSchema    = MappingSchema.CombineSchemas(mappingSchema, MappingSchema);
			_configurationID = null;

			return this;
		}

		#endregion

		#region System.IDisposable Members

		protected bool  Disposed        { get; private set; }
		public    bool? ThrowOnDisposed { get; set; }

		protected void CheckAndThrowOnDisposed()
		{
			if (Disposed && (ThrowOnDisposed ?? Common.Configuration.Data.ThrowOnDisposed))
				throw new ObjectDisposedException("DataConnection", "IDataContext is disposed, see https://github.com/linq2db/linq2db/wiki/Managing-data-connection");
		}

		/// <summary>
		/// Disposes connection.
		/// </summary>
		public void Dispose()
		{
			Disposed = true;

			Close();

#if DEBUG
			Interlocked.Decrement(ref _dataConnectionCount);
#endif
		}

		#endregion

		internal CommandBehavior GetCommandBehavior(CommandBehavior commandBehavior)
		{
			return DataProvider.GetCommandBehavior(commandBehavior);
		}

		IServiceProvider IInfrastructure<IServiceProvider>.Instance => ((IInfrastructure<IServiceProvider>)DataProvider).Instance;
	}
}
