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
			// session means either a stateless session (no first-level cache) or an AsReadOnly() context — both
			// leave the entities detached (untracked).
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
			return _transformFunc(expression, this, _session, _session?.SessionFactory);
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
