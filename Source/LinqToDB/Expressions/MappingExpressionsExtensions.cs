using LinqToDB.Common;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Extensions
{
	static class MappingExpressionsExtensions
	{
		public static TExpression GetExpressionFromExpressionMember<TExpression>(this Type type, string memberName)
			where TExpression : Expression
		{
			var members = type.GetStaticMembersEx(memberName);

			if (members.Length == 0)
				throw new LinqToDBException($"Static member '{memberName}' for type '{type.Name}' not found");

			if (members.Length > 1)
				throw new LinqToDBException($"Ambiguous members '{memberName}' for type '{type.Name}' has been found");

			if (members[0] is PropertyInfo propInfo)
			{
				var value = propInfo.GetValue(null, null);
				if (value == null)
					return null;

				if (value is TExpression expression)
					return expression;

				throw new LinqToDBException($"Property '{memberName}' for type '{type.Name}' should return expression");
			}
			else
			{
				if (members[0] is MethodInfo method)
				{
					if (method.GetParameters().Length > 0)
						throw new LinqToDBException($"Method '{memberName}' for type '{type.Name}' should have no parameters");
					var value = method.Invoke(null, Array<object>.Empty);
					if (value == null)
						return null;

					if (value is TExpression expression)
						return expression;

					throw new LinqToDBException($"Method '{memberName}' for type '{type.Name}' should return expression");
				}
			}

			throw new LinqToDBException(
				$"Member '{memberName}' for type '{type.Name}' should be static property or method");
		}
	}
}
