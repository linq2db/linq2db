using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;

namespace LinqToDB.Data
{
	using Common;
	using Configuration;
	using DataProvider;
	using Expressions;
	using Mapping;
#if !SILVERLIGHT && !WINSTORE
	using RetryPolicy;
#endif

	public partial class DataConnection : ICloneable
	{
		#region .ctor

		public DataConnection() : this((string)null)
		{}

		public DataConnection([JetBrains.Annotations.NotNull] MappingSchema mappingSchema) : this((string)null)
		{
			AddMappingSchema(mappingSchema);
		}

		public DataConnection(string configurationString, [JetBrains.Annotations.NotNull] MappingSchema mappingSchema)
			: this(configurationString)
		{
			AddMappingSchema(mappingSchema);
		}

		public DataConnection(string configurationString)
		{
			InitConfig();

			ConfigurationString = configurationString ?? DefaultConfiguration;

			if (ConfigurationString == null)
				throw new LinqToDBException("Configuration string is not provided.");

			var ci = GetConfigurationInfo(ConfigurationString);

			DataProvider     = ci.DataProvider;
			ConnectionString = ci.ConnectionString;
			_mappingSchema   = DataProvider.MappingSchema;


#if !SILVERLIGHT && !WINSTORE
			RetryPolicy = Configuration.RetryPolicy.Factory != null ? Configuration.RetryPolicy.Factory(this) : null;
#endif
		}

		public DataConnection(
				[JetBrains.Annotations.NotNull] string        providerName,
				[JetBrains.Annotations.NotNull] string        connectionString,
				[JetBrains.Annotations.NotNull] MappingSchema mappingSchema)
			: this(providerName, connectionString)
		{
			AddMappingSchema(mappingSchema);
		}

		public DataConnection(
			[JetBrains.Annotations.NotNull] string providerName,
			[JetBrains.Annotations.NotNull] string connectionString)
		{
			if (providerName     == null) throw new ArgumentNullException("providerName");
			if (connectionString == null) throw new ArgumentNullException("connectionString");

			IDataProvider dataProvider;

			if (!_dataProviders.TryGetValue(providerName, out dataProvider))
				throw new LinqToDBException("DataProvider '{0}' not found.".Args(providerName));

			InitConfig();

			DataProvider     = dataProvider;
			ConnectionString = connectionString;
			_mappingSchema   = DataProvider.MappingSchema;
		}

		public DataConnection(
			[JetBrains.Annotations.NotNull] IDataProvider dataProvider,
			[JetBrains.Annotations.NotNull] string        connectionString,
			[JetBrains.Annotations.NotNull] MappingSchema mappingSchema)
			: this(dataProvider, connectionString)
		{
			AddMappingSchema(mappingSchema);
		}

		public DataConnection(
			[JetBrains.Annotations.NotNull] IDataProvider dataProvider,
			[JetBrains.Annotations.NotNull] string connectionString)
		{
			if (dataProvider     == null) throw new ArgumentNullException("dataProvider");
			if (connectionString == null) throw new ArgumentNullException("connectionString");

			InitConfig();

			DataProvider     = dataProvider;
			_mappingSchema   = DataProvider.MappingSchema;
			ConnectionString = connectionString;
		}

		public DataConnection(
			[JetBrains.Annotations.NotNull] IDataProvider dataProvider,
			[JetBrains.Annotations.NotNull] IDbConnection connection,
			[JetBrains.Annotations.NotNull] MappingSchema mappingSchema)
			: this(dataProvider, connection)
		{
			AddMappingSchema(mappingSchema);
		}

		public DataConnection(
			[JetBrains.Annotations.NotNull] IDataProvider dataProvider,
			[JetBrains.Annotations.NotNull] IDbConnection connection)
		{
			if (dataProvider == null) throw new ArgumentNullException("dataProvider");
			if (connection   == null) throw new ArgumentNullException("connection");
			
			InitConfig();

			if (!Configuration.AvoidSpecificDataProviderAPI && !dataProvider.IsCompatibleConnection(connection))
				throw new LinqToDBException(
					"DataProvider '{0}' and connection '{1}' are not compatible.".Args(dataProvider, connection));

			DataProvider   = dataProvider;
			_mappingSchema = DataProvider.MappingSchema;
			_connection    = connection;
		}

