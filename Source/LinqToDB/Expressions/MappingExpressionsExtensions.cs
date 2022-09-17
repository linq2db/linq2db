using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Extensions
{
	using Common;

	static class MappingExpressionsExtensions
	{
		public static TExpression GetExpressionFromExpressionMember<TExpression>(this Type type, string memberName)
			where TExpression : Expression
		{
			var members = type.GetStaticMembersEx(memberName);

			if (members.Length == 0)
				ThrowHelper.ThrowLinqToDBException($"Static member '{memberName}' for type '{type.Name}' not found");

			if (members.Length > 1)
				ThrowHelper.ThrowLinqToDBException($"Ambiguous members '{memberName}' for type '{type.Name}' has been found");

			switch (members[0])
			{
				case PropertyInfo propInfo:
				{
					if (propInfo.GetValue(null, null) is TExpression expression)
						return expression;

					return ThrowHelper.ThrowLinqToDBException<TExpression>($"Property '{memberName}' for type '{type.Name}' should return expression");
				}
				case MethodInfo method:
				{
					if (method.GetParameters().Length > 0)
						return ThrowHelper.ThrowLinqToDBException<TExpression>($"Method '{memberName}' for type '{type.Name}' should have no parameters");

					if (method.Invoke(null, Array<object>.Empty) is TExpression expression)
						return expression;

					return ThrowHelper.ThrowLinqToDBException<TExpression>($"Method '{memberName}' for type '{type.Name}' should return expression");
				}
				default:
				{
					return ThrowHelper.ThrowLinqToDBException<TExpression>(
						$"Member '{memberName}' for type '{type.Name}' should be static property or method");
				}
			}
		}
	}
}
