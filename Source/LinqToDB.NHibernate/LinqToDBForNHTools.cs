using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Expressions;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using LinqToDB.Metadata;
using LinqToDB.NHibernate.Internal;
using JetBrains.Annotations;
using LinqToDB.Internal.Async;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Linq;
using NHibernate;

namespace LinqToDB.NHibernate
{
	/// <summary>
	/// EF.Core <see cref="DbContext"/> extensions to call LINQ To DB functionality.
	/// </summary>
	[PublicAPI]
	public static partial class LinqToDBForNHibernateTools
	{
		static Lazy<bool> _intialized = new Lazy<bool>(InitializeInternal);

		/// <summary>
		/// Initializes integration of LINQ To DB with EF.Core.
		/// </summary>
		public static void Initialize()
		{
			var _ = _intialized.Value;
		}

		static bool InitializeInternal()
		{
			var prev = LinqExtensions.ProcessSourceQueryable;

			InitializeMapping();

			var instantiator = MemberHelper.MethodOf(() => Internals.CreateExpressionQueryInstance<int>(null!, null!))
				.GetGenericMethodDefinition();

			LinqExtensions.ProcessSourceQueryable = queryable =>
			{
				// our Provider - nothing to do
				if (queryable.Provider is IQueryProviderAsync)
					return queryable;

				var session = Implementation.GetCurrentContext(queryable);
				if (session == null)
					throw new LinqToDBForNHibernateToolsException("Can not evaluate current session from query");

				var dc = CreateLinqToDbContext(session);
				// #5364: this implicit context is never disposed — release the connection per command
				// (NHibernate owns the session connection) instead of holding it open until dispose.
				dc.CloseAfterUse = true;
				var newExpression = queryable.Expression;

				var result = (IQueryable)instantiator.MakeGenericMethod(queryable.ElementType)
					.Invoke(null, new object[] { dc, newExpression })!;

				if (prev != null)
					result = prev(result);

				return result;
			};

			// Phase 5: async ExtensionsAdapter deferred until the async surfaces are re-enabled.
			// LinqExtensions.ExtensionsAdapter = new LinqToDBExtensionsAdapter();

			return true;
		}

		static ILinqToDBForNHibernateTools _implementation = null!;

		/// <summary>
		/// Gets or sets EF.Core to LINQ To DB integration bridge implementation.
		/// </summary>
		public static ILinqToDBForNHibernateTools Implementation
		{
			get => _implementation;
			set
			{
				_implementation = value ?? throw new ArgumentNullException(nameof(value));
				_metadataReaders.Clear();
				_defaultMetadataReader = new Lazy<IMetadataReader?>(() => Implementation.CreateMetadataReader(null));
			}
		}

		static readonly ConcurrentDictionary<ISessionFactory, IMetadataReader?> _metadataReaders = new ConcurrentDictionary<ISessionFactory, IMetadataReader?>();

		static Lazy<IMetadataReader?> _defaultMetadataReader = null!;

		/// <summary>
		/// Clears internal caches
		/// </summary>
		public static void ClearCaches()
		{
			_metadataReaders.Clear();
			Implementation.ClearCaches();
			Query.ClearCaches();
		}

		static LinqToDBForNHibernateTools()
		{
			Implementation = new LinqToDBForNHibernateToolsImplDefault();
			Initialize();
		}

		/// <summary>
		/// Creates or return existing metadata provider for provided EF.Core data model. If model is null, empty metadata
		/// provider will be returned.
		/// </summary>
		/// <param name="model">EF.Core data model instance. Could be <c>null</c>.</param>
		/// <param name="dependencies"></param>
		/// <param name="mappingSource"></param>
		/// <param name="logger"></param>
		/// <returns>LINQ To DB metadata provider.</returns>
		public static IMetadataReader? GetMetadataReader(
			ISessionFactory? sessionFactory)
		{
			if (sessionFactory == null)
				return _defaultMetadataReader.Value;

			return _metadataReaders.GetOrAdd(sessionFactory, m => Implementation.CreateMetadataReader(sessionFactory));
		}

		
		/// <summary>
		/// Returns EF.Core <see cref="DbContextOptions"/> for specific <see cref="DbContext"/> instance.
		/// </summary>
		/// <param name="session">EF.Core <see cref="DbContext"/> instance.</param>
		/// <returns><see cref="DbContextOptions"/> instance.</returns>
		public static ISessionFactory? GetSessionOptions(ISession session)
		{
			return Implementation.GetSessionOptions(session);
		}

