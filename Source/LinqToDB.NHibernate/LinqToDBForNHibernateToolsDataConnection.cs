using System;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Interceptors;

using NHibernate;

namespace LinqToDB.NHibernate
{
	/// <summary>
	/// linq2db NHibernate data connection.
	/// </summary>
	public class LinqToDBForNHibernateToolsDataConnection : DataConnection, IEntityServiceInterceptor
	{
		readonly ISession?                                                                _session;
		readonly Func<Expression, IDataContext, ISession?, ISessionFactory?, Expression>? _transformFunc;

		sealed class ExpressionInterceptor : IQueryExpressionInterceptor
		{
			readonly LinqToDBForNHibernateToolsDataConnection _dataConnection;

			public ExpressionInterceptor(LinqToDBForNHibernateToolsDataConnection dataConnection)
			{
				_dataConnection = dataConnection;
			}

			public Expression ProcessExpression(Expression expression, QueryExpressionArgs args)
			{
				return _dataConnection.ProcessExpression(expression, args);
			}
		}

		/// <summary>
		/// Change tracker enable flag.
		/// </summary>
		public bool Tracking { get; set; }

		/// <summary>
		/// The NHibernate session this connection is attached to, if any. Used by the query-filter bridge to
		/// read the session's currently enabled NHibernate filters at query-build time.
		/// </summary>
		internal ISession? Session => _session;

		/// <summary>
		/// Creates new instance of data connection.
		/// </summary>
		/// <param name="session">NHibernate session.</param>
		/// <param name="options">linq2db data options carrying the provider and the connection or transaction.</param>
		/// <param name="transformFunc">Expression converter.</param>
		public LinqToDBForNHibernateToolsDataConnection(
			ISession?   session,
			DataOptions options,
			Func<Expression, IDataContext, ISession?, ISessionFactory?, Expression>? transformFunc) : base(options)
		{
			_session       = session;
			_transformFunc = transformFunc;
			AddInterceptor(new ExpressionInterceptor(this));

			// Attach the change-tracker only when there is a session to attach materialised entities to. A null
			// session means a stateless session (no first-level cache), which never tracks. With a session, the
			// per-query AsReadOnly() marker (detected during transform, sets Tracking=false) suppresses the
			// attach for that query.
			if (session != null && LinqToDBForNHibernateTools.EnableChangeTracker)
				AddInterceptor(this);
		}

		/// <summary>
		/// Converts expression using the transform function passed to the connection.
		/// </summary>
		/// <param name="expression">Expression to convert.</param>
		/// <param name="args">Query expression interception arguments.</param>
		/// <returns>Converted expression.</returns>
		public Expression ProcessExpression(Expression expression, QueryExpressionArgs args)
		{
			if (_transformFunc == null)
				return expression;

			// Only the main query expression decides tracking. linq2db also runs this interceptor for the
			// exposed-query and query-filter sub-passes, which no longer carry the AsReadOnly() marker (the
			// Query pass already stripped it) — so preserve the Query pass's decision instead of letting a
			// later pass reset it.
			var tracking = Tracking;
			var result   = _transformFunc(expression, this, _session, _session?.SessionFactory);

			if (args.Kind != QueryExpressionArgs.ExpressionKind.Query)
				Tracking = tracking;

			return result;
		}

		/// <summary>
		/// When change tracking is enabled, attaches an entity materialised by linq2db to the NHibernate
		/// session so it becomes a managed (persistent) instance.
		/// </summary>
		/// <param name="eventData">Entity creation event data.</param>
		/// <param name="entity">Materialised entity.</param>
		/// <returns>The entity, now associated with the session where possible.</returns>
		object IEntityServiceInterceptor.EntityCreated(EntityCreatedEventData eventData, object entity)
		{
			if (!LinqToDBForNHibernateTools.EnableChangeTracker || !Tracking || _session is not { IsOpen: true })
				return entity;

			try
			{
				if (!_session.Contains(entity))
					_session.Lock(entity, LockMode.None);
			}
			catch (HibernateException)
			{
				// A different instance with the same identifier is already associated, or the entity
				// cannot be locked in the current state — leave it as the materialised instance.
			}

			return entity;
		}
	}
}
