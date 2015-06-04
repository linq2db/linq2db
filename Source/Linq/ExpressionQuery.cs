using System;
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
		protected ExpressionQuery(IDataContext dataContext, Expression expression)
		{
			_dataContext = dataContext;

			Expression = expression ?? Expression.Constant(this);
		}

		readonly IDataContext _dataContext;

		public Expression     Expression  { get; set; }
		public Type           ElementType { get { return typeof(T); } }
		public IQueryProvider Provider    { get { return this;      } }

		#region SqlText

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
				var hasQueryHints = _dataContext.QueryHints.Count > 0 || _dataContext.NextQueryHints.Count > 0;

				if (_sqlTextHolder == null || hasQueryHints)
				{
//					var info    = GetQuery(Expression, true);
//					var sqlText = info.GetSqlText(_dataContext, Expression, null/*Parameters*/, 0);
//
//					if (hasQueryHints)
//						return sqlText;
//
//					_sqlTextHolder = sqlText;
				}

				return _sqlTextHolder;
			}
		}

		#endregion

		public IQueryable CreateQuery(Expression expression)
		{
			if (expression == null)
				throw new ArgumentNullException("expression");

			var elementType = expression.Type.GetItemType() ?? expression.Type;

			try
			{
				return (IQueryable)Activator.CreateInstance(typeof(ExpressionQueryImpl<>).MakeGenericType(elementType), new object[] { _dataContext, expression });
			}
			catch (TargetInvocationException ex)
			{
				throw ex.InnerException;
			}
		}

		public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
		{
			if (expression == null)
				throw new ArgumentNullException("expression");

			return new ExpressionQueryImpl<TElement>(_dataContext, expression);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return GetQuery(Expression, true).GetIEnumerable(_dataContext, Expression).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetQuery(Expression, true).GetIEnumerable(_dataContext, Expression).GetEnumerator();
		}

		public object Execute(Expression expression)
		{
			return GetQuery(expression, false).GetElement(_dataContext, expression);
		}

		public Task GetForEachAsync(Action<T> action, CancellationToken cancellationToken)
		{
			return GetQuery(Expression, true).GetForEachAsync(_dataContext, Expression, action, cancellationToken);
		}

		public TResult Execute<TResult>(Expression expression)
		{
#if DEBUG
			if (typeof(TResult) != typeof(T))
				throw new InvalidOperationException();
#endif

			return (TResult)(object)GetQuery(expression, false).GetElement(_dataContext, expression);
		}

		Query<T> _info;

		Query<T> GetQuery(Expression expression, bool isEnumerable)
		{
			if (isEnumerable && _info != null)
				throw new InvalidOperationException();
				//return _info;

			var info = Query<T>.GetQuery(_dataContext, expression, isEnumerable);

			if (isEnumerable)
				_info = info;

			return info;
		}
	}
}