		public DataConnection(
			[JetBrains.Annotations.NotNull] IDataProvider dataProvider,
			[JetBrains.Annotations.NotNull] IDbTransaction transaction,
			[JetBrains.Annotations.NotNull] MappingSchema mappingSchema)
			: this(dataProvider, transaction)
		{
			AddMappingSchema(mappingSchema);
		}

		public DataConnection(
			[JetBrains.Annotations.NotNull] IDataProvider dataProvider,
			[JetBrains.Annotations.NotNull] IDbTransaction transaction)
		{
			if (dataProvider == null) throw new ArgumentNullException("dataProvider");
			if (transaction  == null) throw new ArgumentNullException("transaction");
			
			InitConfig();

			if (!Configuration.AvoidSpecificDataProviderAPI && !dataProvider.IsCompatibleConnection(transaction.Connection))
				throw new LinqToDBException(
					"DataProvider '{0}' and connection '{1}' are not compatible.".Args(dataProvider, transaction.Connection));

			DataProvider      = dataProvider;
			_mappingSchema    = DataProvider.MappingSchema;
			_connection       = transaction.Connection;
			Transaction       = transaction;
			_closeTransaction = false;
		}

		#endregion

		#region Public Properties

		public string        ConfigurationString { get; private set; }
		public IDataProvider DataProvider        { get; private set; }
		public string        ConnectionString    { get; private set; }
#if !SILVERLIGHT && !WINSTORE
		public IRetryPolicy  RetryPolicy         { get; set; }
#endif

		static readonly ConcurrentDictionary<string,int> _configurationIDs;
		static int _maxID;

		private int? _id;
		public  int   ID
		{
			get
			{
				if (!_id.HasValue)
				{
					var key = MappingSchema.ConfigurationID + "." + (ConfigurationString ?? ConnectionString ?? Connection.ConnectionString);
					int id;

					if (!_configurationIDs.TryGetValue(key, out id))
						_configurationIDs[key] = id = Interlocked.Increment(ref _maxID);

					_id = id;
				}

				return _id.Value;
			}
		}

		private bool? _isMarsEnabled;
		public  bool   IsMarsEnabled
		{
			get
			{
				if (_isMarsEnabled == null)
					_isMarsEnabled = (bool)(DataProvider.GetConnectionInfo(this, "IsMarsEnabled") ?? false);

				return _isMarsEnabled.Value;
			}
			set { _isMarsEnabled = value; }
		}

		public static string DefaultConfiguration { get; set; }
		public static string DefaultDataProvider  { get; set; }

		private static Action<TraceInfo> _onTrace = OnTraceInternal;
		public  static Action<TraceInfo>  OnTrace
		{
			get { return _onTrace; }
			set { _onTrace = value ?? OnTraceInternal; }
		}

		private Action<TraceInfo> _onTraceConnection = OnTrace;
		[JetBrains.Annotations.CanBeNull]
		public  Action<TraceInfo>  OnTraceConnection
		{
			get { return _onTraceConnection;  }
			set { _onTraceConnection = value; }
		}

		static void OnTraceInternal(TraceInfo info)
		{
			switch (info.TraceInfoStep)
			{
				case TraceInfoStep.BeforeExecute:
					WriteTraceLine(info.SqlText, TraceSwitch.DisplayName);
					break;

				case TraceInfoStep.AfterExecute:
					WriteTraceLine(
						info.RecordsAffected != null
							? "Query Execution Time: {0}. Records Affected: {1}.\r\n".Args(info.ExecutionTime, info.RecordsAffected)
							: "Query Execution Time: {0}\r\n".Args(info.ExecutionTime),
						TraceSwitch.DisplayName);
					break;

				case TraceInfoStep.Error:
					{
						var sb = new StringBuilder();

						for (var ex = info.Exception; ex != null; ex = ex.InnerException)
						{
							try
							{
								sb
									.AppendLine()
										.AppendLine("Exception: {0}".Args(ex.GetType()))
										.AppendLine("Message  : {0}".Args(ex.Message))
									.AppendLine(ex.StackTrace)
									;
							}
							catch
							{
								// Sybase provider could generate exception that will throw another exception when you
								// try to access Message property due to bug in AseErrorCollection.Message property.
								// There it tries to fetch error from first element of list without checking wether
								// list contains any elements or not
								sb
									.AppendLine()
									.AppendFormat("Failed while tried to log failure of type {0}", ex.GetType())
									;
							}
						}

						WriteTraceLine(sb.ToString(), TraceSwitch.DisplayName);
					}

					break;

				case TraceInfoStep.MapperCreated:
					{
						var sb = new StringBuilder();

						if (Configuration.Linq.TraceMapperExpression && info.MapperExpression != null)
							sb.AppendLine(info.MapperExpression.GetDebugView());

						WriteTraceLine(sb.ToString(), TraceSwitch.DisplayName);
					}

					break;

				case TraceInfoStep.Completed:
					{
						var sb = new StringBuilder();

						sb.Append("Total Execution Time: {0}.".Args(info.ExecutionTime));

						if (info.RecordsAffected != null)
							sb.Append(" Rows Count: {0}.".Args(info.RecordsAffected));

						sb.AppendLine();

						WriteTraceLine(sb.ToString(), TraceSwitch.DisplayName);
					}

					break;
			}
		}