		/// <summary>
		/// Returns EF.Core database provider information for specific <see cref="DbContext"/> instance.
		/// </summary>
		/// <param name="session">EF.Core <see cref="DbContext"/> instance.</param>
		/// <returns>EF.Core provider information.</returns>
		public static NHProviderInfo GetNHProviderInfo(ISession session)
		{
			var info = new NHProviderInfo
			{
				Connection = session.Connection,
				Session = session,
				Options = GetSessionOptions(session)
			};

			return info;
		}

		/// <summary>
		/// Returns EF.Core database provider information for specific <see cref="DbConnection"/> instance.
		/// </summary>
		/// <param name="connection">EF.Core <see cref="DbConnection"/> instance.</param>
		/// <returns>EF.Core provider information.</returns>
		public static NHProviderInfo GetNHProviderInfo(DbConnection connection)
		{
			var info = new NHProviderInfo
			{
				Connection = connection,
				Session = null,
				Options = null
			};

			return info;
		}

		/*/// <summary>
		/// Returns EF.Core database provider information for specific <see cref="DbContextOptions"/> instance.
		/// </summary>
		/// <param name="options">EF.Core <see cref="DbContextOptions"/> instance.</param>
		/// <returns>EF.Core provider information.</returns>
		public static NHProviderInfo GetNHProviderInfo(DbContextOptions options)
		{
			var info = new NHProviderInfo
			{
				Connection = null,
				Session = null,
				Options = options
			};

			return info;
		}*/

		/// <summary>
		/// Returns LINQ To DB provider, based on provider data from EF.Core.
		/// </summary>
		/// <param name="info">EF.Core provider information.</param>
		/// <param name="connectionInfo">Database connection information.</param>
		/// <returns>LINQ TO DB provider instance.</returns>
		public static IDataProvider GetDataProvider(NHProviderInfo info, NHConnectionInfo connectionInfo)
		{
			var provider = Implementation.GetDataProvider(info, connectionInfo);

			if (provider == null)
				throw new LinqToDBForNHibernateToolsException("Can not detect provider from Entity Framework or provider not supported");

			return provider;
		}

		/// <summary>
		/// Creates mapping schema using provided EF.Core data model.
		/// </summary>
		/// <param name="model">EF.Core data model.</param>
		/// <param name="convertorSelector">EF Core registry for type conversion.</param>
		/// <param name="dependencies"></param>
		/// <param name="mappingSource"></param>
		/// <param name="logger"></param>
		/// <returns>Mapping schema for provided EF.Core model.</returns>
		public static MappingSchema GetMappingSchema(
			ISessionFactory? sessionFactory)
		{
			return Implementation.GetMappingSchema(sessionFactory, GetMetadataReader(sessionFactory));
		}

		/// <summary>
		/// Transforms EF.Core expression tree to LINQ To DB expression.
		/// </summary>
		/// <param name="expression">EF.Core expression tree.</param>
		/// <param name="dc">LINQ To DB <see cref="IDataContext"/> instance.</param>
		/// <param name="ctx">Optional DbContext instance.</param>
		/// <param name="model">EF.Core data model instance.</param>
		/// <returns>Transformed expression.</returns>
		public static Expression TransformExpression(Expression expression, IDataContext dc, ISession? session, ISessionFactory? sessionFactory)
		{
			return Implementation.TransformExpression(expression, dc, session, sessionFactory);
		}

