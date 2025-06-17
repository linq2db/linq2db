﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
#if NETFRAMEWORK || NETSTANDARD2_0
using System.Text;
#endif

using JetBrains.Annotations;

using LinqToDB.Async;
using LinqToDB.Common;
using LinqToDB.Common.Internal;
using LinqToDB.Data.RetryPolicy;
using LinqToDB.DataProvider;
using LinqToDB.Expressions;
using LinqToDB.Infrastructure;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;
using LinqToDB.Tools;

namespace LinqToDB.Data
{
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions()...)"), EditorBrowsable(EditorBrowsableState.Never)]
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseConfiguration(configurationString)...)"), EditorBrowsable(EditorBrowsableState.Never)]
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseMappingSchema(mappingSchema))"), EditorBrowsable(EditorBrowsableState.Never)]
		public DataConnection(MappingSchema mappingSchema) : this(DefaultDataOptions.UseMappingSchema(mappingSchema))
		{
		}

		/// <summary>
		/// Creates database connection object that uses default connection configuration from <see cref="DefaultConfiguration"/> property and provided mapping schema.
		/// </summary>
		/// <param name="mappingSchema">Mapping schema to use with this connection.</param>
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseMappingSchema(mappingSchema)...)"), EditorBrowsable(EditorBrowsableState.Never)]
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseConfiguration(configurationString, mappingSchema))"), EditorBrowsable(EditorBrowsableState.Never)]
		public DataConnection(string? configurationString, MappingSchema mappingSchema)
			: this(DefaultDataOptions.UseConfiguration(configurationString, mappingSchema))
		{
		}

		/// <summary>
		/// Creates database connection object that uses provided connection configuration and mapping schema.
		/// </summary>
		/// <param name="configurationString">Name of database connection configuration to use with this connection.
		/// In case of null, configuration from <see cref="DefaultConfiguration"/> property will be used.</param>
		/// <param name="mappingSchema">Mapping schema to use with this connection.</param>
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseConfiguration(configurationString, mappingSchema)...)"), EditorBrowsable(EditorBrowsableState.Never)]
		public DataConnection(string? configurationString, MappingSchema mappingSchema, Func<DataOptions,DataOptions> optionsSetter)
			: this(optionsSetter(DefaultDataOptions.UseConfiguration(configurationString, mappingSchema)))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider, connection string and mapping schema.
		/// </summary>
		/// <param name="providerName">Name of database provider to use with this connection. <see cref="ProviderName"/> class for list of providers.</param>
		/// <param name="connectionString">Database connection string to use for connection with database.</param>
		/// <param name="mappingSchema">Mapping schema to use with this connection.</param>
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseConnectionString(providerName, connectionString).UseMappingSchema(mappingSchema))"), EditorBrowsable(EditorBrowsableState.Never)]
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseConnectionString(providerName, connectionString).UseMappingSchema(mappingSchema)...)"), EditorBrowsable(EditorBrowsableState.Never)]
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseConnectionString(dataProvider, connectionString))"), EditorBrowsable(EditorBrowsableState.Never)]
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseConnectionString(dataProvider, connectionString)...)"), EditorBrowsable(EditorBrowsableState.Never)]
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseConnectionString(dataProvider, connectionString).UseMappingSchema(mappingSchema))"), EditorBrowsable(EditorBrowsableState.Never)]
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseConnectionString(dataProvider, connectionString).UseMappingSchema(mappingSchema)...)"), EditorBrowsable(EditorBrowsableState.Never)]
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseConnectionString(dataProvider, connectionString))"), EditorBrowsable(EditorBrowsableState.Never)]
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseConnectionString(dataProvider, connectionString)...)"), EditorBrowsable(EditorBrowsableState.Never)]
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseConnectionFactory(dataProvider, connectionFactory).UseMappingSchema(mappingSchema))"), EditorBrowsable(EditorBrowsableState.Never)]
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseConnectionFactory(dataProvider, connectionFactory).UseMappingSchema(mappingSchema)...)"), EditorBrowsable(EditorBrowsableState.Never)]
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseConnectionFactory(dataProvider, connectionFactory))"), EditorBrowsable(EditorBrowsableState.Never)]
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseConnectionFactory(dataProvider, connectionFactory)...)"), EditorBrowsable(EditorBrowsableState.Never)]
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseConnection(dataProvider, connection).UseMappingSchema(mappingSchema))"), EditorBrowsable(EditorBrowsableState.Never)]
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseConnection(dataProvider, connection).UseMappingSchema(mappingSchema)...)"), EditorBrowsable(EditorBrowsableState.Never)]
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseConnection(dataProvider, connection))"), EditorBrowsable(EditorBrowsableState.Never)]
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseConnection(dataProvider, connection)...)"), EditorBrowsable(EditorBrowsableState.Never)]
		public DataConnection(
			IDataProvider                 dataProvider,
			DbConnection                  connection,
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseConnection(dataProvider, connection, disposeConnection))"), EditorBrowsable(EditorBrowsableState.Never)]
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseConnection(dataProvider, connection, disposeConnection)...)"), EditorBrowsable(EditorBrowsableState.Never)]
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseTransaction(dataProvider, transaction).UseMappingSchema(mappingSchema))"), EditorBrowsable(EditorBrowsableState.Never)]
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseTransaction(dataProvider, transaction).UseMappingSchema(mappingSchema)...)"), EditorBrowsable(EditorBrowsableState.Never)]
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseTransaction(dataProvider, transaction))"), EditorBrowsable(EditorBrowsableState.Never)]
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
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Instead use: new DataConnection(new DataOptions().UseTransaction(dataProvider, transaction)...)"), EditorBrowsable(EditorBrowsableState.Never)]
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
		public IDataProvider DataProvider        { get; private set; }
		/// <summary>
		/// Database connection string.
		/// </summary>
		public string?       ConnectionString    { get; private set; }
		/// <summary>
		/// Retry policy for current connection.
		/// </summary>
		public IRetryPolicy? RetryPolicy
		{
			get;
			// TODO: Make private in v7 and remove obsoletion warning ignores from callers
			[Obsolete("This API scheduled for removal in v7. Use DataOptions's UseRetryPolicy API"), EditorBrowsable(EditorBrowsableState.Never)]
			set;
		}

		/// <summary>
		/// Gets or sets a value to specify additional connection info to be used in tracing.
		/// </summary>
		public string?       Tag                 { get; set; }

		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7"), EditorBrowsable(EditorBrowsableState.Never)]
		private bool? _isMarsEnabled;
		/// <summary>
		/// Gets or sets status of Multiple Active Result Sets (MARS) feature. This feature available only for
		/// SQL Azure and SQL Server 2005+.
		/// </summary>
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7"), EditorBrowsable(EditorBrowsableState.Never)]
		public  bool   IsMarsEnabled
		{
			get
			{
				CheckAndThrowOnDisposed();

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
		public Action<TraceInfo> OnTraceConnection
		{
			get;
			// TODO: Make private in v7 and remove obsoletion warning ignores from callers
			[Obsolete("This API scheduled for removal in v7. Use DataOptions's UseTracing API"), EditorBrowsable(EditorBrowsableState.Never)]
			set;
		} = DefaultOnTraceConnection;

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
			// TODO: Make private in v7 and remove obsoletion warning ignores from callers
			[Obsolete("This API scheduled for removal in v7. Use DataOptions's UseTraceSwitch API"), EditorBrowsable(EditorBrowsableState.Never)]
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
		public static Action<string,string,TraceLevel> WriteTraceLine = (message, category, _) => Debug.WriteLine(message, category);

		/// <summary>
		/// Gets the delegate to write logging messages for this connection.
		/// Defaults to <see cref="WriteTraceLine"/>.
		/// Used for the current instance.
		/// </summary>
		public Action<string,string,TraceLevel> WriteTraceLineConnection
		{
			get;
			// TODO: Make private in v7 and remove obsoletion warning ignores from callers
			[Obsolete("This API scheduled for removal in v7. Use DataOptions's UseTraceWith API"), EditorBrowsable(EditorBrowsableState.Never)]
			protected set;
		} = WriteTraceLine;

		#endregion

		#region Connection

		bool                             _closeConnection;
		bool                             _disposeConnection = true;
		bool                             _closeTransaction;
		IAsyncDbConnection?              _connection;
		Func<DataOptions, DbConnection>? _connectionFactory;

		/// <summary>
		/// Gets underlying database connection, used by current connection object, or opens new.
		/// </summary>
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7. Use TryGetDbConnection, OpenDbConnection or OpenDbConnectionAsync instead based on your use-case"), EditorBrowsable(EditorBrowsableState.Never)]
		public DbConnection Connection => OpenDbConnection();

		/// <summary>
		/// Returns underlying <see cref="DbConnection"/> instance or <c>null</c> if connection is not open.
		/// </summary>
		public DbConnection? TryGetDbConnection() => _connection?.Connection;

		/// <summary>
		/// Returns underlying <see cref="DbConnection"/> instance. If connection is not open yet - it will be opened.
		/// </summary>
		public DbConnection OpenDbConnection() => OpenConnection().Connection;

		internal DbConnection? CurrentConnection => _connection?.Connection;

		/// <summary>
		/// Creates database connection instance (but not open connection) or return already created (and possibly opened) instance.
		/// </summary>
		internal IAsyncDbConnection GetOrCreateConnection()
		{
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

				return _connection;
			}
			catch (Exception ex)
			{
				if (TraceSwitchConnection.TraceError)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.Error, TraceOperation.Open, false)
					{
						TraceLevel = TraceLevel.Error,
						StartTime  = DateTime.UtcNow,
						Exception  = ex,
					});
				}

				throw;
			}
		}

		/// <summary>
		/// Returns connection instance in Open state.
		/// </summary>
		internal IAsyncDbConnection OpenConnection()
		{
			CheckAndThrowOnDisposed();

			var connection = GetOrCreateConnection();

			try
			{
				if (connection.State == ConnectionState.Closed)
				{
					var interceptor = ((IInterceptable<IConnectionInterceptor>)this).Interceptor;
					if (interceptor != null)
					{
						using (ActivityService.Start(ActivityID.ConnectionInterceptorConnectionOpening))
							interceptor.ConnectionOpening(new(this), connection.Connection);
					}

					connection.Open();
					_closeConnection = true;

					if (interceptor != null)
					{
						using (ActivityService.Start(ActivityID.ConnectionInterceptorConnectionOpened))
							interceptor.ConnectionOpened(new(this), connection.Connection);
					}
				}

				return connection;
			}
			catch (Exception ex)
			{
				if (TraceSwitchConnection.TraceError)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.Error, TraceOperation.Open, false)
					{
						TraceLevel = TraceLevel.Error,
						StartTime  = DateTime.UtcNow,
						Exception  = ex,
					});
				}

				throw;
			}
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

