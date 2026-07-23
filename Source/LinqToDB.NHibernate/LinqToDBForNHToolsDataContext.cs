using System;
using System.Linq.Expressions;
using LinqToDB.DataProvider;
using LinqToDB.Linq;
using NHibernate;

namespace LinqToDB.NHibernate
{
	/// <summary>
	/// linq2db NHibernate data session.
	/// </summary>
	public class LinqToDBForNHibernateToolsDataContext : DataContext, IExpressionPreprocessor
	{
		readonly ISession? _session;
		readonly ISessionFactory _sessionFactory;
		readonly Func<Expression, IDataContext, ISession?, ISessionFactory, Expression>? _transformFunc;

		/// <summary>
		/// Creates instance of session.
		/// </summary>
		/// <param name="session">NHibernate database session.</param>
		/// <param name="dataProvider">lin2db database provider instance.</param>
		/// <param name="connectionString">Connection string.</param>
		/// <param name="sessionFactory">NHibernate sessionFactory.</param>
		/// <param name="transformFunc">Expression converter.</param>
		public LinqToDBForNHibernateToolsDataContext(
			ISession?    session,
			IDataProvider dataProvider,
			string        connectionString,
			ISessionFactory sessionFactory,
			Func<Expression, IDataContext, ISession?, ISessionFactory, Expression>? transformFunc) : base(dataProvider, connectionString)
		{
			_session       = session;
			_sessionFactory         = sessionFactory;
			_transformFunc = transformFunc;
		}

		/// <summary>
		/// Converts expression using convert function, passed to session.
		/// </summary>
		/// <param name="expression">Expression to convert.</param>
		/// <returns>Converted expression.</returns>
		public Expression ProcessExpression(Expression expression)
		{
			if (_transformFunc == null)
				return expression;
			return _transformFunc(expression, this, _session, _sessionFactory);
		}

	}
}
