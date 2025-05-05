using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using LinqToDB.Async;
using LinqToDB.Common.Internal;
using LinqToDB.Configuration;
using LinqToDB.Data.RetryPolicy;
using LinqToDB.DataProvider;
using LinqToDB.Interceptors;
using LinqToDB.Interceptors.Internal;

namespace LinqToDB.Data
{
	public partial class DataConnection
	{
		static class Configuration
		{
			static Configuration()
			{
				Info = new();
				EnsureInit();
			}

			public static readonly ConcurrentDictionary<string,ConfigurationInfo> Info;

			public static void EnsureInit()
			{
				if (DefaultSettings != null)
				{
					Info.Clear();
					SetConnectionStrings(DefaultSettings.ConnectionStrings);
				}
			}
		}

		static ILinqToDBSettings? _defaultSettings;

		/// <summary>
		/// Gets or sets default connection settings. By default, contains settings from linq2db configuration section from configuration file (not supported by .Net Core).
		/// <seealso cref="ILinqToDBSettings"/>
		/// </summary>
		public static ILinqToDBSettings? DefaultSettings
		{
#if NETFRAMEWORK
			get => _defaultSettings ??= LinqToDBSection.Instance;
#else
			get => _defaultSettings;
#endif
			set
			{
				_defaultSettings    = value;
				_defaultDataOptions = null;

				Configuration.EnsureInit();
			}
		}

		/// <summary>
		/// Register connection strings for use by data connection class.
		/// </summary>
		/// <param name="connectionStrings">Collection of connection string configurations.</param>
		public static void SetConnectionStrings(IEnumerable<IConnectionStringSettings> connectionStrings)
		{
			foreach (var css in connectionStrings)
			{
				Configuration.Info[css.Name] = new(css);

				if (DefaultConfiguration == null && !css.IsGlobal /*IsMachineConfig(css)*/)
				{
					DefaultConfiguration = css.Name;
				}
			}
		}

		private  static DataOptions? _defaultDataOptions;
		internal static DataOptions   DefaultDataOptions => _defaultDataOptions ??= new(new());

		internal static void ResetDefaultOptions()
		{
			_defaultDataOptions = null;
		}

		internal static ConcurrentDictionary<string,DataOptions> ConnectionOptionsByConfigurationString = new();

		internal sealed class ConfigurationInfo
		{
			readonly bool    _dataProviderSet;
			readonly string? _configurationString;

			public ConfigurationInfo(string configurationString, string connectionString, IDataProvider? dataProvider)
			{
				ConnectionString     = connectionString;
				_dataProvider        = dataProvider;
				_dataProviderSet     = dataProvider != null;
				_configurationString = configurationString;
			}

			public ConfigurationInfo(IConnectionStringSettings connectionStringSettings)
			{
				ConnectionString = connectionStringSettings.ConnectionString;

				_connectionStringSettings = connectionStringSettings;
			}

			private string? _connectionString;
			public  string   ConnectionString
			{
				get => _connectionString!;
				set
				{
					if (!_dataProviderSet)
						_dataProvider = null;

					_connectionString = value;
				}
			}

			readonly IConnectionStringSettings? _connectionStringSettings;

			private IDataProvider? _dataProvider;
			public  IDataProvider   DataProvider
			{
				get
				{
					var dataProvider = _dataProvider ??= GetDataProvider(
						new (ConfigurationString: _connectionStringSettings?.Name, ConnectionString: ConnectionString, ProviderName: _connectionStringSettings?.ProviderName),
						_connectionStringSettings?.IsGlobal ?? false);

					if (dataProvider == null)
						throw new LinqToDBException($"DataProvider is not provided for configuration: {_configurationString}");

					return dataProvider;
				}
			}

