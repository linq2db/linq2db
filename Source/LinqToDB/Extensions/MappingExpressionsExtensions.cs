using System;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Extensions
{
	using Common;
	using Common.Internal;

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

			switch (members[0])
			{
				case PropertyInfo propInfo:
					{
						if (propInfo.GetValue(null, null) is TExpression expression)
							return expression;

						throw new LinqToDBException($"Property '{memberName}' for type '{type.Name}' should return expression");
					}
				case MethodInfo method:
					{
						if (method.GetParameters().Length > 0)
							throw new LinqToDBException($"Method '{memberName}' for type '{type.Name}' should have no parameters");

						return method.InvokeExt<TExpression>(null, []);
					}
				default:
					throw new LinqToDBException(
						$"Member '{memberName}' for type '{type.Name}' should be static property or method");
			}
		}
	}
}
