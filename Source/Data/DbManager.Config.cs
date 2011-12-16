using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using LinqToDB.DataProvider;

namespace LinqToDB.Data
{
	using Configuration;
	using DataProvider;
	using Properties;

	public partial class DbManager
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="DbManager"/> class 
		/// and opens a database connection.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This constructor uses a configuration, which has been used first in your application. 
		/// If there has been no connection used before, an empty string is applied as a default configuration.
		/// </para>
		/// <para>
		/// See the <see cref="ConfigurationString"/> property 
		/// for an explanation and use of the default configuration.
		/// </para>
		/// </remarks>
		/// <include file="Examples.xml" path='examples/db[@name="ctor"]/*' />
		/// <seealso cref="AddConnectionString(string)"/>
		/// <returns>An instance of the database manager class.</returns>
		[DebuggerStepThrough]
		public DbManager() : this(DefaultConfiguration)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DbManager"/> class 
		/// and opens a database connection for the provided configuration.
		/// </summary>
		/// <remarks>
		/// See the <see cref="ConfigurationString"/> property 
		/// for an explanation and use of the configuration string.
		/// </remarks>
		/// <include file="Examples.xml" path='examples/db[@name="ctor(string)"]/*' />
		/// <param name="configurationString">Configuration string.</param>
		/// <returns>An instance of the <see cref="DbManager"/> class.</returns>
		[DebuggerStepThrough]
		public DbManager(string configurationString)
			: this(
				GetDataProvider    (configurationString),
				GetConnectionString(configurationString))
		{
			_configurationString = configurationString;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DbManager"/> class 
		/// and opens a database connection for the provided configuration.
		/// </summary>
		/// <remarks>
		/// See the <see cref="ConfigurationString"/> property 
		/// for an explanation and use of the configuration string.
		/// </remarks>
		/// <param name="configuration">Configuration string not containing provider name.</param>
		/// <param name="providerName">Provider configuration name.</param>
		/// <returns>An instance of the <see cref="DbManager"/> class.</returns>
		[DebuggerStepThrough]
		public DbManager(string providerName, string configuration)
			: this(
				GetDataProvider    (providerName + ProviderNameDivider + configuration),
				GetConnectionString(providerName + ProviderNameDivider + configuration))
		{
			_configurationString = providerName + ProviderNameDivider + configuration;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DbManager"/> class for the provided connection.
		/// </summary>
		/// <remarks>
		/// This constructor tries to open the connection if the connection state equals 
		/// <see cref="System.Data.ConnectionState">ConnectionState.Closed</see>. 
		/// In this case the <see cref="IDbConnection.ConnectionString"/> property of the connection 
		/// must be set before colling the constructor.
		/// Otherwise, it neither opens nor closes the connection. 
		/// </remarks>
		/// <exception cref="DataException">
		/// Type of the connection could not be recognized.
		/// </exception>
		/// <include file="Examples.xml" path='examples/db[@name="ctor(IDbConnection)"]/*' />
		/// <param name="connection">An instance of the <see cref="IDbConnection"/> class.</param>
		/// <returns>An instance of the <see cref="DbManager"/> class.</returns>
		[DebuggerStepThrough]
		public DbManager(IDbConnection connection)
			: this(GetDataProvider(connection), connection)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DbManager"/> class for the provided transaction.
		/// </summary>
		/// <include file="Examples.xml" path='examples/db[@name="ctor(IDbTransaction)"]/*' />
		/// <param name="transaction"></param>
		[DebuggerStepThrough]
		public DbManager(IDbTransaction transaction)
			: this(GetDataProvider(transaction.Connection), transaction)
		{
		}

		/*
		/// <summary>
		/// Initializes a new instance of the <see cref="DbManager"/> class 
		/// and opens a database connection for the provided configuration and database connection.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This constructor opens the connection only if the connection state equals 
		/// <see cref="System.Data.ConnectionState">ConnectionState.Closed</see>. 
		/// Otherwise, it neither opens nor closes the connection.
		/// </para>
		/// <para>
		/// See the <see cref="ConfigurationString"/> property 
		/// for an explanation and use of the configuration string.
		/// </para>
		/// </remarks>
		/// <include file="Examples.xml" path='examples/db[@name="ctor(IDbConnection,string)"]/*' />
		/// <param name="connection">An instance of the <see cref="IDbConnection"/>.</param>
		/// <param name="configurationString">The configuration string.</param>
		/// <returns>An instance of the <see cref="DbManager"/> class.</returns>
		[DebuggerStepThrough]
		public DbManager(
			IDbConnection connection,
			string        configurationString)
		{
			if (connection == null)
			{
				Init(configurationString);

				if (configurationString != null)
					OpenConnection(configurationString);
			}
			else
			{
				Init(connection);

				_configurationString = configurationString;
				_connection.ConnectionString = GetConnectionString(configurationString);

				if (_connection.State == ConnectionState.Closed)
					OpenConnection();
			}
		}
		*/

		#endregion

		#region Public Properties

		private string _configurationString;
		/// <summary>
		/// Gets the string used to open a database.
		/// </summary>
		/// <value>
		/// A string containing configuration settings.
		/// </value>
		/// <remarks>
		/// <para>
		/// An actual database connection string is read from the <i>appSettings</i> section 
		/// of application configuration file (App.config, Web.config, or Machine.config) 
		/// according to the follow rule:
		/// </para>
		/// <code>
		/// &lt;appSettings&gt;
		///     &lt;add 
		///         key   = "ConnectionString.<b>configurationString</b>" 
		///         va<i></i>lue = "Server=(local);Database=Northwind;Integrated Security=SSPI" /&gt;
		/// &lt;/appSettings&gt;
		/// </code>
		/// <para>
		/// If the configuration string is empty, the following rule is applied:
		/// </para>
		/// <code>
		/// &lt;appSettings&gt;
		///     &lt;add 
		///         key   = "ConnectionString" 
		///         va<i></i>lue = "Server=(local);Database=Northwind;Integrated Security=SSPI" /&gt;
		/// &lt;/appSettings&gt;
		/// </code>
		/// <para>
		/// If you don't want to use a configuration file, you can add a database connection string 
		/// using the <see cref="AddConnectionString(string)"/> method.
		/// </para>
		/// <para>
		/// The configuration string may have a prefix used to define a data provider. The following table
		/// contains prefixes for all supported data providers:
		/// <list type="table">
		/// <listheader><term>Prefix</term><description>Provider</description></listheader>
		/// <item><term>Sql</term><description>Data Provider for SQL Server</description></item>
		/// <item><term>OleDb</term><description>Data Provider for OLE DB</description></item>
		/// <item><term>Odbc</term><description>Data Provider for ODBC</description></item>
		/// <item><term>Oracle</term><description>Data Provider for Oracle</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		/// <seealso cref="AddConnectionString(string)"/>
		public string ConfigurationString
		{
			[DebuggerStepThrough]
			get { return _configurationString; }
		}

		#endregion

		#region Config Overrides

		protected virtual void InitDataProvider(IDbConnection connection)
		{
			DataProvider = GetDataProvider(connection);
		}

		protected virtual IDbConnection CloneConnection()
		{
			if (Connection is ICloneable || ConfigurationString == null)
				return _dataProvider.CloneConnection(_connection);

			var con = DataProvider.CreateConnectionObject();

			con.ConnectionString = GetConnectionString(ConfigurationString);

			return con;
		}

		protected virtual string GetConnectionHash()
		{
			return ConfigurationString ?? Connection.ConnectionString.GetHashCode().ToString();
		}

		#endregion

		#region Protected Static Members

		static DbManager()
		{
			AddDataProvider(new Sql2008DataProvider());
			AddDataProvider(new SqlDataProvider());
			AddDataProvider(new AccessDataProvider());
			AddDataProvider(new OleDbDataProvider());
			AddDataProvider(new OdbcDataProvider());

			var section = LinqToDBSection.Instance;

			if (section != null)
			{
				_defaultConfiguration = section.DefaultConfiguration;

				foreach (DataProviderElement provider in section.DataProviders)
				{
					var dataProviderType = Type.GetType(provider.TypeName, true);
					var providerInstance = (DataProviderBase)Activator.CreateInstance(dataProviderType);

					if (!string.IsNullOrEmpty(provider.Name))
						providerInstance.UniqueName = provider.Name;

					providerInstance.Configure(provider.Attributes);

					AddDataProvider(providerInstance);

					if (!provider.Default)
						continue;

					if (_defaultDataProviderName != null)
					{
						throw new ConfigurationErrorsException(string.Format(
							Resources.DbManager_MoreThenOneDefaultProvider, _defaultDataProviderName, providerInstance.UniqueName),
							provider.ElementInformation.Source, provider.ElementInformation.LineNumber);
					}

					_defaultDataProviderName = providerInstance.UniqueName;
				}
			}

			if (string.IsNullOrEmpty(_defaultConfiguration))
				_defaultConfiguration = ConfigurationManager.AppSettings.Get("LinqToDB.DefaultConfiguration");

			if (string.IsNullOrEmpty(_defaultDataProviderName))
				_defaultDataProviderName = SqlDataProviderBase.NameString;
		}

		volatile static string             _firstConfiguration;
		private  static DataProviderBase   _firstProvider;
		private  static readonly Hashtable _configurationList = Hashtable.Synchronized(new Hashtable());
		private  static readonly Hashtable _anyProviderConfigurationList = Hashtable.Synchronized(new Hashtable());

		private static DataProviderBase GetDataProvider(IDbConnection connection)
		{
			if (connection == null) throw new ArgumentNullException("connection");

			var dp = _dataProviderTypeList[connection.GetType()];

			if (dp == null)
				throw new DataException(string.Format(
					Resources.DbManager_UnknownConnectionType, connection.GetType().FullName));

			return dp;
		}

		public static DataProviderBase GetDataProvider(string configurationString)
		{
			if (configurationString == null) throw new ArgumentNullException("configurationString");

			if (configurationString.StartsWith(AnyProvider))
				return FindFirstSuitableProvider(configurationString);

			if (configurationString == _firstConfiguration)
				return _firstProvider;

			var dp = (DataProviderBase)_configurationList[configurationString];

			if (dp == null)
			{
				var css = ConfigurationManager.ConnectionStrings[configurationString];

				if (css != null && !string.IsNullOrEmpty(css.ProviderName))
				{
					// This hack should be redone.
					//
					var provider = css.ProviderName == "System.Data.SqlClient" ?
						configurationString.IndexOf("2008") >= 0 ?
							"MSSQL2008" :
							css.ProviderName :
						css.ProviderName;

					dp = _dataProviderNameList[provider];
				}
				else
				{
					// configurationString can be:
					// ''        : default provider,   default configuration;
					// '.'       : default provider,   default configuration;
					// 'foo.bar' :   'foo' provider,     'bar' configuration;
					// 'foo.'    :   'foo' provider,   default configuration;
					// 'foo'     : default provider,     'foo' configuration or
					//             foo     provider,   default configuration;
					// '.foo'    : default provider,     'foo' configuration;
					// '.foo.bar': default provider, 'foo.bar' configuration;
					//
					// Default provider is SqlDataProvider
					//
					var cs  = configurationString.ToUpper();
					var key = _defaultDataProviderName;

					if (cs.Length > 0)
					{
						cs += ProviderNameDivider;

						foreach (var k in _dataProviderNameList.Keys)
						{
							if (cs.StartsWith(k + ProviderNameDivider))
							{
								key = k;
								break;
							}
						}
					}

					dp = _dataProviderNameList[key];
				}

				if (dp == null)
					throw new DataException(string.Format(
						Resources.DbManager_UnknownDataProvider, configurationString));

				_configurationList[configurationString] = dp;
			}

			if (_firstConfiguration == null)
			{
				lock (_configurationList.SyncRoot)
				{
					if (_firstConfiguration == null)
					{
						_firstConfiguration = configurationString;
						_firstProvider      = dp;
					}
				}
			}

			return dp;
		}

		private static bool IsMatchedConfigurationString(string configurationString, string csWithoutProvider)
		{
			int dividerPos;

			return
				!configurationString.StartsWith(AnyProvider) &&
				(dividerPos = configurationString.IndexOf(ProviderNameDivider)) >= 0 &&
				0 == StringComparer.OrdinalIgnoreCase.Compare
					(configurationString.Substring(dividerPos + ProviderNameDivider.Length), csWithoutProvider);
		}

		private static DataProviderBase FindFirstSuitableProvider(string configurationString)
		{
			var cs = (string)_anyProviderConfigurationList[configurationString];
			var searchRequired = (cs == null);

			if (searchRequired)
			{
				var csWithoutProvider = configurationString.Substring(AnyProvider.Length);

				if (configurationString.Length == 0) throw new ArgumentNullException("configurationString");

				foreach (var str in _connectionStringList.Keys)
				{
					if (IsMatchedConfigurationString(str, csWithoutProvider))
					{
						cs = str;
						break;
					}
				}

				if (cs == null)
				{
					foreach (ConnectionStringSettings css in ConfigurationManager.ConnectionStrings)
					{
						if (IsMatchedConfigurationString(css.Name, csWithoutProvider))
						{
							cs = css.Name;
							break;
						}
					}
				}

				if (cs == null)
				{
					foreach (var name in ConfigurationManager.AppSettings.AllKeys)
					{
						if (name.StartsWith("ConnectionString" + ProviderNameDivider))
						{
							var str = name.Substring(name.IndexOf(ProviderNameDivider) + ProviderNameDivider.Length);

							if (IsMatchedConfigurationString(str, csWithoutProvider))
							{
								cs = str;
								break;
							}
						}
					}
				}

				if (cs == null)
					cs = csWithoutProvider;
			}

			var dp = GetDataProvider(cs);

			if (searchRequired)
				_anyProviderConfigurationList[configurationString] = cs;
			return dp;
		}

		private static readonly Dictionary<string,string> _connectionStringList = new Dictionary<string,string>(4);

		public static string GetConnectionString(string configurationString)
		{
			// Use default configuration.
			//
			if (configurationString == null) 
				configurationString = DefaultConfiguration;

			if (_anyProviderConfigurationList.Contains(configurationString))
				configurationString = (string)_anyProviderConfigurationList[configurationString];

			string str;

			// Check cached strings first.
			//
			if (!_connectionStringList.TryGetValue(configurationString, out str))
			{
				lock (_dataProviderListLock)
				{
					// Connection string is not in the cache.
					//
					var key = string.Format("ConnectionString{0}{1}",
						configurationString.Length == 0? String.Empty: ProviderNameDivider, configurationString);

					var css = ConfigurationManager.ConnectionStrings[configurationString];

					str = css != null? css.ConnectionString: ConfigurationManager.AppSettings.Get(key);

					if (string.IsNullOrEmpty(str))
					{
						throw new DataException(string.Format(
							Resources.DbManager_UnknownConfiguration, key));
					}

					// Store the result in cache.
					//
					_connectionStringList[configurationString] = str;
				}
			}

			return str;
		}

		/*
		private void OpenConnection(string configurationString)
		{
			// If connection is already opened, we close it and open again.
			//
			if (_connection != null)
			{
				Dispose();
				GC.ReRegisterForFinalize(this);
			}

			// Store the configuration string.
			//
			_configurationString = configurationString;

			// Create and open the connection object.
			//
			_connection = _dataProvider.CreateConnectionObject();
			_connection.ConnectionString = GetConnectionString(ConfigurationString);

			OpenConnection();
		}
		 */

		#endregion

		#region AddDataProvider

		static readonly Dictionary<string, DataProviderBase> _dataProviderNameList = new Dictionary<string, DataProviderBase>(8, StringComparer.OrdinalIgnoreCase);
		static readonly Dictionary<Type,   DataProviderBase> _dataProviderTypeList = new Dictionary<Type,   DataProviderBase>(4);
		static readonly object                               _dataProviderListLock = new object();

		/// <summary>
		/// Adds a new data provider.
		/// </summary>
		/// <remarks>
		/// The method can be used to register a new data provider for further use.
		/// </remarks>
		/// <seealso cref="AddConnectionString(string)"/>
		/// <seealso cref="DataProviderBase.Name"/>
		/// <param name="dataProvider">An instance of the <see cref="DataProviderBase"/> interface.</param>
		public static void AddDataProvider(DataProviderBase dataProvider)
		{
			if (null == dataProvider)
				throw new ArgumentNullException("dataProvider");

			if (string.IsNullOrEmpty(dataProvider.UniqueName))
				throw new ArgumentException(Resources.DbManager_InvalidDataProviderName, "dataProvider");

			if (string.IsNullOrEmpty(dataProvider.ProviderName))
				throw new ArgumentException(Resources.DbManager_InvalidDataProviderProviderName, "dataProvider");

			if (dataProvider.ConnectionType == null || !typeof(IDbConnection).IsAssignableFrom(dataProvider.ConnectionType))
				throw new ArgumentException(Resources.DbManager_InvalidDataProviderConnectionType, "dataProvider");

			lock (_dataProviderListLock)
			{
				_dataProviderNameList[dataProvider.UniqueName.ToUpper()] = dataProvider;
				_dataProviderNameList[dataProvider.ProviderName]   = dataProvider;
				_dataProviderTypeList[dataProvider.ConnectionType] = dataProvider;
			}
		}

		/// <summary>
		/// Adds a new data provider witch a specified name.
		/// </summary>
		/// <remarks>
		/// The method can be used to register a new data provider for further use.
		/// </remarks>
		/// <include file="Examples1.xml" path='examples/db[@name="AddDataProvider(DataProvider.IDataProvider)"]/*' />
		/// <seealso cref="AddConnectionString(string)"/>
		/// <seealso cref="DataProviderBase.Name"/>
		/// <param name="providerName">The data provider name.</param>
		/// <param name="dataProvider">An instance of the <see cref="DataProviderBase"/> interface.</param>
		public static void AddDataProvider(string providerName, DataProviderBase dataProvider)
		{
			if (dataProvider == null)
				throw new ArgumentNullException("dataProvider");

			if (string.IsNullOrEmpty(providerName))
				throw new ArgumentException(Resources.DbManager_InvalidDataProviderName, "providerName");

			dataProvider.UniqueName = providerName;
			AddDataProvider(dataProvider);
		}

		/// <summary>
		/// Adds a new data provider.
		/// </summary>
		/// <remarks>
		/// The method can be used to register a new data provider for further use.
		/// </remarks>
		/// <seealso cref="AddConnectionString(string)"/>
		/// <seealso cref="DataProviderBase.Name"/>
		/// <param name="dataProviderType">A data provider type.</param>
		public static void AddDataProvider(Type dataProviderType)
		{
			AddDataProvider((DataProviderBase)Activator.CreateInstance(dataProviderType));
		}

		/// <summary>
		/// Adds a new data provider witch a specified name.
		/// </summary>
		/// <remarks>
		/// The method can be used to register a new data provider for further use.
		/// </remarks>
		/// <seealso cref="AddConnectionString(string)"/>
		/// <seealso cref="DataProviderBase.Name"/>
		/// <param name="providerName">The data provider name.</param>
		/// <param name="dataProviderType">A data provider type.</param>
		public static void AddDataProvider(string providerName, Type dataProviderType)
		{
			AddDataProvider(providerName, (DataProviderBase)Activator.CreateInstance(dataProviderType));
		}

		#endregion

		#region AddConnectionString

		/// <summary>
		/// Adds a new connection string or replaces existing one.
		/// </summary>
		/// <remarks>
		/// Use this method when you use only one configuration and 
		/// you don't want to use a configuration file.
		/// </remarks>
		/// <include file="Examples.xml" path='examples/db[@name="AddConnectionString(string)"]/*' />
		/// <param name="connectionString">A valid database connection string.</param>
		public static void AddConnectionString(string connectionString)
		{
			AddConnectionString(string.Empty, connectionString);
		}

		/// <summary>
		/// Adds a new connection string or replaces existing one.
		/// </summary>
		/// <remarks>
		/// Use this method when you use multiple configurations and 
		/// you don't want to use a configuration file.
		/// </remarks>
		/// <include file="Examples.xml" path='examples/db[@name="AddConnectionString(string,string)"]/*' />
		/// <param name="configurationString">The configuration string.</param>
		/// <param name="connectionString">A valid database connection string.</param>
		public static void AddConnectionString(string configurationString, string connectionString)
		{
			_connectionStringList[configurationString] = connectionString;
		}

		/// <summary>
		/// Adds a new connection string or replaces existing one.
		/// </summary>
		/// <remarks>
		/// Use this method when you use multiple configurations and 
		/// you don't want to use a configuration file.
		/// </remarks>
		/// <include file="Examples.xml" path='examples/db[@name="AddConnectionString(string,string)"]/*' />
		/// <param name="providerName">The data provider name.</param>
		/// <param name="configurationString">The configuration string.</param>
		/// <param name="connectionString">A valid database connection string.</param>
		public static void AddConnectionString(
			string providerName, string configurationString, string connectionString)
		{
			AddConnectionString(providerName + ProviderNameDivider + configurationString, connectionString);
		}

		#endregion

		#region Public Static Properties

		public const string ProviderNameDivider = ".";
		public const string AnyProvider = "*" + ProviderNameDivider;

		private static readonly string _defaultDataProviderName;
		private static          string _defaultConfiguration;

		/// <summary>
		/// Gets and sets the default configuration string.
		/// </summary>
		/// <remarks>
		/// See the <see cref="ConfigurationString"/> property 
		/// for an explanation and use of the default configuration.
		/// </remarks>
		/// <value>
		/// A string containing default configuration settings.
		/// </value>
		/// <seealso cref="ConfigurationString"/>
		public static string DefaultConfiguration
		{
			get
			{
				if (_defaultConfiguration == null)
				{
					// Grab first registered configuration.
					//
					var defaultConfiguration = _connectionStringList.Select(de => de.Key).FirstOrDefault();

					if (defaultConfiguration == null)
					{
						defaultConfiguration = string.Empty;

						foreach (ConnectionStringSettings css in ConfigurationManager.ConnectionStrings)
						{
							if (css.ElementInformation.Source != null &&
								!css.ElementInformation.Source.EndsWith("machine.config", StringComparison.OrdinalIgnoreCase))
							{
								defaultConfiguration = css.Name;
								break;
							}
						}
					}

					_defaultConfiguration = defaultConfiguration;
				}

				return _defaultConfiguration;
			}

			set { _defaultConfiguration = value; }
		}

		#endregion
	}
}
