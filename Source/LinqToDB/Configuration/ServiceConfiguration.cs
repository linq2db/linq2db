using System;
using System.Data;
using System.Reflection;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Extensions;
using LinqToDB.Mapping;

#if NETSTANDARD2_0
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
#endif

namespace LinqToDB.Configuration
{
#if NETSTANDARD2_0
	public static class ServiceConfiguration
	{
		public static IServiceCollection AddLinqToDb(
			this IServiceCollection serviceCollection,
			Action<IServiceProvider, LinqToDbConnectionOptions> configure,
			ServiceLifetime lifetime = ServiceLifetime.Scoped)
		{
			return AddLinqToDbContext<DataConnection>(serviceCollection, configure, lifetime);
		}
		public static IServiceCollection AddLinqToDbContext<TContext>(
			this IServiceCollection serviceCollection,
			Action<IServiceProvider, LinqToDbConnectionOptions> configure,
			ServiceLifetime lifetime = ServiceLifetime.Scoped) where TContext : IDataContext
		{
			return AddLinqToDbContext<TContext, TContext>(serviceCollection, configure, lifetime);
		}

		public static IServiceCollection AddLinqToDbContext<TContext, TContextImplementation>(
			this IServiceCollection serviceCollection,
			Action<IServiceProvider, LinqToDbConnectionOptions> configure,
			ServiceLifetime lifetime = ServiceLifetime.Scoped) where TContextImplementation : IDataContext
		{
			CheckContextConstructor<TContextImplementation>();
			serviceCollection.TryAdd(new ServiceDescriptor(typeof(TContext), typeof(TContextImplementation), lifetime));
			serviceCollection.TryAdd(new ServiceDescriptor(typeof(LinqToDbConnectionOptions<TContextImplementation>),
				provider =>
				{
					var options = new LinqToDbConnectionOptions<TContextImplementation>();
					configure(provider, options);
					return options;
				},
				lifetime));
			serviceCollection.TryAdd(new ServiceDescriptor(typeof(LinqToDbConnectionOptions),
				provider => provider.GetService(typeof(LinqToDbConnectionOptions<TContextImplementation>)), lifetime));
			return serviceCollection;
		}

		private static void CheckContextConstructor<TContext>()
		{
			var constructorInfo = 
				typeof(TContext).GetConstructorEx(new[] {typeof(LinqToDbConnectionOptions<TContext>)}) ??
				typeof(TContext).GetConstructorEx(new[] {typeof(LinqToDbConnectionOptions)});
			if (constructorInfo == null)
			{
				throw new ArgumentException("Missing constructor accepting 'LinqToDbContextOptions' on type "
				                            + typeof(TContext).Name);
			}
		}
	}
#endif
	public class LinqToDbConnectionOptions
	{
		internal MappingSchema _mappingSchema;
		internal IDataProvider _dataProvider;
		internal IDbConnection _dbConnection;
		internal bool _disposeConnection;
		internal string _configurationString;
		internal string _providerName;
		internal string _connectionString;
		internal Func<IDbConnection> _connectionFactory;
		internal IDbTransaction _dbTransaction;

		private void CheckAssignSetupType(ConnectionSetupType type)
		{
			if (SetupType != ConnectionSetupType.DefaultConfiguration)
				throw new LinqToDBException(
					$"LinqToDbConnectionOptions already setup using {SetupType}, use Reset first to overwrite");
			SetupType = type;
		}

		internal ConnectionSetupType SetupType { get; set; }

		internal enum ConnectionSetupType
		{
			DefaultConfiguration,
			ConnectionString,
			ConfigurationString,
			Connection,
			ConnectionFactory,
			Transaction
		}

		public LinqToDbConnectionOptions UseSqlServer([JetBrains.Annotations.NotNull] string connectionString)
		{
			return UseConnectionString(ProviderName.SqlServer, connectionString);
		}
		public LinqToDbConnectionOptions UseOracle([JetBrains.Annotations.NotNull] string connectionString)
		{
			return UseConnectionString(ProviderName.Oracle, connectionString);
		}
		public LinqToDbConnectionOptions UsePostgreSQL([JetBrains.Annotations.NotNull] string connectionString)
		{
			return UseConnectionString(ProviderName.PostgreSQL, connectionString);
		}
		public LinqToDbConnectionOptions UseMySql([JetBrains.Annotations.NotNull] string connectionString)
		{
			return UseConnectionString(ProviderName.MySql, connectionString);
		}
		public LinqToDbConnectionOptions UseSQLite([JetBrains.Annotations.NotNull] string connectionString)
		{
			return UseConnectionString(ProviderName.SQLite, connectionString);
		}

