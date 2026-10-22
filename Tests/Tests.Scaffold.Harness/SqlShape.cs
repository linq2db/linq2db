using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB;

namespace Tests.Scaffold.Harness
{
	/// <summary>
	/// SQL text of query over generated model, built without database access. For providers, unavailable to the test run.
	/// </summary>
	public static class SqlShape
	{
		public static string GetSql(IQueryable query)
		{
			return (string)typeof(SqlShape)
				.GetMethod(nameof(GetSqlImpl), BindingFlags.NonPublic | BindingFlags.Static)!
				.MakeGenericMethod(query.ElementType)
				.Invoke(null, [query])!;
		}

		static string GetSqlImpl<T>(IQueryable<T> query) => query.ToSqlQuery().Sql;

		/// <summary>
		/// <c>source.Select(e => function(e.member, ...))</c>: scalar function call in projection.
		/// </summary>
		public static IQueryable SelectCall(IQueryable source, MethodInfo function, params string[] members)
		{
			var e    = Expression.Parameter(source.ElementType, "e");
			var call = Expression.Call(function, function.GetParameters().Select((p, i) => Convert(Expression.PropertyOrField(e, members[i]), p.ParameterType)));

			return Select(source, Expression.Lambda(call, e));
		}

		/// <summary>
		/// <c>source.GroupBy(e => 1).Select(g => aggregate(g, e => e.member))</c>: aggregate function call.
		/// </summary>
		public static IQueryable GroupAggregate(IQueryable source, MethodInfo aggregate, string member)
		{
			var elementType = source.ElementType;

			if (aggregate.IsGenericMethodDefinition)
				aggregate = aggregate.MakeGenericMethod(elementType);

			var e       = Expression.Parameter(elementType, "e");
			var grouped = Expression.Call(
				typeof(Queryable),
				nameof(Queryable.GroupBy),
				[elementType, typeof(int)],
				source.Expression,
				Expression.Quote(Expression.Lambda(Expression.Constant(1), e)));

			var groupType = typeof(IGrouping<,>).MakeGenericType(typeof(int), elementType);
			var g         = Expression.Parameter(groupType, "g");

			var valueParameter = aggregate.GetParameters()[1].ParameterType;
			var valueDelegate  = valueParameter.IsGenericType && valueParameter.GetGenericTypeDefinition() == typeof(Expression<>)
				? valueParameter.GetGenericArguments()[0]
				: valueParameter;

			var valueReturn = valueDelegate.GetMethod(nameof(Action.Invoke))!.ReturnType;
			var value       = Expression.Lambda(valueDelegate, Convert(Expression.PropertyOrField(e, member), valueReturn), e);

			var call = Expression.Call(
				aggregate,
				g,
				valueParameter == valueDelegate ? value : Expression.Quote(value));

			var select = Expression.Call(
				typeof(Queryable),
				nameof(Queryable.Select),
				[groupType, call.Type],
				grouped,
				Expression.Quote(Expression.Lambda(call, g)));

			return source.Provider.CreateQuery(select);
		}

		static IQueryable Select(IQueryable source, LambdaExpression selector)
		{
			var call = Expression.Call(
				typeof(Queryable),
				nameof(Queryable.Select),
				[source.ElementType, selector.ReturnType],
				source.Expression,
				Expression.Quote(selector));

			return source.Provider.CreateQuery(call);
		}

		static Expression Convert(Expression expression, Type type)
		{
			if (expression.Type == type)
				return expression;

			if (expression.Type.IsGenericType && expression.Type.GetGenericTypeDefinition() == typeof(Microsoft.FSharp.Core.FSharpOption<>))
				return Convert(Expression.Property(expression, nameof(Microsoft.FSharp.Core.FSharpOption<>.Value)), type);

			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Microsoft.FSharp.Core.FSharpOption<>))
				return Expression.Call(type.GetMethod(nameof(Microsoft.FSharp.Core.FSharpOption<>.Some))!, Convert(expression, type.GetGenericArguments()[0]));

			return Expression.Convert(expression, type);
		}
	}
}
