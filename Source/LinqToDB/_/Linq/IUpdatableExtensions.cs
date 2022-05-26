using System;
using System.Linq.Expressions;
using LinqToDB.Reflection;

namespace LinqToDB.Linq
{
	public static class IUpdatableExtensions
	{

		public static IUpdatable<T> SetLambda<T, TV>(this IUpdatable<T> source, LambdaExpression le, TV value) where T : notnull
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (le == null) throw new ArgumentNullException(nameof(le));

			var query = ((LinqExtensions.Updatable<T>)source).Query;

			Expression<Func<T, TV>> extract = Expression.Lambda<Func<T, TV>>(le.Body, le.Parameters);

			query = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					Methods.LinqToDB.Update.SetUpdatableValue.MakeGenericMethod(typeof(T), typeof(TV)),
					query.Expression, Expression.Quote(extract), Expression.Constant(value, typeof(TV))));

			return new LinqExtensions.Updatable<T>(query);
		}

	}
}
