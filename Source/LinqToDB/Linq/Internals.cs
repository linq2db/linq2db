﻿using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	/// <summary>
	/// This is internal API and is not intended for use by Linq To DB applications.
	/// It may change or be removed without further notice.
	/// </summary>
	public static class Internals
	{
		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static IQueryable<T> CreateExpressionQueryInstance<T>(IDataContext dataContext, Expression expression)
		{
			return new ExpressionQueryImpl<T>(dataContext, expression);
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static IDataContext GetDataContext<T>(IQueryable<T> queryable)
		{
			return queryable switch
			{
				ExpressionQuery<T> query => query.DataContext,
				ITable<T> table          => table.DataContext,
				_                        => default!,
			};
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static IDataContext GetDataContext<T>(IUpdatable<T> updatable)
		{
			return GetDataContext(((LinqExtensions.Updatable<T>)updatable).Query);
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static IDataContext GetDataContext<T>(IValueInsertable<T> insertable)
		{
			return GetDataContext(((LinqExtensions.ValueInsertable<T>)insertable).Query);
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static IDataContext GetDataContext<TSource, TTarget>(ISelectInsertable<TSource, TTarget> insertable)
		{
			return GetDataContext(((LinqExtensions.SelectInsertable<TSource, TTarget>)insertable).Query);
		}

	}
}
