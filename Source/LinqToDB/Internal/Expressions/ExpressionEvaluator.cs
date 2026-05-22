using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Extensions;

namespace LinqToDB.Internal.Expressions
{
	public static class ExpressionEvaluator
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T? EvaluateExpression<T>(this Expression? expr)
			where T : class
		{
			return expr.EvaluateExpression() as T;
		}

		static bool IsSimpleEvaluatable(Expression? expr)
		{
			return expr switch
			{
				null => true,

				{ NodeType: ExpressionType.Default } => true,
				{ NodeType: ExpressionType.Constant } => true,

				MemberExpression { NodeType: ExpressionType.MemberAccess } member =>
					member.Member.MemberType is MemberTypes.Field or MemberTypes.Property
					&& IsSimpleEvaluatable(member.Expression),

				MethodCallExpression { NodeType: ExpressionType.Call } mc =>
					IsSimpleEvaluatable(mc.Object) && mc.Arguments.All(IsSimpleEvaluatable),

				_ => false,
			};
		}

		static object? EvaluateExpressionInternal(this Expression? expr)
		{
			if (expr == null)
				return null;

			switch (expr.NodeType)
			{
				case ExpressionType.Default:
					return ReflectionExtensions.GetDefaultValue(expr.Type);

				case ExpressionType.Constant:
					return ((ConstantExpression)expr).Value;

				case ExpressionType.MemberAccess:
				{
					var member = (MemberExpression) expr;

					if (member.Member is FieldInfo fieldInfo)
						return fieldInfo.GetValue(member.Expression.EvaluateExpressionInternal());

					if (member.Member is PropertyInfo propertyInfo)
					{
						var obj = member.Expression.EvaluateExpressionInternal();
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
					var mc        = (MethodCallExpression)expr;
					var arguments = mc.Arguments.Select(a => a.EvaluateExpressionInternal()).ToArray();
					var instance  = mc.Object.EvaluateExpressionInternal();

					if (instance == null && mc.Method.IsNullableGetValueOrDefault())
						return null;

					return mc.Method.InvokeExt(instance, arguments);
				}
			}

			throw new InvalidOperationException($"Expression '{expr}' cannot be evaluated");
		}

		public static object? EvaluateExpression(this Expression? expr)
		{
			if (expr == null)
				return null;

			if (IsSimpleEvaluatable(expr))
			{
				return expr.EvaluateExpressionInternal();
			}

			var value = Expression.Lambda(expr).CompileExpression().DynamicInvokeExt();
			return value;
		}
	}
}