		private static TraceSwitch _traceSwitch;
		public  static TraceSwitch  TraceSwitch
		{
			get
			{
				return _traceSwitch ?? (_traceSwitch = new TraceSwitch("DataConnection", "DataConnection trace switch",
#if DEBUG
				"Warning"
#else
				"Off"
#endif
				));
			}
			set { _traceSwitch = value; }
		}

		public static void TurnTraceSwitchOn(TraceLevel traceLevel = TraceLevel.Info)
		{
			TraceSwitch = new TraceSwitch("DataConnection", "DataConnection trace switch", traceLevel.ToString());
		}

		public static Action<string,string> WriteTraceLine = (message, displayName) => Debug.WriteLine(message, displayName);

#endregion

#region Configuration

		private static ILinqToDBSettings _defaultSettings;

		public static ILinqToDBSettings DefaultSettings
		{
			get
			{
#if !NETSTANDARD
				return _defaultSettings ?? (_defaultSettings = LinqToDBSection.Instance);
#else
				return _defaultSettings;
#endif

			}
			set { _defaultSettings = value; }
		}

		[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
		private static IDataProvider FindProvider(
			string configuration,
			IEnumerable<KeyValuePair<string,IDataProvider>> ps,
			IDataProvider defp)
		{
			foreach (var p in ps.OrderByDescending(kv => kv.Key.Length))
				if (configuration == p.Key || configuration.StartsWith(p.Key + '.'))
					return p.Value;

			foreach (var p in ps.OrderByDescending(kv => kv.Value.Name.Length))
				if (configuration == p.Value.Name || configuration.StartsWith(p.Value.Name + '.'))
					return p.Value;

			return defp;
		}

		static DataConnection()
		{
			_configurationIDs = new ConcurrentDictionary<string,int>();

			LinqToDB.DataProvider.SqlServer. SqlServerTools. GetDataProvider();
#if !NETSTANDARD
			LinqToDB.DataProvider.Access.    AccessTools.    GetDataProvider();
#endif
			LinqToDB.DataProvider.SqlCe.     SqlCeTools.     GetDataProvider();
			LinqToDB.DataProvider.Firebird.  FirebirdTools.  GetDataProvider();
			LinqToDB.DataProvider.MySql.     MySqlTools.     GetDataProvider();
			LinqToDB.DataProvider.SQLite.    SQLiteTools.    GetDataProvider();
			LinqToDB.DataProvider.Sybase.    SybaseTools.    GetDataProvider();
			LinqToDB.DataProvider.Oracle.    OracleTools.    GetDataProvider();
			LinqToDB.DataProvider.PostgreSQL.PostgreSQLTools.GetDataProvider();
			LinqToDB.DataProvider.DB2.       DB2Tools.       GetDataProvider();
			LinqToDB.DataProvider.Informix.  InformixTools.  GetDataProvider();
			LinqToDB.DataProvider.SapHana.   SapHanaTools.   GetDataProvider(); 

			var section = DefaultSettings;

			if (section != null)
			{
				DefaultConfiguration = section.DefaultConfiguration;
				DefaultDataProvider  = section.DefaultDataProvider;

				foreach (var provider in section.DataProviders)
				{
					var dataProviderType = Type.GetType(provider.TypeName, true);
					var providerInstance = (IDataProviderFactory)Activator.CreateInstance(dataProviderType);

					if (!string.IsNullOrEmpty(provider.Name))
						AddDataProvider(provider.Name, providerInstance.GetDataProvider(provider.Attributes));
				}
			}
		}

		static readonly List<Func<IConnectionStringSettings,string,IDataProvider>> _providerDetectors =
			new List<Func<IConnectionStringSettings,string,IDataProvider>>();

		public static void AddProviderDetector(Func<IConnectionStringSettings,string,IDataProvider> providerDetector)
		{
			_providerDetectors.Add(providerDetector);
		}

		static void InitConnectionStrings()
		{
			if (DefaultSettings == null)
				return;
			foreach (var css in DefaultSettings.ConnectionStrings)
			{
				_configurations[css.Name] = new ConfigurationInfo(css);

				if (DefaultConfiguration == null && !css.IsGlobal /*IsMachineConfig(css)*/)
				{
					DefaultConfiguration = css.Name;
				}
			}
		}

		static readonly object _initSyncRoot = new object();
		static          bool   _initialized;

		static void InitConfig()
		{
			lock (_initSyncRoot)
			{
				if (!_initialized)
				{
					_initialized = true;
					InitConnectionStrings();
				}
			}
		}

		static readonly ConcurrentDictionary<string,IDataProvider> _dataProviders =
			new ConcurrentDictionary<string,IDataProvider>();

		public static void AddDataProvider([JetBrains.Annotations.NotNull] string providerName, [JetBrains.Annotations.NotNull] IDataProvider dataProvider)
		{
			if (providerName == null) throw new ArgumentNullException("providerName");
			if (dataProvider == null) throw new ArgumentNullException("dataProvider");

			if (string.IsNullOrEmpty(dataProvider.Name))
				throw new ArgumentException("dataProvider.Name cannot be empty.", "dataProvider");

			_dataProviders[providerName] = dataProvider;
		}

		public static void AddDataProvider([JetBrains.Annotations.NotNull] IDataProvider dataProvider)
		{
			if (dataProvider == null) throw new ArgumentNullException("dataProvider");

			AddDataProvider(dataProvider.Name, dataProvider);
		}

		public static IDataProvider GetDataProvider([JetBrains.Annotations.NotNull] string configurationString)
		{
			InitConfig();

			return GetConfigurationInfo(configurationString).DataProvider;
		}

		class ConfigurationInfo
		{
			private readonly bool _dataProviderSetted;
			public ConfigurationInfo(string connectionString, IDataProvider dataProvider)
			{
				ConnectionString    = connectionString;
				_dataProvider       = dataProvider;
				_dataProviderSetted = dataProvider != null;
			}

			public ConfigurationInfo(IConnectionStringSettings connectionStringSettings)
			{
				ConnectionString = connectionStringSettings.ConnectionString;

				_connectionStringSettings = connectionStringSettings;
			}

			private string _connectionString;
			public  string ConnectionString
			{
				get { return _connectionString; }
				set
				{
					if (!_dataProviderSetted)
						_dataProvider = null;

					_connectionString = value;
				}
			}

			private readonly IConnectionStringSettings _connectionStringSettings;

			private IDataProvider _dataProvider;
			public  IDataProvider  DataProvider
			{
				get { return _dataProvider ?? (_dataProvider = GetDataProvider(_connectionStringSettings, ConnectionString)); }
			}

			static IDataProvider GetDataProvider(IConnectionStringSettings css, string connectionString)
			{
				var configuration = css.Name;
				var providerName  = css.ProviderName;
				var dataProvider  = _providerDetectors.Select(d => d(css, connectionString)).FirstOrDefault(dp => dp != null);

				if (dataProvider == null)
				{
					var defaultDataProvider = DefaultDataProvider != null ? _dataProviders[DefaultDataProvider] : null;

					if (string.IsNullOrEmpty(providerName))
						dataProvider = FindProvider(configuration, _dataProviders, defaultDataProvider);
					else if (_dataProviders.ContainsKey(providerName))
						dataProvider = _dataProviders[providerName];
					else if (_dataProviders.ContainsKey(configuration))
						dataProvider = _dataProviders[configuration];
					else
					{
						var providers = _dataProviders.Where(dp => dp.Value.ConnectionNamespace == providerName).ToList();

						switch (providers.Count)
						{
							case 0  : dataProvider = defaultDataProvider;                                        break;
							case 1  : dataProvider = providers[0].Value;                                         break;
							default : dataProvider = FindProvider(configuration, providers, providers[0].Value); break;
						}
					}
				}

				if (dataProvider != null && DefaultConfiguration == null && !css.IsGlobal/*IsMachineConfig(css)*/)
				{
					DefaultConfiguration = css.Name;
				}

				return dataProvider;
			}
		}

		static ConfigurationInfo GetConfigurationInfo(string configurationString)
		{
			ConfigurationInfo ci;

			var key = configurationString ?? DefaultConfiguration;

			if (key == null)
				throw new LinqToDBException("Configuration string is not provided.");

			if (_configurations.TryGetValue(key, out ci))
				return ci;

			throw new LinqToDBException("Configuration '{0}' is not defined.".Args(configurationString));
		}

		public static void SetConnectionStrings(IEnumerable<IConnectionStringSettings> connectionStrings)
		{
			foreach (var css in connectionStrings)
			{
				_configurations[css.Name] = new ConfigurationInfo(css);

				if (DefaultConfiguration == null && !css.IsGlobal /*IsMachineConfig(css)*/)
				{
					DefaultConfiguration = css.Name;
				}
			}
		}

		static readonly ConcurrentDictionary<string,ConfigurationInfo> _configurations =
			new ConcurrentDictionary<string, ConfigurationInfo>();

		public static void AddConfiguration(
			[JetBrains.Annotations.NotNull] string configuration,
			[JetBrains.Annotations.NotNull] string connectionString,
			IDataProvider dataProvider = null)
		{
			if (configuration    == null) throw new ArgumentNullException("configuration");
			if (connectionString == null) throw new ArgumentNullException("connectionString");

			_configurations[configuration] = new ConfigurationInfo(
				connectionString,
				dataProvider ?? FindProvider(configuration, _dataProviders, _dataProviders[DefaultDataProvider]));
		}

		public static void SetConnectionString(
			[JetBrains.Annotations.NotNull] string configuration,
			[JetBrains.Annotations.NotNull] string connectionString)
		{
			if (configuration    == null) throw new ArgumentNullException("configuration");
			if (connectionString == null) throw new ArgumentNullException("connectionString");

			InitConfig();

			_configurations[configuration].ConnectionString = connectionString;
		}

		[JetBrains.Annotations.Pure]
		public static string GetConnectionString(string configurationString)
		{
			InitConfig();

			ConfigurationInfo ci;

			if (_configurations.TryGetValue(configurationString, out ci))
				return ci.ConnectionString;

			throw new LinqToDBException("Configuration '{0}' is not defined.".Args(configurationString));
		}

#endregion

#region Connection

		bool          _closeConnection;
		bool          _closeTransaction;
		IDbConnection _connection;

		public IDbConnection Connection
		{
			get
			{
				if (_connection == null)
				{
					_connection = DataProvider.CreateConnection(ConnectionString);

#if !SILVERLIGHT && !WINSTORE
					if (RetryPolicy != null)
						_connection = new RetryingDbConnection(this, (DbConnection)_connection, RetryPolicy);
#endif
				}

				if (_connection.State == ConnectionState.Closed)
				{ 
					_connection.Open();
					_closeConnection = true;
				}

				return _connection;
			}
		}

		public event EventHandler OnClosing;
		public event EventHandler OnClosed;

		public virtual void Close()
		{
			if (OnClosing != null)
				OnClosing(this, EventArgs.Empty);

			DisposeCommand();

			if (Transaction != null && _closeTransaction)
			{
				Transaction.Dispose();
				Transaction = null;
			}

			if (_connection != null && _closeConnection)
			{
				_connection.Dispose();
				_connection = null;
			}

			if (OnClosed != null)
				OnClosed(this, EventArgs.Empty);
		}

#endregion

#region Command

		public string LastQuery;

		internal void InitCommand(CommandType commandType, string sql, DataParameter[] parameters, List<string> queryHints)
		{
			if (queryHints != null && queryHints.Count > 0)
			{
				var sqlProvider = DataProvider.CreateSqlBuilder();
				sql = sqlProvider.ApplyQueryHints(sql, queryHints);
				queryHints.Clear();
			}

			DataProvider.InitCommand(this, commandType, sql, parameters);
			LastQuery = Command.CommandText;
		}

		private int? _commandTimeout;
		public  int   CommandTimeout
		{
			get { return _commandTimeout ?? 0; }
			set { _commandTimeout = value;     }
		}

		private IDbCommand _command;
		public  IDbCommand  Command
		{
			get { return _command ?? (_command = CreateCommand()); }
			set { _command = value; }
		}

		public IDbCommand CreateCommand()
		{
			var command = Connection.CreateCommand();

			if (_commandTimeout.HasValue)
				command.CommandTimeout = _commandTimeout.Value;

			if (Transaction != null)
				command.Transaction = Transaction;

			return command;
		}

		public void DisposeCommand()
		{
			if (_command != null)
			{
				DataProvider.DisposeCommand(this);
				_command = null;
			}
		}

		internal int ExecuteNonQuery()
		{
			if (TraceSwitch.Level == TraceLevel.Off || OnTraceConnection == null)
				return Command.ExecuteNonQuery();

			if (TraceSwitch.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(TraceInfoStep.BeforeExecute)
				{
					TraceLevel     = TraceLevel.Info,
					DataConnection = this,
					Command        = Command,
				});
			}

			var now = DateTime.Now;

			try
			{
				var ret = Command.ExecuteNonQuery();

				if (TraceSwitch.TraceInfo)
				{
					OnTraceConnection(new TraceInfo(TraceInfoStep.AfterExecute)
					{
						TraceLevel      = TraceLevel.Info,
						DataConnection  = this,
						Command         = Command,
						ExecutionTime   = DateTime.Now - now,
						RecordsAffected = ret,
					});
				}

				return ret;
			}
			catch (Exception ex)
			{
				if (TraceSwitch.TraceError)
				{
					OnTraceConnection(new TraceInfo(TraceInfoStep.Error)
					{
						TraceLevel     = TraceLevel.Error,
						DataConnection = this,
						Command        = Command,
						ExecutionTime  = DateTime.Now - now,
						Exception      = ex,
					});
				}

				throw;
			}
		}