#pragma warning disable CS0618 // Type or member is obsolete
			DisposeCommand();
#pragma warning restore CS0618 // Type or member is obsolete

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
		internal DbCommand GetOrCreateCommand()
		{
			CheckAndThrowOnDisposed();

			if (_command == null)
			{
				_command = GetOrCreateConnection().CreateCommand();

				if (_commandTimeout.HasValue)
					_command.CommandTimeout = _commandTimeout.Value;

				if (TransactionAsync != null)
					_command.Transaction = Transaction;
			}

			return _command;
		}

		/// <summary>
		/// Contains text of last command, sent to database using current connection.
		/// </summary>
		public string? LastQuery { get; private set; }

		internal void InitCommand(CommandType commandType, string sql, DataParameter[]? parameters, IReadOnlyCollection<string>? queryHints, bool withParameters)
		{
			CheckAndThrowOnDisposed();

			if (queryHints?.Count > 0)
			{
				var sqlProvider = DataProvider.CreateSqlBuilder(MappingSchema, Options);
				sql             = sqlProvider.ApplyQueryHints(sql, queryHints);
			}

			_command = DataProvider.InitCommand(this, GetOrCreateCommand(), commandType, sql, parameters, withParameters);
		}

		internal void CommitCommandInit()
		{
			CheckAndThrowOnDisposed();

			var interceptor = ((IInterceptable<ICommandInterceptor>)this).Interceptor;
			if (interceptor != null)
			{
				using (ActivityService.Start(ActivityID.CommandInterceptorCommandInitialized))
					_command = interceptor.CommandInitialized(new (this), _command!);
			}

			LastQuery = _command!.CommandText;
		}

		private int? _commandTimeout;
