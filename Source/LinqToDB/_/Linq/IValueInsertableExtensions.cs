using System;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq
{
	public static class IValueInsertableExtensions
	{

		public static IValueInsertable<T> ValueLambda<T, TV>(this IValueInsertable<T> source, LambdaExpression le, TV value) where T : notnull
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (le == null) throw new ArgumentNullException(nameof(le));

			var query = ((LinqExtensions.ValueInsertable<T>)source).Query;

			Expression<Func<T, TV>> field = Expression.Lambda<Func<T, TV>>(le.Body, le.Parameters);

			var q = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(LinqExtensions.Value, source, field, value),
					query.Expression, Expression.Quote(field), Expression.Constant(value, field.Body.Type)));

			return new LinqExtensions.ValueInsertable<T>(q);

		}
	}
}
