using System;
using System.Data;
using System.Linq.Expressions;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Linq;
using NHibernate;

namespace LinqToDB.NHibernateExtension
{
	/// <summary>
	/// linq2db EF.Core data connection.
	/// </summary>
	public class LinqToDBForNHToolsDataConnection : DataConnection, IExpressionPreprocessor
	{
		readonly ISession? _session;
		readonly Func<Expression, IDataContext, ISession?, ISessionFactory?, Expression>? _transformFunc;

		private Type?          _lastType;

		/// <summary>
		/// Change tracker enable flag.
		/// </summary>
		public bool      Tracking { get; set; }

		/// <summary>
		/// Creates new instance of data connection.
		/// </summary>
		/// <param name="context">EF.Core database context.</param>
		/// <param name="dataProvider">linq2db database provider.</param>
		/// <param name="connectionString">Connection string.</param>
		/// <param name="session">EF.Core data session.</param>
		/// <param name="transformFunc">Expression converter.</param>
		public LinqToDBForNHToolsDataConnection(
			ISession?     session,
			IDataProvider dataProvider,
			string        connectionString,
			Func<Expression, IDataContext, ISession?, ISessionFactory?, Expression>? transformFunc) : base(dataProvider, connectionString)
		{
			_session          = session;
			_transformFunc    = transformFunc;
			if (LinqToDBForNHTools.EnableChangeTracker)
				OnEntityCreated += OnEntityCreatedHandler;
		}

		/// <summary>
		/// Creates new instance of data connection.
		/// </summary>
		/// <param name="context">EF.Core database context.</param>
		/// <param name="dataProvider">linq2db database provider.</param>
		/// <param name="transaction">Database transaction.</param>
		/// <param name="session">EF.Core data session.</param>
		/// <param name="transformFunc">Expression converter.</param>
		public LinqToDBForNHToolsDataConnection(
			ISession? session,
			IDataProvider  dataProvider,
			IDbTransaction transaction,
			Func<Expression, IDataContext, ISession?, ISessionFactory?, Expression>? transformFunc
			) : base(dataProvider, transaction)
		{
			_session         = session;
			_transformFunc   = transformFunc;
			if (LinqToDBForNHTools.EnableChangeTracker)
				OnEntityCreated += OnEntityCreatedHandler;
		}

		/// <summary>
		/// Creates new instance of data connection.
		/// </summary>
		/// <param name="context">EF.Core database context.</param>
		/// <param name="dataProvider">linq2db database provider.</param>
		/// <param name="connection">Database connection instance.</param>
		/// <param name="session">EF.Core data session.</param>
		/// <param name="transformFunc">Expression converter.</param>
		public LinqToDBForNHToolsDataConnection(
			ISession? session,
			IDataProvider dataProvider,
			IDbConnection connection,
			Func<Expression, IDataContext, ISession?, ISessionFactory?, Expression>? transformFunc) : base(dataProvider, connection)
		{
			_session         = session;
			_transformFunc   = transformFunc;
			if (LinqToDBForNHTools.EnableChangeTracker)
				OnEntityCreated += OnEntityCreatedHandler;
		}

		/// <summary>
		/// Converts expression using convert function, passed to context.
		/// </summary>
		/// <param name="expression">Expression to convert.</param>
		/// <returns>Converted expression.</returns>
		public Expression ProcessExpression(Expression expression)
		{
			if (_transformFunc == null)
				return expression;
			return _transformFunc(expression, this, _session, _session?.SessionFactory);
		}

		private void OnEntityCreatedHandler(EntityCreatedEventArgs args)
		{
		}

	}
}