#if NET8_0_OR_GREATER
		/// <summary>
		/// Gets or sets command execution timeout in seconds.
		/// Supported values:
		/// <list type="bullet">
		/// <item>0 : infinite timeout</item>
		/// <item> &gt; 0 : command timeout in seconds</item>
		/// <item> -1 on property get : default provider/connection command timeout value used (not controlled by Linq To DB)</item>
		/// <item> negative value on property set : throws <see cref="InvalidOperationException"/> exception. To reset timeout to provider/connection defaults use <see cref="ResetCommandTimeout"/> or <see cref="ResetCommandTimeoutAsync"/> methods</item>
		/// </list>
		/// </summary>
#else
		/// <summary>
		/// Gets or sets command execution timeout in seconds.
		/// Supported values:
		/// <list type="bullet">
		/// <item>0 : infinite timeout</item>
		/// <item> &gt; 0 : command timeout in seconds</item>
		/// <item> -1 on property get : default provider/connection command timeout value used (not controlled by Linq To DB)</item>
		/// <item> negative value on property set : throws <see cref="InvalidOperationException"/> exception. To reset timeout to provider/connection defaults use <see cref="ResetCommandTimeout"/> method</item>
		/// </list>
		/// </summary>
#endif
		public int   CommandTimeout
		{
			get => _commandTimeout ?? -1;
			set
			{
				CheckAndThrowOnDisposed();

				if (value < 0)
				{
#if NET8_0_OR_GREATER
					throw new ArgumentOutOfRangeException(nameof(value), "Timeout value cannot be negative. To reset command timeout use ResetCommandTimeout or ResetCommandTimeoutAsync methods instead.");
#else
					throw new ArgumentOutOfRangeException(nameof(value), "Timeout value cannot be negative. To reset command timeout use ResetCommandTimeout method instead.");
#endif
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
			DisposeCommand();
#pragma warning restore CS0618 // Type or member is obsolete
		}

		// TODO: Mark private in v7
		[Obsolete("This API scheduled for removal in v7. Use TryGetConnection/OpenDbConnection or OpenDbConnectionAsync chained with CreateCommand call. Note that it is your responsibility to dispose such command after use."), EditorBrowsable(EditorBrowsableState.Never)]
		public DbCommand CreateCommand()
		{
			CheckAndThrowOnDisposed();

			var command = OpenDbConnection().CreateCommand();

			if (_commandTimeout.HasValue)
				command.CommandTimeout = _commandTimeout.Value;

			if (TransactionAsync != null)
				command.Transaction = Transaction;

			return command;
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// </summary>
		// TODO: Mark private in v7 and remove warning suppressions from callers
		[Obsolete("This API scheduled for removal in v7"), EditorBrowsable(EditorBrowsableState.Never)]
		public void DisposeCommand()
		{
			CheckAndThrowOnDisposed();

			if (_command != null)
			{
				DataProvider.DisposeCommand(_command);
				_command = null;
			}
		}

		#region ExecuteNonQuery

		protected virtual int ExecuteNonQuery(DbCommand command)
		{
			CheckAndThrowOnDisposed();

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

				using (ActivityService.Start(ActivityID.CommandExecuteNonQuery)?.AddQueryInfo(this, command.Connection, command))
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
			CheckAndThrowOnDisposed();

			OpenConnection();

			if (TraceSwitchConnection.Level == TraceLevel.Off)
				using (DataProvider.ExecuteScope(this))
					return ExecuteNonQuery(CurrentCommand!);

			var now = DateTime.UtcNow;
			var sw  = Stopwatch.StartNew();

			if (TraceSwitchConnection.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(this, TraceInfoStep.BeforeExecute, TraceOperation.ExecuteNonQuery, false)
				{
					TraceLevel = TraceLevel.Info,
					Command    = CurrentCommand,
					StartTime  = now,
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
						TraceLevel    = TraceLevel.Error,
						Command       = CurrentCommand,
						StartTime     = now,
						ExecutionTime = sw.Elapsed,
						Exception     = ex,
					});
				}

				throw;
			}
		}

		private int ExecuteNonQueryCustom(DbCommand command, Func<DbCommand, int> customExecute)
		{
			CheckAndThrowOnDisposed();

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

				using (ActivityService.Start(ActivityID.CommandExecuteNonQuery)?.AddQueryInfo(this, command.Connection, command))
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
			CheckAndThrowOnDisposed();

			OpenConnection();

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
					Command    = CurrentCommand,
					StartTime  = now,
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
						TraceLevel    = TraceLevel.Error,
						Command       = CurrentCommand,
						StartTime     = now,
						ExecutionTime = sw.Elapsed,
						Exception     = ex,
					});
				}

				throw;
			}
		}

		#endregion

		#region ExecuteScalar

		protected virtual object? ExecuteScalar(DbCommand command)
		{
			CheckAndThrowOnDisposed();

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

				using (ActivityService.Start(ActivityID.CommandExecuteScalar)?.AddQueryInfo(this, command.Connection, command))
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
			OpenConnection();

			if (TraceSwitchConnection.Level == TraceLevel.Off)
				using (DataProvider.ExecuteScope(this))
					return ExecuteScalar(CurrentCommand!);

			var now = DateTime.UtcNow;
			var sw  = Stopwatch.StartNew();

			if (TraceSwitchConnection.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(this, TraceInfoStep.BeforeExecute, TraceOperation.ExecuteScalar, false)
				{
					TraceLevel = TraceLevel.Info,
					Command    = CurrentCommand,
					StartTime  = now,
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
						TraceLevel    = TraceLevel.Info,
						Command       = CurrentCommand,
						StartTime     = now,
						ExecutionTime = sw.Elapsed,
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
						TraceLevel    = TraceLevel.Error,
						Command       = CurrentCommand,
						StartTime     = now,
						ExecutionTime = sw.Elapsed,
						Exception     = ex,
					});
				}

				throw;
			}
		}

		#endregion

		#region ExecuteReader

		protected virtual DataReaderWrapper ExecuteReader(CommandBehavior commandBehavior)
		{
			CheckAndThrowOnDisposed();

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

		internal DataReaderWrapper ExecuteDataReader(CommandBehavior commandBehavior)
		{
			CheckAndThrowOnDisposed();

			OpenConnection();

			if (TraceSwitchConnection.Level == TraceLevel.Off)
				using (DataProvider.ExecuteScope(this))
					return ExecuteReader(GetCommandBehavior(commandBehavior));

			var now = DateTime.UtcNow;
			var sw  = Stopwatch.StartNew();

			if (TraceSwitchConnection.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(this, TraceInfoStep.BeforeExecute, TraceOperation.ExecuteReader, false)
				{
					TraceLevel = TraceLevel.Info,
					Command    = CurrentCommand,
					StartTime  = now,
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
						TraceLevel    = TraceLevel.Info,
						Command       = ret.Command,
						StartTime     = now,
						ExecutionTime = sw.Elapsed,
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
						TraceLevel    = TraceLevel.Error,
						Command       = CurrentCommand,
						StartTime     = now,
						ExecutionTime = sw.Elapsed,
						Exception     = ex,
					});
				}

				throw;
			}
		}

		#endregion

		/// <summary>
		/// Removes cached data mappers.
		/// </summary>
		// TODO: remove in v7
		[Obsolete("This API scheduled for removal in v7. Use CommandInfo.ClearObjectReaderCache instead"), EditorBrowsable(EditorBrowsableState.Never)]
		public static void ClearObjectReaderCache()
		{
			CommandInfo.ClearObjectReaderCache();
		}

