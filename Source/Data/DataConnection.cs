using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;

using JetBrains.Annotations;

namespace LinqToDB.Data
{
	using Configuration;
	using DataProvider;

	public class DataConnection : ICloneable, IDisposable
	{
		#region .ctor

		public DataConnection()
			: this(DefaultConfiguration)
		{
		}

		public DataConnection([NotNull] string configurationString)
		{
			if (configurationString == null) throw new ArgumentNullException("configurationString");

			ConfigurationString = configurationString;

			ConfigurationInfo ci;

			if (_configurations.TryGetValue(configurationString, out ci))
			{
				DataProvider     = ci.DataProvider;
				ConnectionString = ci.ConnectionString;
			}
			else
			{
				throw new LinqToDBException(string.Format("Configuration '{0}' is not defined.", configurationString));
			}
		}

		public DataConnection([NotNull] IDataProvider dataProvider, [NotNull] string connectionString)
		{
			if (dataProvider     == null) throw new ArgumentNullException("dataProvider");
			if (connectionString == null) throw new ArgumentNullException("connectionString");

			DataProvider     = dataProvider;
			ConnectionString = connectionString;
		}

		public DataConnection([NotNull] IDataProvider dataProvider, [NotNull] IDbConnection connection)
		{
			if (dataProvider == null) throw new ArgumentNullException("dataProvider");
			if (connection   == null) throw new ArgumentNullException("connection");

			DataProvider = dataProvider;
			_connection  = connection;
		}

		public DataConnection([NotNull] IDataProvider dataProvider, [NotNull] IDbTransaction transaction)
		{
			if (dataProvider == null) throw new ArgumentNullException("dataProvider");
			if (transaction  == null) throw new ArgumentNullException("transaction");

			DataProvider      = dataProvider;
			_connection       = transaction.Connection;
			Transaction       = transaction;
			_closeTransaction = false;
		}

		#endregion

		#region Public Properties

		public string        ConfigurationString { get; private set; }
		public IDataProvider DataProvider        { get; private set; }
		public string        ConnectionString    { get; private set; }

		public static Action<string,string> WriteTraceLine = (message, displayName) => Debug.WriteLine(message, displayName);

		public static string DefaultConfiguration { get; set; }
		public static string DefaultDataProvider  { get; set; }

		#endregion

		#region Configuration

		static IDataProvider FindProvider(
			string configuration,
			IEnumerable<KeyValuePair<string,IDataProvider>> ps,
			IDataProvider defp)
		{
			return
				ps.Where(dp => configuration.StartsWith(dp.Key)).       Select(dp => dp.Value).FirstOrDefault() ??
				ps.Where(dp => configuration.StartsWith(dp.Value.Name)).Select(dp => dp.Value).FirstOrDefault() ??
				defp;
		}

