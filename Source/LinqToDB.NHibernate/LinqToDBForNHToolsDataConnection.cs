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
	public class LinqToDBForNHibernateToolsDataConnection : DataConnection
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
	}
}