		/// <summary>
		/// Configure the database to use this provider and connection string
		/// </summary>
		/// <param name="providerName">See <see cref="ProviderName"/> for Default providers</param>
		/// <param name="connectionString">Database specific connections string</param>
		/// <returns></returns>
		public LinqToDbConnectionOptions UseConnectionString(
			[JetBrains.Annotations.NotNull] string providerName,
			[JetBrains.Annotations.NotNull] string connectionString)
		{
			CheckAssignSetupType(ConnectionSetupType.ConnectionString);
			_connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
			_providerName = providerName ?? throw new ArgumentNullException(nameof(providerName));
			return this;
		}

		public LinqToDbConnectionOptions UseConnectionString(
			[JetBrains.Annotations.NotNull] IDataProvider dataProvider,
			[JetBrains.Annotations.NotNull] string connectionString)
		{
			CheckAssignSetupType(ConnectionSetupType.ConnectionString);
			_connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
			_dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
			return this;
		}

		public LinqToDbConnectionOptions UseConfigurationString(
			[JetBrains.Annotations.NotNull] string configurationString)
		{
			CheckAssignSetupType(ConnectionSetupType.ConfigurationString);
			_configurationString = configurationString ?? throw new ArgumentNullException(nameof(configurationString));
			return this;
		}

		public LinqToDbConnectionOptions UseConnectionFactory(
			[JetBrains.Annotations.NotNull] IDataProvider dataProvider,
			[JetBrains.Annotations.NotNull] Func<IDbConnection> connectionFactory)
		{
			CheckAssignSetupType(ConnectionSetupType.ConnectionFactory);
			_dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
			_connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
			return this;
		}

		public LinqToDbConnectionOptions UseConnection([JetBrains.Annotations.NotNull] IDataProvider dataProvider,
			[JetBrains.Annotations.NotNull] IDbConnection connection,
			bool disposeConnection = false)
		{
			CheckAssignSetupType(ConnectionSetupType.Connection);
			_dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
			_dbConnection = connection ?? throw new ArgumentNullException(nameof(connection));

			if (!Common.Configuration.AvoidSpecificDataProviderAPI
			    && !_dataProvider.IsCompatibleConnection(_dbConnection))
				throw new LinqToDBException(
					$"DataProvider '{_dataProvider}' and connection '{_dbConnection}' are not compatible.");

			_disposeConnection = disposeConnection;
			return this;
		}

		public LinqToDbConnectionOptions UseTransaction([JetBrains.Annotations.NotNull] IDataProvider dataProvider,
			[JetBrains.Annotations.NotNull] IDbTransaction transaction)
		{
			CheckAssignSetupType(ConnectionSetupType.Transaction);
			_dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
			_dbTransaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
			return this;
		}

		public LinqToDbConnectionOptions UseMappingSchema([JetBrains.Annotations.NotNull] MappingSchema mappingSchema)
		{
			_mappingSchema = mappingSchema ?? throw new ArgumentNullException(nameof(mappingSchema));
			return this;
		}

		public LinqToDbConnectionOptions UseDataProvider([JetBrains.Annotations.NotNull] IDataProvider dataProvider)
		{
			_dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
			return this;
		}

		public LinqToDbConnectionOptions Reset()
		{
			_mappingSchema = null;
			_dataProvider = null;
			_configurationString = null;
			_connectionString = null;
			_dbConnection = null;
			_providerName = null;
			_dbTransaction = null;
			_connectionFactory = null;
			SetupType = ConnectionSetupType.DefaultConfiguration;
			return this;
		}

		public virtual bool IsValidConfigForConnectionType(DataConnection connection)
		{
			return true;
		}
	}

	//this type is used to discriminate between options for 2 different Contexts
	public class LinqToDbConnectionOptions<T> : LinqToDbConnectionOptions
	{
		public override bool IsValidConfigForConnectionType(DataConnection connection)
		{
			return connection is T;
		}
	}
}
