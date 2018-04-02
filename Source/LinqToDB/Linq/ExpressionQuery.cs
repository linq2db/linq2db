﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB.Linq
{
	using Extensions;

	abstract class ExpressionQuery<T> : IExpressionQuery<T>
	{
		#region Init

		protected void Init([NotNull] IDataContext dataContext, Expression expression)
		{
			DataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
			Expression  = expression  ?? Expression.Constant(this);
		}

		[NotNull] public Expression   Expression  { get; set; }
		[NotNull] public IDataContext DataContext { get; set; }

		internal Query<T> Info;
		internal object[] Parameters;

		#endregion

		#region Public Members

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		string _sqlTextHolder;

		// This property is helpful in Debug Mode.
		//
		[UsedImplicitly]
		// ReSharper disable once InconsistentNaming
		string _sqlText => SqlText;

		public string SqlText
		{
			get
			{
				var hasQueryHints = DataContext.QueryHints.Count > 0 || DataContext.NextQueryHints.Count > 0;

				if (_sqlTextHolder == null || hasQueryHints)
				{
					var expression = Expression;
					var info       = GetQuery(ref expression, true);
					Expression     = expression;
					var sqlText    = QueryRunner.GetSqlText(info, DataContext, Expression, Parameters, 0);

					if (hasQueryHints)
						return sqlText;

					_sqlTextHolder = sqlText;
				}

				return _sqlTextHolder;
			}
		}

		#endregion

		#region Execute

		Query<T> GetQuery(ref Expression expression, bool cache)
		{
			if (cache && Info != null)
				return Info;

			var info = Query<T>.GetQuery(DataContext, ref expression);

			if (cache)
				Info = info;

			return info;
		}

		async Task<TResult> IQueryProviderAsync.ExecuteAsync<TResult>(Expression expression, CancellationToken token)
		{
			var value = await GetQuery(ref expression, false).GetElementAsync(
				DataContext, expression, Parameters, token);

			return (TResult)value;
		}

		public Task GetForEachAsync(Action<T> action, CancellationToken cancellationToken)
		{
			var expression = Expression;
			var query      = GetQuery(ref expression, true);
			Expression     = expression;

			return query
				.GetForEachAsync(DataContext, Expression, Parameters, r => { action(r); return true; }, cancellationToken);
		}

		public Task GetForEachUntilAsync(Func<T,bool> func, CancellationToken cancellationToken)
		{
			var expression = Expression;
			return GetQuery(ref expression, true)
				.GetForEachAsync(DataContext, expression, Parameters, func, cancellationToken);
		}

		#endregion

		#region IQueryable Members

		Type           IQueryable.ElementType => typeof(T);
		Expression     IQueryable.Expression  => Expression;
		IQueryProvider IQueryable.Provider    => this;

		#endregion

		#region IQueryProvider Members

		IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));

			return new ExpressionQueryImpl<TElement>(DataContext, expression);
		}

		IQueryable IQueryProvider.CreateQuery(Expression expression)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));

			var elementType = expression.Type.GetItemType() ?? expression.Type;

			try
			{
				return (IQueryable)Activator.CreateInstance(
					typeof(ExpressionQueryImpl<>).MakeGenericType(elementType),
					DataContext, expression);
			}
			catch (TargetInvocationException ex)
			{
				throw ex.InnerException;
			}
		}

		TResult IQueryProvider.Execute<TResult>(Expression expression)
		{
			return (TResult)GetQuery(ref expression, false).GetElement(DataContext, expression, Parameters);
		}

		object IQueryProvider.Execute(Expression expression)
		{
			return GetQuery(ref expression, false).GetElement(DataContext, expression, Parameters);
		}

		#endregion

		#region IEnumerable Members

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			var expression = Expression;
			var query      = GetQuery(ref expression, true);
			Expression     = expression;

			return query.GetIEnumerable(DataContext, Expression, Parameters).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			var expression = Expression;
			var query      = GetQuery(ref expression, true);
			Expression     = expression;

			return query.GetIEnumerable(DataContext, Expression, Parameters).GetEnumerator();
		}

		#endregion
	}
}