		/// <summary>
		/// Creates LINQ To DB <see cref="DataConnection"/> instance, attached to provided
		/// EF.Core <see cref="DbContext"/> instance connection and transaction.
		/// </summary>
		/// <param name="session">EF.Core <see cref="DbContext"/> instance.</param>
		/// <param name="transaction">Optional transaction instance, to which created connection should be attached.
		/// If not specified, will use current <see cref="DbContext"/> transaction if it available.</param>
		/// <returns>LINQ To DB <see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateLinqToDbConnection(this ISession session,
			ITransaction? transaction = null)
		{
			if (session == null) throw new ArgumentNullException(nameof(session));

			var info = GetNHProviderInfo(session);

			DataConnection? dc = null;

			transaction ??= session.GetCurrentTransaction();

			var connectionInfo = GetConnectionInfo(info);
			var provider = GetDataProvider(info, connectionInfo);

			if (transaction != null && transaction.IsActive)
			{
				throw new NotImplementedException();
				// (transaction-attach path not yet implemented)
				// TODO: we need API for testing current connection
				/*
				//if (provider.IsCompatibleConnection(dbTrasaction.Connection))
					dc = new LinqToDBForNHibernateToolsDataConnection(session, provider, dbTrasaction, TransformExpression);
			*/
			}

			if (dc == null)
			{
				var dbConnection = session.Connection;
				// TODO: we need API for testing current connection
				if (true /*provider.IsCompatibleConnection(dbConnection)*/)
					dc = new LinqToDBForNHibernateToolsDataConnection(session, new DataOptions().UseConnection(provider, dbConnection), TransformExpression);
				else
				{
					//dc = new LinqToDBForNHibernateToolsDataConnection(session, provider, connectionInfo.ConnectionString, session.Model, TransformExpression);
				}
			}

			/*
			var logger = CreateLogger(info.Options);
			if (logger != null)
			{
				EnableTracing(dc, logger);
			}

			var dependencies  = session.GetService<RelationalSqlTranslatingExpressionVisitorDependencies>();
			var mappingSource = session.GetService<IRelationalTypeMappingSource>();
			var converters    = session.GetService<IValueConverterSelector>();
			var dLogger       = session.GetService<IDiagnosticsLogger<DbLoggerCategory.Query>>();
			*/

			var mappingSchema = GetMappingSchema(session.SessionFactory);
			if (mappingSchema != null)
				dc.AddMappingSchema(mappingSchema);

			return dc;
		}

		private static TraceSwitch _defaultTraceSwitch =
			new TraceSwitch("DataConnection", "DataConnection trace switch", TraceLevel.Info.ToString());

		/*
		static void EnableTracing(DataConnection dc, ILogger logger)
		{
			dc.OnTraceConnection = t => Implementation.LogConnectionTrace(t, logger);
			dc.TraceSwitchConnection = _defaultTraceSwitch;
		}
		*/

		/*/// <summary>
		/// Creates logger intance.
		/// </summary>
		/// <param name="options"><see cref="DbContext" /> options.</param>
		/// <returns>Logger instance.</returns>
		public static ILogger? CreateLogger(IDbContextOptions? options)
		{
			return Implementation.CreateLogger(options);
		}*/

