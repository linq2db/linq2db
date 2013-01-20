using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace LinqToDB.Data
{
	using Configuration;
	using DataProvider;
	using Mapping;

	public partial class DataConnection : ICloneable
	{
		#region .ctor

		public DataConnection() : this(DefaultConfiguration)
		{
		}

		public DataConnection([JetBrains.Annotations.NotNull] string configurationString)
		{
			if (configurationString == null) throw new ArgumentNullException("configurationString");

			InitConfig();

			ConfigurationString = configurationString;

			ConfigurationInfo ci;

			if (_configurations.TryGetValue(configurationString, out ci))
			{
				DataProvider     = ci.DataProvider;
				_mappingSchema   = DataProvider.MappingSchema;
				ConnectionString = ci.ConnectionString;
			}
			else
			{
				throw new LinqToDBException(string.Format("Configuration '{0}' is not defined.", configurationString));
			}
		}

		public DataConnection([JetBrains.Annotations.NotNull] IDataProvider dataProvider, [JetBrains.Annotations.NotNull] string connectionString)
		{
			if (dataProvider     == null) throw new ArgumentNullException("dataProvider");
			if (connectionString == null) throw new ArgumentNullException("connectionString");

			InitConfig();

			DataProvider     = dataProvider;
			_mappingSchema   = DataProvider.MappingSchema;
			ConnectionString = connectionString;
		}

		public DataConnection([JetBrains.Annotations.NotNull] IDataProvider dataProvider, [JetBrains.Annotations.NotNull] IDbConnection connection)
		{
			if (dataProvider == null) throw new ArgumentNullException("dataProvider");
			if (connection   == null) throw new ArgumentNullException("connection");
			
			InitConfig();

			DataProvider   = dataProvider;
			_mappingSchema = DataProvider.MappingSchema;
			_connection    = connection;
		}

		public DataConnection([JetBrains.Annotations.NotNull] IDataProvider dataProvider, [JetBrains.Annotations.NotNull] IDbTransaction transaction)
		{
			if (dataProvider == null) throw new ArgumentNullException("dataProvider");
			if (transaction  == null) throw new ArgumentNullException("transaction");
			
			InitConfig();

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

		static readonly ConcurrentDictionary<string,int> _configurationIDs = new ConcurrentDictionary<string,int>();
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

		private static TraceSwitch _traceSwitch;
		public  static TraceSwitch  TraceSwitch
		{
			get { return _traceSwitch ?? (_traceSwitch = new TraceSwitch("DataConnection", "DataConnection trace switch",
#if DEBUG
				"Warning"
#else
				"Off"
#endif
				)); }
			set { _traceSwitch = value; }
		}

		public static void TurnTraceSwitchOn()
		{
			TraceSwitch = new TraceSwitch("DataConnection", "DataConnection trace switch", "Info");
		}

		public static Action<string,string> WriteTraceLine = (message, displayName) => Debug.WriteLine(message, displayName);

		#endregion

		#region Configuration

		static IDataProvider FindProvider(
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
			var section = LinqToDBSection.Instance;

			if (section != null)
			{
				DefaultConfiguration = section.DefaultConfiguration;
				DefaultDataProvider  = section.DefaultDataProvider;

				foreach (DataProviderElement provider in section.DataProviders)
				{
					var dataProviderType = Type.GetType(provider.TypeName, true);
					var providerInstance = (IDataProviderFactory)Activator.CreateInstance(dataProviderType);

					if (!string.IsNullOrEmpty(provider.Name))
						AddDataProvider(provider.Name, providerInstance.GetDataProvider(provider.Attributes));
				}
			}
		}

		static void InitConnectionStrings()
		{
			var defaultDataProvider = DefaultDataProvider != null ? _dataProviders[DefaultDataProvider] : null;

			foreach (ConnectionStringSettings css in ConfigurationManager.ConnectionStrings)
			{
				var configuration    = css.Name;
				var connectionString = css.ConnectionString;
				var providerName     = css.ProviderName;

				IDataProvider dataProvider;

				if (string.IsNullOrEmpty(providerName))
					dataProvider = FindProvider(configuration, _dataProviders, defaultDataProvider);
				else if (_dataProviders.ContainsKey(providerName))
					dataProvider = _dataProviders[providerName];
				else if (_dataProviders.ContainsKey(configuration))
					dataProvider = _dataProviders[configuration];
				else
				{
					var providers = _dataProviders.Where(dp => dp.Value.ConnectionType.Namespace == providerName).ToList();

					switch (providers.Count)
					{
						case 0  : dataProvider = defaultDataProvider;                                        break;
						case 1  : dataProvider = providers[0].Value;                                         break;
						default : dataProvider = FindProvider(configuration, providers, providers[0].Value); break;
					}
				}

				if (dataProvider != null)
				{
					AddConfiguration(configuration, connectionString, dataProvider);

					if (DefaultConfiguration == null &&
						css.ElementInformation.Source != null &&
						!css.ElementInformation.Source.EndsWith("machine.config", StringComparison.OrdinalIgnoreCase))
					{
						DefaultConfiguration = css.Name;
					}
				}
			}
		}

		static          bool   _isInitialized;
		static readonly object _initSync = new object();

		static void InitConfig()
		{
			if (!_isInitialized)
				lock (_initSync)
					if (!_isInitialized)
					{
						InitConnectionStrings();
						_isInitialized = true;
					}
		}

		static readonly ConcurrentDictionary<string,IDataProvider> _dataProviders =
			new ConcurrentDictionary<string,IDataProvider>();

		public static void AddDataProvider([JetBrains.Annotations.NotNull] string providerName, [JetBrains.Annotations.NotNull] IDataProvider dataProvider)
		{
			if (providerName == null) throw new ArgumentNullException("providerName");
			if (dataProvider == null) throw new ArgumentNullException("dataProvider");

			if (string.IsNullOrEmpty(dataProvider.Name))
				throw new ArgumentException("dataProvider.Name cant be empty.", "dataProvider");

			_dataProviders[providerName] = dataProvider;
		}

		public static void AddDataProvider([JetBrains.Annotations.NotNull] IDataProvider dataProvider)
		{
			if (dataProvider == null) throw new ArgumentNullException("dataProvider");

			AddDataProvider(dataProvider.Name, dataProvider);
		}

		public static IDataProvider GetDataProvider([JetBrains.Annotations.NotNull] string dataProviderName)
		{
			if (dataProviderName == null) throw new ArgumentNullException("dataProviderName");

			return _dataProviders[dataProviderName];
		}

		class ConfigurationInfo
		{
			public ConfigurationInfo(string connectionString, IDataProvider dataProvider)
			{
				ConnectionString = connectionString;
				DataProvider     = dataProvider;
			}

			public readonly string        ConnectionString;
			public readonly IDataProvider DataProvider;
		}

		static readonly ConcurrentDictionary<string,ConfigurationInfo> _configurations =
			new ConcurrentDictionary<string, ConfigurationInfo>();

		public static void AddConfiguration([JetBrains.Annotations.NotNull] string configuration, [JetBrains.Annotations.NotNull] string connectionString, IDataProvider dataProvider = null)
		{
			if (configuration    == null) throw new ArgumentNullException("configuration");
			if (connectionString == null) throw new ArgumentNullException("connectionString");

			_configurations[configuration] = new ConfigurationInfo(
				connectionString,
				dataProvider ?? FindProvider(configuration, _dataProviders, _dataProviders[DefaultDataProvider]));
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
					_connection = DataProvider.CreateConnection(ConnectionString);

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

			if (_command != null)
			{
				_command.Dispose();
				_command = null;
			}

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
				_command.CommandTimeout = _commandTimeout.Value;

			if (Transaction != null)
				command.Transaction = Transaction;

			return command;
		}

		#endregion

		#region Transaction

		public IDbTransaction Transaction { get; private set; }
		
		public virtual void BeginTransaction()
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
		}

		public virtual void BeginTransaction(IsolationLevel isolationLevel)
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

		public DataConnection AddMappingSchema(MappingSchema mappingSchema)
		{
			_mappingSchema = new MappingSchema(mappingSchema, _mappingSchema);
			_id            = null;

			return this;
		}

		public 

		#endregion

		#region ICloneable Members

		DataConnection(string configurationString, IDataProvider dataProvider, string connectionString, IDbConnection connection, MappingSchema mappingSchema)
		{
			ConfigurationString = configurationString;
			DataProvider        = dataProvider;
			ConnectionString    = connectionString;
			_connection         = connection;
			_mappingSchema      = mappingSchema;
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

		public void Dispose()
		{
			Close();
		}

		#endregion
	}
}