		static DataConnection()
		{
			AddDataProvider(                                  new SqlServerDataProvider(SqlServerVersion.v2008));
			AddDataProvider(ProviderName.SqlServer + ".2008", new SqlServerDataProvider(SqlServerVersion.v2008));
			AddDataProvider(ProviderName.SqlServer + ".2005", new SqlServerDataProvider(SqlServerVersion.v2005));
			AddDataProvider(                                  new AccessDataProvider());

			var section = LinqToDBSection.Instance;

			if (section != null)
			{
				DefaultConfiguration = section.DefaultConfiguration;
				DefaultDataProvider  = section.DefaultDataProvider;

				foreach (DataProviderElement provider in section.DataProviders)
				{
					var dataProviderType = Type.GetType(provider.TypeName, true);
					var providerInstance = (IDataProvider)Activator.CreateInstance(dataProviderType);
					var providerName     = string.IsNullOrEmpty(provider.Name) ? providerInstance.Name : provider.Name;

					providerInstance.Configure(provider.Attributes);

					AddDataProvider(providerName, providerInstance);
				}
			}

			if (string.IsNullOrEmpty(DefaultDataProvider))
				DefaultDataProvider = ProviderName.SqlServer;

			foreach (ConnectionStringSettings css in ConfigurationManager.ConnectionStrings)
			{
				var configuration    = css.Name;
				var connectionString = css.ConnectionString;
				var providerName     = css.ProviderName;

				IDataProvider dataProvider;

				if (string.IsNullOrEmpty(providerName))
					dataProvider = FindProvider(configuration, _dataProviders, _dataProviders[DefaultDataProvider]);
				else if (_dataProviders.ContainsKey(providerName))
					dataProvider = _dataProviders[providerName];
				else
				{
					var providers = _dataProviders.Where(dp => dp.Value.ProviderName == providerName).ToList();

					switch (providers.Count)
					{
						case 0  : dataProvider = _dataProviders[DefaultDataProvider];                        break;
						case 1  : dataProvider = providers[0].Value;                                         break;
						default : dataProvider = FindProvider(configuration, providers, providers[0].Value); break;
					}
				}

				AddConfiguration(configuration, connectionString, dataProvider);
					
				if (DefaultConfiguration == null &&
					css.ElementInformation.Source != null &&
					!css.ElementInformation.Source.EndsWith("machine.config", StringComparison.OrdinalIgnoreCase))
				{
					DefaultConfiguration = css.Name;
				}
			}
		}

		static readonly Dictionary<string,IDataProvider> _dataProviders = new Dictionary<string,IDataProvider>(4);

		public static void AddDataProvider([NotNull] string providerName, [NotNull] IDataProvider dataProvider)
		{
			if (providerName == null) throw new ArgumentNullException("providerName");
			if (dataProvider == null) throw new ArgumentNullException("dataProvider");

			if (string.IsNullOrEmpty(dataProvider.Name))
				throw new ArgumentException("dataProvider.Name cant be empty.", "dataProvider");

			_dataProviders[providerName] = dataProvider;
		}

		public static void AddDataProvider([NotNull] IDataProvider dataProvider)
		{
			if (dataProvider == null) throw new ArgumentNullException("dataProvider");

			AddDataProvider(dataProvider.Name, dataProvider);
		}

		class ConfigurationInfo
		{
			public ConfigurationInfo(string connectionString, IDataProvider dataProvider)
			{
				ConnectionString = connectionString;
				DataProvider     = dataProvider;
			}

			public string        ConnectionString;
			public IDataProvider DataProvider;
		}

		static readonly Dictionary<string,ConfigurationInfo> _configurations = new Dictionary<string,ConfigurationInfo>(4);

		public static void AddConfiguration([NotNull] string configuration, [NotNull] string connectionString, IDataProvider dataProvider = null)
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
		IDbCommand    _command;

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

		#region Transaction

		public IDbTransaction Transaction { get; private set; }
		
		public virtual void BeginTransaction()
		{
			BeginTransaction(Connection.BeginTransaction);
		}

		public virtual void BeginTransaction(IsolationLevel isolationLevel)
		{
			BeginTransaction(() => Connection.BeginTransaction(isolationLevel));
		}

		void BeginTransaction(Func<IDbTransaction> func)
		{
			// If transaction is open, we dispose it, it will rollback all changes.
			//
			if (Transaction != null)
				Transaction.Dispose();

			// Create new transaction object.
			//
			Transaction = func();

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

		#region ICloneable Members

		DataConnection(string configurationString, IDataProvider dataProvider, string connectionString, IDbConnection connection)
		{
			ConfigurationString = configurationString;
			DataProvider        = dataProvider;
			ConnectionString    = connectionString;
			_connection         = connection;
		}

		public object Clone()
		{
			var connection =
				_connection == null       ? null :
				_connection is ICloneable ? (IDbConnection)((ICloneable)_connection).Clone() :
				                            DataProvider.CreateConnection(ConnectionString);

			return new DataConnection(ConfigurationString, DataProvider, ConnectionString, connection);
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
