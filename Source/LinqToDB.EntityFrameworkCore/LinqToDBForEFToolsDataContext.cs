using System;
using System.Linq.Expressions;

using LinqToDB.DataProvider;
using LinqToDB.Interceptors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LinqToDB.EntityFrameworkCore
{
	/// <summary>
	/// Linq To DB EF.Core data context.
	/// </summary>
	public class LinqToDBForEFToolsDataContext : DataContext, IQueryExpressionInterceptor
	{
		readonly DbContext? _context;
		readonly IModel _model;
		readonly Func<Expression, IDataContext, DbContext?, IModel, bool, Expression>? _transformFunc;

		/// <summary>
		/// Creates instance of context.
		/// </summary>
		/// <param name="context">EF.Core database context.</param>
		/// <param name="dataProvider">lin2db database provider instance.</param>
		/// <param name="connectionString">Connection string.</param>
		/// <param name="model">EF.Core model.</param>
		/// <param name="transformFunc">Expression converter.</param>
		public LinqToDBForEFToolsDataContext(
			DbContext?                                                            context,
			IDataProvider                                                         dataProvider,
			string                                                                connectionString,
			IModel                                                                model,
			Func<Expression, IDataContext, DbContext?, IModel, bool, Expression>? transformFunc) : base(dataProvider, connectionString)
		{
			_context       = context;
			_model         = model;
			_transformFunc = transformFunc;
		}

		/// <summary>
		/// Converts expression using convert function, passed to context.
		/// </summary>
		/// <param name="expression">Expression to convert.</param>
		/// <param name="args"></param>
		/// <returns>Converted expression.</returns>
		public Expression ProcessExpression(Expression expression, QueryExpressionArgs args)
		{
			if (_transformFunc == null)
				return expression;

			var transformed = _transformFunc(expression, this, _context, _model, args.Kind == QueryExpressionArgs.ExpressionKind.Query);
			return transformed;
		}
	}
}