		object ExecuteScalar()
		{
			if (TraceSwitch.Level == TraceLevel.Off || OnTraceConnection == null)
				return Command.ExecuteScalar();

			if (TraceSwitch.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(TraceInfoStep.BeforeExecute)
				{
					TraceLevel     = TraceLevel.Info,
					DataConnection = this,
					Command        = Command
				});
			}

			var now = DateTime.Now;

			try
			{
				var ret = Command.ExecuteScalar();

				if (TraceSwitch.TraceInfo)
				{
					OnTraceConnection(new TraceInfo(TraceInfoStep.AfterExecute)
					{
						TraceLevel     = TraceLevel.Info,
						DataConnection = this,
						Command        = Command,
						ExecutionTime  = DateTime.Now - now,
					});
				}

				return ret;
			}
			catch (Exception ex)
			{
				if (TraceSwitch.TraceError)
				{
					OnTraceConnection(new TraceInfo(TraceInfoStep.Error)
					{
						TraceLevel     = TraceLevel.Error,
						DataConnection = this,
						Command        = Command,
						ExecutionTime  = DateTime.Now - now,
						Exception      = ex,
					});
				}

				throw;
			}
		}

		private IDataReader ExecuteReader()
		{
			return ExecuteReader(GetCommandBehavior(CommandBehavior.Default));
		}

