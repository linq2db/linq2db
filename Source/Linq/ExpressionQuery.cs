﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.Linq
{
	using Extensions;

	abstract class ExpressionQuery<T> : IExpressionQuery<T>
	{
		#region Init

		protected void Init([NotNull] IDataContext dataContext, Expression expression)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");

			DataContext = dataContext;
			Expression  = expression ?? Expression.Constant(this);
		}

		[NotNull] public Expression   Expression  { get; set; }
		[NotNull] public IDataContext DataContext { get; set; }

		internal  Query<T> Info;
		internal  object[] Parameters;

		#endregion

		#region Public Members

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private string _sqlTextHolder;

// ReSharper disable InconsistentNaming
		[UsedImplicitly]
		private string _sqlText { get { return SqlText; }}
// ReSharper restore InconsistentNaming

		public  string  SqlText
		{
			get
			{
				var hasQueryHints = DataContext.QueryHints.Count > 0 || DataContext.NextQueryHints.Count > 0;

				if (_sqlTextHolder == null || hasQueryHints)
				{
					var info    = GetQuery(Expression, true);
					var sqlText = info.GetSqlText(DataContext, Expression, Parameters, 0);

					if (hasQueryHints)
						return sqlText;

					_sqlTextHolder = sqlText;
				}

				return _sqlTextHolder;
			}
		}

		#endregion

		#region Execute

		Query<T> GetQuery(Expression expression, bool cache)
		{
			if (cache && Info != null)
				return Info;

			var info = Query<T>.GetQuery(DataContext, expression);

			if (cache)
				Info = info;

			return info;
		}

		#endregion

		#region IQueryable Members

		Type IQueryable.ElementType
		{
			get { return typeof(T); }
		}

		Expression IQueryable.Expression
		{
			get { return Expression; }
		}

		IQueryProvider IQueryable.Provider
		{
			get { return this; }
		}

		#endregion

		#region IQueryProvider Members

		IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
		{
			if (expression == null)
				throw new ArgumentNullException("expression");

			return new ExpressionQueryImpl<TElement>(DataContext, expression);
		}

		IQueryable IQueryProvider.CreateQuery(Expression expression)
		{
			if (expression == null)
				throw new ArgumentNullException("expression");

			var elementType = expression.Type.GetItemType() ?? expression.Type;

			try
			{
				return (IQueryable)Activator.CreateInstance(typeof(ExpressionQueryImpl<>).MakeGenericType(elementType), new object[] { DataContext, expression });
			}
			catch (TargetInvocationException ex)
			{
				throw ex.InnerException;
			}
		}

		TResult IQueryProvider.Execute<TResult>(Expression expression)
		{
			return (TResult)GetQuery(expression, false).GetElement(null, DataContext, expression, Parameters);
		}

		object IQueryProvider.Execute(Expression expression)
		{
			return GetQuery(expression, false).GetElement(null, DataContext, expression, Parameters);
		}

		#endregion

		#region IEnumerable Members

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetQuery(Expression, true).GetIEnumerable(null, DataContext, Expression, Parameters).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetQuery(Expression, true).GetIEnumerable(null, DataContext, Expression, Parameters).GetEnumerator();
		}

		#endregion
	}
}