		/// <summary>
		/// Creates linq2db data session for EF.Core database session.
		/// </summary>
		/// <param name="session">EF.Core database session.</param>
		/// <param name="transaction">Transaction instance.</param>
		/// <returns>linq2db data session.</returns>
		public static IDataContext CreateLinqToDbContext(this ISession session,
			ITransaction? transaction = null)
		{
			if (session == null) throw new ArgumentNullException(nameof(session));

			var info = GetNHProviderInfo(session);

			DataConnection? dc = null;

			//transaction = transaction ?? session.Database.CurrentTransaction;

			var connectionInfo = GetConnectionInfo(info);
			var provider       = GetDataProvider(info, connectionInfo);
			var mappingSchema  = GetMappingSchema(session.SessionFactory);
			//var logger         = CreateLogger(info.Options);

			if (transaction != null)
			{
				throw new NotImplementedException();
				//var dbTransaction = transaction.GetDbTransaction();

				//dc = new LinqToDBForNHibernateToolsDataConnection(session, provider, dbTransaction, session.Model, TransformExpression);
			}

			if (dc == null)
			{
				var dbConnection = session.Connection;
				// TODO: we need API for testing current connection
				if (true /*provider.IsCompatibleConnection(dbConnection)*/)
					dc = new LinqToDBForNHibernateToolsDataConnection(session, new DataOptions().UseConnection(provider, dbConnection), TransformExpression);
				else
				{
					/*
					// special case when we have to create data connection by itself
					var dataContext = new LinqToDBForNHibernateToolsDataContext(session, provider, connectionInfo.ConnectionString, session.Model, TransformExpression);

					if (mappingSchema != null)
						dataContext.MappingSchema = mappingSchema;
					
					if (logger != null)
						dataContext.OnTraceConnection = t => Implementation.LogConnectionTrace(t, logger);
						
					return dataContext;
					*/
				}
			}

			if (mappingSchema != null)
				dc.AddMappingSchema(mappingSchema);

			/*
			if (logger != null)
			{
				EnableTracing(dc, logger);
			}*/

			return dc;
		}

		/// <summary>
		/// Creates LINQ To DB <see cref="DataConnection"/> instance that creates new database connection using connection
		/// information from EF.Core <see cref="DbContext"/> instance.
		/// </summary>
		/// <param name="session">EF.Core <see cref="DbContext"/> instance.</param>
		/// <returns>LINQ To DB <see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateLinq2DbConnectionDetached(this ISession session)
		{
			if (session == null) throw new ArgumentNullException(nameof(session));

			var info           = GetNHProviderInfo(session);
			var connectionInfo = GetConnectionInfo(info);
			var dataProvider   = GetDataProvider(info, connectionInfo);

			var dc = new LinqToDBForNHibernateToolsDataConnection(session, new DataOptions().UseConnectionString(dataProvider, connectionInfo.ConnectionString!), TransformExpression);
			/*
			var logger = CreateLogger(info.Options);

			if (logger != null)
			{
				EnableTracing(dc, logger);
			}
			*/

			var mappingSchema = GetMappingSchema(session.SessionFactory);
			if (mappingSchema != null)
				dc.AddMappingSchema(mappingSchema);

			return dc;
		}

		/// <summary>
		/// Extracts database connection information from EF.Core provider data.
		/// </summary>
		/// <param name="info">EF.Core database provider data.</param>
		/// <returns>Database connection information.</returns>
		public static NHConnectionInfo GetConnectionInfo(NHProviderInfo info)
		{
			var connection = info.Connection;
			string? connectionString = info.Connection?.ConnectionString;
			

			if (connection != null && connectionString != null)
				return new NHConnectionInfo { Connection = connection, ConnectionString = connectionString };

			var extracted = Implementation.ExtractConnectionInfo(info.Options);

			return new NHConnectionInfo
			{
				Connection = connection ?? extracted?.Connection,
				ConnectionString = extracted?.ConnectionString
			};
		}

