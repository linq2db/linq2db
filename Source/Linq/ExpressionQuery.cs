using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq
{
	using Extensions;

	abstract class ExpressionQuery<T> : IExpressionQuery<T>
	{
		protected ExpressionQuery(IDataContext dataContext, Expression expression)
		{
			_dataContext = dataContext;

			Expression   = expression ?? Expression.Constant(this);
		}

		readonly IDataContext _dataContext;

		public Expression     Expression  { get; set; }
		public Type           ElementType { get { return typeof(T); } }
		public IQueryProvider Provider    { get { return this;      } }

		public string SqlText
		{
			get;
			private set;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return GetQuery(Expression, true).GetIEnumerable(null, _dataContext, Expression, Parameters).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetQuery(Expression, true).GetIEnumerable(null, _dataContext, Expression, Parameters).GetEnumerator();
		}

		public IQueryable CreateQuery(Expression expression)
		{
			if (expression == null)
				throw new ArgumentNullException("expression");

			var elementType = expression.Type.GetItemType() ?? expression.Type;

			try
			{
				return (IQueryable)Activator.CreateInstance(typeof(ExpressionQueryOldImpl<>).MakeGenericType(elementType), new object[] { _dataContext, expression });
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

		public object Execute(Expression expression)
		{
			return GetQuery(expression, false).GetElement(null, _dataContext, expression, Parameters);
		}

		public TResult Execute<TResult>(Expression expression)
		{
#if DEBUG
			if (typeof(TResult) != typeof(T))
				throw new InvalidOperationException();
#endif

			return (TResult)GetQuery(expression, false).GetElement(null, _dataContext, expression, Parameters);
		}

		internal Query<T> Info;
		internal object[] Parameters;

		#region Execute

		Query<T> GetQuery(Expression expression, bool cache)
		{
			if (cache && Info != null)
				return Info;

			var info = Query<T>.GetQuery(_dataContext, expression);

			if (cache)
				Info = info;

			return info;
		}

		#endregion

	}
}
