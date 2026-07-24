using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
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
	/// NHibernate <see cref="ISession"/> extensions to call LINQ To DB functionality.
	/// </summary>
	[PublicAPI]
	public static partial class LinqToDBForNHibernateTools
	{
		static Lazy<bool> _intialized = new Lazy<bool>(InitializeInternal);

		/// <summary>
		/// Initializes integration of LINQ To DB with NHibernate.
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

				var result = instantiator.MakeGenericMethod(queryable.ElementType)
					.InvokeExt<IQueryable>(null, new object[] { dc, newExpression });

				if (prev != null)
					result = prev(result);

				return result;
			};

			// Route core linq2db async operations over a native NHibernate query to NHibernate's own async.
			LinqExtensions.ExtensionsAdapter = new LinqToDBExtensionsAdapter();

			return true;
		}

		static ILinqToDBForNHibernateTools _implementation = null!;

		/// <summary>
		/// Gets or sets NHibernate to LINQ To DB integration bridge implementation.
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
		/// Creates or return existing metadata provider for provided NHibernate data model. If model is null, empty metadata
		/// provider will be returned.
		/// </summary>
		/// <returns>LINQ To DB metadata provider.</returns>
		public static IMetadataReader? GetMetadataReader(
			ISessionFactory? sessionFactory)
		{
			if (sessionFactory == null)
				return _defaultMetadataReader.Value;

			return _metadataReaders.GetOrAdd(sessionFactory, m => Implementation.CreateMetadataReader(m));
		}

		
		/// <summary>
		/// Returns NHibernate <see cref="ISessionFactory"/> for specific <see cref="ISession"/> instance.
		/// </summary>
		/// <param name="session">NHibernate <see cref="ISession"/> instance.</param>
		/// <returns><see cref="ISessionFactory"/> instance.</returns>
		public static ISessionFactory? GetSessionOptions(ISession session)
		{
			return Implementation.GetSessionOptions(session);
		}

		/// <summary>
		/// Returns NHibernate database provider information for specific <see cref="ISession"/> instance.
		/// </summary>
		/// <param name="session">NHibernate <see cref="ISession"/> instance.</param>
		/// <returns>NHibernate provider information.</returns>
		public static NHProviderInfo GetNHProviderInfo(ISession session)
		{
			var info = new NHProviderInfo
			{
				Connection = session.Connection,
				Session = session,
				Options = GetSessionOptions(session),
			};

			return info;
		}

		/// <summary>
		/// Returns NHibernate database provider information for specific <see cref="DbConnection"/> instance.
		/// </summary>
		/// <param name="connection">NHibernate <see cref="DbConnection"/> instance.</param>
		/// <returns>NHibernate provider information.</returns>
		public static NHProviderInfo GetNHProviderInfo(DbConnection connection)
		{
			var info = new NHProviderInfo
			{
				Connection = connection,
				Session = null,
				Options = null,
			};

			return info;
		}

		/// <summary>
		/// Returns LINQ To DB provider, based on provider data from NHibernate.
		/// </summary>
		/// <param name="info">NHibernate provider information.</param>
		/// <param name="connectionInfo">Database connection information.</param>
		/// <returns>LINQ TO DB provider instance.</returns>
		public static IDataProvider GetDataProvider(NHProviderInfo info, NHConnectionInfo connectionInfo)
		{
			var provider = Implementation.GetDataProvider(info, connectionInfo);

			if (provider == null)
				throw new LinqToDBForNHibernateToolsException("Can not detect data provider or provider not supported");

			return provider;
		}

		/// <summary>
		/// Creates mapping schema using provided NHibernate data model.
		/// </summary>
		/// <returns>Mapping schema for provided NHibernate model.</returns>
		public static MappingSchema GetMappingSchema(
			ISessionFactory? sessionFactory)
		{
			return Implementation.GetMappingSchema(sessionFactory, GetMetadataReader(sessionFactory));
		}

		/// <summary>
		/// Transforms NHibernate expression tree to LINQ To DB expression.
		/// </summary>
		/// <param name="expression">NHibernate expression tree.</param>
		/// <param name="dc">LINQ To DB <see cref="IDataContext"/> instance.</param>
		/// <returns>Transformed expression.</returns>
		public static Expression TransformExpression(Expression expression, IDataContext dc, ISession? session, ISessionFactory? sessionFactory)
		{
			return Implementation.TransformExpression(expression, dc, session, sessionFactory);
		}

		// Builds a linq2db connection attached to the given NHibernate ADO resources. trackingSession is the
		// ISession whose change-tracker should receive materialised entities, or null for a stateless session
		// (which has no first-level cache). With a session, the per-query AsReadOnly() marker opts individual
		// queries out of tracking. When a transaction is active it is wired in via UseTransaction so linq2db
		// commands share it (required by providers such as SQL Server that reject a command with no transaction
		// while one is pending); otherwise the bare connection is attached.
		static LinqToDBForNHibernateToolsDataConnection CreateAttachedConnection(
			DbConnection connection, DbTransaction? transaction, ISessionFactory? sessionFactory, ISession? trackingSession)
		{
			var info           = new NHProviderInfo { Connection = connection, Session = trackingSession, Options = sessionFactory };
			var connectionInfo = GetConnectionInfo(info);
			connectionInfo.Transaction = transaction; // so provider auto-detection probes within the active transaction
			var provider       = GetDataProvider(info, connectionInfo);

			var options = transaction != null
				? new DataOptions().UseTransaction(provider, transaction)
				: new DataOptions().UseConnection(provider, connection);

			var dc = new LinqToDBForNHibernateToolsDataConnection(trackingSession, WithTracing(options), TransformExpression);

			var mappingSchema = GetMappingSchema(sessionFactory);
			if (mappingSchema != null)
				dc.AddMappingSchema(mappingSchema);

			return dc;
		}

		// NHibernate does not expose its ADO transaction directly, so recover it by enlisting a throwaway command:
		// Enlist sets the command's Transaction to the active DbTransaction.
		static DbTransaction? GetActiveDbTransaction(DbConnection connection, ITransaction? currentTransaction)
		{
			if (currentTransaction is not { IsActive: true })
				return null;

			using var command = connection.CreateCommand();
			currentTransaction.Enlist(command);

			return command.Transaction;
		}

		/// <summary>
		/// Creates LINQ To DB <see cref="DataConnection"/> instance, attached to provided
		/// NHibernate <see cref="ISession"/> instance connection and transaction.
		/// </summary>
		/// <param name="session">NHibernate <see cref="ISession"/> instance.</param>
		/// <param name="transaction">Optional transaction instance, to which created connection should be attached.
		/// If not specified, will use current <see cref="ISession"/> transaction if it available.</param>
		/// <returns>LINQ To DB <see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateLinqToDbConnection(this ISession session,
			ITransaction? transaction = null)
		{
			ArgumentNullException.ThrowIfNull(session);

			var connection = session.Connection;
			return CreateAttachedConnection(connection, GetActiveDbTransaction(connection, session.GetCurrentTransaction()), session.SessionFactory, session);
		}

		/// <summary>
		/// Creates a LINQ To DB <see cref="DataConnection"/> attached to an NHibernate <see cref="IStatelessSession"/>.
		/// A stateless session has no first-level cache and no change tracking, so entities materialised by linq2db
		/// are left detached (untracked).
		/// </summary>
		/// <param name="session">NHibernate <see cref="IStatelessSession"/> instance.</param>
		/// <returns>LINQ To DB <see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateLinqToDbConnection(this IStatelessSession session)
		{
			ArgumentNullException.ThrowIfNull(session);

			var connection = session.Connection;
			return CreateAttachedConnection(connection, GetActiveDbTransaction(connection, session.GetCurrentTransaction()), session.GetSessionImplementation().Factory, trackingSession: null);
		}

		static readonly INHibernateLogger _traceLogger = NHibernateLogger.For("LinqToDB.NHibernate");

		/// <summary>
		/// Adds tracing that routes linq2db's connection trace to NHibernate's logger (category
		/// <c>LinqToDB.NHibernate</c>), at the level NHibernate is configured to log, so linq2db SQL appears
		/// alongside NHibernate's own log output. A no-op when that category is not enabled.
		/// </summary>
		static DataOptions WithTracing(DataOptions options)
		{
			var level =
				_traceLogger.IsEnabled(NHibernateLogLevel.Debug) ? TraceLevel.Verbose :
				_traceLogger.IsEnabled(NHibernateLogLevel.Info)  ? TraceLevel.Info    :
				_traceLogger.IsEnabled(NHibernateLogLevel.Warn)  ? TraceLevel.Warning :
				_traceLogger.IsEnabled(NHibernateLogLevel.Error) ? TraceLevel.Error   :
				TraceLevel.Off;

			return level == TraceLevel.Off ? options : options.UseTracing(level, LogConnectionTrace);
		}

		static void LogConnectionTrace(TraceInfo info)
		{
			var level = info.TraceLevel switch
			{
				TraceLevel.Error   => NHibernateLogLevel.Error,
				TraceLevel.Warning => NHibernateLogLevel.Warn,
				TraceLevel.Info    => NHibernateLogLevel.Info,
				_                  => NHibernateLogLevel.Debug,
			};

			if (!_traceLogger.IsEnabled(level))
				return;

			switch (info.TraceInfoStep)
			{
				case TraceInfoStep.BeforeExecute:
					_traceLogger.Log(level, new NHibernateLogValues("{0}", new object[] { info.SqlText ?? string.Empty }), null);
					break;

				case TraceInfoStep.AfterExecute:
					_traceLogger.Log(level, new NHibernateLogValues("Execution time: {0}, records affected: {1}.",
						new object[] { Format(info.ExecutionTime), Format(info.RecordsAffected) }), null);
					break;

				case TraceInfoStep.Error:
					_traceLogger.Log(level, new NHibernateLogValues("Failed executing command.", Array.Empty<object>()), info.Exception);
					break;

				case TraceInfoStep.Completed:
					_traceLogger.Log(level, new NHibernateLogValues("Total execution time: {0}.",
						new object[] { Format(info.ExecutionTime) }), null);
					break;
			}

			static string Format(IFormattable? value) => value?.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;
		}

		/// <summary>
		/// Creates linq2db data session for NHibernate database session.
		/// </summary>
		/// <param name="session">NHibernate database session.</param>
		/// <param name="transaction">Transaction instance.</param>
		/// <returns>linq2db data session.</returns>
		public static IDataContext CreateLinqToDbContext(this ISession session,
			ITransaction? transaction = null)
		{
			ArgumentNullException.ThrowIfNull(session);

			var connection = session.Connection;
			return CreateAttachedConnection(connection, GetActiveDbTransaction(connection, session.GetCurrentTransaction()), session.SessionFactory, session);
		}

		/// <summary>
		/// Creates a linq2db data context for an NHibernate <see cref="IStatelessSession"/>. A stateless session
		/// has no first-level cache and no change tracking, so entities materialised by linq2db are left detached.
		/// </summary>
		/// <param name="session">NHibernate stateless database session.</param>
		/// <returns>linq2db data context.</returns>
		public static IDataContext CreateLinqToDbContext(this IStatelessSession session)
		{
			ArgumentNullException.ThrowIfNull(session);

			var connection = session.Connection;
			return CreateAttachedConnection(connection, GetActiveDbTransaction(connection, session.GetCurrentTransaction()), session.GetSessionImplementation().Factory, trackingSession: null);
		}

		internal static readonly MethodInfo AsReadOnlyMethodInfo =
			MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.AsReadOnly());

		/// <summary>
		/// Marks a query as read-only — the NHibernate analogue of Entity Framework Core's <c>AsNoTracking()</c>.
		/// Entities the marked query materialises are left detached instead of being attached to the session's
		/// change tracker. The marker is detected and removed when the query is translated to linq2db, so apply it
		/// to a query from <see cref="GetTable{T}(ISession)"/> (or another linq2db context), or before
		/// <c>ToLinqToDB()</c> on a native NHibernate query. It has no effect on a query executed by NHibernate itself.
		/// </summary>
		/// <typeparam name="T">Query element type.</typeparam>
		/// <param name="source">Query to mark as read-only.</param>
		/// <returns>The query, carrying the read-only marker.</returns>
		public static IQueryable<T> AsReadOnly<T>(this IQueryable<T> source)
		{
			ArgumentNullException.ThrowIfNull(source);

			return source.Provider.CreateQuery<T>(
				Expression.Call(null, AsReadOnlyMethodInfo.MakeGenericMethod(typeof(T)), source.Expression));
		}

		/// <summary>
		/// Creates LINQ To DB <see cref="DataConnection"/> instance that creates new database connection using connection
		/// information from NHibernate <see cref="ISession"/> instance.
		/// </summary>
		/// <param name="session">NHibernate <see cref="ISession"/> instance.</param>
		/// <returns>LINQ To DB <see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateLinq2DbConnectionDetached(this ISession session)
		{
			ArgumentNullException.ThrowIfNull(session);

			var info           = GetNHProviderInfo(session);
			var connectionInfo = GetConnectionInfo(info);
			var dataProvider   = GetDataProvider(info, connectionInfo);

			var dc = new LinqToDBForNHibernateToolsDataConnection(session, WithTracing(new DataOptions().UseConnectionString(dataProvider, connectionInfo.ConnectionString!)), TransformExpression);

			var mappingSchema = GetMappingSchema(session.SessionFactory);
			if (mappingSchema != null)
				dc.AddMappingSchema(mappingSchema);

			return dc;
		}

		/// <summary>
		/// Extracts database connection information from NHibernate provider data.
		/// </summary>
		/// <param name="info">NHibernate database provider data.</param>
		/// <returns>Database connection information.</returns>
		public static NHConnectionInfo GetConnectionInfo(NHProviderInfo info)
		{
			return new NHConnectionInfo
			{
				Connection       = info.Connection,
				ConnectionString = (info.Connection as DbConnection)?.ConnectionString,
			};
		}

		/// <summary>
		/// Converts NHibernate's query to LINQ To DB query and attach it to provided LINQ To DB <see cref="IDataContext"/>.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="query">NHibernate query.</param>
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

			// Return the linq2db ExpressionQuery directly (not the wrapper) so that linq2db's AsyncExtensions
			// recognises it and executes truly async, instead of falling through to the ExtensionsAdapter.
			return Internals.CreateExpressionQueryInstance<T>(dc, expression);
		}

		/// <summary>
		/// Converts NHibernate's query to LINQ To DB query and attach it to current NHibernate connection.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="query">NHibernate query.</param>
		/// <returns>LINQ To DB query, attached to current NHibernate connection.</returns>
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

			// Return the linq2db ExpressionQuery directly (not the wrapper) so that linq2db's AsyncExtensions
			// recognises it and executes truly async, instead of falling through to the ExtensionsAdapter.
			return Internals.CreateExpressionQueryInstance<T>(dc, expression);
		}

		/// <summary>
		/// Extracts <see cref="ISession"/> instance from <see cref="IQueryable"/> object.
		/// </summary>
		/// <param name="query">NHibernate query.</param>
		/// <returns>Current <see cref="ISession"/> instance.</returns>
		public static ISession? GetCurrentContext(IQueryable query)
		{
			return Implementation.GetCurrentContext(query);
		}

		/// <summary>
		/// Enables attaching entities materialised by a linq2db query to the NHibernate session's change tracker,
		/// so that subsequent modifications are persisted when the session is flushed.
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
