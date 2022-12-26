﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LinqToDB.Data
{
	using Async;
	using Common.Internal;
	using Configuration;
	using DataProvider;
	using RetryPolicy;

	public partial class DataConnection : IConfigurationID
	{
		static class Configuration
		{
			static Configuration()
			{
				Info = new();

				if (DefaultSettings != null)
					SetConnectionStrings(DefaultSettings.ConnectionStrings);
			}

			public static readonly ConcurrentDictionary<string,ConfigurationInfo> Info;
		}

		static ILinqToDBSettings? _defaultSettings;

		/// <summary>
		/// Gets or sets default connection settings. By default contains settings from linq2db configuration section from configuration file (not supported by .Net Core).
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
					var dataProvider = _dataProvider ??= GetDataProvider(_connectionStringSettings!, ConnectionString);

					if (dataProvider == null)
						throw new LinqToDBException($"DataProvider is not provided for configuration: {_configurationString}");

					return dataProvider;
				}
			}

			public static IDataProvider? GetDataProvider(IConnectionStringSettings css, string connectionString)
			{
				var configuration = css.Name;
				var providerName  = css.ProviderName;
				var dataProvider  = _providerDetectors.Select(d => d(css, connectionString)).FirstOrDefault(dp => dp != null);

				if (dataProvider == null)
				{
					IDataProvider? defaultDataProvider = null;

					if (DefaultDataProvider != null)
						_dataProviders.TryGetValue(DefaultDataProvider, out defaultDataProvider);

					if (string.IsNullOrEmpty(providerName))
						dataProvider = FindProvider(configuration, _dataProviders, defaultDataProvider);
					else if (!_dataProviders.TryGetValue(providerName!, out dataProvider) &&
					         !_dataProviders.TryGetValue(configuration, out dataProvider))
					{
						var providers = _dataProviders.Where(dp => dp.Value.ConnectionNamespace == providerName).ToList();

						dataProvider = providers.Count switch
						{
							0 => defaultDataProvider,
							1 => providers[0].Value,
							_ => FindProvider(configuration, providers, providers[0].Value),
						};
					}
				}

				if (dataProvider != null && DefaultConfiguration == null && !css.IsGlobal/*IsMachineConfig(css)*/)
				{
					DefaultConfiguration = css.Name;
				}

				return dataProvider;
			}
		}

		[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
		static IDataProvider? FindProvider(
			string                                          configuration,
			IEnumerable<KeyValuePair<string,IDataProvider>> providers,
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
			AddProviderDetector(LinqToDB.DataProvider.Access    .AccessTools    .ProviderDetector);
			AddProviderDetector(LinqToDB.DataProvider.DB2       .DB2Tools       .ProviderDetector);
			AddProviderDetector(LinqToDB.DataProvider.Firebird  .FirebirdTools  .ProviderDetector);
			AddProviderDetector(LinqToDB.DataProvider.Informix  .InformixTools  .ProviderDetector);
			AddProviderDetector(LinqToDB.DataProvider.MySql     .MySqlTools     .ProviderDetector);
			AddProviderDetector(LinqToDB.DataProvider.Oracle    .OracleTools    .ProviderDetector.DetectProvider);
			AddProviderDetector(LinqToDB.DataProvider.PostgreSQL.PostgreSQLTools.ProviderDetector.DetectProvider);
			AddProviderDetector(LinqToDB.DataProvider.SapHana   .SapHanaTools   .ProviderDetector);
			AddProviderDetector(LinqToDB.DataProvider.SqlCe     .SqlCeTools     .ProviderDetector);
			AddProviderDetector(LinqToDB.DataProvider.SQLite    .SQLiteTools    .ProviderDetector);
			AddProviderDetector(LinqToDB.DataProvider.SqlServer .SqlServerTools .ProviderDetector.DetectProvider);
			AddProviderDetector(LinqToDB.DataProvider.Sybase    .SybaseTools    .ProviderDetector);
			AddProviderDetector(LinqToDB.DataProvider.ClickHouse.ClickHouseTools.ProviderDetector);

			var section = DefaultSettings;

			if (section != null)
			{
				DefaultConfiguration = section.DefaultConfiguration;
				DefaultDataProvider  = section.DefaultDataProvider;

				foreach (var provider in section.DataProviders)
				{
					var dataProviderType = Type.GetType(provider.TypeName, true)!;
					var providerInstance = (IDataProviderFactory)Activator.CreateInstance(dataProviderType)!;

					if (!string.IsNullOrEmpty(provider.Name))
						AddDataProvider(provider.Name!, providerInstance.GetDataProvider(provider.Attributes));
				}
			}
		}

		static readonly List<Func<IConnectionStringSettings,string,IDataProvider?>> _providerDetectors = new();

		/// <summary>
		/// Registers database provider factory method.
		/// Factory accepts connection string settings and connection string. Could return <c>null</c>, if cannot create provider
		/// instance using provided options.
		/// </summary>
		/// <param name="providerDetector">Factory method delegate.</param>
		public static void AddProviderDetector(Func<IConnectionStringSettings,string,IDataProvider?> providerDetector)
		{
			_providerDetectors.Add(providerDetector);
		}

		/// <summary>
		/// Registers database provider factory method.
		/// Factory accepts connection string settings and connection string. Could return <c>null</c>, if cannot create provider
		/// instance using provided options.
		/// </summary>
		/// <param name="providerDetector">Factory method delegate.</param>
		public static void InsertProviderDetector(Func<IConnectionStringSettings,string,IDataProvider?> providerDetector)
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
				throw new ArgumentException("dataProvider.Name cannot be empty.", nameof(dataProvider));

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
				new ConnectionStringSettings(configurationString, connectionString, providerName),
				connectionString);
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
				new ConnectionStringSettings(providerName, connectionString, providerName),
				connectionString);
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

		internal static Lazy<IDataProvider> CreateDataProvider<T>()
			where T : IDataProvider, new()
		{
			return new(() =>
			{
				var provider = new T();
				AddDataProvider(provider);
				return provider;
			}, true);
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
				if (_configurationID == null || _msID != ((IConfigurationID)MappingSchema).ConfigurationID)
				{
					_configurationID = new IdentifierBuilder()
						.Add(_msID = ((IConfigurationID)MappingSchema).ConfigurationID)
						.Add(ConfigurationString ?? ConnectionString ?? Connection.ConnectionString)
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
				if (options.SavedDataProvider != null)
				{
					dataConnection.DataProvider        = options.SavedDataProvider;
					dataConnection.ConnectionString    = options.SavedConnectionString;
					dataConnection.MappingSchema       = options.SavedMappingSchema!;
					dataConnection.ConfigurationString = options.SavedConfigurationString;

					if (options.SavedEnableAutoFluentMapping)
						dataConnection.MappingSchema = new(dataConnection.MappingSchema);

					return;
				}

				var dataProvider = options.DataProviderFactory == null ? options.DataProvider : options.DataProviderFactory();
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
					case (_,               {} connectionString, {} provider, _,               _,             _,              _) :
					{
						dataConnection.DataProvider     = provider;
						dataConnection.ConnectionString = connectionString;
						dataConnection.MappingSchema    = provider.MappingSchema;

						break;
					}
					case (_,               {} connectionString, _,           {} providerName, _,             _,              _) :
					{
						dataConnection.DataProvider     = GetDataProviderEx(providerName, connectionString);
						dataConnection.ConnectionString = connectionString;
						dataConnection.MappingSchema    = dataConnection.DataProvider.MappingSchema;

						break;
					}
					case (_,               {},                  _,           _,               _,             _,              _) :
					case (_,               _,                   null,        _,               {},            _,              _) :
					case (_,               _,                   null,        _,               _,             {},             _) :
					case (_,               _,                   null,        _,               _,             _,              {}) :
					{
						throw new LinqToDBException("DataProvider was not specified");
					}
					case (_,               _,                   {} provider, _,               {} connection, _,              _) :
					{
						dataConnection._connection        = WrapConnection(connection);
						dataConnection._disposeConnection = options.DisposeConnection;

						dataConnection.DataProvider  = provider;
						dataConnection.MappingSchema = provider.MappingSchema;

						doSave = false;

						break;
					}
					case (_,               _,                   {} provider, _,               _,             {} transaction, _) :
					{
						dataConnection._connection        = WrapConnection(transaction.Connection!);
						dataConnection._closeTransaction  = false;
						dataConnection._closeConnection   = false;
						dataConnection._disposeConnection = false;

						dataConnection.TransactionAsync = AsyncFactory.Create(transaction);
						dataConnection.DataProvider     = provider;
						dataConnection.MappingSchema    = provider.MappingSchema;

						doSave = false;

						break;
					}
					case (_,               _,                   {} provider, _,               _,             _,              {} factory) :
					{
						dataConnection._connectionFactory = factory;

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

				if (options.MappingSchema != null)
				{
					dataConnection.AddMappingSchema(options.MappingSchema);
				}
				else if (dataConnection.Options.LinqOptions.EnableAutoFluentMapping)
				{
					options.SavedEnableAutoFluentMapping = true;
				}

				if (doSave)
				{
					options.SavedDataProvider        = dataConnection.DataProvider;
					options.SavedMappingSchema       = dataConnection.MappingSchema;
					options.SavedConnectionString    = dataConnection.ConnectionString;
					options.SavedConfigurationString = dataConnection.ConfigurationString;
				}

				if (options.SavedEnableAutoFluentMapping)
				{
					dataConnection.MappingSchema = new (dataConnection.MappingSchema);
				}

				IAsyncDbConnection WrapConnection(DbConnection connection)
				{
					// TODO: IT Look into.
					return connection is IAsyncDbConnection asyncDbConnection
						? asyncDbConnection
						: AsyncFactory.Create(connection);
				}
			}

			public static void Apply(DataConnection dataConnection, RetryPolicyOptions options)
			{
				dataConnection.RetryPolicy = options.RetryPolicy ?? options.Factory?.Invoke(dataConnection);
			}

			public static void Apply(DataConnection dataConnection, DataContextOptions options)
			{
				dataConnection._commandTimeout = options.CommandTimeout;

				if (options.Interceptors != null)
					foreach (var interceptor in options.Interceptors)
						dataConnection.AddInterceptor(interceptor);
			}

			public static void Apply(DataConnection dataConnection, QueryTraceOptions options)
			{
				if (options.OnTrace    != null) dataConnection.OnTraceConnection        = options.OnTrace;
				if (options.TraceLevel != null) dataConnection.TraceSwitchConnection    = new("DataConnection", "DataConnection trace switch") {Level = options.TraceLevel.Value};
				if (options.WriteTrace != null) dataConnection.WriteTraceLineConnection = options.WriteTrace;
			}
		}
	}
}
