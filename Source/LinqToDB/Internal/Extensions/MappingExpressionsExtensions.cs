using System;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.Extensions
{
	static class MappingExpressionsExtensions
	{
		public static TExpression GetExpressionFromExpressionMember<TExpression>(this Type type, string memberName)
			where TExpression : Expression
		{
			return type.GetStaticMembersEx(memberName) switch
			{
				[] => throw new LinqToDBException($"Static member '{memberName}' for type '{type.Name}' not found"),

				[PropertyInfo propInfo] => propInfo.GetValue(null, null) switch
				{
					TExpression expression => expression,
					_ => throw new LinqToDBException($"Property '{memberName}' for type '{type.Name}' should return expression"),
				},

				[MethodInfo method] => method.GetParameters() switch
				{
					[] => method.InvokeExt<TExpression>(null, []),
					_ => throw new LinqToDBException($"Method '{memberName}' for type '{type.Name}' should have no parameters"),
				},

				[_] => throw new LinqToDBException($"Member '{memberName}' for type '{type.Name}' should be static property or method"),

				_ => throw new LinqToDBException($"Ambiguous members '{memberName}' for type '{type.Name}' has been found"),
			};
		}
	}
}