		internal IDataReader ExecuteReader(CommandBehavior commandBehavior)
		{
			if (TraceSwitch.Level == TraceLevel.Off || OnTraceConnection == null)
				return Command.ExecuteReader(GetCommandBehavior(CommandBehavior.Default));

			if (TraceSwitch.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(TraceInfoStep.BeforeExecute)
				{
					TraceLevel     = TraceLevel.Info,
					DataConnection = this,
					Command        = Command,
				});
			}

			var now = DateTime.Now;

			try
			{
				var ret = Command.ExecuteReader(GetCommandBehavior(CommandBehavior.Default));

				if (TraceSwitch.TraceInfo)
				{
					OnTraceConnection(new TraceInfo(TraceInfoStep.AfterExecute)
					{
						TraceLevel     = TraceLevel.Info,
						DataConnection = this,
						Command        = Command,
						ExecutionTime  = DateTime.Now - now,
					});
				}

				return ret;
			}
			catch (Exception ex)
			{
				if (TraceSwitch.TraceError)
				{
					OnTraceConnection(new TraceInfo(TraceInfoStep.Error)
					{
						TraceLevel     = TraceLevel.Error,
						DataConnection = this,
						Command        = Command,
						ExecutionTime  = DateTime.Now - now,
						Exception      = ex,
					});
				}

				throw;
			}
		}

		public static void ClearObjectReaderCache()
		{
			CommandInfo.ClearObjectReaderCache();
		}