			public static IDataProvider? GetDataProvider(ConnectionOptions options, bool isGlobal)
			{
				var configuration = options.ConfigurationString;
				var providerName  = options.ProviderName;
				var dataProvider  = _providerDetectors.Select(d => d(options)).FirstOrDefault(dp => dp != null);

				if (dataProvider == null)
				{
					IDataProvider? defaultDataProvider = null;

					if (DefaultDataProvider != null)
						_dataProviders.TryGetValue(DefaultDataProvider, out defaultDataProvider);

					if (string.IsNullOrEmpty(providerName))
						dataProvider = FindProvider(configuration!, _dataProviders, defaultDataProvider);
					else if (!_dataProviders.TryGetValue(providerName!, out dataProvider) &&
					         !_dataProviders.TryGetValue(configuration!, out dataProvider))
					{
						var providers = _dataProviders.Where(dp => dp.Value.ConnectionNamespace == providerName).ToList();

						dataProvider = providers.Count switch
						{
							0 => defaultDataProvider,
							1 => providers[0].Value,
							_ => FindProvider(configuration!, providers, providers[0].Value),
						};
					}
				}

				if (dataProvider != null && DefaultConfiguration == null && !isGlobal)
				{
					DefaultConfiguration = configuration;
				}

				return dataProvider;
			}
		}

