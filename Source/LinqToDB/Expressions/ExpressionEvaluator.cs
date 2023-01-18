using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LinqToDB.Expressions
{
	using LinqToDB.Extensions;
	using LinqToDB.Common;
	using LinqToDB.Common.Internal;

	/// <summary>
	/// Internal API.
	/// </summary>
	public static class ExpressionEvaluator
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T? EvaluateExpression<T>(this Expression? expr)
			where T : class
		{
			return expr.EvaluateExpression() as T;
		}

		public static object? EvaluateExpression(this Expression? expr)
		{
			if (expr == null)
				return null;

			switch (expr.NodeType)
			{
				case ExpressionType.Default:
					return !expr.Type.IsNullableType() ? Activator.CreateInstance(expr.Type) : null;

				case ExpressionType.Constant:
					return ((ConstantExpression)expr).Value;

				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
				{
					var unary = (UnaryExpression)expr;
					var operand = unary.Operand.EvaluateExpression();
					if (operand == null)
						return null;
					break;
				}

				case ExpressionType.MemberAccess:
				{
					var member = (MemberExpression) expr;

					if (member.Member.IsFieldEx())
						return ((FieldInfo)member.Member).GetValue(member.Expression.EvaluateExpression());

					if (member.Member is PropertyInfo propertyInfo)
					{
						var obj = member.Expression.EvaluateExpression();
						if (obj == null)
						{
							if (propertyInfo.IsNullableValueMember())
								return null;
							if (propertyInfo.IsNullableHasValueMember())
								return false;
						}
						return propertyInfo.GetValue(obj, null);
					}

					break;
				}

				case ExpressionType.Call:
				{
					var mc = (MethodCallExpression)expr;
					var arguments = mc.Arguments.Select(EvaluateExpression).ToArray();
					var instance  = mc.Object.EvaluateExpression();

					if (instance == null && mc.Method.IsNullableGetValueOrDefault())
						return null;

					return mc.Method.Invoke(instance, arguments);
				}
			}

			var value = Expression.Lambda(expr).CompileExpression().DynamicInvoke();
			return value;
		}
	}
}
