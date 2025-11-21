using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Reflection;

namespace LinqToDB.Internal.Common
{
	public static class BuildExpressionUtils
	{
		public static Expression UnwrapEnumerableCasting(Expression expression)
		{
			if (expression is MethodCallExpression methodCall)
			{
				if (methodCall.IsQueryable(nameof(Queryable.AsQueryable)) || methodCall.IsQueryable(nameof(Enumerable.AsEnumerable)))
				{
					return UnwrapEnumerableCasting(methodCall.Arguments[0]);
				}
			}

			return expression;
		}

		public static Expression EnsureEnumerableType(Expression expression)
		{
			var elementType = TypeHelper.GetEnumerableElementType(expression.Type);
			return EnsureEnumerableType(expression, typeof(IEnumerable<>).MakeGenericType(elementType));
		}

		public static Expression EnsureEnumerableType(Expression expression, Type targetType)
		{
			if (expression.Type == targetType)
				return expression;

			if (targetType.IsAssignableFrom(expression.Type))
				return expression;

			var unwrapped = UnwrapEnumerableCasting(expression);
			if (!ReferenceEquals(unwrapped, expression))
				return EnsureEnumerableType(unwrapped, targetType);

			var elementType = TypeHelper.GetEnumerableElementType(targetType);

			var queryableType = typeof(IQueryable<>).MakeGenericType(elementType);

			if (queryableType.IsSameOrParentOf(targetType))
			{
				if (queryableType != expression.Type)
				{
					return Expression.Call(Methods.Queryable.AsQueryable.MakeGenericMethod(elementType), expression);
				}

				return expression;
			}

			var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);

			if (enumerableType.IsSameOrParentOf(targetType))
			{
				if (enumerableType != expression.Type)
				{
					return Expression.Call(Methods.Enumerable.AsEnumerable.MakeGenericMethod(elementType), expression);
				}

				return expression;
			}

			return Expression.Convert(expression, targetType);
		}
	}
}