		[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
		static IDataProvider? FindProvider(
			string                                          configuration,
			ICollection<KeyValuePair<string,IDataProvider>> providers,
			IDataProvider?                                  defp)
		{
			foreach (var p in providers.OrderByDescending(kv => kv.Key.Length))
				if (configuration == p.Key || configuration.StartsWith(p.Key + '.'))
					return p.Value;

			foreach (var p in providers.OrderByDescending(kv => kv.Value.Name.Length))
				if (configuration == p.Value.Name || configuration.StartsWith(p.Value.Name + '.'))
					return p.Value;

			return defp;
		}

		static DataConnection()
		{
			// lazy registration of embedded providers using detectors
			AddProviderDetector(LinqToDB.DataProvider.Access    .AccessTools    .ProviderDetector.DetectProvider);
			AddProviderDetector(LinqToDB.DataProvider.DB2       .DB2Tools       .ProviderDetector.DetectProvider);
			AddProviderDetector(LinqToDB.DataProvider.Firebird  .FirebirdTools  .ProviderDetector.DetectProvider);
			AddProviderDetector(LinqToDB.DataProvider.Informix  .InformixTools  .ProviderDetector.DetectProvider);
			AddProviderDetector(LinqToDB.DataProvider.MySql     .MySqlTools     .ProviderDetector.DetectProvider);
			AddProviderDetector(LinqToDB.DataProvider.Oracle    .OracleTools    .ProviderDetector.DetectProvider);
			AddProviderDetector(LinqToDB.DataProvider.PostgreSQL.PostgreSQLTools.ProviderDetector.DetectProvider);
			AddProviderDetector(LinqToDB.DataProvider.SapHana   .SapHanaTools   .ProviderDetector.DetectProvider);
			AddProviderDetector(LinqToDB.DataProvider.SqlCe     .SqlCeTools     .ProviderDetector);
			AddProviderDetector(LinqToDB.DataProvider.SQLite    .SQLiteTools    .ProviderDetector.DetectProvider);
			AddProviderDetector(LinqToDB.DataProvider.SqlServer .SqlServerTools .ProviderDetector.DetectProvider);
			AddProviderDetector(LinqToDB.DataProvider.Sybase    .SybaseTools    .ProviderDetector.DetectProvider);
			AddProviderDetector(LinqToDB.DataProvider.ClickHouse.ClickHouseTools.ProviderDetector.DetectProvider);

			var section = DefaultSettings;

			if (section != null)
			{
				DefaultConfiguration = section.DefaultConfiguration;
				DefaultDataProvider  = section.DefaultDataProvider;

				foreach (var provider in section.DataProviders)
				{
					var dataProviderType = Type.GetType(provider.TypeName, true)!;
					var providerInstance = ActivatorExt.CreateInstance<IDataProviderFactory>(dataProviderType);

					if (!string.IsNullOrEmpty(provider.Name))
						AddDataProvider(provider.Name!, providerInstance.GetDataProvider(provider.Attributes));
				}
			}
		}

		static readonly List<Func<ConnectionOptions,IDataProvider?>> _providerDetectors = new();

		/// <summary>
		/// Registers database provider factory method.
		/// Factory accepts connection string settings and connection string. Could return <c>null</c>, if cannot create provider
		/// instance using provided options.
		/// </summary>
		/// <param name="providerDetector">Factory method delegate.</param>
		public static void AddProviderDetector(Func<ConnectionOptions, IDataProvider?> providerDetector)
		{
			_providerDetectors.Add(providerDetector);
		}

		/// <summary>
		/// Registers database provider factory method.
		/// Factory accepts connection string settings and connection string. Could return <c>null</c>, if cannot create provider
		/// instance using provided options.
		/// </summary>
		/// <param name="providerDetector">Factory method delegate.</param>
		public static void InsertProviderDetector(Func<ConnectionOptions, IDataProvider?> providerDetector)
		{
			_providerDetectors.Insert(0, providerDetector);
		}

		static readonly ConcurrentDictionary<string,IDataProvider> _dataProviders = new();

		internal static IDataProvider GetDataProviderEx(string providerName, string connectionString)
		{
			if (!_dataProviders.TryGetValue(providerName, out var dataProvider))
				dataProvider = GetDataProvider(providerName, connectionString);

			return  dataProvider ?? throw new LinqToDBException($"DataProvider '{providerName}' not found.");
		}

		/// <summary>
		/// Registers database provider implementation by provided unique name.
		/// </summary>
		/// <param name="providerName">Provider name, to which provider implementation will be mapped.</param>
		/// <param name="dataProvider">Database provider implementation.</param>
		public static void AddDataProvider(
			string        providerName,
			IDataProvider dataProvider)
		{
			if (providerName == null) throw new ArgumentNullException(nameof(providerName));
			if (dataProvider == null) throw new ArgumentNullException(nameof(dataProvider));

			if (string.IsNullOrEmpty(dataProvider.Name))
				// temporary (?) suppression due to https://github.com/dotnet/roslyn-analyzers/issues/6863
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
				throw new ArgumentException("dataProvider.Name cannot be empty.", nameof(dataProvider));
#pragma warning restore CA2208 // Instantiate argument exceptions correctly

			_dataProviders[providerName] = dataProvider;
		}

		/// <summary>
		/// Registers database provider implementation using <see cref="IDataProvider.Name"/> name.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation.</param>
		public static void AddDataProvider(IDataProvider dataProvider)
		{
			if (dataProvider == null) throw new ArgumentNullException(nameof(dataProvider));

			AddDataProvider(dataProvider.Name, dataProvider);
		}

		/// <summary>
		/// Returns database provider implementation, associated with provided connection configuration name.
		/// </summary>
		/// <param name="configurationString">Connection configuration name.</param>
		/// <returns>Database provider.</returns>
		public static IDataProvider GetDataProvider(string configurationString)
		{
			return GetConfigurationInfo(configurationString).DataProvider;
		}

		/// <summary>
		/// Returns database provider associated with provider name, configuration and connection string.
		/// </summary>
		/// <param name="providerName">Provider name.</param>
		/// <param name="configurationString">Connection configuration name.</param>
		/// <param name="connectionString">Connection string.</param>
		/// <returns>Database provider.</returns>
		public static IDataProvider? GetDataProvider(
			string providerName,
			string configurationString,
			string connectionString)
		{
			return ConfigurationInfo.GetDataProvider(
				new (ConfigurationString: configurationString, ConnectionString: connectionString, ProviderName: providerName),
				false);
		}

		/// <summary>
		/// Returns database provider associated with provider name and connection string.
		/// </summary>
		/// <param name="providerName">Provider name.</param>
		/// <param name="connectionString">Connection string.</param>
		/// <returns>Database provider.</returns>
		public static IDataProvider? GetDataProvider(
			string providerName,
			string connectionString)
		{
			return ConfigurationInfo.GetDataProvider(
				new(ConfigurationString: providerName, ConnectionString: connectionString, ProviderName: providerName),
				false);
		}

		/// <summary>
		/// Returns registered database providers.
		/// </summary>
		/// <returns>
		/// Returns registered providers collection.
		/// </returns>
		public static IReadOnlyDictionary<string, IDataProvider> GetRegisteredProviders() =>
			_dataProviders.ToDictionary(p => p.Key, p => p.Value);

		internal static ConfigurationInfo GetConfigurationInfo(string? configurationString)
		{
			var key = configurationString ?? DefaultConfiguration;

			if (key == null)
				throw new LinqToDBException("Configuration string is not provided.");

			if (Configuration.Info.TryGetValue(key, out var ci))
				return ci;

			throw new LinqToDBException($"Configuration '{configurationString}' is not defined.");
		}

		/// <summary>
		/// Register connection configuration with specified connection string and database provider implementation.
		/// </summary>
		/// <param name="configuration">Connection configuration name.</param>
		/// <param name="connectionString">Connection string.</param>
		/// <param name="dataProvider">Database provider. If not specified, will use provider, registered using <paramref name="configuration"/> value.</param>
		public static void AddConfiguration(
			string         configuration,
			string         connectionString,
			IDataProvider? dataProvider = null)
		{
			if (configuration    == null) throw new ArgumentNullException(nameof(configuration));
			if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));

