using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;
	using Mapping;
	using SqlQuery;

	internal sealed class EagerLoading
	{
		static bool IsDetailType(Type type)
		{
			var isEnumerable = type != typeof(string) && typeof(IEnumerable<>).IsSameOrParentOf(type);

			if (!isEnumerable && type.IsClass && type.IsGenericType && type.Name.StartsWith("<>"))
			{
				isEnumerable = type.GenericTypeArguments.Any(IsDetailType);
			}

			return isEnumerable;
		}

		public static bool IsDetailsMember(IBuildContext context, Expression expression)
		{
			if (IsDetailType(expression.Type))
			{
				if (expression.NodeType == ExpressionType.Call)
				{
					return true;
				}

				var buildInfo = new BuildInfo(context, expression, new SelectQuery());
				if (context.Builder.IsSequence(buildInfo))
					return true;
			}

			return false;
		}

		public static Type GetEnumerableElementType(Type type, MappingSchema mappingSchema)
		{
			if (!IsEnumerableType(type, mappingSchema))
				return type;
			if (type.IsArray)
				return type.GetElementType()!;
			if (typeof(IGrouping<,>).IsSameOrParentOf(type))
				return type.GetGenericArguments()[1];
			return type.GetGenericArguments()[0];
		}

		public static bool IsEnumerableType(Type type, MappingSchema mappingSchema)
		{
			if (mappingSchema.IsScalarType(type))
				return false;
			if (!typeof(IEnumerable<>).IsSameOrParentOf(type))
				return false;
			return true;
		}

		static bool IsQueryableMethod(Expression expression, string methodName, [NotNullWhen(true)] out MethodCallExpression? queryableMethod)
		{
			expression = expression.Unwrap();
			if (expression.NodeType == ExpressionType.Call)
			{
				queryableMethod = (MethodCallExpression)expression;
				return queryableMethod.IsQueryable(methodName);
			}

			queryableMethod = null;
			return false;
		}

		public static Expression EnsureEnumerable(Expression expression, MappingSchema mappingSchema)
		{
			var enumerable = typeof(IEnumerable<>).MakeGenericType(GetEnumerableElementType(expression.Type, mappingSchema));
			if (expression.Type != enumerable)
				expression = Expression.Convert(expression, enumerable);
			return expression;
		}
	}
}