#endregion

#region Transaction

		public IDbTransaction Transaction { get; private set; }
		
		public virtual DataConnectionTransaction BeginTransaction()
		{
			// If transaction is open, we dispose it, it will rollback all changes.
			//
			if (Transaction != null)
				Transaction.Dispose();

			// Create new transaction object.
			//
			Transaction = Connection.BeginTransaction();

			_closeTransaction = true;

			// If the active command exists.
			//
			if (_command != null)
				_command.Transaction = Transaction;

			return new DataConnectionTransaction(this);
		}

		public virtual DataConnectionTransaction BeginTransaction(IsolationLevel isolationLevel)
		{
			// If transaction is open, we dispose it, it will rollback all changes.
			//
			if (Transaction != null)
				Transaction.Dispose();

			// Create new transaction object.
			//
			Transaction = Connection.BeginTransaction(isolationLevel);

			_closeTransaction = true;

			// If the active command exists.
			//
			if (_command != null)
				_command.Transaction = Transaction;

			return new DataConnectionTransaction(this);
		}

		public virtual void CommitTransaction()
		{
			if (Transaction != null)
			{
				Transaction.Commit();

				if (_closeTransaction)
				{
					Transaction.Dispose();
					Transaction = null;

					if (_command != null)
						_command.Transaction = null;
				}
			}
		}

		public virtual void RollbackTransaction()
		{
			if (Transaction != null)
			{
				Transaction.Rollback();

				if (_closeTransaction)
				{
					Transaction.Dispose();
					Transaction = null;

					if (_command != null)
						_command.Transaction = null;
				}
			}
		}