#endregion

		#region Transaction

		/// <summary>
		/// Gets current transaction, associated with connection.
		/// </summary>
		public DbTransaction? Transaction
		{
			get
			{
				CheckAndThrowOnDisposed();
				return TransactionAsync?.Transaction;
			}
		}

		/// <summary>
		/// Async transaction wrapper over <see cref="Transaction"/>.
		/// </summary>
		internal IAsyncDbTransaction? TransactionAsync { get; private set; }

		/// <summary>
		/// Starts new transaction for current connection with default isolation level.
		/// If connection already has transaction, it will throw <see cref="InvalidOperationException"/>.
		/// </summary>
		/// <returns>Database transaction object.</returns>
		public virtual DataConnectionTransaction BeginTransaction()
		{
			CheckAndThrowOnDisposed();

			if (!DataProvider.TransactionsSupported)
				return new(this);

			if (TransactionAsync != null) throw new InvalidOperationException("Data connection already has transaction");

			var connection = OpenConnection();

			var dataConnectionTransaction = TraceAction(
				this,
				TraceOperation.BeginTransaction,
				static _ => "BeginTransaction",
				connection,
				static (dataContext, connection) =>
				{
					// Create new transaction object.
					//
					dataContext.TransactionAsync = connection.BeginTransaction();

					dataContext._closeTransaction = true;

					// If the active command exists.
					if (dataContext._command != null)
						dataContext._command.Transaction = dataContext.Transaction;

					return new DataConnectionTransaction(dataContext);
				});

			return dataConnectionTransaction;
		}

		/// <summary>
		/// Starts new transaction for current connection with specified isolation level.
		/// If connection already has transaction, it will throw <see cref="InvalidOperationException"/>.
		/// </summary>
		/// <param name="isolationLevel">Transaction isolation level.</param>
		/// <returns>Database transaction object.</returns>
		public virtual DataConnectionTransaction BeginTransaction(IsolationLevel isolationLevel)
		{
			CheckAndThrowOnDisposed();

			if (!DataProvider.TransactionsSupported)
				return new(this);

			if (TransactionAsync != null) throw new InvalidOperationException("Data connection already has transaction");

			var connection = OpenConnection();

			var dataConnectionTransaction = TraceAction(
				this,
				TraceOperation.BeginTransaction,
				static ctx => $"BeginTransaction({ctx.isolationLevel})",
				(isolationLevel, connection),
				static (dataConnection, ctx) =>
				{
					// Create new transaction object.
					//
					dataConnection.TransactionAsync = ctx.connection.BeginTransaction(ctx.isolationLevel);

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
			CheckAndThrowOnDisposed();

			if (TransactionAsync != null)
			{
				TraceAction(
					this,
					TraceOperation.CommitTransaction,
					static _ => "CommitTransaction",
					TransactionAsync,
					static (dataConnection, transaction) =>
					{
						transaction.Commit();

						if (dataConnection._closeTransaction)
						{
							transaction.Dispose();
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
			CheckAndThrowOnDisposed();

			if (TransactionAsync != null)
			{
				TraceAction(
					this,
					TraceOperation.RollbackTransaction,
					static _ => "RollbackTransaction",
					TransactionAsync,
					static (dataConnection, transaction) =>
					{
						transaction.Rollback();

						if (dataConnection._closeTransaction)
						{
							transaction.Dispose();
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
			CheckAndThrowOnDisposed();

			if (TransactionAsync != null)
			{
				TraceAction(
					this,
					TraceOperation.DisposeTransaction,
					static _ => "DisposeTransaction",
					TransactionAsync,
					static (dataConnection, transaction) =>
					{
						transaction.Dispose();
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
		public List<string> QueryHints
		{
			get
			{
				CheckAndThrowOnDisposed();

				return _queryHints ??= new();
			}
		}

		private List<string>? _nextQueryHints;
		/// <summary>
		/// Gets list of query hints (writable collection), that will be used only for next query, executed through current connection.
		/// </summary>
		public List<string> NextQueryHints
		{
			get
			{
				CheckAndThrowOnDisposed();

				return _nextQueryHints ??= new();
			}
		}

		/// <summary>
		/// Adds additional mapping schema to current connection.
		/// </summary>
		/// <remarks><see cref="DataConnection"/> will share <see cref="Mapping.MappingSchema"/> instances that were created by combining same mapping schemas.</remarks>
		/// <param name="mappingSchema">Mapping schema.</param>
		/// <returns>Current connection object.</returns>
		public void AddMappingSchema(MappingSchema mappingSchema)
		{
			CheckAndThrowOnDisposed();

			MappingSchema    = MappingSchema.CombineSchemas(mappingSchema, MappingSchema);
			_configurationID = null;
		}

		#endregion

		#region System.IDisposable Members

		protected bool  Disposed        { get; private set; }
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7"), EditorBrowsable(EditorBrowsableState.Never)]
		public bool? ThrowOnDisposed { get; set; }

		protected void CheckAndThrowOnDisposed()
		{
#pragma warning disable CS0618 // Type or member is obsolete
			if (Disposed && (ThrowOnDisposed ?? Common.Configuration.Data.ThrowOnDisposed))
#pragma warning restore CS0618 // Type or member is obsolete
				throw new ObjectDisposedException(GetType().FullName, "IDataContext is disposed, see https://github.com/linq2db/linq2db/wiki/Managing-data-connection");
		}

		/// <summary>
		/// Disposes connection.
		/// </summary>
		public void Dispose()
		{
			Close();

			Disposed = true;

#if DEBUG
			Interlocked.Decrement(ref _dataConnectionCount);
#endif
		}

		#endregion

		internal CommandBehavior GetCommandBehavior(CommandBehavior commandBehavior)
		{
			CheckAndThrowOnDisposed();

			return DataProvider.GetCommandBehavior(commandBehavior);
		}

		IServiceProvider IInfrastructure<IServiceProvider>.Instance => ((IInfrastructure<IServiceProvider>)DataProvider).Instance;
	}
}