		/*
		/// <summary>
		/// Creates new LINQ To DB <see cref="DataConnection"/> instance using connectivity information from
		/// EF.Core <see cref="DbContextOptions"/> instance.
		/// </summary>
		/// <param name="options">EF.Core <see cref="DbContextOptions"/> instance.</param>
		/// <returns>New LINQ To DB <see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateLinqToDbConnection(this DbContextOptions options)
		{
			var info = GetNHProviderInfo(options);

			DataConnection? dc = null;

			var connectionInfo = GetConnectionInfo(info);
			var dataProvider   = GetDataProvider(info, connectionInfo);
			var model          = GetModel(options);

			if (connectionInfo.Connection != null)
				dc = new LinqToDBForNHibernateToolsDataConnection(null, dataProvider, connectionInfo.Connection, model, TransformExpression);
			else if (connectionInfo.ConnectionString != null)
				dc = new LinqToDBForNHibernateToolsDataConnection(null, dataProvider, connectionInfo.ConnectionString, model, TransformExpression);

			if (dc == null)
				throw new LinqToDBForNHibernateToolsException($"Can not extract connection information from {nameof(DbContextOptions)}");

			var logger = CreateLogger(info.Options);
			if (logger != null)
			{
				EnableTracing(dc, logger);
			}

			if (model != null)
			{
				var mappingSchema = GetMappingSchema(model, null, null, null, null);
				if (mappingSchema != null)
					dc.AddMappingSchema(mappingSchema);
			}

			return dc;
		}
		*/

		/// <summary>
		/// Converts EF.Core's query to LINQ To DB query and attach it to provided LINQ To DB <see cref="IDataContext"/>.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="query">EF.Core query.</param>
		/// <param name="dc">LINQ To DB <see cref="IDataContext"/> to use with provided query.</param>
		/// <returns>LINQ To DB query, attached to provided <see cref="IDataContext"/>.</returns>
		public static IQueryable<T> ToLinqToDB<T>(this IQueryable<T> query, IDataContext dc)
		{
			var session = Implementation.GetCurrentContext(query);
			if (session == null)
				throw new LinqToDBForNHibernateToolsException("Can not evaluate current session from query");

			// Rewrite the NHibernate expression to a linq2db one up front (NhQueryable roots -> GetTable)
			// so the whole query runs on the provided context — no second implicit context is spun up.
			var expression = TransformExpression(query.Expression, dc, session, session.SessionFactory);

			return new LinqToDBForNHibernateQueryProvider<T>(dc, expression);
		}

		/// <summary>
		/// Converts EF.Core's query to LINQ To DB query and attach it to current EF.Core connection.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="query">EF.Core query.</param>
		/// <returns>LINQ To DB query, attached to current EF.Core connection.</returns>
		public static IQueryable<T> ToLinqToDB<T>(this IQueryable<T> query)
		{
			if (query.Provider is IQueryProviderAsync)
			{
				return query;
			}

			var session = Implementation.GetCurrentContext(query);
			if (session == null)
				throw new LinqToDBForNHibernateToolsException("Can not evaluate current session from query");

			var dc = CreateLinqToDbContext(session);
			// #5364: this implicit context is never disposed — release the connection per command.
			dc.CloseAfterUse = true;

			// Rewrite the NHibernate expression to a linq2db one up front (NhQueryable roots -> GetTable)
			// so the whole query runs on this single context — no second implicit context is spun up.
			var expression = TransformExpression(query.Expression, dc, session, session.SessionFactory);

			return new LinqToDBForNHibernateQueryProvider<T>(dc, expression);
		}

		/// <summary>
		/// Extracts <see cref="DbContext"/> instance from <see cref="IQueryable"/> object.
		/// </summary>
		/// <param name="query">EF.Core query.</param>
		/// <returns>Current <see cref="DbContext"/> instance.</returns>
		public static ISession? GetCurrentContext(IQueryable query)
		{
			return Implementation.GetCurrentContext(query);
		}

		/// <summary>
		/// Enables attaching entities to change tracker.
		/// Entities will be attached only if AsNoTracking() is not used in query and DbContext is configured to track entities. 
		/// </summary>
		public static bool EnableChangeTracker 
		{ 
			get => Implementation.EnableChangeTracker;
			set => Implementation.EnableChangeTracker = value;
		}

		public static void AddMappingSchema(ISessionFactory sessionFactory, MappingSchema mappingSchema)
			=> Implementation.AddMappingSchema(sessionFactory, mappingSchema);

	}
}