#endregion

#region MappingSchema

		private MappingSchema _mappingSchema;

		public  MappingSchema  MappingSchema
		{
			get { return _mappingSchema; }
		}

		public bool InlineParameters { get; set; }

		private List<string> _queryHints;
		public  List<string>  QueryHints
		{
			get { return _queryHints ?? (_queryHints = new List<string>()); }
		}

		private List<string> _nextQueryHints;
		public  List<string>  NextQueryHints
		{
			get { return _nextQueryHints ?? (_nextQueryHints = new List<string>()); }
		}

		public DataConnection AddMappingSchema(MappingSchema mappingSchema)
		{
			_mappingSchema = new MappingSchema(mappingSchema, _mappingSchema);
			_id            = null;

			return this;
		}

#endregion

#region ICloneable Members

		DataConnection(string configurationString, IDataProvider dataProvider, string connectionString, IDbConnection connection, MappingSchema mappingSchema)
		{
			ConfigurationString = configurationString;
			DataProvider        = dataProvider;
			ConnectionString    = connectionString;
			_connection         = connection;
			_mappingSchema      = mappingSchema;
			_closeConnection    = true;
		}

		public object Clone()
		{
			var connection =
				_connection == null       ? null :
				_connection is ICloneable ? (IDbConnection)((ICloneable)_connection).Clone() :
				                            DataProvider.CreateConnection(ConnectionString);

			return new DataConnection(ConfigurationString, DataProvider, ConnectionString, connection, MappingSchema);
		}
		
#endregion

#region System.IDisposable Members

		protected bool Disposed { get; private set; }

		protected void ThrowOnDisposed()
		{
			if (Disposed)
				throw new ObjectDisposedException("DataConnection", "IDataContext is disposed, see https://github.com/linq2db/linq2db/wiki/Managing-data-connection");
		}

		public void Dispose()
		{
			Disposed = true;
			Close();
		}

#endregion

		internal CommandBehavior GetCommandBehavior(CommandBehavior commandBehavior)
		{
			return DataProvider.GetCommandBehavior(commandBehavior);
		}
	}
}