			if (dataProvider == null)
			{
				IDataProvider? defaultDataProvider = null;
				if (DefaultDataProvider != null)
					_dataProviders.TryGetValue(DefaultDataProvider, out defaultDataProvider);

				dataProvider = FindProvider(configuration, _dataProviders, defaultDataProvider);
			}

			var info = new ConfigurationInfo(
				configuration,
				connectionString,
				dataProvider);

			Configuration.Info.AddOrUpdate(configuration, info, (_, _) => info);

			_defaultDataOptions = null;
			ConnectionOptionsByConfigurationString.Clear();
		}

		public static void AddOrSetConfiguration(
			string configuration,
			string connectionString,
			string dataProvider)
		{
			if (configuration    == null) throw new ArgumentNullException(nameof(configuration));
			if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));
			if (dataProvider     == null) throw new ArgumentNullException(nameof(dataProvider));

			var info = new ConfigurationInfo(
				new ConnectionStringSettings(configuration, connectionString, dataProvider));

			Configuration.Info.AddOrUpdate(configuration, info, (_,_) => info);
		}

		/// <summary>
		/// Sets connection string for specified connection name.
		/// </summary>
		/// <param name="configuration">Connection name.</param>
		/// <param name="connectionString">Connection string.</param>
		public static void SetConnectionString(
			string configuration,
			string connectionString)
		{
			if (configuration    == null) throw new ArgumentNullException(nameof(configuration));
			if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));

			GetConfigurationInfo(configuration).ConnectionString = connectionString;
		}

		/// <summary>
		/// Returns connection string for specified connection name.
		/// </summary>
		/// <param name="configurationString">Connection name.</param>
		/// <returns>Connection string.</returns>
		public static string GetConnectionString(string configurationString)
		{
			return GetConfigurationInfo(configurationString).ConnectionString;
		}

		/// <summary>
		/// Returns connection string for specified configuration name or NULL.
		/// </summary>
		/// <param name="configurationString">Configuration.</param>
		/// <returns>Connection string or NULL.</returns>
		public static string? TryGetConnectionString(string? configurationString)
		{
			var key = configurationString ?? DefaultConfiguration;

			return key != null && Configuration.Info.TryGetValue(key, out var ci) ? ci.ConnectionString : null;
		}

		static string? _defaultConfiguration;
		static string? _defaultDataProvider;

		/// <summary>
		/// Gets or sets default connection configuration name. Used by <see cref="DataConnection"/> by default and could be set automatically from:
		/// <para> - <see cref="ILinqToDBSettings.DefaultConfiguration"/>;</para>
		/// <para> - first non-global connection string name from <see cref="ILinqToDBSettings.ConnectionStrings"/>;</para>
		/// <para> - first non-global connection string name passed to <see cref="SetConnectionStrings"/> method.</para>
		/// </summary>
		/// <seealso cref="DefaultConfiguration"/>
		public static string? DefaultConfiguration
		{
			get => _defaultConfiguration;
			set
			{
				_defaultConfiguration = value;
				_defaultDataOptions   = null;
			}
		}

		/// <summary>
		/// Gets or sets name of default data provider, used by new connection if user didn't specified provider explicitly in constructor or in connection options.
		/// Initialized with value from <see cref="DefaultSettings"/>.<see cref="ILinqToDBSettings.DefaultDataProvider"/>.
		/// </summary>
		/// <seealso cref="DefaultConfiguration"/>
		public static string? DefaultDataProvider
		{
			get => _defaultDataProvider;
			set
			{
				_defaultDataProvider = value;
				_defaultDataOptions  = null;
			}
		}

		int  _msID;
		int? _configurationID;

		int IConfigurationID.ConfigurationID
		{
			get
			{
				CheckAndThrowOnDisposed();

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

		internal static class ConfigurationApplier
		{
			public static void Apply(DataConnection dataConnection, ConnectionOptions options)
			{
				if (options.ConnectionInterceptor != null)
					dataConnection.AddInterceptor(options.ConnectionInterceptor);

				if (options.SavedDataProvider != null)
				{
					dataConnection.DataProvider        = options.SavedDataProvider;
					dataConnection.ConnectionString    = options.SavedConnectionString;
					dataConnection.MappingSchema       = options.SavedMappingSchema!;
					dataConnection.ConfigurationString = options.SavedConfigurationString;

					if (options.SavedEnableContextSchemaEdit)
						dataConnection.MappingSchema = new(dataConnection.MappingSchema);

					return;
				}

				var dataProvider = options.DataProviderFactory == null ? options.DataProvider : options.DataProviderFactory(options);
				var doSave       = true;

				switch (
				          options.ConfigurationString,
				                           options.ConnectionString,
				                                                dataProvider,
				                                                             options.ProviderName,
				                                                                              options.DbConnection,
				                                                                                             options.DbTransaction,
				                                                                                                             options.ConnectionFactory)
				{
					case (_,               {} connectionString, {} provider, _,               null,          null,           null) :
					{
						dataConnection.DataProvider     = provider;
						dataConnection.ConnectionString = connectionString;
						dataConnection.MappingSchema    = provider.MappingSchema;

						break;
					}
					case (_,               {} connectionString, _,           {} providerName, null,          null,           null) :
					{
						dataConnection.DataProvider     = GetDataProviderEx(providerName, connectionString);
						dataConnection.ConnectionString = connectionString;
						dataConnection.MappingSchema    = dataConnection.DataProvider.MappingSchema;

						break;
					}
					case ({} configString, _,                   {} provider, _,               null,          null,           null) :
					{
						dataConnection.ConfigurationString = configString;

						var ci = GetConfigurationInfo(configString);

						dataConnection.DataProvider     = provider;
						dataConnection.ConnectionString = ci.ConnectionString;
						dataConnection.MappingSchema    = provider.MappingSchema;

						break;
					}
					case ({} configString, {} connectionString, _,           _,               null,          null,           null) :
					{
						dataConnection.ConfigurationString = configString;

						var ci = GetConfigurationInfo(configString);

						dataConnection.DataProvider     = ci.DataProvider;
						dataConnection.ConnectionString = connectionString;
						dataConnection.MappingSchema    = ci.DataProvider.MappingSchema;

						break;
					}
					case (_,               not null,            null,        null,            _,             _,              _) :
					case (_,               _,                   null,        null,            not null,      _,              _) :
					case (_,               _,                   null,        null,            _,             not null,       _) :
					case (_,               _,                   null,        _,               _,             _,              not null) :
					{
						throw new LinqToDBException("DataProvider was not specified");
					}
					case (_,               _,                   {} provider, _,               {} connection, _,              _) :
					{
						SetConnection(provider, connection);
						break;
					}
					case (_,               _,                   _,           {} providerName, {} connection, _,              _) :
					{
						var provider = GetDataProviderEx(providerName, connection.ConnectionString);
						SetConnection(provider, connection);
						break;
					}
					case (_,               _,                   {} provider, _,               _,             {} transaction, _) :
					{
						SetTransaction(provider, transaction);
						break;
					}
					case (_,               _,                   _,           {} providerName, _,             {} transaction, _) :
					{
						var provider = GetDataProviderEx(providerName, transaction.Connection?.ConnectionString ?? throw new LinqToDBException("Transaction.Connection is null"));
						SetTransaction(provider, transaction);
						break;
					}
					case (_,               _,                   {} provider, _,               _,             _,              {} connectionFactory) :
					{
						dataConnection._connectionFactory = connectionFactory;
						dataConnection._disposeConnection = options.DisposeConnection ?? true;

						dataConnection.DataProvider  = provider;
						dataConnection.MappingSchema = provider.MappingSchema;

						doSave = false;

						break;
					}
					case ({} configString, _,                   _,           _,               _,             _,              _) :
					{
						dataConnection.ConfigurationString = configString;

						var ci = GetConfigurationInfo(configString);

						dataConnection.DataProvider     = ci.DataProvider;
						dataConnection.ConnectionString = ci.ConnectionString;
						dataConnection.MappingSchema    = ci.DataProvider.MappingSchema;

						break;
					}
					case (null,            _,                   _,           _,               _,             _,              _)
						when DefaultConfiguration != null :
					{
						dataConnection.ConfigurationString = DefaultConfiguration;

						var ci = GetConfigurationInfo(DefaultConfiguration);

						dataConnection.DataProvider     = ci.DataProvider;
						dataConnection.ConnectionString = ci.ConnectionString;
						dataConnection.MappingSchema    = ci.DataProvider.MappingSchema;

						break;
					}
					default :
						throw new LinqToDBException("Invalid configuration. Configuration string is not provided.");
				}

				// If DataProvider and ConnectionString are set, we do not really need ConfigurationString.
				// However, it can be useful for logging and debugging.
				//
				dataConnection.ConfigurationString ??= options.ConfigurationString ?? dataConnection.DataProvider.Name;

				if (options.MappingSchema != null)
				{
					dataConnection.AddMappingSchema(options.MappingSchema);
				}
				else if (dataConnection.Options.LinqOptions.EnableContextSchemaEdit)
				{
					options.SavedEnableContextSchemaEdit = true;
				}

				if (doSave)
				{
					options.SavedDataProvider        = dataConnection.DataProvider;
					options.SavedMappingSchema       = dataConnection.MappingSchema;
					options.SavedConnectionString    = dataConnection.ConnectionString;
					options.SavedConfigurationString = dataConnection.ConfigurationString;
				}

				if (options.SavedEnableContextSchemaEdit)
					dataConnection.MappingSchema = new (dataConnection.MappingSchema);

				IAsyncDbConnection WrapConnection(DbConnection connection)
				{
					return connection is IAsyncDbConnection asyncDbConnection
						? asyncDbConnection
						: AsyncFactory.CreateAndSetDataContext(dataConnection, connection);
				}

				void SetConnection(IDataProvider provider, DbConnection dbConnection)
				{
					dataConnection._connection        = WrapConnection(dbConnection);
					dataConnection._disposeConnection = options.DisposeConnection ?? false;

					dataConnection.DataProvider  = provider;
					dataConnection.MappingSchema = provider.MappingSchema;

					doSave = false;
				}

				void SetTransaction(IDataProvider provider, DbTransaction transaction)
				{
					dataConnection._connection        = WrapConnection(transaction.Connection!);
					dataConnection._closeTransaction  = false;
					dataConnection._closeConnection   = false;
					dataConnection._disposeConnection = false;

					dataConnection.TransactionAsync = AsyncFactory.CreateAndSetDataContext(dataConnection, transaction);
					dataConnection.DataProvider     = provider;
					dataConnection.MappingSchema    = provider.MappingSchema;

					doSave = false;
				}

			}

			public static Action? Reapply(DataConnection dataConnection, ConnectionOptions options, ConnectionOptions? previousOptions)
			{
				// For ConnectionOptions we reapply only mapping schema and connection interceptor.
				// Connection string, configuration, data provider, etc. are not reapplyable.
				//
				if (options.ConfigurationString       != previousOptions?.ConfigurationString)       throw new LinqToDBException("Configuration string cannot be changed.");
				if (options.ConnectionString          != previousOptions?.ConnectionString)          throw new LinqToDBException("ConnectionString cannot be changed.");
				if (options.ProviderName              != previousOptions?.ProviderName)              throw new LinqToDBException("ProviderName cannot be changed.");
				if (options.DbConnection              != previousOptions?.DbConnection)              throw new LinqToDBException("DbConnection cannot be changed.");
				if (options.DbTransaction             != previousOptions?.DbTransaction)             throw new LinqToDBException("DbTransaction cannot be changed.");
				if (options.DisposeConnection         != previousOptions?.DisposeConnection)         throw new LinqToDBException("DisposeConnection cannot be changed.");
				if (options.DataProvider              != previousOptions?.DataProvider)              throw new LinqToDBException("DataProvider cannot be changed.");
				if (options.ConnectionFactory         != previousOptions?.ConnectionFactory)         throw new LinqToDBException("ConnectionFactory cannot be changed.");
				if (options.DataProviderFactory       != previousOptions?.DataProviderFactory)       throw new LinqToDBException("DataProviderFactory cannot be changed.");
				if (options.OnEntityDescriptorCreated != previousOptions?.OnEntityDescriptorCreated) throw new LinqToDBException("OnEntityDescriptorCreated cannot be changed.");

				Action? action = null;

				if (!ReferenceEquals(options.ConnectionInterceptor, previousOptions?.ConnectionInterceptor))
				{
					if (previousOptions?.ConnectionInterceptor != null)
						dataConnection.RemoveInterceptor(previousOptions.ConnectionInterceptor);

					if (options.ConnectionInterceptor != null)
						dataConnection.AddInterceptor(options.ConnectionInterceptor);

					action += () =>
					{
						if (options.ConnectionInterceptor != null)
							dataConnection.RemoveInterceptor(options.ConnectionInterceptor);

						if (previousOptions?.ConnectionInterceptor != null)
							dataConnection.AddInterceptor(previousOptions.ConnectionInterceptor);
					};
				}

				if (!ReferenceEquals(options.MappingSchema, previousOptions?.MappingSchema))
				{
					var mappingSchema = dataConnection.MappingSchema;

					dataConnection.MappingSchema = dataConnection.DataProvider.MappingSchema;

					if (options.MappingSchema != null)
					{
						dataConnection.AddMappingSchema(options.MappingSchema);
					}
					else if (dataConnection.Options.LinqOptions.EnableContextSchemaEdit)
					{
						dataConnection.MappingSchema = new (dataConnection.MappingSchema);
					}

					action += () => dataConnection.MappingSchema = mappingSchema;
				}

				return action;
			}

			public static void Apply(DataConnection dataConnection, RetryPolicyOptions options)
			{
#pragma warning disable CS0618 // Type or member is obsolete
				dataConnection.RetryPolicy = options.RetryPolicy ?? options.Factory?.Invoke(dataConnection);
#pragma warning restore CS0618 // Type or member is obsolete
			}

			public static Action? Reapply(DataConnection dataConnection, RetryPolicyOptions options, RetryPolicyOptions? previousOptions)
			{
				var retryPolicy = dataConnection.RetryPolicy;

				Apply(dataConnection, options);

#pragma warning disable CS0618 // Type or member is obsolete
				return () => dataConnection.RetryPolicy = retryPolicy;
#pragma warning restore CS0618 // Type or member is obsolete
			}

			public static void Apply(DataConnection dataConnection, DataContextOptions options)
			{
				dataConnection._commandTimeout = options.CommandTimeout;

				if (options.Interceptors != null)
					foreach (var interceptor in options.Interceptors)
						dataConnection.AddInterceptor(interceptor);
			}

			public static Action? Reapply(DataConnection dataConnection, DataContextOptions options, DataContextOptions? previousOptions)
			{
				Action? action = null;

				if (options.CommandTimeout != previousOptions?.CommandTimeout)
				{
					var commandTimeout = dataConnection._commandTimeout;

					dataConnection.CommandTimeout = options.CommandTimeout ?? -1;

					action += () => dataConnection.CommandTimeout = commandTimeout ?? -1;
				}

				if (!ReferenceEquals(options.Interceptors, previousOptions?.Interceptors))
				{
					if (previousOptions?.Interceptors != null)
						foreach (var interceptor in previousOptions.Interceptors)
							dataConnection.RemoveInterceptor(interceptor);

					if (options.Interceptors != null)
						foreach (var interceptor in options.Interceptors)
							dataConnection.AddInterceptor(interceptor);

					action += () =>
					{
						if (options.Interceptors != null)
							foreach (var interceptor in options.Interceptors)
								dataConnection.RemoveInterceptor(interceptor);

						if (previousOptions?.Interceptors != null)
							foreach (var interceptor in previousOptions.Interceptors)
								dataConnection.AddInterceptor(interceptor);
					};
				}

				return action;
			}

			public static void Apply(DataConnection dataConnection, QueryTraceOptions options)
			{
#pragma warning disable CS0618 // Type or member is obsolete
				dataConnection.OnTraceConnection        = options.OnTrace    ?? DefaultOnTraceConnection;
				dataConnection.WriteTraceLineConnection = options.WriteTrace ?? WriteTraceLine;
				dataConnection._traceSwitchConnection   =
					options.TraceSwitch != null
						? dataConnection.TraceSwitchConnection = options.TraceSwitch
						: options.TraceLevel != null
							? new ("DataConnection", "DataConnection trace switch") { Level = options.TraceLevel.Value }
							: null;
#pragma warning restore CS0618 // Type or member is obsolete
			}

			public static Action Reapply(DataConnection dataConnection, QueryTraceOptions options, QueryTraceOptions? previousOptions)
			{
				var onTraceConnection        = dataConnection.OnTraceConnection;
				var writeTraceLineConnection = dataConnection.WriteTraceLineConnection;
				var traceSwitchConnection    = dataConnection._traceSwitchConnection;

				Apply(dataConnection, options);

				return () =>
				{
#pragma warning disable CS0618 // Type or member is obsolete
					dataConnection.OnTraceConnection        = onTraceConnection;
					dataConnection.WriteTraceLineConnection = writeTraceLineConnection;
					dataConnection._traceSwitchConnection   = traceSwitchConnection;
#pragma warning restore CS0618 // Type or member is obsolete
				};
			}
		}
	}
}
